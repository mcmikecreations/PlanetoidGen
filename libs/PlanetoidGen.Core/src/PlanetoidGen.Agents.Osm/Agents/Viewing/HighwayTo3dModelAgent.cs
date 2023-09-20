using Assimp;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing
{
    public class HighwayTo3dModelAgent : BaseEntityTo3dModelAgent<HighwayTo3dModelAgent, HighwayEntity>
    {
        private IHighwayTo3dModelService? _highwayTo3DModelService;

        protected override string Prefix => "Highway";

        public override string Title => $"{nameof(PlanetoidGen)}.{nameof(Osm)}.{nameof(HighwayTo3dModelAgent)}";

        public override string Description => string.Empty;

        protected override async ValueTask InitializeServices(ConvertTo3dModelAgentSettings settings, IServiceProvider serviceProvider)
        {
            await base.InitializeServices(settings, serviceProvider);

            _highwayTo3DModelService = new HighwayTo3dModelService();
        }

        protected override TableSchema GetSchema(int planetoidId, string? schema, string? tableName, int? srid = null)
        {
            return HighwayEntity.GetSchema(
                schema ?? "dyn",
                tableName ?? $"{nameof(HighwayEntity)}Collection_{planetoidId}");
        }

        protected override async Task ProcessEntity(
            GenerationJobMessage job,
            PlanetoidInfoModel planetoid,
            HighwayEntity entity,
            TableSchema tableSchema,
            Vector3D pivot,
            Scene scene,
            Node parent,
            CancellationToken token)
        {
            int? srcProjection = entity.Geom.SRID == 0 ? (int?)null : entity.Geom.SRID, dstProjection = null;

            try
            {
                _highwayTo3DModelService!.ProcessEntity(
                    entity,
                    _settings!,
                    planetoid,
                    await _assimpGeometryConversionService!.ToAssimpVectors(AssembleCoordinates(entity), planetoid, _settings!.YUp, token, srcProjection, dstProjection),
                    pivot,
                    scene,
                    parent,
                    job.Z);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "Failed to process highway entity {Entity}.", entity.GID);
            }
        }

        private IEnumerable<Coordinate[]> AssembleCoordinates(HighwayEntity entity)
        {
            return entity.Path.OfType<LineString>().Select(x => x.Coordinates);
        }
    }
}
