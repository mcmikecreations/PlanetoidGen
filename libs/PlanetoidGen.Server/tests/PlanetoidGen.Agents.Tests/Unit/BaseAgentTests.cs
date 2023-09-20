using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.BusinessLogic.Common.Services.Generation;
using PlanetoidGen.Contracts.Factories.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models.Services.GeoInfo;
using PlanetoidGen.Contracts.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Generation;
using PlanetoidGen.Contracts.Repositories.Info;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using PlanetoidGen.Domain.Models.Meta;
using System.Data;
using Xunit.Abstractions;

namespace PlanetoidGen.Agents.Tests.Unit
{
    public abstract class BaseAgentTests
    {
        private readonly IEnumerable<PlanetoidInfoModel> _planetoidInfos;

        protected ITestOutputHelper OutputHelper { get; }

        protected IServiceCollection ServiceCollectionMock { get; set; }

        /// <summary>
        /// Create a new instance of the provider from the service collection.
        /// </summary>
        protected IServiceProvider ServiceProviderMock => ServiceCollectionMock.BuildServiceProvider();

        public BaseAgentTests(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;

            _planetoidInfos = GetPlanetoidInfos();

            ServiceCollectionMock = SetupServices();
        }

        protected virtual IServiceCollection SetupServices()
        {
            var configuration = CreateConfigurationBuilder().Build();
            var serviceCollection = new ServiceCollection();

            if (configuration != null)
            {
                serviceCollection.AddSingleton<IConfiguration>(configuration);
                serviceCollection
                    .AddOptions<GeoInfoServiceOptions>()
                    .Bind(configuration.GetSection(GeoInfoServiceOptions.DefaultConfigurationSectionName));
            }

            // Mock Logger
            serviceCollection.AddLogging(x => x.AddXUnit(OutputHelper));

            // Mock Coordinate Mapping
            serviceCollection.AddSingleton<ICoordinateMappingService>(new CoordinateMappingService(new QuadSphereCubeProjectionService()));

            // Mock SRS Info Repository
            var srsInfoMock = new Mock<ISpatialReferenceSystemRepository>();
            srsInfoMock
                .Setup(x => x.GetSRS(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken token) =>
                {
                    return Result<SpatialReferenceSystemModel>.CreateSuccess(new SpatialReferenceSystemModel(id, "Tests", id, null, null));
                });
            serviceCollection.AddSingleton(srsInfoMock.Object);

            // Mock Geometry Conversion Service
            serviceCollection.AddSingleton<IGeometryConversionService>(new GeometryConversionService(srsInfoMock.Object));

            // Mock Planetoid Info Repository
            var planetoidInfoMock = new Mock<IPlanetoidInfoRepository>();
            planetoidInfoMock
                .Setup(x => x.GetPlanetoidById(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken token) =>
                {
                    var planet = _planetoidInfos.FirstOrDefault(x => x.Id == id);

                    return planet != null
                        ? Result<PlanetoidInfoModel>.CreateSuccess(planet)
                        : Result<PlanetoidInfoModel>.CreateFailure($"Planetoid with id {id} not found");
                });
            serviceCollection.AddSingleton(planetoidInfoMock.Object);

            // Mock Planetoid Info Service
            var planetoidServiceMock = new Mock<IPlanetoidService>();
            planetoidServiceMock
                .Setup(x => x.GetPlanetoid(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken token) =>
                {
                    return planetoidInfoMock.Object.GetPlanetoidById(id, token).Result;
                });
            serviceCollection.AddSingleton(planetoidServiceMock.Object);

            // Mock Meta Dynamic Repository
            var metaDynamicMock = new Mock<IMetaDynamicRepository>();
            metaDynamicMock
                .Setup(x => x.InsertDynamic(It.IsAny<MetaDynamicModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MetaDynamicModel model, CancellationToken token) =>
                {
                    return Result<int>.CreateSuccess(model.Id);
                });
            metaDynamicMock
                .Setup(x => x.GetMetaDynamicModel(It.IsAny<TableSchema>(), It.IsAny<int>()))
                .Returns((TableSchema schema, int planetoidId) =>
                {
                    return new MetaDynamicModel(0, planetoidId, "public", "hello", "integer world");
                });
            serviceCollection.AddSingleton(metaDynamicMock.Object);

            // Mock Dynamic BuildingEntity Repository
            var buildingDynamicRepoMock = new Mock<IDynamicRepository<BuildingEntity>>();
            buildingDynamicRepoMock
                .Setup(x => x.Create(It.IsAny<BuildingEntity>(), It.IsAny<CancellationToken>(), It.IsAny<IDbConnection>()))
                .ReturnsAsync((BuildingEntity value, CancellationToken token) =>
                {
                    return Result<BuildingEntity>.CreateSuccess(value);
                });
            buildingDynamicRepoMock
                .Setup(x => x.CreateMultiple(It.IsAny<IEnumerable<BuildingEntity>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<IDbConnection>()))
                .ReturnsAsync((IEnumerable<BuildingEntity> values, bool ignoreErrors, CancellationToken token) =>
                {
                    return Result<IEnumerable<BuildingEntity>>.CreateSuccess(values);
                });
            buildingDynamicRepoMock
                .Setup(x => x.TableCreateIfNotExists(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationToken token) =>
                {
                    return Result.CreateSuccess();
                });
            var buildingDynamicRepoFactoryMock = new Mock<IGeometricDynamicRepositoryFactory<BuildingEntity>>();
            buildingDynamicRepoFactoryMock
                .Setup(x => x.CreateRepository(It.IsAny<TableSchema>()))
                .Returns((TableSchema schema) => Result<IDynamicRepository<BuildingEntity>>.CreateSuccess(buildingDynamicRepoMock.Object));
            serviceCollection.AddSingleton(buildingDynamicRepoFactoryMock.Object);

            // Mock Dynamic HighwayEntity Repository
            var highwayDynamicRepoMock = new Mock<IDynamicRepository<HighwayEntity>>();
            highwayDynamicRepoMock
                .Setup(x => x.Create(It.IsAny<HighwayEntity>(), It.IsAny<CancellationToken>(), It.IsAny<IDbConnection>()))
                .ReturnsAsync((HighwayEntity value, CancellationToken token) =>
                {
                    return Result<HighwayEntity>.CreateSuccess(value);
                });
            highwayDynamicRepoMock
                .Setup(x => x.CreateMultiple(It.IsAny<IEnumerable<HighwayEntity>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<IDbConnection>()))
                .ReturnsAsync((IEnumerable<HighwayEntity> values, bool ignoreErrors, CancellationToken token) =>
                {
                    return Result<IEnumerable<HighwayEntity>>.CreateSuccess(values);
                });
            highwayDynamicRepoMock
                .Setup(x => x.TableCreateIfNotExists(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationToken token) =>
                {
                    return Result.CreateSuccess();
                });
            var highwayDynamicRepoFactoryMock = new Mock<IGeometricDynamicRepositoryFactory<HighwayEntity>>();
            highwayDynamicRepoFactoryMock
                .Setup(x => x.CreateRepository(It.IsAny<TableSchema>()))
                .Returns((TableSchema schema) => Result<IDynamicRepository<HighwayEntity>>.CreateSuccess(highwayDynamicRepoMock.Object));
            serviceCollection.AddSingleton(highwayDynamicRepoFactoryMock.Object);

            // Mock Dynamic RailwayEntity Repository
            var railwayDynamicRepoMock = new Mock<IDynamicRepository<RailwayEntity>>();
            railwayDynamicRepoMock
                .Setup(x => x.Create(It.IsAny<RailwayEntity>(), It.IsAny<CancellationToken>(), It.IsAny<IDbConnection>()))
                .ReturnsAsync((RailwayEntity value, CancellationToken token) =>
                {
                    return Result<RailwayEntity>.CreateSuccess(value);
                });
            railwayDynamicRepoMock
                .Setup(x => x.CreateMultiple(It.IsAny<IEnumerable<RailwayEntity>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<IDbConnection>()))
                .ReturnsAsync((IEnumerable<RailwayEntity> values, bool ignoreErrors, CancellationToken token) =>
                {
                    return Result<IEnumerable<RailwayEntity>>.CreateSuccess(values);
                });
            railwayDynamicRepoMock
                .Setup(x => x.TableCreateIfNotExists(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationToken token) =>
                {
                    return Result.CreateSuccess();
                });
            var railwayDynamicRepoFactoryMock = new Mock<IGeometricDynamicRepositoryFactory<RailwayEntity>>();
            railwayDynamicRepoFactoryMock
                .Setup(x => x.CreateRepository(It.IsAny<TableSchema>()))
                .Returns((TableSchema schema) => Result<IDynamicRepository<RailwayEntity>>.CreateSuccess(railwayDynamicRepoMock.Object));
            serviceCollection.AddSingleton(railwayDynamicRepoFactoryMock.Object);

            return serviceCollection;
        }

        protected virtual IConfigurationBuilder CreateConfigurationBuilder()
        {
            return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "GeoInfoServiceOptions:OverpassConnectionString", "https://overpass-api.de/api/interpreter" }, // For foreign country data
            });
        }

        protected virtual IEnumerable<PlanetoidInfoModel> GetPlanetoidInfos()
        {
            return new List<PlanetoidInfoModel>
            {
                new PlanetoidInfoModel(1, "Moon", 69, 1737400),
                new PlanetoidInfoModel(2, "Planet", 69, 1188300),
                new PlanetoidInfoModel(3, "Earth", 0, 6_371_000),
            };
        }
    }
}
