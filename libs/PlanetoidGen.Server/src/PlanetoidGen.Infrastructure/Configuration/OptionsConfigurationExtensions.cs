using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanetoidGen.Contracts.Models.Repositories.Documents;
using PlanetoidGen.Contracts.Models.Repositories.Messaging.Kafka;
using PlanetoidGen.Contracts.Models.Services.Agents;
using PlanetoidGen.Contracts.Models.Services.GeoInfo;
using PlanetoidGen.Contracts.Models.Services.Meta;

namespace PlanetoidGen.Infrastructure.Configuration
{
    public static class OptionsConfigurationExtensions
    {
        public static IServiceCollection ConfigureServiceOptions(this IServiceCollection collection, IConfiguration configuration)
        {
            collection
                .AddOptions();

            collection
                .AddOptions<MetaProcedureOptions>()
                .Bind(configuration.GetSection(MetaProcedureOptions.DefaultConfigurationSectionName))
                .ValidateDataAnnotations();

            collection
                .AddOptions<AgentLoaderServiceOptions>()
                .Bind(configuration.GetSection(AgentLoaderServiceOptions.DefaultConfigurationSectionName));

            return collection;
        }

        public static IServiceCollection ConfigureMessageBrokerOptions(this IServiceCollection collection, IConfiguration configuration)
        {
            collection
                .AddOptions<KafkaOptions>()
                .Bind(configuration.GetSection(KafkaOptions.DefaultConfigurationSectionName))
                .ValidateDataAnnotations();

            return collection;
        }

        public static IServiceCollection ConfigureDocumentDbOptions(this IServiceCollection collection, IConfiguration configuration)
        {
            collection
                .AddOptions<DocumentDbOptions>()
                .Bind(configuration.GetSection(DocumentDbOptions.DefaultConfigurationSectionName));

            return collection;
        }

        public static IServiceCollection ConfigureGeoInfoOptions(this IServiceCollection collection, IConfiguration configuration)
        {
            collection
                .AddOptions<GeoInfoServiceOptions>()
                .Bind(configuration.GetSection(GeoInfoServiceOptions.DefaultConfigurationSectionName));

            return collection;
        }

        public static IServiceCollection ConfigureAgentWorkerServiceOptions(this IServiceCollection collection, IConfiguration configuration)
        {
            collection
                .AddOptions<AgentWorkerServiceOptions>()
                .Bind(configuration.GetSection(AgentWorkerServiceOptions.DefaultConfigurationSectionName))
                .ValidateDataAnnotations();

            return collection;
        }
    }
}
