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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing
{
    public class BuildingTo3dModelAgent : BaseEntityTo3dModelAgent<BuildingTo3dModelAgent, BuildingEntity>
    {
        protected IBuildingTo3dModelService? _buildingTo3DModelService;

        protected override string Prefix => "Building";

        public override string Title => $"{nameof(PlanetoidGen)}.{nameof(Osm)}.{nameof(BuildingTo3dModelAgent)}";

        public override string Description => string.Empty;

        protected override async ValueTask InitializeServices(ConvertTo3dModelAgentSettings settings, IServiceProvider serviceProvider)
        {
            await base.InitializeServices(settings, serviceProvider);

            _buildingTo3DModelService = new BuildingTo3dModelService();
        }

        protected override TableSchema GetSchema(int planetoidId, string? schema, string? tableName, int? srid = null)
        {
            return BuildingEntity.GetSchema(
                schema ?? "dyn",
                tableName ?? $"{nameof(BuildingEntity)}Collection_{planetoidId}");
        }

        protected override async Task ProcessEntity(
            GenerationJobMessage job,
            PlanetoidInfoModel planetoid,
            BuildingEntity entity,
            TableSchema tableSchema,
            Vector3D pivot,
            Scene scene,
            Node parent,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(entity.DetailedDescription))
            {
                _logger!.LogDebug("No detailed description for building {Id} in {Schema}.{Title}.", entity.GID, tableSchema.Schema, tableSchema.Title);
                return;
            }

            var descriptions = JsonSerializer.Deserialize<IList<Domain.Models.Descriptions.Building.BuildingModel>>(entity.DetailedDescription!, _settings!.GetJsonSerializerOptions())!;

            if (descriptions.Any(x => x.LevelCollection == null || x.LevelCollection.Count == 0))
            {
                _logger!.LogDebug("Detailed description for building {Id} in {Schema}.{Title} missing levels.", entity.GID, tableSchema.Schema, tableSchema.Title);
                return;
            }

            // TODO: get WKT projection id
            int? srcProjection = null, dstProjection = null;

            try
            {
                _buildingTo3DModelService!.ProcessEntity(
                    entity,
                    _settings!,
                    planetoid,
                    descriptions,
                    await _assimpGeometryConversionService!.ToAssimpVectors(AssembleCoordinates(entity), planetoid, _settings!.YUp, token, srcProjection, dstProjection),
                    pivot,
                    scene,
                    parent,
                    job.Z);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "Failed to process building entity {Entity}.", entity.GID);
            }
        }

        private IEnumerable<Coordinate[]> AssembleCoordinates(BuildingEntity entity)
        {
            return entity.Path.OfType<Polygon>().Select(x => x.Shell.Coordinates);
        }
    }
}
