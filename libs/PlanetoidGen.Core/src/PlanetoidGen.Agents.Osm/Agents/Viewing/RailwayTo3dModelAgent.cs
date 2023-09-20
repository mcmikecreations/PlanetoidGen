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
    public class RailwayTo3dModelAgent : BaseEntityTo3dModelAgent<RailwayTo3dModelAgent, RailwayEntity>
    {
        private IRailwayTo3dModelService? _railwayTo3DModelService;

        protected override string Prefix => "Railway";

        public override string Title => $"{nameof(PlanetoidGen)}.{nameof(Osm)}.{nameof(RailwayTo3dModelAgent)}";

        public override string Description => string.Empty;

        protected override async ValueTask InitializeServices(ConvertTo3dModelAgentSettings settings, IServiceProvider serviceProvider)
        {
            await base.InitializeServices(settings, serviceProvider);

            _railwayTo3DModelService = new RailwayTo3dModelService();
        }

        protected override TableSchema GetSchema(int planetoidId, string? schema, string? tableName, int? srid = null)
        {
            return RailwayEntity.GetSchema(
                schema ?? "dyn",
                tableName ?? $"{nameof(RailwayEntity)}Collection_{planetoidId}");
        }

        protected override async Task ProcessEntity(
            GenerationJobMessage job,
            PlanetoidInfoModel planetoid,
            RailwayEntity entity,
            TableSchema tableSchema,
            Vector3D pivot,
            Scene scene,
            Node parent,
            CancellationToken token)
        {
            int? srcProjection = null, dstProjection = null;

            try
            {
                _railwayTo3DModelService!.ProcessEntity(
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
                _logger!.LogError(ex, "Failed to process railway entity {Entity}.", entity.GID);
            }
        }

        private IEnumerable<Coordinate[]> AssembleCoordinates(RailwayEntity entity)
        {
            return entity.Path.OfType<LineString>().Select(x => x.Coordinates);
        }
    }
}
