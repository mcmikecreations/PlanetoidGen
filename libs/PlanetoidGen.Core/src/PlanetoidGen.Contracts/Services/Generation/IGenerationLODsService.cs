using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Generation;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Services.Generation
{
    public interface IGenerationLODsService
    {
        ValueTask<Result<int>> ClearLODs(int planetoidId, CancellationToken token);
        ValueTask<Result<GenerationLODModel>> GetLOD(int planetoidId, short lod, CancellationToken token);
        ValueTask<Result<IEnumerable<GenerationLODModel>>> GetLODs(int planetoidId, CancellationToken token);
        ValueTask<Result<int>> InsertLODs(IEnumerable<GenerationLODModel> models, CancellationToken token);
    }
}
