using Insight.Database;
using Insight.Database.Structure;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories
{
    public class RepositoryAccessWrapper : IRepositoryAccessWrapper
    {
        protected IStaticRepository? _static;
        protected readonly DbConnectionStringBuilder _connection;

        public RepositoryAccessWrapper(DbConnectionStringBuilder connection, IStaticRepository? meta)
        {
            _static = meta;
            _connection = connection;
        }

        public async ValueTask<Result<T>> RunSingleFunction<T>(
            string name,
            object? arguments,
            CancellationToken token,
            IDbConnection? connection = null,
            Func<IDataReader, T>? constructor = null)
        {
            return await RunQuery(async (c) =>
            {
                T result = default;
                if (constructor == null)
                {
                    result = await c.SingleAsync<T>(name, arguments, cancellationToken: token);
                }
                else
                {
                    var sql = $"SELECT * FROM {name}";

                    var reader = await c.GetReaderAsync(
                        name,
                        arguments,
                        cancellationToken: token);
                    if (reader.HasRows)
                    {
                        var list = await reader.ToListAsync(CustomRecordReader<T>.Read(constructor), cancellationToken: token);

                        if (list != null)
                        {
                            result = list.FirstOrDefault();
                        }
                    }

                    await reader.CloseAsync();
                }

                return result != null
                    ? Result<T>.CreateSuccess(result)
                    : Result<T>.CreateFailure(GeneralStringMessages.DatabaseProcedureError, name, GeneralStringMessages.DatabaseProcedureRecordNotExist);
            }, token, connection);
        }

        public async ValueTask<Result<IReadOnlyList<T>>> RunMultipleFunction<T>(
            string name,
            object? arguments,
            CancellationToken token,
            IDbConnection? connection = null,
            Func<IDataReader, T>? constructor = null)
        {
            return await RunQuery(async (c) =>
            {
                IReadOnlyList<T>? result = null;
                if (constructor == null)
                {
                    var queryResult = await c.QueryAsync<T>(name, arguments, cancellationToken: token);
                    result = queryResult?.ToList();
                }
                else
                {
                    result = new List<T>();
                    var reader = await c.GetReaderAsync(
                        name,
                        arguments,
                        cancellationToken: token);
                    if (reader.HasRows)
                    {
                        var queryResult = await reader.ToListAsync(CustomRecordReader<T>.Read(constructor), cancellationToken: token);
                        result = queryResult?.ToList();
                    }

                    await reader.CloseAsync();
                }

                return result != null
                    ? Result<IReadOnlyList<T>>.CreateSuccess(result)
                    : Result<IReadOnlyList<T>>.CreateFailure(GeneralStringMessages.DatabaseProcedureError, name);
            }, token, connection);
        }

        public async ValueTask<Result<T>> RunSingleProcedure<T>(
            string prefix,
            string name,
            string sqlParams,
            object? arguments,
            CancellationToken token,
            IDbConnection? connection = null,
            Func<IDataReader, T>? constructor = null)
        {
            return await RunQuery(async (c) =>
            {
                var sql = $"{prefix} {name}({sqlParams})";
                T result = default;

                var reader = await c.GetReaderSqlAsync(
                    sql,
                    arguments,
                    cancellationToken: token);
                if (reader.HasRows)
                {
                    var list = await reader.ToListAsync(
                        constructor != null
                            ? (IRecordReader<T>)CustomRecordReader<T>.Read(constructor)
                            : OneToOne<T>.Records,
                        cancellationToken: token);

                    if (list != null)
                    {
                        result = list.FirstOrDefault();
                    }
                }

                await reader.CloseAsync();

                return result != null
                    ? Result<T>.CreateSuccess(result)
                    : Result<T>.CreateFailure(GeneralStringMessages.DatabaseProcedureError, name);
            }, token, connection);
        }

        public async ValueTask<Result<IReadOnlyList<T>>> RunMultipleProcedure<T>(
            string prefix,
            string name,
            string sqlParams,
            object? arguments,
            CancellationToken token,
            IDbConnection? connection = null,
            Func<IDataReader, T>? constructor = null)
        {
            return await RunQuery(async (c) =>
            {
                var sql = $"{prefix} {name}({sqlParams})";
                IReadOnlyList<T>? result = null;
                if (constructor != null)
                {
                    var reader = await c.GetReaderSqlAsync(
                        sql,
                        arguments,
                        cancellationToken: token);
                    if (reader.HasRows)
                    {
                        var queryResult = await reader.ToListAsync(CustomRecordReader<T>.Read(constructor), cancellationToken: token);
                        result = queryResult?.ToList();
                    }

                    await reader.CloseAsync();
                }
                else
                {
                    var queryResult = await c.QuerySqlAsync<T>(
                        sql,
                        arguments,
                        cancellationToken: token);
                    result = queryResult?.ToList();
                }

                return result != null
                    ? Result<IReadOnlyList<T>>.CreateSuccess(result)
                    : Result<IReadOnlyList<T>>.CreateFailure(GeneralStringMessages.DatabaseProcedureError, name);
            }, token, connection);
        }

        public async ValueTask<Result<T>> RunSingleQuery<T>(
            string sql,
            CancellationToken token,
            object? parameters = null,
            IDbConnection? connection = null,
            Func<IDataReader, T>? constructor = null)
        {
            return await RunQueryTransaction(async (c) => await RunSingleQueryRaw(sql, c, token, parameters, constructor), token, connection);
        }

        public async ValueTask<Result<IReadOnlyList<T>>> RunMultipleQuery<T>(
            string sql,
            CancellationToken token,
            IDbConnection? connection = null,
            Func<IDataReader, T>? constructor = null)
        {
            return await RunQueryTransaction(async (c) =>
            {
                List<T>? result = null;

                var reader = await c.GetReaderSqlAsync(
                    sql,
                    cancellationToken: token);
                if (reader.HasRows)
                {
                    var list = await reader.ToListAsync(
                        constructor != null
                            ? (IRecordReader<T>)CustomRecordReader<T>.Read(constructor)
                            : OneToOne<T>.Records,
                        cancellationToken: token);
                    result = list?.ToList();
                }

                await reader.CloseAsync();

                return result != null
                    ? Result<IReadOnlyList<T>>.CreateSuccess(result)
                    : Result<IReadOnlyList<T>>.CreateFailure(GeneralStringMessages.DatabaseProcedureError, sql);
            }, token, connection);
        }

        public async ValueTask<Result> RunSingleProcedure(
            string prefix,
            string name,
            string sqlParams,
            object? arguments,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunQuery(async (c) =>
            {
                var sql = $"{prefix} {name}({sqlParams})";

                var reader = await c.GetReaderSqlAsync(
                    sql,
                    arguments,
                    cancellationToken: token);

                var rows = reader.RecordsAffected;

                await reader.CloseAsync();

                return rows != 0
                    ? Result.CreateSuccess()
                    : Result.CreateFailure(GeneralStringMessages.DatabaseProcedureError, name);
            }, token, connection);
        }

        /// <summary>
        /// Run query without initialization or exception handling.
        /// </summary>
        /// <typeparam name="T">Return object/row type.</typeparam>
        /// <param name="sql">SQL command to execute.</param>
        /// <param name="c">Opened connection.</param>
        /// <param name="constructor">Optional constructor for resulting rows from a row-based data reader.</param>
        /// <returns>The resulting object/row.</returns>
        protected async ValueTask<Result<T>> RunSingleQueryRaw<T>(
            string sql,
            IDbConnection c,
            CancellationToken token,
            object? parameters = null,
            Func<IDataReader, T>? constructor = null)
        {
            T result = default;

            var reader = await c.GetReaderSqlAsync(
                sql,
                parameters: parameters,
                cancellationToken: token);
            if (reader.HasRows)
            {
                var list = await reader.ToListAsync(
                    constructor != null
                        ? (IRecordReader<T>)CustomRecordReader<T>.Read(constructor)
                        : OneToOne<T>.Records,
                    cancellationToken: token);

                if (list != null)
                {
                    result = list.FirstOrDefault();
                }
            }

            await reader.CloseAsync();

            return result != null
                ? Result<T>.CreateSuccess(result)
                : Result<T>.CreateFailure(GeneralStringMessages.DatabaseProcedureError, sql);
        }

        protected async ValueTask<Result<T>> RunQuery<T>(Func<IDbConnection, ValueTask<Result<T>>> query, CancellationToken token, IDbConnection? connection = null)
        {
            if (_static != null)
            {
                var initResult = await _static!.EnsureExistsAsync(token);
                if (!initResult.Success)
                {
                    return Result<T>.CreateFailure(
                        GeneralStringMessages.ObjectNotInitialized,
                        initResult.ErrorMessage!);
                }
            }

            try
            {
                if (connection != null)
                {
                    return await query(connection);
                }
                else
                {
                    using (var c = await _connection.OpenAsync(token))
                    {
                        return await query(c);
                    }
                }
            }
            catch (DbException e)
            {
                return Result<T>.CreateFailure(e);
            }
            catch (InvalidOperationException e)
            {
                return Result<T>.CreateFailure(e);
            }
        }

        protected async ValueTask<Result> RunQuery(Func<IDbConnection, ValueTask<Result>> query, CancellationToken token, IDbConnection? connection = null)
        {
            if (_static != null)
            {
                var initResult = await _static!.EnsureExistsAsync(token);
                if (!initResult.Success)
                {
                    return Result.CreateFailure(
                        GeneralStringMessages.ObjectNotInitialized,
                        initResult.ErrorMessage!);
                }
            }

            try
            {
                if (connection != null)
                {
                    return await query(connection);
                }
                else
                {
                    using (var c = await _connection.OpenAsync(token))
                    {
                        return await query(c);
                    }
                }
            }
            catch (DbException e)
            {
                return Result.CreateFailure(e);
            }
            catch (InvalidOperationException e)
            {
                return Result.CreateFailure(e);
            }
        }

        protected async ValueTask<Result<T>> RunQueryTransaction<T>(Func<IDbConnection, ValueTask<Result<T>>> query, CancellationToken token, IDbConnection? connection = null)
        {
            if (_static != null)
            {
                var initResult = await _static!.EnsureExistsAsync(token);
                if (!initResult.Success)
                {
                    return Result<T>.CreateFailure(
                        GeneralStringMessages.ObjectNotInitialized,
                        initResult.ErrorMessage!);
                }
            }

            try
            {
                if (connection != null)
                {
                    return await query(connection);
                }
                else
                {
                    using (var c = await _connection.OpenWithTransactionAsync(token))
                    {
                        var result = await query(c);

                        if (result.Success)
                        {
                            c.Commit();
                        }
                        else
                        {
                            c.Rollback();
                        }

                        return result;
                    }
                }
            }
            catch (DbException e)
            {
                return Result<T>.CreateFailure(e);
            }
            catch (InvalidOperationException e)
            {
                return Result<T>.CreateFailure(e);
            }
        }

        protected async ValueTask<Result> RunQuery(string sql, IDbConnection c, CancellationToken token)
        {
            try
            {
                // May throw an exception trying to load some internal DLLs.
                var command = c.CreateCommandSql(sql);

                command.Prepare();
                var result = await command.QueryAsync(cancellationToken: token);
                return Result.CreateSuccess();
            }
            catch (DbException e)
            {
                return Result.CreateFailure(e);
            }
            catch (InvalidOperationException e)
            {
                return Result.CreateFailure(e);
            }
            catch (FileNotFoundException e)
            {
                return Result.CreateFailure(GeneralStringMessages.InternalError, e);
            }
        }

        public async ValueTask<Result<IDbConnection>> OpenTransaction(CancellationToken token)
        {
            try
            {
                var connection = await _connection.OpenWithTransactionAsync(token);
                return Result<IDbConnection>.CreateSuccess(connection);
            }
            catch (Exception e)
            {
                return Result<IDbConnection>.CreateFailure(e);
            }
        }

        public ValueTask<Result> CommitTransaction(IDbConnection connection, CancellationToken token)
        {
            if (!(connection is DbConnectionWrapper conn))
            {
                return new ValueTask<Result>(Result.CreateFailure(GeneralStringMessages.ObjectNotExist,
                    new ArgumentNullException(nameof(connection), $"Connection is of wrong type: {connection.GetType().Name}.")));
            }

            conn.Commit();

            return new ValueTask<Result>(Result.CreateSuccess());
        }

        public ValueTask<Result> RollbackTransaction(IDbConnection connection, CancellationToken token)
        {
            if (!(connection is DbConnectionWrapper conn))
            {
                return new ValueTask<Result>(Result.CreateFailure(GeneralStringMessages.ObjectNotExist,
                    new ArgumentNullException(nameof(connection), $"Connection is of wrong type: {connection.GetType().Name}.")));
            }

            conn.Rollback();

            return new ValueTask<Result>(Result.CreateSuccess());
        }
    }
}
