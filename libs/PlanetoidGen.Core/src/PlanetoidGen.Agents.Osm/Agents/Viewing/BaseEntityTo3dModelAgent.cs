using Assimp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Agents.Standard.Constants.StringMessages;
using PlanetoidGen.BusinessLogic.Common.Helpers;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Factories.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Documents;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Repositories.Generation;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Documents;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Documents;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static PlanetoidGen.Contracts.Constants.FileContentConstants;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing
{
    public abstract class BaseEntityTo3dModelAgent<TAgent, TEntity> : ITypedAgent<ConvertTo3dModelAgentSettings>
        where TEntity : BaseEntity
        where TAgent : ITypedAgent<ConvertTo3dModelAgentSettings>
    {
        const string ExportFormat = "obj";

        protected virtual string Prefix => "Base";

        protected bool _initialized = false;
        protected ConvertTo3dModelAgentSettings? _settings;

        protected IFileContentService? _fileContentService;
        protected IGeometryConversionService? _geometryConversionService;
        protected IAssimpGeometryConversionService? _assimpGeometryConversionService;
        protected ISpatialReferenceSystemRepository? _srsRepository;
        protected IPlanetoidService? _planetoidService;
        protected ICoordinateMappingService? _coordinateMappingService;
        protected IMetaDynamicRepository? _metaDynamicRepository;
        protected IGeometricDynamicRepositoryFactory<TEntity>? _entityRepositoryFactory;
        protected ILogger? _logger;

        protected SpatialReferenceSystemModel? _srcSRS;
        protected SpatialReferenceSystemModel? _dstSRS;

        public bool IsVisibleToClient => true;

        public abstract string Title { get; }

        public abstract string Description { get; }

        public async ValueTask<Result> Execute(GenerationJobMessage job, CancellationToken token)
        {
            if (!_initialized)
            {
                _logger!.LogError("Failed to execute {JobId}, because {Agent} was not initialized.", job.Id, Title);
                return Result.CreateFailure(GeneralStringMessages.ObjectNotInitialized, Title);
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

            var entityTableSchema = GetSchema(planetoid.Id, _settings!.EntityTableSchema, _settings!.EntityTableName, _settings!.SourceProjection);

            var repoResult = _entityRepositoryFactory!.CreateGeometricRepository(entityTableSchema);

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
            var pivot = GetPivot(coordinatesSpherical, _srcSRS!, _dstSRS!, planetoid);
            var context = new AssimpContext();

            if (_settings!.MergeModels)
            {
                var scene = new Scene();
                var rootNode = new Node("RootNode");
                scene.RootNode = rootNode;

                for (var i = 0; i < entities.Count; ++i)
                {
                    await ProcessEntity(job, planetoid, entities[i], entityTableSchema, pivot, scene, rootNode, token);
                }

                var export = await SaveModel(job, context, scene, _settings!.DestinationProjection, Prefix, token);

                if (!export.Success)
                {
                    _logger!.LogError(
                        "Failed to export planetoid {0} z {1} x {2} y {3}: {4}",
                        job.PlanetoidId,
                        job.Z,
                        job.X,
                        job.Y,
                        export.ErrorMessage!.ToString());
                    return Result.CreateFailure("Failed to export planetoid {0} z {1} x {2} y {3}: {4}",
                        job.PlanetoidId.ToString(),
                        job.Z.ToString(),
                        job.X.ToString(),
                        job.Y.ToString(),
                        export.ErrorMessage!.ToString());
                }
            }
            else
            {
                var exportErrors = new List<Result>();
                
                for (var i = 0; i < entities.Count; ++i)
                {
                    var scene = new Scene();
                    var rootNode = new Node("RootNode");
                    scene.RootNode = rootNode;

                    var entity = entities[i];

                    await ProcessEntity(job, planetoid, entity, entityTableSchema, pivot, scene, rootNode, token);

                    var fileName = $"{Prefix}_{entity.GID}";

                    var export = await SaveModel(job, context, scene, _settings!.DestinationProjection, fileName, token);

                    if (!export.Success)
                    {
                        _logger!.LogError(
                            "Failed to export entity {0} planetoid {1} z {2} x {3} y {4}: {5}",
                            entity.GID,
                            job.PlanetoidId,
                            job.Z,
                            job.X,
                            job.Y,
                            export.ErrorMessage!.ToString());
                        exportErrors.Add(export);
                    }
                }
                
                if (exportErrors.Any())
                {
                    return Result.CreateFailure(string.Join('\n', exportErrors.Select(x => x.ErrorMessage!.ToString())));
                }
            }

            return Result.CreateSuccess();
        }

        public ConvertTo3dModelAgentSettings GetTypedDefaultSettings()
        {
            return new ConvertTo3dModelAgentSettings();
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

                await InitializeServices(_settings, serviceProvider);

                _initialized = true;
            }
            catch (Exception ex)
            {
                _initialized = false;
                return Result.CreateFailure(ex);
            }

            return Result.CreateSuccess();
        }

        protected abstract TableSchema GetSchema(int planetoidId, string? schema, string? tableName, int? srid = null);

        protected abstract Task ProcessEntity(
            GenerationJobMessage job,
            PlanetoidInfoModel planetoid,
            TEntity entity,
            TableSchema tableSchema,
            Vector3D pivot,
            Scene scene,
            Node parent,
            CancellationToken token);

        protected virtual async ValueTask InitializeServices(ConvertTo3dModelAgentSettings settings, IServiceProvider serviceProvider)
        {
            _geometryConversionService = serviceProvider.GetService<IGeometryConversionService>()
                ?? throw new ArgumentNullException(nameof(IGeometryConversionService));

            _assimpGeometryConversionService = new AssimpGeometryConversionService(_geometryConversionService!);

            _planetoidService = serviceProvider.GetService<IPlanetoidService>()
                ?? throw new ArgumentNullException(nameof(IPlanetoidService));

            _coordinateMappingService = serviceProvider.GetService<ICoordinateMappingService>()
                ?? throw new ArgumentNullException(nameof(ICoordinateMappingService));

            _metaDynamicRepository = serviceProvider.GetService<IMetaDynamicRepository>()
                ?? throw new ArgumentNullException(nameof(IMetaDynamicRepository));

            _entityRepositoryFactory = serviceProvider.GetService<IGeometricDynamicRepositoryFactory<TEntity>>()
                ?? throw new ArgumentNullException($"{nameof(IDynamicRepositoryFactory<TEntity>)}<{typeof(TEntity).Name}>");

            _fileContentService = serviceProvider.GetRequiredService<IFileContentService>()
                ?? throw new ArgumentNullException(nameof(IFileContentService));

            _logger = serviceProvider.GetService<ILogger<TAgent>>()
                ?? throw new ArgumentNullException($"{nameof(ILogger<TAgent>)}<{typeof(TAgent).Name}>");

            _srsRepository = serviceProvider.GetService<ISpatialReferenceSystemRepository>()
                ?? throw new ArgumentNullException(nameof(ISpatialReferenceSystemRepository));

            var token = CancellationToken.None;

            var srcSrsResult = await _srsRepository!.GetSRS(_settings!.SourceProjection, token);

            if (!srcSrsResult.Success)
            {
                throw new ArgumentException(srcSrsResult.ErrorMessage!.ToString(), nameof(_settings.SourceProjection));
            }

            _srcSRS = srcSrsResult.Data;

            var dstSrsResult = await _srsRepository!.GetSRS(_settings!.DestinationProjection, token);

            if (!dstSrsResult.Success)
            {
                throw new ArgumentException(dstSrsResult.ErrorMessage!.ToString(), nameof(_settings.DestinationProjection));
            }

            _dstSRS = dstSrsResult.Data;
        }

        private async ValueTask<Result> SaveModel(GenerationJobMessage job, AssimpContext context, Scene scene, int sridDst, string fileName, CancellationToken token)
        {
            var materialFiles = scene.Materials
                .SelectMany(x => x.GetAllMaterialTextures())
                .Select(x => x.FilePath)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct();

            var modelFileEnumerator = context.ExportToBlob(
                scene,
                ExportFormat,
                PostProcessSteps.Triangulate
                | PostProcessSteps.GenerateNormals
                | PostProcessSteps.FindInvalidData);

            if (modelFileEnumerator == null)
            {
                string message = string.Format("Failed to create blob for the scene, planetoid={0}, z={1}, x={2},  y={3}", job.PlanetoidId, job.Z, job.X, job.Y);
                _logger!.LogError(message);
                return Result.CreateFailure(GeneralStringMessages.InternalError, message);
            }

            var materialDependencyIds = new List<string>();
            var materialDependencies = new List<FileModel>();

            var materialDependenciesCheck = materialFiles.Select(async (path) =>
            {
                var fileExists = await _fileContentService!.FileIdExists(path, token);

                if (!fileExists.Success)
                {
                    return Result.Convert(fileExists);
                }
                else if (fileExists.Data)
                {
                    materialDependencyIds.Add(path);

                    return Result.CreateSuccess();
                }
                else if (!File.Exists(path))
                {
                    return Result.CreateFailure(GeneralStringMessages.ObjectNotExist, path);
                }
                else
                {
                    materialDependencyIds.Add(path);

                    var fileContent = await File.ReadAllBytesAsync(path, token);

                    materialDependencies.Add(new FileModel()
                    {
                        FileId = path,
                        Content = new FileContentModel()
                        {
                            Id = path,
                            FileName = Path.GetFileName(path),
                            LocalPath = Path.GetDirectoryName(path),
                            Content = fileContent,
                        },
                    });

                    return Result.CreateSuccess();
                }
            });
            var checkResult = await Task.WhenAll(materialDependenciesCheck);
            var failedCheckResult = checkResult.FirstOrDefault(x => !x.Success);
            if (failedCheckResult != null)
            {
                return failedCheckResult;
            }

            var files = new List<FileModel>();

            while (modelFileEnumerator != null)
            {
                var isNameSpecified = string.IsNullOrWhiteSpace(modelFileEnumerator.Name);
                var fileExtension = isNameSpecified ? ExportFormat : modelFileEnumerator.Name;

                var dataType = fileExtension switch
                {
                    "obj" => DataTypes.Obj3dModel,
                    "mtl" => DataTypes.ObjMaterial,
                    _ => DataTypes.Dummy,
                };
                var fileId = FileModelFormatter.FormatFileId(
                    job.PlanetoidId,
                    $"{dataType}.{fileName}",
                    job.Z,
                    job.X,
                    job.Y);
                files.Add(new FileModel()
                {
                    FileId = fileId,
                    TileBasedFileInfo = new TileBasedFileInfoModel(
                        fileId,
                        job.PlanetoidId,
                        job.Z,
                        job.X,
                        job.Y),
                    Content = new FileContentModel()
                    {
                        Id = fileId,
                        FileName = $"{job.Y}_{fileName}.{fileExtension}",
                        LocalPath = FileModelFormatter.FormatLocalPath(
                            job.PlanetoidId,
                            "Models",
                            job.Z,
                            job.X),
                        Content = modelFileEnumerator.Data,
                        Attributes = new Dictionary<string, string>
                        {
                            { TileMapAttributes.Srid, sridDst.ToString() },
                            { CommonAttributes.ContentType, "model/" + fileExtension },
                        },
                    },
                });

                modelFileEnumerator = modelFileEnumerator.NextBlob;
            }

            var mainModelFile = files.FirstOrDefault();

            if (mainModelFile == null)
            {
                return Result.CreateFailure(GeneralStringMessages.ObjectNotExist, "Main model file was empty.");
            }

            var modelDependencies = files.Skip(1);

            var dependencies = new List<FileDependencyModel>();

            dependencies.AddRange(materialDependencyIds
                .Select(x => new FileDependencyModel(mainModelFile.FileId, x, true, false)));

            dependencies.AddRange(modelDependencies
                .Select(x => new FileDependencyModel(mainModelFile.FileId, x.FileId, true, true)));

            mainModelFile.DependentFiles = dependencies;

            var addFileTasks = materialDependencies.Select(async (x) => await _fileContentService!.SaveFileContentWithDependencies(x, token));
            var addResults = await Task.WhenAll(addFileTasks);

            var failedResult = addResults.FirstOrDefault(x => !x.Success);
            if (failedResult != null)
            {
                return Result.Convert(failedResult);
            }

            addFileTasks = modelDependencies.Select(async (x) => await _fileContentService!.SaveFileContentWithDependencies(x, token));
            addResults = await Task.WhenAll(addFileTasks);

            failedResult = addResults.FirstOrDefault(x => !x.Success);
            if (failedResult != null)
            {
                return Result.Convert(failedResult);
            }

            var addMainResult = await _fileContentService!.SaveFileContentWithDependencies(mainModelFile, token);
            return Result.Convert(addMainResult);
        }

        private Vector3D GetPivot(
            SphericalCoordinateModel sphericalCoordinates,
            SpatialReferenceSystemModel srcProjection,
            SpatialReferenceSystemModel dstProjection,
            PlanetoidInfoModel planetoid)
        {
            var pivots = _assimpGeometryConversionService!.ToAssimpVectors(new Coordinate[]
            {
                new Coordinate(
                    sphericalCoordinates.Longtitude * 180.0 / Math.PI,
                    sphericalCoordinates.Latitude * 180.0 / Math.PI),
            }, planetoid, _settings!.YUp, srcProjection, dstProjection);

            return pivots[0];
        }
    }
}
