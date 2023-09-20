using PlanetoidGen.Domain.Models.Generation;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Contracts.Services.Controllers
{
    public interface IGenerationLODController
    {
        Task<int> ClearLODs(int planetoidId, CancellationToken token = default);
        Task<GenerationLODModel> GetLOD(int planetoidId, int lod, CancellationToken token = default);
        Task<IEnumerable<GenerationLODModel>> GetLODs(int planetoidId, CancellationToken token = default);
        Task<int> InsertLODs(IEnumerable<GenerationLODModel> lods, CancellationToken token = default);
    }
}
