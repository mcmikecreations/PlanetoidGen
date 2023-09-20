using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PlanetoidGen.Infrastructure.Configuration
{
    public static class DataAccessConfigurationExtensions
    {
        public static IServiceCollection ConfigureDataAccess(this IServiceCollection collection, IConfiguration configuration)
        {
            return DataAccess.Helpers.Extensions.ConfigurationExtensions.ConfigureDataAccess(collection, configuration);
        }
    }
}
