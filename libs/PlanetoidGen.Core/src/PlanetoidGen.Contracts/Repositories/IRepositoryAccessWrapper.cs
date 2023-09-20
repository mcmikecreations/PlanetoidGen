using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories
{
    public interface IRepositoryAccessWrapper
    {
        ValueTask<Result<IDbConnection>> OpenTransaction(CancellationToken token);

        /// <summary>
        /// Commits a transaction opened by <seealso cref="OpenTransaction(CancellationToken)"/>.
        /// </summary>
        ValueTask<Result> CommitTransaction(IDbConnection connection, CancellationToken token);

        /// <summary>
        /// Rolls back a transaction opened by <seealso cref="OpenTransaction(CancellationToken)"/>.
        /// </summary>
        ValueTask<Result> RollbackTransaction(IDbConnection connection, CancellationToken token);

        ValueTask<Result<T>> RunSingleFunction<T>(
            string name,
            object? arguments,
            CancellationToken token,
            IDbConnection? connection = null,
            Func<IDataReader, T>? constructor = null);

        ValueTask<Result<IReadOnlyList<T>>> RunMultipleFunction<T>(
            string name,
            object? arguments,
            CancellationToken token,
            IDbConnection? connection = null,
            Func<IDataReader, T>? constructor = null);

        ValueTask<Result> RunSingleProcedure(
            string prefix,
            string name,
            string sqlParams,
            object? arguments,
            CancellationToken token,
            IDbConnection? connection = null);

        ValueTask<Result<T>> RunSingleProcedure<T>(
            string prefix,
            string name,
            string sqlParams,
            object? arguments,
            CancellationToken token,
            IDbConnection? connection = null,
            Func<IDataReader, T>? constructor = null);

        ValueTask<Result<IReadOnlyList<T>>> RunMultipleProcedure<T>(
            string prefix,
            string name,
            string sqlParams,
            object? arguments,
            CancellationToken token,
            IDbConnection? connection = null,
            Func<IDataReader, T>? constructor = null);

        /// <summary>
        /// Run an SQL query that returns a single item in a default transaction.
        /// </summary>
        /// <typeparam name="T">Return data type.</typeparam>
        /// <param name="sql">SQL query to run.</param>
        /// <param name="constructor">Mapper from response to desired data type <typeparamref name="T"/>.</param>
        /// <returns>Resulting data if successful, error message otherwise.</returns>
        ValueTask<Result<T>> RunSingleQuery<T>(
            string sql,
            CancellationToken token,
            object? parameters = null,
            IDbConnection? connection = null,
            Func<IDataReader, T>? constructor = null);

        /// <summary>
        /// Run an SQL query that returns multiple items in a default transaction.
        /// </summary>
        /// <typeparam name="T">Return data type.</typeparam>
        /// <param name="sql">SQL query to run.</param>
        /// <param name="constructor">Mapper from response to desired data type <typeparamref name="T"/>.</param>
        /// <returns>Resulting data list if successful, error message otherwise.</returns>
        ValueTask<Result<IReadOnlyList<T>>> RunMultipleQuery<T>(
            string sql,
            CancellationToken token,
            IDbConnection? connection = null,
            Func<IDataReader, T>? constructor = null);
    }
}
