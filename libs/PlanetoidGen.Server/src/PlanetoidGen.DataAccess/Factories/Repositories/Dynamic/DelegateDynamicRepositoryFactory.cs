using Microsoft.Extensions.Configuration;
using PlanetoidGen.Contracts.Factories.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Repositories.Dynamic;
using System;
using System.Data.Common;

namespace PlanetoidGen.DataAccess.Factories.Repositories.Dynamic
{
    public class DelegateDynamicRepositoryFactory<TData> : IDynamicRepositoryFactory<TData>
    {
        protected readonly DbConnectionStringBuilder _connection;
        protected readonly IMetaProcedureRepository _meta;
        protected readonly IConfiguration _configuration;

        protected readonly Func<TableSchema, ITableProcedureGenerator> _tableProcGenDelegate;
        protected readonly Func<TableSchema, IRowSerializer<TData>> _rowSerializerDelegate;

        public DelegateDynamicRepositoryFactory(
            DbConnectionStringBuilder connection,
            IMetaProcedureRepository meta,
            IConfiguration configuration,
            Func<TableSchema, ITableProcedureGenerator>? tableProcGenDelegate = null,
            Func<TableSchema, IRowSerializer<TData>>? rowSerializerDelegate = null)
        {
            _connection = connection;
            _meta = meta;
            _configuration = configuration;

            _tableProcGenDelegate = tableProcGenDelegate ?? ((schema) => new UniversalTableProcedureGenerator(schema));
            _rowSerializerDelegate = rowSerializerDelegate ?? ((schema) => new AnonymousTypeRowSerializer<TData>(schema));
        }

        public virtual Result<IDynamicRepository<TData>> CreateRepository(TableSchema schema)
        {
            return Result<IDynamicRepository<TData>>.CreateSuccess(
                new DynamicRepository<TData>(
                    _connection,
                    _meta,
                    _tableProcGenDelegate(schema),
                    _rowSerializerDelegate(schema),
                    _configuration));
        }
    }
}
