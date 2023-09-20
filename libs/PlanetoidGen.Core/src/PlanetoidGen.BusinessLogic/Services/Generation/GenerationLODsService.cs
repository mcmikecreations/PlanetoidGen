using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Repositories.Generation;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Generation;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.BusinessLogic.Services.Generation
{
    public class GenerationLODsService : IGenerationLODsService
    {
        private readonly IGenerationLODsRepository _generationLODsRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<GenerationLODsService> _logger;

        public GenerationLODsService(
            IGenerationLODsRepository generationLODsRepository,
            IMemoryCache memoryCache,
            ILogger<GenerationLODsService> logger)
        {
            _generationLODsRepository = generationLODsRepository ?? throw new ArgumentNullException(nameof(generationLODsRepository));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Result<int>> ClearLODs(int planetoidId, CancellationToken token)
        {
            return await _generationLODsRepository.ClearLODs(planetoidId, token);
        }

        public async ValueTask<Result<GenerationLODModel>> GetLOD(int planetoidId, short lod, CancellationToken token)
        {
            return await _generationLODsRepository.GetLOD(planetoidId, lod, token);
        }

        public async ValueTask<Result<IEnumerable<GenerationLODModel>>> GetLODs(int planetoidId, CancellationToken token)
        {
            var result = await _memoryCache.GetOrCreateAsync(
                $"{nameof(GetLODs)}__{planetoidId}",
                async (cacheEntry) =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120);
                    return await _generationLODsRepository.GetLODs(planetoidId, token);
                });

            return result ?? Result<IEnumerable<GenerationLODModel>>.CreateFailure("Failed to get LODs from cache.");
        }

        public async ValueTask<Result<int>> InsertLODs(IEnumerable<GenerationLODModel> models, CancellationToken token)
        {
            return await _generationLODsRepository.InsertLODs(models, token);
        }
    }
}
