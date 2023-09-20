using Assimp;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.BusinessLogic.Common.Services.Generation;
using PlanetoidGen.Contracts.Repositories.Generation;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Info;
using Xunit;
using Xunit.Abstractions;

namespace PlanetoidGen.Agents.Tests.Unit.Viewing
{
    public class RailwayTo3dModelAgentTests : BaseAgentTests
    {
        public RailwayTo3dModelAgentTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task GivenDefaultSettings_TestAssimpExport()
        {
            var rootDir = AppContext.BaseDirectory;
            var testFilesDirPath = Path.Combine(rootDir, "TestFiles");

            if (!Directory.Exists(testFilesDirPath))
            {
                Directory.CreateDirectory(testFilesDirPath);
            }

            var filePath = Path.Combine(rootDir, "TestFiles/ExportedRailway.obj");

            var serviceProvider = ServiceProviderMock;

            var service = new RailwayTo3dModelService();
            var conversionService = new AssimpGeometryConversionService(new GeometryConversionService(serviceProvider.GetService<ISpatialReferenceSystemRepository>()!));

            var planetoidInfo = new PlanetoidInfoModel(0, "Earth", 0, 6371000);

            var scene = await GenerateScene(planetoidInfo, service, conversionService, serviceProvider, CancellationToken.None);
            var context = new AssimpContext();
            //var blob = context.ExportToBlob(scene, "obj", PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals | PostProcessSteps.FindInvalidData);
            bool export = context.ExportFile(
                scene,
                filePath,
                "obj",
                PostProcessSteps.Triangulate
                | PostProcessSteps.GenerateNormals
                | PostProcessSteps.FindInvalidData);
            Assert.True(export);
            Assert.True(File.Exists(filePath));
        }

        private static async Task<Scene> GenerateScene(
            PlanetoidInfoModel planetoid,
            RailwayTo3dModelService service,
            AssimpGeometryConversionService geometryConversionService,
            IServiceProvider provider,
            CancellationToken token)
        {
            string geoText = "MULTILINESTRING((38.356866 48.9922603,38.356038 48.9915433,38.3559003 48.9914306,38.3557816 48.9913146,38.3556676 48.991187))";
            var geometry = new WKTReader().Read(geoText);
            var entity = new RailwayEntity(42772501, "rail", 1, "no", geometry);

            var options = new ConvertTo3dModelAgentSettings();

            // 26918 copy-pasted from the nyc dataset, no thoughts given.
            return service.ProcessEntity(
                entity,
                options,
                (await provider.GetService<IPlanetoidService>()!.GetPlanetoid(3, CancellationToken.None)).Data,
                (await Task.WhenAll((geometry as MultiLineString)!.OfType<LineString>().Select(async x => await geometryConversionService.ToAssimpVectors(
                    x.Coordinates, planetoid, options.YUp, token, null, null)))).ToList(),
                (await geometryConversionService.ToAssimpVectors(new Coordinate[]
                {
                    new Coordinate(38.3773891, 49.0000514),
                }, planetoid, options.YUp, token, null, null))[0],
                15
            );
        }
    }
}
