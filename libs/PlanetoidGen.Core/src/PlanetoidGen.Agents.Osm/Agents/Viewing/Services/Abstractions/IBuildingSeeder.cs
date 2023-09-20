using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Domain.Models.Info;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions
{
    internal interface IBuildingSeeder
    {
        BuildingEntity ProcessBuilding(
            BuildingInformationSeedingAgentSettings options,
            PlanetoidInfoModel planetoid,
            BuildingEntity entity,
            Litdex.Random.Random random);
    }
}
