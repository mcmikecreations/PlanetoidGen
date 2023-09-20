﻿using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Dtos;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Osm.Agents.OpenStreetMap
{
    public class HighwayLoadingAgent : BaseLoadingAgent<HighwayLoadingAgent, HighwayEntity>
    {
        public override string Title => $"{nameof(PlanetoidGen)}.{nameof(Osm)}.{nameof(HighwayLoadingAgent)}";

        public override string Description => string.Empty;

        protected override async ValueTask<Result<OverpassResponseDto>> GetOverpassEntities(BoundingBoxDto bbox, CancellationToken token)
        {
            return await _osmApi!.GetHighways(bbox, token);
        }

        protected override IReadOnlyList<HighwayEntity> ToEntityList(OverpassResponseDto response, int srid)
        {
            return _osmApi!.ToHighwayEntityList(response, srid);
        }

        protected override TableSchema GetSchema(int planetoidId, string? schema, string? tableName, int? srid = null)
        {
            return HighwayEntity.GetSchema(
                schema ?? "dyn",
                tableName ?? $"{nameof(HighwayEntity)}Collection_{planetoidId}",
                srid);
        }
    }
}
