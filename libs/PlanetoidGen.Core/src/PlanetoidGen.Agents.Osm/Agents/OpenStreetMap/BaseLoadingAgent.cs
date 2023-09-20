using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Dtos;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Services.Implementations;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Factories.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Models.Services.GeoInfo;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Osm.Agents.OpenStreetMap
{
    public abstract class BaseLoadingAgent<TAgent, TEntity> : ITypedAgent<OpenStreetMapLoadingAgentSettings>
        where TEntity : BaseEntity
        where TAgent : ITypedAgent<OpenStreetMapLoadingAgentSettings>
    {
        protected bool _initialized = false;
        protected OpenStreetMapLoadingAgentSettings? _settings;

        protected IOverpassApiService? _osmApi;
        protected ICoordinateMappingService? _coordinateMappingService;
        protected IMetaDynamicRepository? _metaDynamicRepository;
        protected IDynamicRepositoryFactory<TEntity>? _entityRepositoryFactory;
        protected ILogger? _logger;

        public abstract string Title { get; }

        public abstract string Description { get; }

        public bool IsVisibleToClient => true;

        public async ValueTask<Result> Execute(GenerationJobMessage job, CancellationToken token)
        {
            if (!_initialized)
            {
                _logger!.LogError("Failed to execute {JobId}, because {Agent} was not initialized.", job.Id, Title);
                return Result.CreateFailure(GeneralStringMessages.ObjectNotInitialized, Title);
            }

            var coordinatesSpherical = _coordinateMappingService!.ToSpherical(new PlanarCoordinateModel(job.PlanetoidId, job.Z, job.X, job.Y));

            var bbox = _osmApi!.GetBoundingBox(_coordinateMappingService!.ToAxisAlignedBoundingBox(coordinatesSpherical));

            var entityResponse = await GetOverpassEntities(bbox, token);

            if (!entityResponse.Success)
            {
                _logger!.LogError("Failed to execute {Method} from OSM API for bbox {Bbox} in {Agent}. {Error}", nameof(GetOverpassEntities), bbox, Title, entityResponse.ErrorMessage!);
                return Result.CreateFailure(IOStringMessages.RequestFailed, entityResponse.ErrorMessage!.ToString());
            }

            const int srid = IOverpassApiService.WGS84Srid;

            var entities = ToEntityList(entityResponse.Data!, srid);

            if (!entities.Any())
            {
                _logger!.LogDebug("{Method} from OSM API for bbox {Bbox} in {Agent} returned no entities.", nameof(GetOverpassEntities), bbox, Title);
                return Result.CreateSuccess();
            }

            var entityTableSchema = GetSchema(job.PlanetoidId, _settings!.EntityTableSchema, _settings!.EntityTableName, srid);

            var repoResult = _entityRepositoryFactory!.CreateRepository(entityTableSchema);

            if (!repoResult.Success)
            {
                _logger!.LogError("Failed to create a repository for dynamic table {Schema}.{Title}. {Error}", entityTableSchema.Schema, entityTableSchema.Title, repoResult.ErrorMessage!);
                return Result.CreateFailure(repoResult.ErrorMessage!);
            }

            var repo = repoResult.Data!;

            var createTableResult = await repo.TableCreateIfNotExists(token);

            if (!createTableResult.Success)
            {
                _logger!.LogError("Failed to create or initialize dynamic SQL table for {Schema}.{Title}. {Error}", entityTableSchema.Schema, entityTableSchema.Title, createTableResult.ErrorMessage!);
                return createTableResult;
            }

            var metaModel = _metaDynamicRepository!.GetMetaDynamicModel(entityTableSchema, job.PlanetoidId);

            var metaCreateResult = await _metaDynamicRepository!.InsertDynamic(metaModel, token);

            if (!metaCreateResult.Success)
            {
                _logger!.LogError("Failed to create or get dynamic table meta entry for collection {Schema}.{Title}. {Error}", metaModel.Schema, metaModel.Title, metaCreateResult.ErrorMessage!);
                return Result.CreateFailure(metaCreateResult.ErrorMessage!);
            }

            _logger!.LogDebug("Using dynamic table {Id} {Title}.", metaCreateResult.Data, metaModel.Title);

            var createResult = await repo.CreateMultiple(entities, true, token);

            if (!createResult.Success)
            {
                _logger!.LogError("Failed to create entities in dynamic table for collection {Schema}.{Title} using bbox {Bbox}. {Error}", metaModel.Schema, metaModel.Title, bbox, createResult.ErrorMessage!);
                return Result.CreateFailure(createResult.ErrorMessage!);
            }
            else if (createResult.Data!.Count() != entities.Count())
            {
                _logger!.LogError("Not all entities created in dynamic table for collection {Schema}.{Title}.", metaModel.Schema, metaModel.Title);
                return Result.CreateFailure(
                    GeneralStringMessages.DatabaseProcedureError,
                    new ArgumentOutOfRangeException(nameof(entities), "Not all entities written to database."));
            }
            else if (createResult.Data!.Any(x => x == null))
            {
                _logger!.LogDebug("Possible duplicate in dynamic table for collection {Schema}.{Title}.", metaModel.Schema, metaModel.Title);
            }

            _logger!.LogDebug("Wrote {Num} entities to {Table} for agent {Agent} job {Job}.", createResult.Data!.Count(), metaModel.Title, Title, job);

            if (_settings!.PushToGeoServer)
            {
                throw new NotImplementedException("Pushing to GeoServer not yet supported.");
            }

            return Result.CreateSuccess();
        }

        public OpenStreetMapLoadingAgentSettings GetTypedDefaultSettings()
        {
            return new OpenStreetMapLoadingAgentSettings();
        }

        public ValueTask<string> GetDefaultSettings()
        {
            return GetTypedDefaultSettings().Serialize();
        }

        public ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies(int z)
        {
            return GetDependencies();
        }

        public ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies()
        {
            return new ValueTask<IEnumerable<AgentDependencyModel>>(Array.Empty<AgentDependencyModel>());
        }

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs(int z)
        {
            return GetOutputs();
        }

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs()
        {
            return new ValueTask<IEnumerable<DataTypeInfoModel>>(Array.Empty<DataTypeInfoModel>());
        }

        public async ValueTask<Result> Initialize(string settings, IServiceProvider serviceProvider)
        {
            try
            {
                _initialized = false;

                var deserializationResult = await GetTypedDefaultSettings().Deserialize(settings);

                if (!deserializationResult.Success)
                {
                    return Result.CreateFailure(deserializationResult);
                }

                _settings = deserializationResult.Data;

                _coordinateMappingService = serviceProvider.GetService<ICoordinateMappingService>()
                    ?? throw new ArgumentNullException(nameof(ICoordinateMappingService));

                _metaDynamicRepository = serviceProvider.GetService<IMetaDynamicRepository>()
                    ?? throw new ArgumentNullException(nameof(IMetaDynamicRepository));

                _entityRepositoryFactory = serviceProvider.GetService<IGeometricDynamicRepositoryFactory<TEntity>>()
                    ?? throw new ArgumentNullException($"{nameof(IDynamicRepositoryFactory<TEntity>)}<{typeof(TEntity).Name}>");

                _logger = serviceProvider.GetService<ILogger<TAgent>>()
                    ?? throw new ArgumentNullException($"{nameof(ILogger<TAgent>)}<{typeof(TAgent).Name}>");

                var overpassLogger = serviceProvider.GetService<ILogger<OverpassApiService>>()
                    ?? throw new ArgumentNullException($"{nameof(ILogger<OverpassApiService>)}<{nameof(OverpassApiService)}>");
                var geoInfoOptions = serviceProvider.GetService<IOptions<GeoInfoServiceOptions>>()?.Value
                    ?? throw new ArgumentNullException($"{nameof(IOptions<GeoInfoServiceOptions>)}<{nameof(GeoInfoServiceOptions)}>");

                var osmOptions = new GeoInfoServiceOptions()
                {
                    OverpassConnectionString = _settings.OverpassBaseUrl ?? geoInfoOptions.OverpassConnectionString ?? string.Empty,
                    AvailableMinSrid = geoInfoOptions.AvailableMinSrid,
                    AvailableMaxSrid = geoInfoOptions.AvailableMaxSrid,
                    TransformGeodeticToGeocentric = geoInfoOptions.TransformGeodeticToGeocentric,
                };
                _osmApi = new OverpassApiService(osmOptions, overpassLogger);

                _initialized = true;
            }
            catch (Exception ex)
            {
                _initialized = false;
                return Result.CreateFailure(ex);
            }

            return Result.CreateSuccess();
        }

        protected abstract ValueTask<Result<OverpassResponseDto>> GetOverpassEntities(BoundingBoxDto bbox, CancellationToken token);

        protected abstract IReadOnlyList<TEntity> ToEntityList(OverpassResponseDto response, int srid);

        protected abstract TableSchema GetSchema(int planetoidId, string? schema, string? tableName, int? srid = null);
    }
}
