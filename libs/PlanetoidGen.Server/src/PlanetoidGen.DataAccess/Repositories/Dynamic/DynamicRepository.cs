using Insight.Database;
using Microsoft.Extensions.Configuration;
using Nito.AsyncEx;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories;
using PlanetoidGen.Contracts.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Repositories.Generic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Dynamic
{
    public class DynamicRepository<TData> : RepositoryAccessWrapper<TData>, INamedRepository<TData>, IRepositoryAccessWrapper, IDynamicRepository<TData>, IStaticRepository
    {
        protected readonly IMetaProcedureRepository _metaRepo;
        protected readonly ITableProcedureGenerator _generator;
        protected readonly IRowSerializer<TData> _serializer;
        protected readonly TableSchema _schema;

        protected bool _initialized;
        protected readonly bool _recreateTables;
        protected readonly AsyncLock _mutex = new AsyncLock();

        public DynamicRepository(
            DbConnectionStringBuilder connection,
            IMetaProcedureRepository meta,
            ITableProcedureGenerator generator,
            IRowSerializer<TData> serializer,
            IConfiguration configuration)
            : base(connection, null) // Meta doesn't need to be passed down, but it is manually checked in EnsureExistsAsync
        {
            _static = this;
            _metaRepo = meta;
            _generator = generator;
            _serializer = serializer;
            _schema = _generator.Schema;

            var options = meta.Options;
            _recreateTables = options.RecreateDynamicTables || options.RecreateExtensions || options.RecreateSchemas;

            _initialized = false;
        }

        public override string Name => _schema.Title;

        public override Func<IDataReader, TData>? Reader => _serializer.Deserializer;

        public TableSchema Schema => _schema;

        public IRowSerializer<TData> Serializer => _serializer;

        public ITableProcedureGenerator ProcedureGenerator => _generator;

        public async ValueTask<Result> EnsureExistsAsync(CancellationToken token)
        {
            if (_initialized)
            {
                return Result.CreateSuccess();
            }

            using (await _mutex.LockAsync(token))
            {
                if (_initialized)
                {
                    return Result.CreateSuccess();
                }

                var result = await _metaRepo.EnsureExistsAsync(token);

                if (!result.Success)
                {
                    return result;
                }

                using (var c = await _connection.OpenWithTransactionAsync(token))
                {
                    if (_recreateTables)
                    {
                        var sql = _generator.TableDeleteStatement();
                        result = await RunQuery(sql, c, token);
                    }

                    result = await CreateObjects(c, token);
                    if (!result.Success)
                    {
                        return result;
                    }

                    c.Commit();
                    _initialized = true;
                }

                return result;
            }
        }

        public ValueTask<bool> ExistsAsync(CancellationToken token)
        {
            return new ValueTask<bool>(_initialized);
        }

        protected virtual async Task<Result> CreateObjects(DbConnectionWrapper c, CancellationToken token)
        {
            Result result;

            // Check for function. If schema missing, function is missing too.
            var schemaExists = await _metaRepo.SchemaExists(_schema.Schema, token);
            var functionExists = schemaExists.Success ? await _metaRepo.FunctionNameExists(_generator.RowCreateName(), token) : schemaExists;

            if (!functionExists.Success || _recreateTables)
            {
                var taskSqls = new List<string>()
                {
                    _generator.TableCreateStatement(),
                    _generator.RowCreateProcedure(),
                    _generator.RowCreateMultipleProcedure(),
                    _generator.RowReadProcedure(),
                    _generator.RowUpdateProcedure(),
                    _generator.RowUpdateMultipleProcedure(),
                    _generator.RowDeleteProcedure(),
                    _generator.RowDeleteMultipleProcedure(),
                };

                foreach (var sql in taskSqls)
                {
                    result = await RunQuery(sql, c, token);
                    if (!result.Success)
                    {
                        return result;
                    }
                }
            }

            return Result.CreateSuccess();
        }

        public virtual async ValueTask<Result<TData>> Create(TData value, CancellationToken token, IDbConnection? connection = null)
        {
            return await RunSingleFunction(_generator.RowCreateName(), _serializer.SerializeCreate(value), token, connection, Reader);
        }

        public virtual async ValueTask<Result<IEnumerable<TData>>> CreateMultiple(
            IEnumerable<TData> values,
            bool ignoreErrors,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            if (!values.Any())
            {
                return Result<IEnumerable<TData>>.CreateSuccess(new List<TData>());
            }

            var modelCount = values.Count();
            var results = new List<Result<TData>>(modelCount);

            // Use unsafe procedure with a global transaction instead of safe function with N local transactions.
            // TODO: when table-valued functions start being supported in insight and npgsql, use those
            if (connection != null)
            {
                var result = await CreateMultipleInternal(values, results, ignoreErrors, connection, token);
                if (!result.Success)
                {
                    return Result<IEnumerable<TData>>.CreateFailure(result);
                }
            }
            else
            {
                using (var c = await _connection.OpenWithTransactionAsync(token))
                {
                    var result = await CreateMultipleInternal(values, results, ignoreErrors, c, token);

                    if (result.Success)
                    {
                        c.Commit();
                    }
                    else
                    {
                        c.Rollback();
                        return Result<IEnumerable<TData>>.CreateFailure(result);
                    }
                }
            }

            return Result<IEnumerable<TData>>.CreateSuccess(results.Select(x => x.Success ? x.Data! : default));
        }

        private async Task<Result> CreateMultipleInternal(
            IEnumerable<TData> values,
            List<Result<TData>> results,
            bool ignoreErrors,
            IDbConnection connection,
            CancellationToken token)
        {
            var i = 0;

            foreach (var value in values)
            {
                results.Add(await RunSingleProcedure(
                    "CALL",
                    _generator.RowCreateMultipleName(),
                string.Join(", ", Schema.Columns.Select(x => $"@{x.Title}")),
                    _serializer.SerializeCreateMultiple(value), token, connection, Reader));
                if (!results[i].Success && !ignoreErrors)
                {
                    return Result.CreateFailure(results[i]);
                }

                ++i;
            }

            return Result.CreateSuccess();
        }

        public virtual async ValueTask<Result<TData>> Read(TData value, CancellationToken token, IDbConnection? connection = null)
        {
            return await RunSingleFunction(_generator.RowReadName(), _serializer.SerializeRead(value), token, connection, Reader);
        }

        public virtual async ValueTask<Result<TData>> Update(TData value, CancellationToken token, IDbConnection? connection = null)
        {
            return await RunSingleFunction(_generator.RowUpdateName(), _serializer.SerializeUpdate(value), token, connection, Reader);
        }

        public virtual async ValueTask<Result<TData>> Delete(TData value, CancellationToken token, IDbConnection? connection = null)
        {
            return await RunSingleFunction(_generator.RowDeleteName(), _serializer.SerializeDelete(value), token, connection, Reader);
        }

        public virtual ValueTask<Result<int>> Clear(CancellationToken token, IDbConnection? connection = null)
        {
            throw new NotImplementedException();
        }

        public virtual async ValueTask<Result> TableCreateIfNotExists(CancellationToken token)
        {
            return await EnsureExistsAsync(token);
        }

        public virtual async ValueTask<Result> TableDrop(CancellationToken token)
        {
            var sql = _generator.TableDeleteStatement();

            using (var c = await _connection.OpenWithTransactionAsync(token))
            {
                var result = await RunQuery(sql, c, token);

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

        public virtual async ValueTask<Result<IEnumerable<TData>>> UpdateMultiple(IEnumerable<TData> models, CancellationToken token, IDbConnection? connection = null)
        {
            if (!models.Any())
            {
                return Result<IEnumerable<TData>>.CreateSuccess(new List<TData>());
            }

            var modelCount = models.Count();
            var results = new List<Result<TData>>(modelCount);

            // Use unsafe procedure with a global transaction instead of safe function with N local transactions.
            // TODO: when table-valued functions start being supported in insight and npgsql, use those
            if (connection != null)
            {
                var result = await UpdateMultipleInternal(models, results, connection, token);
                if (!result.Success)
                {
                    return Result<IEnumerable<TData>>.CreateFailure(result);
                }
            }
            else
            {
                using (var c = await _connection.OpenWithTransactionAsync(token))
                {
                    var result = await UpdateMultipleInternal(models, results, c, token);

                    if (result.Success)
                    {
                        c.Commit();
                    }
                    else
                    {
                        c.Rollback();
                        return Result<IEnumerable<TData>>.CreateFailure(result);
                    }
                }
            }

            return Result<IEnumerable<TData>>.CreateSuccess(results.Select(x => x.Success ? x.Data! : default));
        }

        private async Task<Result> UpdateMultipleInternal(
            IEnumerable<TData> values,
            List<Result<TData>> results,
            IDbConnection connection,
            CancellationToken token)
        {
            var i = 0;

            foreach (var value in values)
            {
                var updateResult = await RunSingleProcedure(
                    "CALL",
                    _generator.RowUpdateMultipleName(),
                    string.Join(", ", Schema.Columns.Select(x => $"@{x.Title}")),
                    _serializer.SerializeUpdateMultiple(value), token, connection);

                if (!updateResult.Success)
                {
                    return Result.CreateFailure(updateResult.ErrorMessage!);
                }

                results.Add(Result<TData>.CreateSuccess(value));
                ++i;
            }

            return Result.CreateSuccess();
        }

        public virtual async ValueTask<Result> DeleteMultiple(IEnumerable<TData> models, CancellationToken token, IDbConnection? connection = null)
        {
            if (!models.Any())
            {
                return Result.CreateSuccess();
            }

            // Use unsafe procedure with a global transaction instead of safe function with N local transactions.
            // TODO: when table-valued functions start being supported in insight and npgsql, use those
            if (connection != null)
            {
                return await DeleteMultipleInternal(models, connection, token);
            }
            else
            {
                using (var c = await _connection.OpenWithTransactionAsync(token))
                {
                    var result = await DeleteMultipleInternal(models, c, token);

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

        private async Task<Result> DeleteMultipleInternal(
            IEnumerable<TData> values,
            IDbConnection connection,
            CancellationToken token)
        {
            foreach (var value in values)
            {
                var result = await RunSingleProcedure(
                    "CALL",
                    _generator.RowDeleteMultipleName(),
                    string.Join(", ", Schema.Columns.Select(x => $"@{x.Title}")),
                    _serializer.SerializeDeleteMultiple(value), token, connection);

                if (!result.Success)
                {
                    return result;
                }
            }

            return Result.CreateSuccess();
        }
    }
}
