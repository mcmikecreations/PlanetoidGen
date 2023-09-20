using System;
using System.Data;

namespace PlanetoidGen.Contracts.Repositories
{
    public interface INamedRepository<TData>
    {
        string Name { get; }

        Func<IDataReader, TData>? Reader { get; }
    }
}
