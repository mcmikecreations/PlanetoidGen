using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Generation;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Generation
{
    public interface IGenerationLODsRepository : INamedRepository<GenerationLODModel>
    {
        /// <summary>
        /// Insert LOD list into the repository.
        /// </summary>
        /// <param name="models">Ordered set of unique LODs.</param>
        /// <returns>Number of LODs for the planetoid in the repository.</returns>
        ValueTask<Result<int>> InsertLODs(IEnumerable<GenerationLODModel> models, CancellationToken token);

        /// <summary>
        /// Get zooms for all LODs of a planetoid.
        /// </summary>
        /// <returns>Zoom models for the specified planetoid.</returns>
        ValueTask<Result<IEnumerable<GenerationLODModel>>> GetLODs(int planetoidId, CancellationToken token);

        /// <summary>
        /// Get zoom for a certain LOD.
        /// </summary>
        /// <returns>Zoom model for the specified LOD.</returns>
        ValueTask<Result<GenerationLODModel>> GetLOD(int planetoidId, short lod, CancellationToken token);

        /// <summary>
        /// Clears all LODs from the repository.
        /// </summary>
        /// <returns>Number of removed LODs.</returns>
        ValueTask<Result<int>> ClearLODs(int planetoidId, CancellationToken token);
    }
}
