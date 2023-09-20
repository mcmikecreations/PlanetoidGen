using Assimp;
using NetTopologySuite.Geometries;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions
{
    public interface IAssimpGeometryConversionService
    {
        ValueTask<IList<Vector3D[]>> ToAssimpVectors(
            IEnumerable<Coordinate[]> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            CancellationToken token,
            int? srcProjection = null,
            int? dstProjection = null);

        ValueTask<Vector3D[]> ToAssimpVectors(
            IEnumerable<Coordinate> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            CancellationToken token,
            int? srcProjection = null,
            int? dstProjection = null);

        IList<Vector3D[]> ToAssimpVectors(
            IEnumerable<Coordinate[]> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            SpatialReferenceSystemModel srcProjection,
            SpatialReferenceSystemModel dstProjection);

        Vector3D[] ToAssimpVectors(
            IEnumerable<Coordinate> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            SpatialReferenceSystemModel srcProjection,
            SpatialReferenceSystemModel dstProjection);
    }
}
