using System;
using System.Data;

namespace PlanetoidGen.Contracts.Repositories.Dynamic
{
    /// <summary>
    /// A factory to serialize and deserialize data from rows to models.
    /// </summary>
    public interface IRowSerializer<TData>
    {
        object SerializeCreate(TData data);

        object SerializeCreateMultiple(TData data);

        object SerializeRead(TData data);

        object SerializeUpdate(TData data);

        object SerializeUpdateMultiple(TData data);

        object SerializeDelete(TData data);

        object SerializeDeleteMultiple(TData data);

        Func<IDataReader, TData>? Deserializer { get; }
    }
}
