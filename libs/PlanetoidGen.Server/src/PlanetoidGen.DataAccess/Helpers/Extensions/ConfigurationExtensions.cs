using Insight.Database.Providers.PostgreSQL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PlanetoidGen.Contracts.Factories.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories;
using PlanetoidGen.Contracts.Repositories.Documents;
using PlanetoidGen.Contracts.Repositories.Generation;
using PlanetoidGen.Contracts.Repositories.Info;
using PlanetoidGen.Contracts.Repositories.Messaging;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Factories.Repositories.Dynamic;
using PlanetoidGen.DataAccess.Repositories;
using PlanetoidGen.DataAccess.Repositories.Documents;
using PlanetoidGen.DataAccess.Repositories.Generation;
using PlanetoidGen.DataAccess.Repositories.Info;
using PlanetoidGen.DataAccess.Repositories.Messaging.Kafka;
using PlanetoidGen.DataAccess.Repositories.Meta;
using System.Data.Common;

namespace PlanetoidGen.DataAccess.Helpers.Extensions
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection ConfigureConnection(this IServiceCollection collection, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("PlanetoidGen.Database");
            NpgsqlConnection.GlobalTypeMapper.UseNetTopologySuite(/*handleOrdinates: Ordinates.XYZ*/);
            PostgreSQLInsightDbProvider.RegisterProvider();

            return collection
                .AddSingleton<DbConnectionStringBuilder>((p) => new NpgsqlConnectionStringBuilder(connectionString));
        }

        public static IServiceCollection ConfigureDataAccess(this IServiceCollection collection, IConfiguration configuration)
        {
            return collection
                .ConfigureConnection(configuration)
                .AddSingleton<IRepositoryAccessWrapper, RepositoryAccessWrapper>(x => new RepositoryAccessWrapper(
                    x.GetRequiredService<DbConnectionStringBuilder>(),
                    x.GetService<IMetaProcedureRepository>()))
                .AddSingleton<IMetaProcedureRepository, MetaProcedureRepository>()
                .AddSingleton<IMetaDynamicRepository, MetaDynamicRepository>()
                .AddSingleton<ITileInfoRepository, TileInfoRepository>()
                .AddSingleton<IAgentInfoRepository, AgentInfoRepository>()
                .AddSingleton<IPlanetoidInfoRepository, PlanetoidInfoRepository>()
                .AddSingleton<ISpatialReferenceSystemRepository, SpatialReferenceSystemRepository>()
                .AddSingleton<IGenerationLODsRepository, GenerationLODsRepository>()
                .AddSingleton(typeof(IDynamicRepositoryFactory<>), typeof(UniversalDynamicRepositoryFactory<>))
                .AddSingleton(typeof(IGeometricDynamicRepositoryFactory<>), typeof(GeometricDynamicRepositoryFactory<>))
                .AddSingleton<IFileContentRepository, FileContentRepository>()
                .AddSingleton<IFileInfoRepository, FileInfoRepository>()
                .AddSingleton<ITileBasedFileInfoRepository, TileBasedFileInfoRepository>()
                .AddSingleton<IFileDependencyRepository, FileDependencyRepository>()
                .AddTransient<IGenerationJobMessageAdminRepository, KafkaGenerationJobMessageAdminRepository>()
                .AddSingleton<IGenerationJobMessageProducerRepository, KafkaGenerationJobMessageProducerRepository>()
                .AddTransient<IGenerationJobMessageConsumerRepository, KafkaGenerationJobMessageConsumerRepository>();
        }
    }
}
