using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Dtos;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Osm.Agents.OpenStreetMap
{
    public class BuildingLoadingAgent : BaseLoadingAgent<BuildingLoadingAgent, BuildingEntity>
    {
        public override string Title => $"{nameof(PlanetoidGen)}.{nameof(Osm)}.{nameof(BuildingLoadingAgent)}";

        public override string Description => string.Empty;

        protected override async ValueTask<Result<OverpassResponseDto>> GetOverpassEntities(BoundingBoxDto bbox, CancellationToken token)
        {
            return await _osmApi!.GetBuildings(bbox, token);
        }

        protected override IReadOnlyList<BuildingEntity> ToEntityList(OverpassResponseDto response, int srid)
        {
            return _osmApi!.ToBuildingEntityList(response, srid);
        }

        protected override TableSchema GetSchema(int planetoidId, string? schema, string? tableName, int? srid = null)
        {
            return BuildingEntity.GetSchema(
                schema ?? "dyn",
                tableName ?? $"{nameof(BuildingEntity)}Collection_{planetoidId}",
                srid);
        }
    }
}
