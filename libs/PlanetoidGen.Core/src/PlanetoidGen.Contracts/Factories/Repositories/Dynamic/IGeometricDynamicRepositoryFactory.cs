using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Dynamic;

namespace PlanetoidGen.Contracts.Factories.Repositories.Dynamic
{
    public interface IGeometricDynamicRepositoryFactory<TData> : IDynamicRepositoryFactory<TData>
    {
        Result<IGeometricDynamicRepository<TData>> CreateGeometricRepository(TableSchema schema);
    }
}
