using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Collections;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Constants.KindValues;
using PlanetoidGen.Agents.Osm.Helpers;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Domain.Models.Descriptions.Building;
using PlanetoidGen.Domain.Models.Info;
using System.Linq;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations.Builders.SurfaceParts
{
    internal class Porch3dModelBuilder : ISurfacePartTo3dModelBuilder
    {
        private readonly DecorationTo3dConverter _decoratorTo3dConverter;

        public Porch3dModelBuilder(DecorationTo3dConverter decoratorTo3dConverter)
        {
            _decoratorTo3dConverter = decoratorTo3dConverter;
        }

        public void BuildPart(
            BuildingEntity entity,
            VertexRing partRing,
            ref int startIndex,
            float bottomHeight,
            SurfaceSideModel sideModel,
            SurfacePartModel partModel,
            LevelModel level,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            BuildingModel description,
            Scene scene,
            Node buildingNode,
            Mesh mesh,
            Vector3D[] outerRing,
            int lod)
        {
            mesh.Faces.Add(new Face(partRing.Indices.ToArray()));

            var levelIndex = description.LevelCollection.IndexOf(level);
            var sideIndex = level.Sides.IndexOf(sideModel);
            var partIndex = sideModel.Parts.IndexOf(partModel);
            var partTotalIndex = description.LevelCollection.Take(levelIndex).Sum(x => x.Sides.Sum(y => y.Parts.Count)) +
                level.Sides.Take(sideIndex).Sum(x => x.Parts.Count) +
                partIndex;
            var decorationNode = _decoratorTo3dConverter.AddDecoration(
                "porches.obj", PorchKindValues.PickPorchType(description, (int)(entity.GID + partTotalIndex)), scene, buildingNode);

            if (decorationNode == null)
            {
                return;
            }

            var decorationPosition = AssimpHelpers.VerticalPartRingBottomCenter(partRing, options.YUp);
            var up = options.YUp ? new Vector3D(0f, 1f, 0f) : new Vector3D(0f, 0f, 1f);
            var right = partRing.Vertices[1] - partRing.Vertices[0];
            right.Normalize();

            var angle = AssimpHelpers.AngleBetweenVectors(new Vector3D(1f, 0f, 0f), right, up);

            decorationNode.Transform = Matrix4x4.FromAngleAxis(angle, up) * Matrix4x4.FromTranslation(decorationPosition);
        }

        public int GetSupportedLODCount()
        {
            return 2;
        }
    }
}
