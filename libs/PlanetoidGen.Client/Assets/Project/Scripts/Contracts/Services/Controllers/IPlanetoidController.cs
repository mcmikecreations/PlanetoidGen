using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Contracts.Services.Controllers
{
    public interface IPlanetoidController
    {
        Task<int> AddPlanetoid(PlanetoidInfoModel planetoid, CancellationToken token = default);
        Task<int> ClearPlanetoids(CancellationToken token = default);
        Task<IEnumerable<PlanetoidInfoModel>> GetAllPlanetoids(CancellationToken token = default);
        Task<PlanetoidInfoModel> GetPlanetoid(int id, CancellationToken token = default);
        Task<bool> RemovePlanetoid(int id, CancellationToken token = default);
    }
}
