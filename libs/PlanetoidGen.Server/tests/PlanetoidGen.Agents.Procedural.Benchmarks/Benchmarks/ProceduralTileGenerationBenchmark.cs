using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlanetoidGen.Agents.Procedural.Benchmarks.Extensions;
using PlanetoidGen.Agents.Procedural.Benchmarks.Helpers;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using PlanetoidGen.Infrastructure.Configuration;
using System.Diagnostics;

namespace PlanetoidGen.Agents.Procedural.Benchmarks.Benchmarks
{
    [NativeMemoryProfiler]
    //[MemoryDiagnoser]
    // LaunchCount: how many times we should launch process with target benchmark
    // WarmupCount: how many warmup iterations should be performed
    // IterationCount: how many target iterations should be performed(if specified, BenchmarkDotNet.Jobs.RunMode.MinIterationCount and BenchmarkDotNet.Jobs.RunMode.MaxIterationCount will be ignored)
    // IterationTime: desired time of a single iteration
    // InvocationCount: count of invocation in a single iteration(if specified, IterationTime will be ignored), must be a multiple of UnrollFactor
    [SimpleJob(BenchmarkDotNet.Engines.RunStrategy.ColdStart, targetCount: 1, launchCount: 1, invocationCount: 1, warmupCount: 0)]
    public class ProceduralTileGenerationBenchmark
    {
        private const double Lon = 38.6321;
        private const double Lat = 48.7249;
        private const short Zoom = 12;
        private const short LOD = 12;
        private const int TileSizeInPixels = 2048;
        private const int Seed = 42;
        private const int TileCountRow = 8;

        private static readonly CancellationToken _cancellationToken = CancellationToken.None;

        private readonly IServiceProvider _serviceProvider;
        private readonly IAgentLoaderService _agentLoaderService;
        private readonly IPlanetoidService _planetoidService;
        private readonly IAgentService _agentService;
        private readonly IGenerationLODsService _generationLODsService;
        private readonly ICoordinateMappingService _coordinateMappingService;
        private readonly ITileInfoService _tileInfoService;
        private readonly JobGenerationHelper _jobGenerationHelper;

        public ProceduralTileGenerationBenchmark()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddLogging(b => b.AddConsole());
            serviceCollection
                .ConfigureServiceOptions(configuration)
                .ConfigureMessageBrokerOptions(configuration)
                .ConfigureDocumentDbOptions(configuration)
                .ConfigureGeoInfoOptions(configuration)
                .ConfigureServices()
                .ConfigureDataAccess(configuration);

            _serviceProvider = serviceCollection.BuildServiceProvider();

            _agentLoaderService = _serviceProvider.GetRequiredService<IAgentLoaderService>()!;
            _planetoidService = _serviceProvider.GetRequiredService<IPlanetoidService>()!;
            _generationLODsService = _serviceProvider.GetRequiredService<IGenerationLODsService>()!;
            _agentService = _serviceProvider.GetRequiredService<IAgentService>()!;
            _coordinateMappingService = _serviceProvider.GetRequiredService<ICoordinateMappingService>()!;
            _tileInfoService = _serviceProvider.GetRequiredService<ITileInfoService>()!;
            _jobGenerationHelper = new JobGenerationHelper(_agentLoaderService, _agentService, _coordinateMappingService);
        }

        [Benchmark]
        public async Task GenerateTile()
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            var planetoidIdResult = await _planetoidService.AddPlanetoid(
                new PlanetoidInfoModel(0, $"{nameof(ProceduralTileGenerationBenchmark)}_Earth_{Guid.NewGuid()}", Seed, 6_371_000),
                _cancellationToken);
            var planetoidId = planetoidIdResult.Data;
            planetoidIdResult.EnsureSuccess();

            var lodResult = await _generationLODsService.InsertLODs(new List<GenerationLODModel> { new GenerationLODModel(planetoidId, LOD, Zoom) }, _cancellationToken);
            lodResult.EnsureSuccess();

            var agents = new List<AgentInfoModel>
            {
                new AgentInfoModel(planetoidId, 0, "PlanetoidGen.Procedural.ReliefAgent", $"{{\"TileSizeInPixels\":{TileSizeInPixels},\"MaxMaskAltitude\":50,\"MaskEdgeThresholdNegativePercentage\":-0.32,\"MaskEdgeThresholdPositivePercentage\":0.16,\"MaxMountainAltittude\":1000,\"MinMountainThreshold\":15,\"MaxHillAltittude\":250,\"MinHillThreshold\":25,\"MinShorelineAltitude\":0,\"MaxShorelineAltitude\":4,\"GaussianKernelSize\":5}}", false),
                new AgentInfoModel(planetoidId, 1, "PlanetoidGen.Procedural.HeightMapEncoderAgent", "{\"MaxMaskAltitude\":50,\"MaxAltitude\":1500}", false),
            };

            var setAgentsResult = await _agentService.SetAgents(agents, _cancellationToken);
            setAgentsResult.EnsureSuccess();

            stopwatch.Stop();

            Console.WriteLine($"Planetoid setup: {stopwatch.ElapsedMilliseconds}");

            stopwatch.Reset();

            stopwatch.Start();

            var baseTile = new SphericalCoordinateModel(planetoidId, Lon / 180.0 * Math.PI, Lat / 180.0 * Math.PI, Zoom);
            var tiles = TileGenerationHelper.GenerateTilesForRequest(TileCountRow, baseTile, _coordinateMappingService)
                .Select(x => new SphericalCoordinateModel(planetoidId, x.Longtitude, x.Latitude, x.LOD))
                .ToList();

            Console.WriteLine($"Starting tile generation. {tiles.Count} tiles in total.");

            var jobsResult = await _jobGenerationHelper.QueueTilesGeneration(tiles, "connectionId", _cancellationToken);
            var jobs = jobsResult.Data;
            jobsResult.EnsureSuccess();

            Console.WriteLine($"{jobs.Count} jobs in total.");

            var agentInfosResult = await _agentService.GetAgents(planetoidId, _cancellationToken);
            agentInfosResult.EnsureSuccess();
            var allAgents = agentInfosResult.Data.ToList();

            foreach (var job in jobs)
            {
                var tileResult = await _tileInfoService.SelectTile(
                    new PlanarCoordinateModel(job.PlanetoidId, job.Z, job.X, job.Y),
                    _cancellationToken);
                tileResult.EnsureSuccess();

                var agent = allAgents.FirstOrDefault(x => x.IndexId == job.AgentIndex);
                var agentInstanceResult = _agentLoaderService.GetAgent(agent!.Title);
                var agentImplementation = agentInstanceResult.Data;
                agentInstanceResult.EnsureSuccess();

                var initResult = await agentImplementation!.Initialize(agent.Settings, _serviceProvider);
                initResult.EnsureSuccess();

                var executionResult = await agentImplementation!.Execute(job, _cancellationToken);
                executionResult.EnsureSuccess();
            }

            stopwatch.Stop();

            Console.WriteLine($"Tile generation: {stopwatch.ElapsedMilliseconds}");
        }
    }
}
