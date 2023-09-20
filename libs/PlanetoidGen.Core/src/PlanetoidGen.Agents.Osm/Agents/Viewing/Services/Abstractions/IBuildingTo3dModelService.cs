using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Domain.Models.Descriptions.Building;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions
{
    public interface IBuildingTo3dModelService
    {
        int GetSupportedLODCount(BuildingModel description);

        Scene ProcessEntity(
            BuildingEntity entity,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            IList<BuildingModel> descriptions,
            IList<Vector3D[]> outerRings,
            Vector3D pivot,
            int z);

        void ProcessEntity(
            BuildingEntity entity,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            IList<BuildingModel> descriptions,
            IList<Vector3D[]> outerRings,
            Vector3D pivot,
            Scene scene,
            Node parent,
            int z);
    }
}
