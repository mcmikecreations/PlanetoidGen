using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Dynamic
{
    /// <summary>
    /// Repository for a table of a dynamic data type belonging to a planetoid.
    /// Stores some state regarding table name, etc., but otherwise can be safely
    /// used in a multithreaded environment with per-thread or per-job instances.
    /// </summary>
    public interface IDynamicRepository<TData>
    {
        TableSchema Schema { get; }

        IRowSerializer<TData> Serializer { get; }

        ITableProcedureGenerator ProcedureGenerator { get; }

        ValueTask<Result> TableCreateIfNotExists(CancellationToken token);

        ValueTask<Result> TableDrop(CancellationToken token);

        ValueTask<Result<TData>> Create(TData value, CancellationToken token, IDbConnection? connection = null);

        ValueTask<Result<IEnumerable<TData>>> CreateMultiple(IEnumerable<TData> values, bool ignoreErrors, CancellationToken token, IDbConnection? connection = null);

        ValueTask<Result<TData>> Read(TData value, CancellationToken token, IDbConnection? connection = null);

        ValueTask<Result<TData>> Update(TData value, CancellationToken token, IDbConnection? connection = null);

        /// <summary>
        /// Update multiple objects based on the provided data.
        /// </summary>
        /// <param name="models">The list of objects to update. It is assumed all of them already exist in the table.</param>
        /// <returns>The list of updated models.</returns>
        ValueTask<Result<IEnumerable<TData>>> UpdateMultiple(IEnumerable<TData> models, CancellationToken token, IDbConnection? connection = null);

        ValueTask<Result<TData>> Delete(TData value, CancellationToken token, IDbConnection? connection = null);

        ValueTask<Result> DeleteMultiple(IEnumerable<TData> models, CancellationToken token, IDbConnection? connection = null);

        ValueTask<Result<int>> Clear(CancellationToken token, IDbConnection? connection = null);
    }
}
