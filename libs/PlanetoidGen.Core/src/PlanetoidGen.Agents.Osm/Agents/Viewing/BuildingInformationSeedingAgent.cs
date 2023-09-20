using Litdex.Random.PRNG;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations.Seeders;
using PlanetoidGen.Agents.Osm.Constants.KindValues;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Factories.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing
{
    public class BuildingInformationSeedingAgent : ITypedAgent<BuildingInformationSeedingAgentSettings>
    {
        private bool _initialized = false;
        private BuildingInformationSeedingAgentSettings? _settings;
        private Dictionary<string, IBuildingSeeder>? _seeders;

        private IPlanetoidService? _planetoidService;
        private ICoordinateMappingService? _coordinateMappingService;
        private IMetaDynamicRepository? _metaDynamicRepository;
        private IGeometricDynamicRepositoryFactory<BuildingEntity>? _buildingRepositoryFactory;
        private ILogger? _logger;

        public string Title => $"{nameof(PlanetoidGen)}.{nameof(Osm)}.{nameof(BuildingInformationSeedingAgent)}";

        public string Description => string.Empty;

        public bool IsVisibleToClient => true;

        public async ValueTask<Result> Execute(GenerationJobMessage job, CancellationToken token)
        {
            if (!_initialized)
            {
                _logger!.LogError("Failed to execute {JobId}, because {Agent} was not initialized.", job.Id, Title);
                return Result.CreateFailure(GeneralStringMessages.ObjectNotInitialized);
            }

            var planetoidResult = await _planetoidService!.GetPlanetoid(job.PlanetoidId, token);
            if (!planetoidResult.Success)
            {
                _logger!.LogError("Failed to obtain planetoid info in {Agent}. {Error}", Title, planetoidResult.ErrorMessage!);
                return Result.CreateFailure(planetoidResult.ErrorMessage!);
            }

            var planetoid = planetoidResult.Data!;

            var coordinatesSpherical = _coordinateMappingService!.ToSpherical(new PlanarCoordinateModel(job.PlanetoidId, job.Z, job.X, job.Y));

            var bbox = _coordinateMappingService!.ToBoundingBox(coordinatesSpherical);

            const int srid = IOverpassApiService.WGS84Srid;

            var buildingTableSchema = BuildingEntity.GetSchema(
                _settings!.BuildingTableSchema ?? "dyn",
                _settings!.BuildingTableName ?? $"{nameof(BuildingEntity)}Collection_{coordinatesSpherical.PlanetoidId}",
                srid);

            var repoResult = _buildingRepositoryFactory!.CreateGeometricRepository(buildingTableSchema);

            if (!repoResult.Success)
            {
                _logger!.LogError("Failed to create a repository for dynamic table {Schema}.{Title}. {Error}", buildingTableSchema.Schema, buildingTableSchema.Title, repoResult.ErrorMessage!);
                return Result.CreateFailure(repoResult.ErrorMessage!);
            }

            var repo = repoResult.Data!;

            var createTableResult = await repo.TableCreateIfNotExists(token);

            if (!createTableResult.Success)
            {
                _logger!.LogError("Failed to create or initialize dynamic SQL table for {Schema}.{Title}. {Error}", buildingTableSchema.Schema, buildingTableSchema.Title, createTableResult.ErrorMessage!);
                return createTableResult;
            }

            var metaModel = _metaDynamicRepository!.GetMetaDynamicModel(buildingTableSchema, job.PlanetoidId);

            var metaCreateResult = await _metaDynamicRepository!.InsertDynamic(metaModel, token);

            if (!metaCreateResult.Success)
            {
                _logger!.LogError("Failed to create or get dynamic table meta entry for collection {Schema}.{Title}. {Error}", metaModel.Schema, metaModel.Title, metaCreateResult.ErrorMessage!);
                return Result.CreateFailure(metaCreateResult.ErrorMessage!);
            }

            _logger!.LogDebug("Created or got dynamic table meta entry {id}.", metaCreateResult.Data!);

            var readResult = await repo.ReadMultipleByBoundingBox(bbox, token);

            if (!readResult.Success)
            {
                _logger!.LogError("Failed to read dynamic table for collection {Schema}.{Title} using bbox {Bbox}. {Error}", metaModel.Schema, metaModel.Title, bbox, readResult.ErrorMessage!);
                return Result.CreateFailure(readResult.ErrorMessage!);
            }
            else if (!readResult.Data.Any())
            {
                _logger!.LogDebug("No data read for dynamic table for collection {Schema}.{Title} using bbox {Bbox}.", metaModel.Schema, metaModel.Title, bbox);
                return Result.CreateSuccess();
            }

            _logger!.LogDebug("Read {Num} entities from {Table} for agent {Agent} job {Job} using bbox {Bbox}.", readResult.Data!.Count(), metaModel.Title, Title, job, bbox);

            var entities = readResult.Data!.ToList();
            var unsupportedEntityTypes = new HashSet<string>();
            var unsupportedEntityCount = 0;

            for (var i = 0; i < entities.Count; ++i)
            {
                entities[i] = ProcessEntity(planetoid, entities[i], unsupportedEntityTypes, ref unsupportedEntityCount);
            }

            if (unsupportedEntityTypes.Count > 0)
            {
                _logger!.LogDebug("Building type {Types} are not supported for {Count} entities in {Agent}. Replaced with some of {NewKeys}.",
                    string.Join(", ", unsupportedEntityTypes),
                    unsupportedEntityCount,
                    Title,
                    string.Join(", ", _seeders!.Keys));
            }

            var updateResult = await repo.UpdateMultiple(entities, token);
            if (!updateResult.Success)
            {
                _logger!.LogError("Failed to update dynamic table for collection {Schema}.{Title}. {Error}", metaModel.Schema, metaModel.Title, updateResult.ErrorMessage!);
                return Result.CreateFailure(updateResult.ErrorMessage!);
            }
            else if (updateResult.Data.Count() != entities.Count())
            {
                _logger!.LogError("Not all entities updated in dynamic table for collection {Schema}.{Title}.", metaModel.Schema, metaModel.Title);
                return Result.CreateFailure(
                    GeneralStringMessages.DatabaseProcedureError,
                    new ArgumentOutOfRangeException(nameof(entities), "Not all entities written to database."));
            }

            return Result.CreateSuccess();
        }

        public BuildingInformationSeedingAgentSettings GetTypedDefaultSettings()
        {
            return new BuildingInformationSeedingAgentSettings();
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

                _planetoidService = serviceProvider.GetService<IPlanetoidService>()
                    ?? throw new ArgumentNullException(nameof(IPlanetoidService));

                _coordinateMappingService = serviceProvider.GetService<ICoordinateMappingService>()
                    ?? throw new ArgumentNullException(nameof(ICoordinateMappingService));

                _metaDynamicRepository = serviceProvider.GetService<IMetaDynamicRepository>()
                    ?? throw new ArgumentNullException(nameof(IMetaDynamicRepository));

                _buildingRepositoryFactory = serviceProvider.GetService<IGeometricDynamicRepositoryFactory<BuildingEntity>>()
                    ?? throw new ArgumentNullException($"{nameof(IDynamicRepositoryFactory<BuildingEntity>)}<{nameof(BuildingEntity)}>");

                _logger = serviceProvider.GetService<ILogger<BuildingInformationSeedingAgent>>()
                    ?? throw new ArgumentNullException($"{nameof(ILogger<BuildingInformationSeedingAgent>)}<{nameof(BuildingInformationSeedingAgent)}>");

                var serializerOptions = _settings.GetJsonSerializerOptions();

                _seeders = new Dictionary<string, IBuildingSeeder>()
                {
                    { BuildingKindValues.BuildingHouse, new HouseSeeder(_logger, serializerOptions, Title) },
                    { BuildingKindValues.BuildingIndustrial, new IndustrialSeeder(_logger, serializerOptions, Title) },
                    { BuildingKindValues.BuildingApartments, new ApartmentsSeeder(_logger, serializerOptions, Title) },
                };

                _initialized = true;
            }
            catch (Exception ex)
            {
                _initialized = false;
                return Result.CreateFailure(ex);
            }

            return Result.CreateSuccess();
        }

        #region Process Building Kinds

        private BuildingEntity ProcessEntity(PlanetoidInfoModel planetoid, BuildingEntity entity, ISet<string> unsupportedEntityTypes, ref int unsupportedEntityCount)
        {
            var random = new Seiran(unchecked((ulong)(planetoid.Seed ^ _settings!.Seed)), unchecked((ulong)entity.GID));
            var seeders = _seeders!;

            var seeder = seeders.ContainsKey(entity.Kind ?? string.Empty)
                ? seeders[entity.Kind ?? string.Empty]
                : null;

            if (seeder == null)
            {
                var keyCount = seeders.Count;
                var newKindIndex = random.NextInt(0, keyCount);
                var newKey = seeders.Keys.ElementAt(newKindIndex);
                seeder = seeders[newKey];

                unsupportedEntityTypes.Add(entity.Kind ?? "null");
                ++unsupportedEntityCount;
            }

            return seeder.ProcessBuilding(_settings!, planetoid, entity, random);
        }

        #endregion
    }
}
