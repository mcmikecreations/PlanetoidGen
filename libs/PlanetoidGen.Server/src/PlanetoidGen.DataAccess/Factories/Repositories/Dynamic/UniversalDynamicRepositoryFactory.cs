using Microsoft.Extensions.Configuration;
using PlanetoidGen.Contracts.Factories.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Repositories.Dynamic;
using System.Data.Common;

namespace PlanetoidGen.DataAccess.Factories.Repositories.Dynamic
{
    public class UniversalDynamicRepositoryFactory<TData> : IDynamicRepositoryFactory<TData>
    {
        protected readonly DbConnectionStringBuilder _connection;
        protected readonly IMetaProcedureRepository _meta;
        protected readonly IConfiguration _configuration;

        public UniversalDynamicRepositoryFactory(
            DbConnectionStringBuilder connection,
            IMetaProcedureRepository meta,
            IConfiguration configuration)
        {
            _connection = connection;
            _meta = meta;
            _configuration = configuration;
        }

        public virtual Result<IDynamicRepository<TData>> CreateRepository(TableSchema schema)
        {
            return Result<IDynamicRepository<TData>>.CreateSuccess(
                new DynamicRepository<TData>(
                    _connection,
                    _meta,
                    new UniversalTableProcedureGenerator(schema),
                    new AnonymousTypeRowSerializer<TData>(schema),
                    _configuration));
        }
    }
}
