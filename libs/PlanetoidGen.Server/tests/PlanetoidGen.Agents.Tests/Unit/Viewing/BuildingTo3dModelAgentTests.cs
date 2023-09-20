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
using PlanetoidGen.Domain.Models.Descriptions.Building;
using PlanetoidGen.Domain.Models.Info;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace PlanetoidGen.Agents.Tests.Unit.Viewing
{
    public class BuildingTo3dModelAgentTests : BaseAgentTests
    {
        public BuildingTo3dModelAgentTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task GivenDefaultSettings_TestAssimpExport_MainFlow()
        {
            var rootDir = AppContext.BaseDirectory;
            var testFilesDirPath = Path.Combine(rootDir, "TestFiles");

            if (!Directory.Exists(testFilesDirPath))
            {
                Directory.CreateDirectory(testFilesDirPath);
            }

            var filePath = Path.Combine(rootDir, "TestFiles/ExportedBuilding.obj");

            var serviceProvider = ServiceProviderMock;

            var service = new BuildingTo3dModelService();
            var conversionService = new AssimpGeometryConversionService(new GeometryConversionService(serviceProvider.GetService<ISpatialReferenceSystemRepository>()!));

            var planetoidInfo = new PlanetoidInfoModel(0, "Earth", 0, 6371000);

            var scene = await GenerateSceneMainFlow(planetoidInfo, service, conversionService, serviceProvider, CancellationToken.None);
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

        [Fact]
        public async Task GivenDefaultSettings_TestAssimpExport_Edgecase()
        {
            var rootDir = AppContext.BaseDirectory;
            var testFilesDirPath = Path.Combine(rootDir, "TestFiles");

            if (!Directory.Exists(testFilesDirPath))
            {
                Directory.CreateDirectory(testFilesDirPath);
            }

            var filePath = Path.Combine(rootDir, "TestFiles/ExportedBuilding1.obj");

            var serviceProvider = ServiceProviderMock;

            var service = new BuildingTo3dModelService();
            var conversionService = new AssimpGeometryConversionService(new GeometryConversionService(serviceProvider.GetService<ISpatialReferenceSystemRepository>()!));

            var planetoidInfo = new PlanetoidInfoModel(0, "Earth", 0, 6371000);

            var scene = await GenerateSceneEdgecase(planetoidInfo, service, conversionService, serviceProvider, CancellationToken.None);
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

        // Taken from https://bitbucket.org/Starnick/assimpnet/src/0e82d0d472ae891b371807e6f2d8d8bb97f5fd3e/AssimpNet.Test/AssimpContextTestFixture.cs#lines-117
        [Fact]
        public async Task GivenDefaultSettings_TestAssimpSimpleExport()
        {
            var rootDir = AppContext.BaseDirectory;
            var testFilesDirPath = Path.Combine(rootDir, "TestFiles");
            if (!Directory.Exists(testFilesDirPath))
            {
                Directory.CreateDirectory(testFilesDirPath);
            }

            String triangleFilePath = Path.Combine(rootDir, "TestFiles/ExportedTriangle.obj");

            //Create a very simple scene a single node with a mesh that has a single face, a triangle and a default material
            var scene = new Scene();
            scene.RootNode = new Node("Root");

            var triangle = new Mesh("", PrimitiveType.Triangle);
            triangle.Vertices.Add(new Vector3D(1, 0, 0));
            triangle.Vertices.Add(new Vector3D(5, 5, 0));
            triangle.Vertices.Add(new Vector3D(10, 0, 0));
            triangle.Faces.Add(new Face(new int[] { 0, 1, 2 }));
            triangle.MaterialIndex = 0;

            scene.Meshes.Add(triangle);
            scene.RootNode.MeshIndices.Add(0);

            var mat = new Material();
            mat.Name = "MyMaterial";
            scene.Materials.Add(mat);

            //Export the scene then read it in and compare!

            var context = new AssimpContext();
            bool export = context.ExportFile(scene, triangleFilePath, "obj");
            Assert.True(export);
            Assert.True(File.Exists(triangleFilePath));

            var importedScene = context.ImportFile(triangleFilePath);
            Assert.True(importedScene.MeshCount == scene.MeshCount);
            Assert.True(importedScene.MaterialCount == 2); //Always has the default material, should also have our material

            //Compare the meshes
            var importedTriangle = importedScene.Meshes[0];

            Assert.True(importedTriangle.VertexCount == triangle.VertexCount);
            for (int i = 0; i < importedTriangle.VertexCount; i++)
            {
                Assert.True(importedTriangle.Vertices[i].Equals(triangle.Vertices[i]));
            }

            Assert.True(importedTriangle.FaceCount == triangle.FaceCount);
            for (int i = 0; i < importedTriangle.FaceCount; i++)
            {
                var importedFace = importedTriangle.Faces[i];
                var face = triangle.Faces[i];

                for (int j = 0; j < importedFace.IndexCount; j++)
                {
                    Assert.True(importedFace.Indices[j] == face.Indices[j]);
                }
            }
        }

        private static async Task<Scene> GenerateSceneEdgecase(
            PlanetoidInfoModel planetoid,
            BuildingTo3dModelService service,
            AssimpGeometryConversionService geometryConversionService,
            IServiceProvider provider,
            CancellationToken token)
        {
            string desc = "[{\"Kind\":\"yes\",\"Levels\":1,\"MinLevel\":0,\"UndergroundLevels\":null,\"Flats\":null,\"SoftStorey\":null,\"Colour\":null,\"Material\":\"brick\",\"Cladding\":null,\"Walls\":null,\"Structure\":null,\"Part\":null,\"Fireproof\":null,\"Entrance\":null,\"Access\":null,\"StartDate\":null,\"Roof\":{\"Levels\":0,\"Shape\":\"skillion\",\"Orientation\":null,\"Height\":2.7,\"Angle\":null,\"Direction\":null,\"Colour\":null,\"Material\":\"bitumen\",\"LevelCollection\":null},\"LevelCollection\":[{\"Height\":2.7,\"Sides\":[{\"Width\":32.774937196565844,\"Parts\":[{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":5.774937196565844,\"Kind\":\"window\"}]},{\"Width\":10.117013866992133,\"Parts\":[{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":4.117013866992133,\"Kind\":\"wall\"}]},{\"Width\":12.422691086162226,\"Parts\":[{\"Width\":3,\"Kind\":\"porch\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3.422691086162226,\"Kind\":\"window\"}]},{\"Width\":13.191206840238527,\"Parts\":[{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":4.191206840238527,\"Kind\":\"wall\"}]},{\"Width\":12.456043625228856,\"Parts\":[{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":3.4560436252288564,\"Kind\":\"window\"}]},{\"Width\":11.21751721388468,\"Parts\":[{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":5.21751721388468,\"Kind\":\"window\"}]},{\"Width\":33.553439923183035,\"Parts\":[{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":3.553439923183035,\"Kind\":\"wall\"}]},{\"Width\":10.711332802405408,\"Parts\":[{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":4.711332802405408,\"Kind\":\"window\"}]},{\"Width\":15.269743423942527,\"Parts\":[{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3.2697434239425274,\"Kind\":\"wall\"}]},{\"Width\":12.780101734973243,\"Parts\":[{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3.7801017349732433,\"Kind\":\"wall\"}]},{\"Width\":14.535710894554912,\"Parts\":[{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"window\"},{\"Width\":5.535710894554912,\"Kind\":\"wall\"}]},{\"Width\":11.034235749531767,\"Parts\":[{\"Width\":3,\"Kind\":\"window\"},{\"Width\":3,\"Kind\":\"wall\"},{\"Width\":5.034235749531767,\"Kind\":\"wall\"}]}]}],\"Height\":2.7,\"MinHeight\":null,\"Description\":null}]";
            string geoText = "MULTIPOLYGON(((38.6339515 48.7249307,38.6339431 48.7252254,38.634081 48.7252271,38.6340842 48.7251154,38.634264 48.7251177,38.6342608 48.7252297,38.6344137 48.7252316,38.6344223 48.7249299,38.6342763 48.7249281,38.6342724 48.7250654,38.6340982 48.7250633,38.6341019 48.7249326,38.6339515 48.7249307)))";
            var geometry = new WKTReader().Read(geoText);
            var descriptions = JsonSerializer.Deserialize<IList<BuildingModel>>(desc, new JsonSerializerOptions()
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            })!;
            var entity = new BuildingEntity(443221955, "yes", "brick", "skillion", "bitumen", 1, null, null, null, null, desc, geometry);

            var options = new ConvertTo3dModelAgentSettings();

            // 26918 copy-pasted from the nyc dataset, no thoughts given.
            return service.ProcessEntity(
                entity,
                options,
                (await provider.GetService<IPlanetoidService>()!.GetPlanetoid(3, CancellationToken.None)).Data,
                descriptions,
                (await Task.WhenAll((geometry as MultiPolygon)!.OfType<Polygon>().Select(async x => await geometryConversionService.ToAssimpVectors(
                    x.Shell.Coordinates, planetoid, options.YUp, token, null, null)))).ToList(),
                (await geometryConversionService.ToAssimpVectors(new Coordinate[]
                {
                    new Coordinate(38.6339515, 48.7249307),
                }, planetoid, options.YUp, token, null, null))[0],
                15
            );
        }

        private static async Task<Scene> GenerateSceneMainFlow(
            PlanetoidInfoModel planetoid,
            BuildingTo3dModelService service,
            AssimpGeometryConversionService geometryConversionService,
            IServiceProvider provider,
            CancellationToken token)
        {
            string desc = "[{\"Kind\":\"house\",\"Material\":\"plaster\",\"Levels\":\"1\",\"MinLevel\":\"0\",\"Roof\":{\"Material\":\"bitumen\",\"Levels\":\"0\",\"Shape\":\"hipped\",\"Height\":\"2.7\"},\"LevelCollection\":[{\"Height\":\"2.7\",\"Sides\":[{\"Width\":\"9.270905634822881\",\"Parts\":[{\"Width\":\"2.696569179837079\",\"Kind\":\"insulationporch\"},{\"Width\":\"6.5743364549858025\",\"Kind\":\"wall\"}]},{\"Width\":\"7.070991670985675\",\"Parts\":[{\"Width\":\"3.3487451860049338\",\"Kind\":\"wall\"},{\"Width\":\"3.7222464849807415\",\"Kind\":\"wall\"}]},{\"Width\":\"9.270899814847713\",\"Parts\":[{\"Width\":\"6.71333455378942\",\"Kind\":\"wall\"},{\"Width\":\"2.5575652610582926\",\"Kind\":\"wall\"}]},{\"Width\":\"7.070988932533942\",\"Parts\":[{\"Width\":\"2.6020354393769076\",\"Kind\":\"wall\"},{\"Width\":\"4.468953493157035\",\"Kind\":\"insulation\"}]}]}],\"Height\":\"2.7\"}]";
            string geoText = "MULTIPOLYGON(((38.3773891 49.0000514,38.3774894 49.0000002,38.3775489 49.0000504,38.3774486 49.0001016,38.3773891 49.0000514)))";
            var geometry = new WKTReader().Read(geoText);
            var descriptions = JsonSerializer.Deserialize<IList<BuildingModel>>(desc, new JsonSerializerOptions()
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            })!;
            var entity = new BuildingEntity(0, "house", null, null, null, 1, null, null, null, null, desc, geometry);

            var options = new ConvertTo3dModelAgentSettings();

            // 26918 copy-pasted from the nyc dataset, no thoughts given.
            return service.ProcessEntity(
                entity,
                options,
                (await provider.GetService<IPlanetoidService>()!.GetPlanetoid(3, token)).Data,
                descriptions,
                (await Task.WhenAll((geometry as MultiPolygon)!.OfType<Polygon>().Select(async x => await geometryConversionService.ToAssimpVectors(
                    x.Shell.Coordinates, planetoid, options.YUp, token, null, null)))).ToList(),
                (await geometryConversionService.ToAssimpVectors(new Coordinate[]
                {
                    new Coordinate(38.3773891, 49.0000514),
                }, planetoid, options.YUp, token, null, null))[0],
                15
            );
        }
    }
}
