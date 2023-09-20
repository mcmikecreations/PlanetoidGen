using PlanetoidGen.API.Helpers.Abstractions;
using PlanetoidGen.API.Helpers.Implementations;

namespace PlanetoidGen.API.Helpers
{
    public static class HelpersConfigurationExtensions
    {
        public static IServiceCollection ConfigureHelpers(this IServiceCollection collection)
        {
            collection
                .AddSingleton(typeof(IStreamContext<>), typeof(StreamContext<>));

            return collection;
        }
    }
}
