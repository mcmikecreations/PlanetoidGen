using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Collections;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Domain.Models.Descriptions.Building;
using PlanetoidGen.Domain.Models.Info;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions
{
    internal interface ISurfacePartTo3dModelBuilder
    {
        void BuildPart(
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
            int lod);

        int GetSupportedLODCount();
    }
}
