using Microsoft.Extensions.Options;
using PlanetoidGen.Contracts.Enums.Messaging;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Models.Services.Agents;
using PlanetoidGen.Contracts.Repositories.Info;
using PlanetoidGen.Contracts.Repositories.Messaging;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Info;

namespace PlanetoidGen.API.AgentWorker.Workers
{
    public class AgentWorkerService : BackgroundService
    {
        private const int DefaultAgentExecutionSlidingTimeoutMilliseconds = 45000;
        private const int StartingAgentCount = 1;

        private readonly int _workerCount;
        private readonly TimeSpan _agentExecutionSlidingTimeout;
        private readonly AgentWorkerServiceOptions _agentWorkerOptions;

        private readonly IGenerationJobMessageConsumerRepository _consumerRepository;
        private readonly IGenerationJobMessageAdminRepository _messageAdminRepository;
        private readonly IAgentInfoRepository _agentInfoRepository;
        private readonly ITileInfoService _tileInfoService;
        private readonly IAgentLoaderService _agentLoaderService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AgentWorkerService> _logger;

        public AgentWorkerService(
            IGenerationJobMessageConsumerRepository consumerRepository,
            IGenerationJobMessageAdminRepository messageAdminRepository,
            ITileInfoService tileInfoService,
            IAgentLoaderService agentLoaderService,
            IAgentInfoRepository agentInfoRepository,
            IServiceProvider serviceProvider,
            IOptions<AgentWorkerServiceOptions> options,
            ILogger<AgentWorkerService> logger)
        {
            _consumerRepository = consumerRepository ?? throw new ArgumentNullException(nameof(consumerRepository));
            _messageAdminRepository = messageAdminRepository ?? throw new ArgumentNullException(nameof(messageAdminRepository));
            _tileInfoService = tileInfoService ?? throw new ArgumentNullException(nameof(tileInfoService));
            _agentLoaderService = agentLoaderService ?? throw new ArgumentNullException(nameof(agentLoaderService));
            _agentInfoRepository = agentInfoRepository ?? throw new ArgumentNullException(nameof(agentInfoRepository));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _agentWorkerOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _workerCount = _agentWorkerOptions.AgentWorkersCount ?? Environment.ProcessorCount;
            _agentExecutionSlidingTimeout = TimeSpan.FromMilliseconds(_agentWorkerOptions.AgentExecutionSlidingTimeoutMilliseconds
                ?? DefaultAgentExecutionSlidingTimeoutMilliseconds);

            _logger.LogInformation("Agent worker service will start with {n} instances.", _workerCount);
            _logger.LogInformation("Agent worker service will use '{timeout}' as agent execution sliding timeout.", _agentExecutionSlidingTimeout);
        }

        /// <summary>
        /// Entrypoint for <see cref="AgentWorkerService"/>.
        /// Creates N tasks based on <see cref="_workerCount"/> value.
        /// Each task starts to consume generation job messages using starting with agentsCount = 1.
        /// This means that at the start we will consume messages only from the very first agent topic.
        /// When first message is consumed then <see cref="IGenerationJobMessageConsumerRepository.ConsumeAsync"/>
        /// will use <see cref="IGenerationJobMessage.PlanetoidAgentsCount"/> as agent count value.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var existsResult = await _messageAdminRepository.EnsureExists(StartingAgentCount);

            if (!existsResult.Success)
            {
                throw new InvalidOperationException("Unable to start worker as messaging topics do not exist.");
            }

            if (existsResult.Data.Any())
            {
                _logger.LogInformation("Topics [{topics}] were created before worker start.", string.Join(",", existsResult.Data));
            }

            await Task.WhenAll(Enumerable
                .Range(0, _workerCount)
                .Select(i =>
                {
                    return AgentExecutionFailurePolicies
                        .MessageConsumeWaitAndRetryForever(
                            _agentWorkerOptions.AgentExecutionRetryWaitMilliseconds * 2,
                            _logger)
                        .ExecuteAsync(async () => await _consumerRepository.ConsumeAsync<GenerationJobMessage>(
                            $"consumer_{i}_{Guid.NewGuid()}",
                            agentsCount: StartingAgentCount,
                            job => ProcessMessageAsync(job, stoppingToken),
                            stoppingToken));
                })
                .ToList());
        }

        /// <summary>
        /// Checks if <paramref name="job"/> should be skipped based on <see cref="TileInfoModel.LastAgent"/>,
        /// <see cref="TileInfoModel.ModifiedDate"/>, and <paramref name="agentExecutionSlidingTimeout"/>.
        /// </summary>
        /// <param name="job">Agent generation job.</param>
        /// <param name="tile">Tile info.</param>
        /// <param name="agentExecutionSlidingTimeout">Agent execution sliding timeout.</param>
        /// <returns></returns>
        private static bool ShouldSkipJob(GenerationJobMessage job, TileInfoModel tile, TimeSpan agentExecutionSlidingTimeout)
        {
            return job.AgentIndex == tile.LastAgent
                && tile.ModifiedDate.HasValue
                && DateTime.UtcNow.Subtract(tile.ModifiedDate.Value.UtcDateTime) < agentExecutionSlidingTimeout;
        }

        /// <summary>
        /// Checks if job should be rerun based on <see cref="TileInfoModel.LastIndexedAgent"/>,
        /// <see cref="AgentInfoModel.IndexId"/>, <see cref="AgentInfoModel.ShouldRerunIfLast"/>,
        /// and <paramref name="allAgentsCount"/>.
        /// </summary>
        /// <param name="agent">Current agent related to <see cref="GenerationJobMessage"/>.</param>
        /// <param name="allAgentsCount">All planetoid agents count.</param>
        /// <param name="tile">Tile info.</param>
        /// <returns></returns>
        private static bool ShouldRerunJob(AgentInfoModel agent, int allAgentsCount, TileInfoModel tile)
        {
            return tile.LastIndexedAgent == agent.IndexId && agent.IndexId == allAgentsCount - 1 && agent.ShouldRerunIfLast;
        }

        /// <summary>
        /// A callback that is used to process each consumed generation job message.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        private async ValueTask<Result<GenerationJobMessageProcessingStatus>> ProcessMessageAsync(
            GenerationJobMessage job,
            CancellationToken stoppingToken)
        {
            var tileResult = await _tileInfoService.SelectTile(
                new PlanarCoordinateModel(job.PlanetoidId, job.Z, job.X, job.Y),
                stoppingToken);

            if (!tileResult.Success)
            {
                return Result<GenerationJobMessageProcessingStatus>.CreateFailure(tileResult);
            }

            var agentInfosResult = await _agentInfoRepository.GetAgents(job.PlanetoidId, stoppingToken);

            if (!agentInfosResult.Success)
            {
                return Result<GenerationJobMessageProcessingStatus>.CreateFailure(agentInfosResult);
            }

            var allAgents = agentInfosResult.Data.ToList();
            var agent = allAgents.FirstOrDefault(x => x.IndexId == job.AgentIndex);

            return agent == null
                ? Result<GenerationJobMessageProcessingStatus>.CreateFailure(
                    $"Agent with index {job.AgentIndex} and planetoid id {job.PlanetoidId} does not exist.")
                : await ProcessMessageAsync(job, agent, allAgents, tileResult.Data, stoppingToken);
        }

        /// <summary>
        /// Processes <paramref name="job"/> with already retrieved <paramref name="agent"/>,
        /// <paramref name="allAgents"/>, and <paramref name="tile"/>.
        /// </summary>
        /// <param name="job">Job message to be processed.</param>
        /// <param name="agent">Agent info selected based on <paramref name="job"/> properties.</param>
        /// <param name="allAgents">All planetoid agents infos selected based on <paramref name="job"/> properties.</param>
        /// <param name="tile">Selected tile based on <paramref name="job"/> properties.</param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        private async Task<Result<GenerationJobMessageProcessingStatus>> ProcessMessageAsync(
            GenerationJobMessage job,
            AgentInfoModel agent,
            IList<AgentInfoModel> allAgents,
            TileInfoModel tile,
            CancellationToken stoppingToken)
        {
            if (ShouldSkipJob(job, tile, _agentExecutionSlidingTimeout))
            {
                _logger.LogInformation(
                    "Job is skipped:\n\tJob: ({job});\n\tTile: ({tile});\n\tAgent: ({agent}).",
                    job.ToString(),
                    tile.ToString(),
                    agent.ToString());

                return Result<GenerationJobMessageProcessingStatus>.CreateSuccess(GenerationJobMessageProcessingStatus.Skip);
            }

            if (ShouldRerunJob(agent, allAgents.Count, tile))
            {
                _logger.LogInformation(
                    "Job will be rerun:\n\tJob: ({job});\n\tTile: ({tile});\n\tAgent: ({agent}).",
                    job.ToString(),
                    tile.ToString(),
                    agent.ToString());
            }
            else if (tile.LastIndexedAgent >= agent.IndexId)
            {
                _logger.LogInformation(
                    "Job was already executed:\n\tJob: ({job});\n\tTile: ({tile});\n\tAgent: ({agent}).",
                    job.ToString(),
                    tile.ToString(),
                    agent.ToString());

                return Result<GenerationJobMessageProcessingStatus>.CreateSuccess(GenerationJobMessageProcessingStatus.Completion);
            }
            else if (tile.LastIndexedAgent < agent.IndexId - 1)
            {
                _logger.LogInformation(
                    "Job is waiting for previous agent:\n\tJob: ({job});\n\tTile: ({tile});\n\tAgent: ({agent}).",
                    job.ToString(),
                    tile.ToString(),
                    agent.ToString());

                return Result<GenerationJobMessageProcessingStatus>.CreateSuccess(GenerationJobMessageProcessingStatus.WaitingForPreviousAgent);
            }

            IAgent? agentImplementation = null;

            return await ExecutePipeline(
                job,
                (
                    (_) =>
                    {
                        var agentInstanceResult = _agentLoaderService.GetAgent(agent.Title);
                        agentImplementation = agentInstanceResult.Data;

                        return new ValueTask<Result>(Result.Convert(agentInstanceResult));
                    },
                    continueOnError: false
                ),
                (async (_) => await agentImplementation!.Initialize(agent.Settings, _serviceProvider), continueOnError: false),
                (async (_) => Result.Convert(await _tileInfoService.UpdateTileLastModifiedDateSetCurrentTimestamp(tile, stoppingToken)), continueOnError: false),
                (async (_) => await ExecuteAgent(job, agentImplementation!, stoppingToken), continueOnError: true),
                (
                    async (prev) =>
                    {
                        if (!prev.Success)
                        {
                            _logger.LogError("Failed to execute agent job ({job}). Error: {error}", job.ToString(), prev.ErrorMessage!.ToString());
                        }

                        var updateResult = prev.Success
                            ? await _tileInfoService.UpdateTileLastAgentResetLastModifiedDate(tile.Id, agent.IndexId, stoppingToken)
                            : await _tileInfoService.UpdateTileLastModifiedDate(tile, modifiedDate: null, stoppingToken);

                        return prev.Success ? Result.Convert(updateResult) : prev;
                    },
                    continueOnError: false
                 )
            );
        }

        private async Task<Result<GenerationJobMessageProcessingStatus>> ExecutePipeline(
            GenerationJobMessage job,
            params (Func<Result, ValueTask<Result>> func, bool continueOnError)[] steps)
        {
            var prev = Result.CreateSuccess();

            foreach (var (step, continueOnError) in steps)
            {
                var result = await step(prev);

                if (!result.Success && !continueOnError)
                {
                    _logger.LogError("Failed to run job ({job}). Error: {error}", job.ToString(), result.ErrorMessage!.ToString());

                    return Result<GenerationJobMessageProcessingStatus>.CreateFailure(result);
                }

                prev = result;
            }

            _logger.LogInformation("Job successfully executed: ({job}).", job.ToString());

            return Result<GenerationJobMessageProcessingStatus>.CreateSuccess(GenerationJobMessageProcessingStatus.Completion);
        }

        private async ValueTask<Result> ExecuteAgent(GenerationJobMessage job, IAgent agent, CancellationToken stoppingToken)
        {
            var result = await AgentExecutionFailurePolicies.AgentExecuteWaitAndRetry(
                    _agentWorkerOptions.AgentExecutionRetryCount,
                    _agentWorkerOptions.AgentExecutionRetryWaitMilliseconds,
                    _logger)
               .ExecuteAndCaptureAsync(async (token) => await agent!.Execute(job, token), stoppingToken);

            if (result.FinalException != null)
            {
                return Result.CreateFailure(result.FinalException);
            }

            return result.FinalHandledResult != null && !result.FinalHandledResult.Success
                ? result.FinalHandledResult
                : Result.CreateSuccess();
        }
    }
}
