using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Collections;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Domain.Models.Descriptions.Building;
using PlanetoidGen.Domain.Models.Info;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations.Builders.SurfaceParts
{
    internal class Wall3dModelBuilder : ISurfacePartTo3dModelBuilder
    {
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
            /// partRing already contains all necessary vertices and indices,
            /// so only add faces and uvs.

            mesh.Faces.Add(new Face(partRing.Indices.ToArray()));
        }

        public int GetSupportedLODCount()
        {
            return 2;
        }
    }
}
