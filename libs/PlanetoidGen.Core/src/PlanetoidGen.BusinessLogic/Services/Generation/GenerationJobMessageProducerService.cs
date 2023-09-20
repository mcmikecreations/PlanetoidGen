using Microsoft.Extensions.Logging;
using PlanetoidGen.Contracts.Comparers;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Repositories.Messaging;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.BusinessLogic.Services.Generation
{
    public class GenerationJobMessageProducerService : IGenerationService
    {
        private static readonly object _lock = new object();

        private readonly GenerationJobMessageComparer _generationJobMessageComparer;

        private readonly ICoordinateMappingService _coordinateMapper;
        private readonly IAgentService _agentService;
        private readonly IAgentLoaderService _agentLoaderService;
        private readonly ICoordinateMappingService _coordinateMappingService;
        private readonly IGenerationJobMessageProducerRepository _producerRepository;
        private readonly IGenerationJobMessageAdminRepository _adminRepository;
        private readonly ILogger<GenerationJobMessageProducerService> _logger;
        private int _currentMaxAgentsCount = -1;

        public GenerationJobMessageProducerService(
            ICoordinateMappingService coordinateMapper,
            IAgentService agentService,
            IAgentLoaderService agentLoaderService,
            ICoordinateMappingService coordinateMappingService,
            IGenerationJobMessageProducerRepository producerRepository,
            IGenerationJobMessageAdminRepository adminRepository,
            ILogger<GenerationJobMessageProducerService> logger)
        {
            _generationJobMessageComparer = new GenerationJobMessageComparer();
            _coordinateMapper = coordinateMapper ?? throw new ArgumentNullException(nameof(coordinateMapper));
            _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
            _agentLoaderService = agentLoaderService ?? throw new ArgumentNullException(nameof(agentLoaderService));
            _coordinateMappingService = coordinateMappingService ?? throw new ArgumentNullException(nameof(coordinateMappingService));
            _producerRepository = producerRepository ?? throw new ArgumentNullException(nameof(producerRepository));
            _adminRepository = adminRepository ?? throw new ArgumentNullException(nameof(adminRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Result> QueueTilesGeneration(IEnumerable<SphericalCoordinateModel> tileGenerationInfos, string connectionId, CancellationToken token)
        {
            foreach (var tileGenerationInfo in tileGenerationInfos)
            {
                var queueResult = await QueueZoomedTileGeneration(
                    tileGenerationInfo,
                    connectionId,
                    token);

                if (!queueResult.Success)
                {
                    return Result.CreateFailure(queueResult);
                }
            }

            return Result.CreateSuccess();
        }

        public async ValueTask<Result> QueueTileGeneration(SphericalCoordinateModel tileInfo, string connectionId, CancellationToken token)
        {
            return await QueueZoomedTileGeneration(tileInfo, connectionId, token);
        }

        private async ValueTask<Result> QueueZoomedTileGeneration(SphericalCoordinateModel tileCoords, string connectionId, CancellationToken token)
        {
            var planetoidAgentsResult = await _agentService.GetAgents(tileCoords.PlanetoidId, token);

            if (!planetoidAgentsResult.Success)
            {
                return Result.CreateFailure(planetoidAgentsResult);
            }

            var generationJobs = new List<GenerationJobMessage>();
            var planetoidAgents = planetoidAgentsResult.Data!.OrderBy(x => x.IndexId).ToList();

            foreach (var agentInfo in planetoidAgents)
            {
                var generationJobsResult = await GenerateJobsForTile(
                    tileCoords,
                    planetoidAgents,
                    agentInfo.IndexId,
                    connectionId,
                    token);

                if (!generationJobsResult.Success)
                {
                    return Result.CreateFailure(generationJobsResult);
                }

                generationJobs.AddRange(generationJobsResult.Data!);
            }

            var ensureResult = EnsureMessagingTopicsExist(planetoidAgents.Count);

            return !ensureResult.Success
                ? Result.CreateFailure(ensureResult)
                : await _producerRepository.ProduceAsync(
                    generationJobs.Distinct(_generationJobMessageComparer).OrderBy(x => x.AgentIndex),
                    token);
        }

        private Result EnsureMessagingTopicsExist(int planetoidAgentsCount)
        {
            lock (_lock)
            {
                if (planetoidAgentsCount > _currentMaxAgentsCount)
                {
                    var ensureResult = _adminRepository.EnsureExists(planetoidAgentsCount).Result;

                    if (!ensureResult.Success)
                    {
                        return Result.CreateFailure(ensureResult);
                    }

                    _logger.LogInformation(
                        "The following topics were created before producing messaging: [{topics}].",
                        string.Join(", ", ensureResult.Data));

                    _currentMaxAgentsCount = planetoidAgentsCount;
                }
            }

            return Result.CreateSuccess();
        }

        private async ValueTask<Result<IEnumerable<GenerationJobMessage>>> GenerateJobsForTile(
            SphericalCoordinateModel tileInfo,
            IReadOnlyList<AgentInfoModel> planetoidAgents,
            int agentId,
            string connectionId,
            CancellationToken token)
        {
            var agent = planetoidAgents.FirstOrDefault(x => x.IndexId == agentId);
            var planarModel = GetPlanarCoordinates(tileInfo.PlanetoidId, tileInfo.Longtitude, tileInfo.Latitude, tileInfo.Zoom);
            var agentInstanceResult = _agentLoaderService.GetAgent(agent.Title);

            if (!agentInstanceResult.Success)
            {
                return Result<IEnumerable<GenerationJobMessage>>.CreateFailure(agentInstanceResult);
            }

            var generationJobs = new List<GenerationJobMessage>();
            var agentDependencies = await agentInstanceResult.Data!.GetDependencies(planarModel.Z);

            foreach (var dependency in agentDependencies)
            {
                var relatedGenerationJobsResult = await GenerateJobsForTile(
                    _coordinateMappingService.RelativeTile(tileInfo, dependency.Direction),
                    planetoidAgents,
                    agent.IndexId - 1,
                    connectionId,
                    token);

                if (!relatedGenerationJobsResult.Success)
                {
                    return Result<IEnumerable<GenerationJobMessage>>.CreateFailure(relatedGenerationJobsResult);
                }

                generationJobs.AddRange(relatedGenerationJobsResult.Data!);
            }

            generationJobs.Add(
                new GenerationJobMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    AgentIndex = agent.IndexId,
                    PlanetoidId = tileInfo.PlanetoidId,
                    PlanetoidAgentsCount = planetoidAgents.Count,
                    Z = planarModel.Z,
                    X = planarModel.X,
                    Y = planarModel.Y,
                    ConnectionId = connectionId,
                    DeliveryAttempt = 1,
                });

            return Result<IEnumerable<GenerationJobMessage>>.CreateSuccess(generationJobs);
        }

        private PlanarCoordinateModel GetPlanarCoordinates(int planetoidId, double lon, double lat, short zoom)
        {
            var sphericalModel = new SphericalCoordinateModel(planetoidId, lon, lat, zoom);
            var cubicModel = _coordinateMapper.ToCubic(sphericalModel);

            return _coordinateMapper.ToPlanar(cubicModel);
        }
    }
}
