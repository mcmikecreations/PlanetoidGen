using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Dynamic;

namespace PlanetoidGen.Contracts.Factories.Repositories.Dynamic
{
    public interface IDynamicRepositoryFactory<TData>
    {
        Result<IDynamicRepository<TData>> CreateRepository(TableSchema schema);
    }
}
