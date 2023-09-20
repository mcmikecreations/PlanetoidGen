using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Generic;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Dynamic
{
    public interface IGeometricDynamicRepository<TData> : IDynamicRepository<TData>
    {
        /// <summary>
        /// Read multiple objects based on the bounding box calling (integer planetoidId, geom envelope).
        /// </summary>
        /// <param name="boundingBox">The bounding box containing the planetoid id and envelope to query.</param>
        /// <returns>A possibly empty collection of items if successful query, error otherwise.</returns>
        ValueTask<Result<IEnumerable<TData>>> ReadMultipleByBoundingBox(AxisAlignedBoundingBoxCoordinateModel boundingBox, CancellationToken token, IDbConnection? connection = null);

        ValueTask<Result<IEnumerable<TData>>> ReadMultipleByBoundingBox(BoundingBoxCoordinateModel boundingBox, CancellationToken token, IDbConnection? connection = null);
    }
}
