using PlanetoidGen.Contracts.Comparers;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Info;

namespace PlanetoidGen.Agents.Procedural.Benchmarks.Helpers
{
    public class JobGenerationHelper
    {
        private readonly GenerationJobMessageComparer _generationJobMessageComparer;

        private readonly ICoordinateMappingService _coordinateMappingService;
        private readonly IAgentService _agentService;
        private readonly IAgentLoaderService _agentLoaderService;

        public JobGenerationHelper(
            IAgentLoaderService agentLoaderService,
            IAgentService agentService,
            ICoordinateMappingService coordinateMappingService)
        {
            _generationJobMessageComparer = new GenerationJobMessageComparer();

            _agentLoaderService = agentLoaderService ?? throw new ArgumentNullException(nameof(agentLoaderService));
            _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
            _coordinateMappingService = coordinateMappingService ?? throw new ArgumentNullException(nameof(coordinateMappingService));
        }

        public async ValueTask<Result<IList<GenerationJobMessage>>> QueueTilesGeneration(
            IEnumerable<SphericalCoordinateModel> tileGenerationInfos,
            string connectionId,
            CancellationToken token)
        {
            var jobs = new List<GenerationJobMessage>();

            foreach (var tileGenerationInfo in tileGenerationInfos)
            {
                var queueResult = await QueueZoomedTileGeneration(
                    tileGenerationInfo,
                    connectionId,
                    token);

                if (!queueResult.Success)
                {
                    return Result<IList<GenerationJobMessage>>.CreateFailure(queueResult);
                }

                jobs.AddRange(queueResult.Data);
            }

            return Result<IList<GenerationJobMessage>>.CreateSuccess(jobs);
        }

        private async ValueTask<Result<IList<GenerationJobMessage>>> QueueZoomedTileGeneration(
            SphericalCoordinateModel tileCoords,
            string connectionId,
            CancellationToken token)
        {
            var planetoidAgentsResult = await _agentService.GetAgents(tileCoords.PlanetoidId, token);

            if (!planetoidAgentsResult.Success)
            {
                return Result<IList<GenerationJobMessage>>.CreateFailure(planetoidAgentsResult);
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
                    return Result<IList<GenerationJobMessage>>.CreateFailure(generationJobsResult);
                }

                generationJobs.AddRange(generationJobsResult.Data!);
            }

            return Result<IList<GenerationJobMessage>>.CreateSuccess(generationJobs.Distinct(_generationJobMessageComparer).OrderBy(x => x.AgentIndex).ToList());
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
            var agentInstanceResult = _agentLoaderService.GetAgent(agent!.Title);

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
            var cubicModel = _coordinateMappingService.ToCubic(sphericalModel);

            return _coordinateMappingService.ToPlanar(cubicModel);
        }
    }
}
