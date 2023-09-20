using PlanetoidGen.Contracts.Repositories;
using System;
using System.Data;
using System.Data.Common;

namespace PlanetoidGen.DataAccess.Repositories.Generic
{
    public abstract class RepositoryAccessWrapper<TData> : RepositoryAccessWrapper, INamedRepository<TData>
    {
        public abstract string Name { get; }

        public abstract Func<IDataReader, TData>? Reader { get; }

        public RepositoryAccessWrapper(DbConnectionStringBuilder connection, IStaticRepository? meta)
            : base(connection, meta)
        {
        }
    }
}
