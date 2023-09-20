using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Dtos;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Services.Implementations;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Models.Services.GeoInfo;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Generation;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace PlanetoidGen.Agents.Tests.Unit.OpenStreetMap
{
    public class HighwayLoadingAgentTests : BaseAgentTests
    {
        public HighwayLoadingAgentTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task GivenDefaultSettings_TestOverpassService_Bohn()
        {
            var serviceProvider = ServiceProviderMock;

            // Bbox from default Overpass example in Bonn, Germany
            var bbox = new BoundingBoxDto()
            {
                South = 50.746,
                West = 7.154,
                North = 50.748,
                East = 7.157,
            };

            var service = new OverpassApiService(
                serviceProvider.GetService<IOptions<GeoInfoServiceOptions>>()!.Value,
                serviceProvider.GetService<ILogger<OverpassApiService>>()!);

            var response = await service.GetHighways(bbox, CancellationToken.None);
            Assert.True(response.Success, response.ErrorMessage?.ToString() ?? string.Empty);

            Assert.NotNull(response.Data);
            var data = response.Data!;

            var highways = data.Ways;
            var nodes = data.Nodes;
            // There's at least one highway, if not the API has changed or there is an error
            Assert.True(highways.Any());
            // All highways need to contain the highway keyword tag, e.g. highway
            Assert.All(highways, highway => Assert.True(highway.Tags.ContainsKey(service.HighwayKeyword), string.Join(',', highway.Tags.Keys)));
            // At least one highway node contains a tag, e.g. a crossing or a circle
            Assert.Contains(nodes, node => node.Tags.Any());

            var highwayEntities = service.ToHighwayEntityList(data);
            Assert.True(highwayEntities.Any());
            Assert.All(highwayEntities, x => Assert.True(x.Path.Any(), x.Path.ToString()));
        }

        [Fact]
        public async Task GivenDefaultSettings_TestHighwayLoadingAgent_Rubizhne()
        {
            var serviceProvider = ServiceProviderMock;

            var planetoidService = serviceProvider.GetService<IPlanetoidService>();
            Assert.NotNull(planetoidService);

            var planetoid = await planetoidService!.GetPlanetoid(3, CancellationToken.None);
            Assert.NotNull(planetoid);
            Assert.True(planetoid.Success, planetoid.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(planetoid.Data);
            Assert.Equal("Earth", planetoid.Data.Title);

            var coordinateMappingService = serviceProvider.GetService<ICoordinateMappingService>();
            Assert.NotNull(coordinateMappingService);

            // Rough coordinates of Rubizhne, Рубіжанська міська громада, Sievierodonetsk Raion, Luhansk Oblast, Ukraine
            // Zoom 13 covers the city in a ~4 tiles
            var coordinatesSpherical = new SphericalCoordinateModel(
                planetoidId: planetoid.Data.Id,
                longtitude: 38.3869 / 180.0 * Math.PI,
                latitude: 48.9951 / 180.0 * Math.PI,
                zoom: 13);
            var coordinatesCubic = coordinateMappingService!.ToCubic(coordinatesSpherical);
            var coordinatesPlanar = coordinateMappingService!.ToPlanar(coordinatesCubic);

            var job = new GenerationJobMessage
            {
                Id = "id",
                PlanetoidId = coordinatesPlanar.PlanetoidId,
                Z = coordinatesPlanar.Z,
                X = coordinatesPlanar.X,
                Y = coordinatesPlanar.Y,
                AgentIndex = 1
            };

            IAgent agent = new HighwayLoadingAgent();

            var options = JsonSerializer.Deserialize<Dictionary<string, object>>(await agent.GetDefaultSettings());
            Assert.NotNull(options);

            options!["PushToGeoServer"] = false;

            var initResult = await agent.Initialize(JsonSerializer.Serialize(options), serviceProvider);
            Assert.True(initResult.Success, initResult.ErrorMessage?.ToString() ?? "");

            var executionResult = await agent.Execute(job, CancellationToken.None);
            Assert.True(executionResult.Success, executionResult.ErrorMessage?.ToString() ?? "");
        }
    }
}
