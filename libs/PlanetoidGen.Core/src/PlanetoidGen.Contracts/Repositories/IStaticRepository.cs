using PlanetoidGen.Contracts.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories
{
    public interface IStaticRepository
    {
        ValueTask<Result> EnsureExistsAsync(CancellationToken token);

        ValueTask<bool> ExistsAsync(CancellationToken token);
    }
}
