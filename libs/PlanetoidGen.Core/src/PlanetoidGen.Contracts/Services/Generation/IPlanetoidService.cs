using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Services.Generation
{
    public interface IPlanetoidService
    {
        ValueTask<Result<PlanetoidInfoModel>> GetPlanetoid(int id, CancellationToken token);

        ValueTask<Result<int>> AddPlanetoid(PlanetoidInfoModel planetoid, CancellationToken token);

        ValueTask<Result<bool>> RemovePlanetoid(int id, CancellationToken token);

        ValueTask<Result<int>> ClearPlanetoids(CancellationToken token);

        ValueTask<Result<IReadOnlyList<PlanetoidInfoModel>>> GetAllPlanetoids(CancellationToken token);
    }
}
