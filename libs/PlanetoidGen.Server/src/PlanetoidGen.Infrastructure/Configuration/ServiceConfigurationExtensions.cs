using Microsoft.Extensions.DependencyInjection;
using PlanetoidGen.BusinessLogic.Common.Services.Generation;
using PlanetoidGen.BusinessLogic.Services.Agents;
using PlanetoidGen.BusinessLogic.Services.Documents;
using PlanetoidGen.BusinessLogic.Services.Generation;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Documents;
using PlanetoidGen.Contracts.Services.Generation;

namespace PlanetoidGen.Infrastructure.Configuration
{
    public static class ServiceConfigurationExtensions
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection collection)
        {
            return collection
                .AddMemoryCache()
                .AddSingleton<ICubeProjectionService, ProjCubeProjectionService>()
                .AddSingleton<ICoordinateMappingService, CoordinateMappingService>()
                .AddSingleton<IGeometryConversionService, GeometryConversionService>()
                .AddSingleton<IPlanetoidService, PlanetoidService>()
                .AddSingleton<IAgentService, AgentService>()
                .AddSingleton<IGenerationService, GenerationJobMessageProducerService>()
                .AddSingleton<IAgentLoaderService, AgentLoaderService>()
                .AddSingleton<IGenerationLODsService, GenerationLODsService>()
                .AddSingleton<IFileContentService, FileContentService>()
                .AddSingleton<ITileInfoService, TileInfoService>();
        }
    }
}
