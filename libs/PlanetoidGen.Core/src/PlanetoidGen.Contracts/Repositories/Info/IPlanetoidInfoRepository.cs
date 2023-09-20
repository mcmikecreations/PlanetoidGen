using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Info
{
    public interface IPlanetoidInfoRepository : INamedRepository<PlanetoidInfoModel>
    {
        /// <summary>
        /// Insert a new planetoid into the repository.
        /// </summary>
        /// <param name="model">The input planetoid model to insert.</param>
        /// <returns>The inserted planetoid id.</returns>
        ValueTask<Result<int>> InsertPlanetoid(PlanetoidInfoModel model, CancellationToken token);

        /// <summary>
        /// Get a planetoid model by its id.
        /// </summary>
        /// <param name="id">The id of the planetoid to get.</param>
        /// <returns>The requested planetoid in an array, if present.</returns>
        ValueTask<Result<PlanetoidInfoModel>> GetPlanetoidById(int id, CancellationToken token);

        /// <summary>
        /// Remove a planetoid from the repository.
        /// </summary>
        /// <param name="id">The id of the planetoid to remove.</param>
        /// <returns>True if the planetoid was removed, false otherwise.</returns>
        ValueTask<Result<bool>> RemovePlanetoidById(int id, CancellationToken token);

        /// <summary>
        /// Clear all planetoids from the repository.
        /// </summary>
        /// <returns>Number of removed planetoids.</returns>
        ValueTask<Result<int>> ClearPlanetoids(CancellationToken token);

        /// <summary>
        /// Gets all planetoids.
        /// </summary>
        /// <param name="token"></param>
        /// <returns>A collection of <see cref="PlanetoidInfoModel"/>.</returns>
        ValueTask<Result<IReadOnlyList<PlanetoidInfoModel>>> GetAllPlanetoids(CancellationToken token);
    }
}
