using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap;
using PlanetoidGen.Agents.Osm.Agents.Viewing;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Enums;
using PlanetoidGen.Domain.Models.Info;
using PlanetoidGen.Infrastructure.Configuration;
using System.Text.Json;
using Xunit;

namespace PlanetoidGen.Agents.Tests.Integration
{
    /// <summary>
    /// Tests to resemble real-world usage of the OSM chain of agents.
    /// </summary>
    public class OpenStreetMapChainTests
    {
        private readonly IServiceProvider _serviceProvider;

        public OpenStreetMapChainTests()
        {
            _serviceProvider = SetupServiceProvider();
        }

        private IServiceProvider SetupServiceProvider()
        {
            var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.Testing.json")
                    .AddInMemoryCollection(new Dictionary<string, string?>()
                    {
                        { "MetaProcedureOptions:RecreateExtensions", "false" },
                        { "MetaProcedureOptions:RecreateSchemas", "false" },
                        { "MetaProcedureOptions:RecreateProcedures", "true" },
                        { "MetaProcedureOptions:RecreateTables", "false" },
                        { "MetaProcedureOptions:RecreateDynamicTables", "false" },
                        { "MetaProcedureOptions:ServerName", "main" },
                    })
                    .Build();

            return new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddSingleton(typeof(ILogger<>), typeof(TestLogger<>))
                .ConfigureServiceOptions(configuration)
                .ConfigureDocumentDbOptions(configuration)
                .ConfigureGeoInfoOptions(configuration)
                .ConfigureServices()
                .ConfigureDataAccess(configuration)
                .BuildServiceProvider();
        }

        [Fact]
        public async Task GivenDefaultSettings_TestOSM3dAgents_Rubizhne()
        {
            var token = CancellationToken.None;

            var planetoid = await GetPlanetoid(_serviceProvider, token);

            // Rough coordinates of Rubizhne, Рубіжанська міська громада, Sievierodonetsk Raion, Luhansk Oblast, Ukraine
            // Zoom 12 covers the city in a ~4 tiles
            var coordinatesSpherical = new SphericalCoordinateModel(
                planetoidId: planetoid.Id,
                longtitude: 38.3869 / 180.0 * Math.PI,
                latitude: 48.9951 / 180.0 * Math.PI,
                zoom: 12);

            var coordinatesPlanarSet = GetCoordinates(_serviceProvider, coordinatesSpherical, false);

            await RunInfrastructureAgents(coordinatesPlanarSet, runBuilding: false, runRailway: false);
        }

        [Fact]
        public async Task GivenDefaultSettings_TestOSM3dAgents_Dovhenke()
        {
            var token = CancellationToken.None;

            var planetoid = await GetPlanetoid(_serviceProvider, token);

            // Rough coordinates of Dovhenke, Оскільська сільська громада, Izium Raion, Kharkiv Oblast, 64371, Ukraine
            var coordinatesSpherical = new SphericalCoordinateModel(
                planetoidId: planetoid.Id,
                longtitude: 37.309891 / 180.0 * Math.PI,
                latitude: 49.020627 / 180.0 * Math.PI,
                zoom: 12);

            var coordinatesPlanarSet = GetCoordinates(_serviceProvider, coordinatesSpherical, false);

            await RunInfrastructureAgents(coordinatesPlanarSet);
        }

        [Fact]
        public async Task GivenDefaultSettings_TestAllAgents_Berestove()
        {
            var token = CancellationToken.None;

            var planetoid = await GetPlanetoid(_serviceProvider, token);

            // Rough coordinates of Berestove, Soledar Urban Hromada, Bakhmut Raion, Donetsk Oblast, 84541, Ukraine
            var coordinatesSpherical = new SphericalCoordinateModel(
                planetoidId: planetoid.Id,
                longtitude: 38.254114972340055 / 180.0 * Math.PI,
                latitude: 48.75116294910429 / 180.0 * Math.PI,
                zoom: 12);

            var mappingService = _serviceProvider.GetService<ICoordinateMappingService>()!;
            var coordinatesPlanarSet = GetCoordinates(_serviceProvider, coordinatesSpherical, false);
            await RunInfrastructureAgents(coordinatesPlanarSet
                .Append(mappingService.ToPlanar(
                    mappingService.RelativeTile(coordinatesSpherical, RelativeTileDirectionType.Right)))
                .Append(mappingService.ToPlanar(
                    mappingService.RelativeTile(coordinatesSpherical, RelativeTileDirectionType.Down)))
                );

            var coordinatePlanarWithNeighbors = GetCoordinates(_serviceProvider, coordinatesSpherical, true);
            await RunTileAgents(coordinatePlanarWithNeighbors);
        }

        [Fact]
        public async Task GivenDefaultSettings_TestAllAgents_Novotoshkivske()
        {
            var token = CancellationToken.None;

            var planetoid = await GetPlanetoid(_serviceProvider, token);

            // Rough coordinates of Партизанська вулиця, Novotoshkivske, Гірська міська громада, Sievierodonetsk Raion, Luhansk Oblast, 93891, Ukraine
            var coordinatesSpherical = new SphericalCoordinateModel(
                planetoidId: planetoid.Id,
                longtitude: 38.6321 / 180.0 * Math.PI,
                latitude: 48.7249 / 180.0 * Math.PI,
                zoom: 14);

            var coordinatesPlanarSet = GetCoordinates(_serviceProvider, coordinatesSpherical, false);
            await RunInfrastructureAgents(coordinatesPlanarSet);

            var coordinatePlanarWithNeighbors = GetCoordinates(_serviceProvider, coordinatesSpherical, true);
            await RunTileAgents(coordinatePlanarWithNeighbors);
        }

        [Fact]
        public async Task GivenDefaultSettings_TestAllAgents_Mariupol()
        {
            var token = CancellationToken.None;

            var planetoid = await GetPlanetoid(_serviceProvider, token);

            // Rough coordinates of Myru Avenue, Старе місто, Tsentralnyi District, Mariupol, Mariupol Urban Hromada, Mariupol Raion, Donetsk Oblast, 88500, Ukraine
            var coordinatesSpherical = new SphericalCoordinateModel(
                planetoidId: planetoid.Id,
                longtitude: 37.5497 / 180.0 * Math.PI,
                latitude: 47.0946 / 180.0 * Math.PI,
                zoom: 14);

            var mappingService = _serviceProvider.GetService<ICoordinateMappingService>()!;
            var coordinatesPlanarSet = GetCoordinates(_serviceProvider, coordinatesSpherical, false);
            await RunInfrastructureAgents(coordinatesPlanarSet
                .Append(mappingService.ToPlanar(
                    mappingService.RelativeTile(coordinatesSpherical, RelativeTileDirectionType.Right)))
                );

            var coordinatesSphericalTile = new SphericalCoordinateModel(
                planetoidId: planetoid.Id,
                longtitude: 37.5497 / 180.0 * Math.PI,
                latitude: 47.0946 / 180.0 * Math.PI,
                zoom: 14);

            var coordinatePlanarTile = GetCoordinates(_serviceProvider, coordinatesSphericalTile, true);
            await RunTileAgents(coordinatePlanarTile);
        }

        [Fact]
        public async Task GivenDefaultSettings_TestAllAgents_Yakovlivka()
        {
            var token = CancellationToken.None;

            var planetoid = await GetPlanetoid(_serviceProvider, token);

            // Rough coordinates of Yakovlivka, Soledar Urban Hromada, Bakhmut Raion, Donetsk Oblast, 84542, Ukraine
            var coordinatesSpherical1 = new SphericalCoordinateModel(
                planetoidId: planetoid.Id,
                longtitude: 38.1475 / 180.0 * Math.PI,
                latitude: 48.7083 / 180.0 * Math.PI,
                zoom: 14);
            var coordinatesSpherical2 = new SphericalCoordinateModel(
                planetoidId: planetoid.Id,
                longtitude: 38.1394 / 180.0 * Math.PI,
                latitude: 48.7106 / 180.0 * Math.PI,
                zoom: 14);

            var coordinatesPlanarSet1 = GetCoordinates(_serviceProvider, coordinatesSpherical1, false);
            await RunInfrastructureAgents(coordinatesPlanarSet1);
            var coordinatesPlanarSet2 = GetCoordinates(_serviceProvider, coordinatesSpherical2, false);
            await RunInfrastructureAgents(coordinatesPlanarSet2);

            var coordinatePlanarWithNeighbors = GetCoordinates(_serviceProvider, coordinatesSpherical1, true);
            await RunTileAgents(coordinatePlanarWithNeighbors);
        }

        [Fact]
        public async Task GivenDefaultSettings_TestOSMTileAgents_Dovhenke()
        {
            var token = CancellationToken.None;

            var planetoid = await GetPlanetoid(_serviceProvider, token);

            // Rough coordinates of Dovhenke, Оскільська сільська громада, Izium Raion, Kharkiv Oblast, 64371, Ukraine
            var coordinatesSpherical = new SphericalCoordinateModel(
                planetoidId: planetoid.Id,
                longtitude: 37.309891 / 180.0 * Math.PI,
                latitude: 49.020627 / 180.0 * Math.PI,
                zoom: 14);

            var coordinatesPlanarSet = GetCoordinates(_serviceProvider, coordinatesSpherical, true);

            await RunTileAgents(coordinatesPlanarSet);
        }

        private async ValueTask RunTileAgents(IEnumerable<PlanarCoordinateModel> coordinates)
        {
            var jobs = coordinates.Select(c => new GenerationJobMessage
            {
                Id = "id",
                PlanetoidId = c.PlanetoidId,
                Z = c.Z,
                X = c.X,
                Y = c.Y,
                AgentIndex = 0
            });

            var tileInfoService = _serviceProvider.GetRequiredService<ITileInfoService>();

            foreach (var job in jobs)
            {
                await tileInfoService.SelectTile(new PlanarCoordinateModel(job.PlanetoidId, job.Z, job.X, job.Y), CancellationToken.None);
            }

            var agents = new List<IAgent>();
            var settings = new List<string>();

            bool areLoadingAgentsActive = true;

            if (areLoadingAgentsActive)
            {
                var agent = new TileLoadingAgent();
                var options = JsonSerializer.Deserialize<Dictionary<string, object>>(await agent.GetDefaultSettings());
                Assert.NotNull(options);

                options["AccessToken"] = Environment.GetEnvironmentVariable("MAPBOX_TOKEN") ?? string.Empty;

                agents.Add(agent);
                settings.Add(JsonSerializer.Serialize(options));
            }

            Assert.Equal(agents.Count, settings.Count);

            for (int i = 0; i < settings.Count; ++i)
            {
                foreach (var job in jobs)
                {
                    await RunAgent(agents[i], settings[i], job);
                }
            }
        }

        private async ValueTask RunInfrastructureAgents(IEnumerable<PlanarCoordinateModel> coordinates, bool runBuilding = true, bool runHighway = true, bool runRailway = true)
        {
            var jobs = coordinates.Select(c => new GenerationJobMessage
            {
                Id = "id",
                PlanetoidId = c.PlanetoidId,
                Z = c.Z,
                X = c.X,
                Y = c.Y,
                AgentIndex = 0
            });

            var tileInfoService = _serviceProvider.GetRequiredService<ITileInfoService>();

            foreach (var job in jobs)
            {
                await tileInfoService.SelectTile(new PlanarCoordinateModel(job.PlanetoidId, job.Z, job.X, job.Y), CancellationToken.None);
            }

            var agents = new List<IAgent>();

            bool areLoadingAgentsActive = true, areSeedingAgentsActive = true, are3dAgentsActive = true;

            var settings = new List<string>();

            if (areLoadingAgentsActive)
            {
                var agent = new BuildingLoadingAgent();
                var options = JsonSerializer.Deserialize<Dictionary<string, object>>(await agent.GetDefaultSettings());
                Assert.NotNull(options);

                options!["PushToGeoServer"] = false;
                options!["OverpassBaseUrl"] = "http://localhost:31123/api/interpreter";

                if (runBuilding)
                {
                    agents.Add(agent);
                    settings.Add(JsonSerializer.Serialize(options));
                }

                if (runHighway)
                {
                    agents.Add(new HighwayLoadingAgent());
                    settings.Add(JsonSerializer.Serialize(options));
                }

                if (runRailway)
                {
                    agents.Add(new RailwayLoadingAgent());
                    settings.Add(JsonSerializer.Serialize(options));
                }
            }

            if (areSeedingAgentsActive && runBuilding)
            {
                var agent = new BuildingInformationSeedingAgent();
                var options = JsonSerializer.Deserialize<Dictionary<string, object>>(await agent.GetDefaultSettings());
                Assert.NotNull(options);

                options!["Seed"] = 0L;

                agents.Add(agent);
                settings.Add(JsonSerializer.Serialize(options));
            }

            if (are3dAgentsActive)
            {
                var agent = new BuildingTo3dModelAgent();
                var options = JsonSerializer.Deserialize<Dictionary<string, object>>(await agent.GetDefaultSettings());
                Assert.NotNull(options);

                //options!["Seed"] = 0L;

                if (runBuilding)
                {
                    agents.Add(agent);
                    settings.Add(JsonSerializer.Serialize(options));
                }

                if (runHighway)
                {
                    agents.Add(new HighwayTo3dModelAgent());
                    settings.Add(JsonSerializer.Serialize(options));
                }

                if (runRailway)
                {
                    agents.Add(new RailwayTo3dModelAgent());
                    settings.Add(JsonSerializer.Serialize(options));
                }
            }

            Assert.Equal(agents.Count, settings.Count);

            for (int i = 0; i < settings.Count; ++i)
            {
                foreach (var job in jobs)
                {
                    await RunAgent(agents[i], settings[i], job);
                }
            }
        }

        private async ValueTask RunAgent(IAgent agent, string settings, GenerationJobMessage job)
        {
            var initResult = await agent.Initialize(settings, _serviceProvider);
            Assert.True(initResult.Success, $"{agent.Title}: {initResult.ErrorMessage?.ToString() ?? ""}");

            var executionResult = await agent.Execute(job, CancellationToken.None);
            Assert.True(executionResult.Success, $"{agent.Title}: {executionResult.ErrorMessage?.ToString() ?? ""}");
        }

        private async ValueTask<PlanetoidInfoModel> GetPlanetoid(IServiceProvider provider, CancellationToken token)
        {
            var planetoid = new PlanetoidInfoModel(3, "Earth", 0, 6_371_000);

            var planetoidService = provider.GetService<IPlanetoidService>()!;
            var addResult = await planetoidService.AddPlanetoid(planetoid, token);

            Assert.True(addResult.Success, addResult.ErrorMessage?.ToString() ?? "");

            var getResult = await planetoidService.GetPlanetoid(addResult.Data!, token);
            Assert.True(getResult.Success, getResult.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(getResult.Data);

            return getResult.Data!;
        }

        private IEnumerable<PlanarCoordinateModel> GetCoordinates(IServiceProvider provider, SphericalCoordinateModel coordinatesSpherical, bool neighbors)
        {
            var coordinateMappingService = provider.GetService<ICoordinateMappingService>()!;

            var coordinatesCubic = coordinateMappingService.ToCubic(coordinatesSpherical);
            var coordinatesPlanar = coordinateMappingService.ToPlanar(coordinatesCubic);

            var coordinates = new List<PlanarCoordinateModel>()
            {
                coordinatesPlanar,
            };

            if (neighbors)
            {
                coordinates.Add(coordinateMappingService.ToPlanar(coordinateMappingService.RelativeTile(coordinatesSpherical, RelativeTileDirectionType.Up)));
                coordinates.Add(coordinateMappingService.ToPlanar(coordinateMappingService.RelativeTile(coordinatesSpherical, RelativeTileDirectionType.Left)));
                coordinates.Add(coordinateMappingService.ToPlanar(coordinateMappingService.RelativeTile(coordinatesSpherical, RelativeTileDirectionType.Down)));
                coordinates.Add(coordinateMappingService.ToPlanar(coordinateMappingService.RelativeTile(coordinatesSpherical, RelativeTileDirectionType.Right)));
            }

            return coordinates;
        }
    }
}
