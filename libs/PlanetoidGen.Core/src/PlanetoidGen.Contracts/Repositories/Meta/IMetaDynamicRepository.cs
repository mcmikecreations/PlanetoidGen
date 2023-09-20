using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Domain.Models.Meta;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Meta
{
    public interface IMetaDynamicRepository : INamedRepository<MetaDynamicModel>
    {
        /// <summary>
        /// Convert dynamic table schema to a dynamic table declaration model.
        /// </summary>
        /// <param name="tableSchema">Dynamic table schema to convert.</param>
        /// <returns>The dynamic table declaration model corresponding to the table schema.</returns>
        MetaDynamicModel GetMetaDynamicModel(TableSchema tableSchema, int planetoidId);

        /// <summary>
        /// Insert a new dynamic table declaration into the repository.
        /// </summary>
        /// <param name="model">The input dynamic table model to insert.</param>
        /// <returns>The inserted dynamic table id.</returns>
        ValueTask<Result<int>> InsertDynamic(MetaDynamicModel model, CancellationToken token);

        /// <summary>
        /// Get a dynamic table model by its id.
        /// </summary>
        /// <param name="id">The id of the dynamic table to get.</param>
        /// <returns>The requested dynamic table declaration, if present.</returns>
        ValueTask<Result<MetaDynamicModel>> GetDynamicById(int id, CancellationToken token);

        /// <summary>
        /// Get a dynamic table model by its schema and name.
        /// </summary>
        /// <param name="schema">The schema name of the dynamic table to get.</param>
        /// <param name="name">The table name of the dynamic table to get.</param>
        /// <returns>The requested dynamic table declaration, if present.</returns>
        ValueTask<Result<MetaDynamicModel>> GetDynamicByName(int planetoidId, string schema, string title, CancellationToken token);

        /// <summary>
        /// Remove a dynamic table declaration from the repository.
        /// </summary>
        /// <param name="id">The id of the dynamic table declaration to remove.</param>
        /// <returns>True if the dynamic table declaration was removed, false otherwise.</returns>
        ValueTask<Result<bool>> RemoveDynamicById(int id, CancellationToken token);

        /// <summary>
        /// Clear all dynamic table declarations from the repository.
        /// </summary>
        /// <returns>Number of removed declarations.</returns>
        ValueTask<Result<int>> ClearDynamicMeta(CancellationToken token);

        /// <summary>
        /// Clear dynamic table content.
        /// </summary>
        /// <returns>Number of removed items.</returns>
        ValueTask<Result<int>> ClearDynamicContent(MetaDynamicModel model, CancellationToken token);
    }
}
