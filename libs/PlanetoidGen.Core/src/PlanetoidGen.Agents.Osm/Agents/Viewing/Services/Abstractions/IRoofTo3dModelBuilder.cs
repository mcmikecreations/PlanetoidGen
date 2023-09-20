using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Collections;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Domain.Models.Descriptions.Building;
using PlanetoidGen.Domain.Models.Info;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions
{
    internal interface IRoofTo3dModelBuilder
    {
        void BuildRoof(
            VertexRing bottomRing,
            ref int startIndex,
            float bottomHeight,
            LevelModel topLevel,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            BuildingModel description,
            Scene scene,
            Node parentNode,
            Mesh buildingMesh,
            Vector3D[] outerRing,
            int lod);

        int GetSupportedLODCount();
    }
}
