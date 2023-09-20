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
    public class GeometricDynamicRepositoryFactory<TData> :
        UniversalDynamicRepositoryFactory<TData>,
        IGeometricDynamicRepositoryFactory<TData>,
        IDynamicRepositoryFactory<TData>
    {
        public GeometricDynamicRepositoryFactory(
            DbConnectionStringBuilder connection,
            IMetaProcedureRepository meta,
            IConfiguration configuration) : base(
                connection, meta, configuration)
        {
        }

        public Result<IGeometricDynamicRepository<TData>> CreateGeometricRepository(TableSchema schema)
        {
            return Result<IGeometricDynamicRepository<TData>>.CreateSuccess(CreateInstance(schema));
        }

        public override Result<IDynamicRepository<TData>> CreateRepository(TableSchema schema)
        {
            return Result<IDynamicRepository<TData>>.CreateSuccess(CreateInstance(schema));
        }

        protected GeometricDynamicRepository<TData> CreateInstance(TableSchema schema)
        {
            return new GeometricDynamicRepository<TData>(
                    _connection,
                    _meta,
                    new UniversalTableProcedureGenerator(schema),
                    new EmitTypeRowSerializer<TData>(schema),
                    _configuration);
        }
    }
}
