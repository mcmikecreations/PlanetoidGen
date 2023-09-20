using Assimp;
using NetTopologySuite.Geometries;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations
{
    public class AssimpGeometryConversionService : IAssimpGeometryConversionService
    {
        private readonly IGeometryConversionService _geometryConversionService;

        public AssimpGeometryConversionService(IGeometryConversionService geometryConversionService)
        {
            _geometryConversionService = geometryConversionService;
        }

        public async ValueTask<Vector3D[]> ToAssimpVectors(
            IEnumerable<Coordinate> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            CancellationToken token,
            int? srcProjection = null,
            int? dstProjection = null)
        {
            var coords = coordinates.Select(c => new CoordinateModel(
                double.IsFinite(c.X) ? c.X : 0.0,
                double.IsFinite(c.Y) ? c.Y : 0.0,
                double.IsFinite(c.Z) ? c.Z : 0.0));

            return (await _geometryConversionService.ToAssimpVectors(coords, planetoid, yUp, token, srcProjection, dstProjection))
                .Select(c => new Vector3D((float)c[0], (float)c[1], (float)c[2]))
                .ToArray();
        }

        public async ValueTask<IList<Vector3D[]>> ToAssimpVectors(
            IEnumerable<Coordinate[]> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            CancellationToken token,
            int? srcProjection = null,
            int? dstProjection = null)
        {
            var tasks = coordinates
                    .Select(async x => await ToAssimpVectors(x, planetoid, yUp, token, srcProjection, dstProjection));

            var results = await Task.WhenAll(tasks);

            return results.ToList();
        }

        public IList<Vector3D[]> ToAssimpVectors(
            IEnumerable<Coordinate[]> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            SpatialReferenceSystemModel srcProjection,
            SpatialReferenceSystemModel dstProjection)
        {
            return coordinates
                    .Select(x => ToAssimpVectors(x, planetoid, yUp, srcProjection, dstProjection))
                    .ToList();
        }

        public Vector3D[] ToAssimpVectors(
            IEnumerable<Coordinate> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            SpatialReferenceSystemModel srcProjection,
            SpatialReferenceSystemModel dstProjection)
        {
            var coords = coordinates.Select(c => new CoordinateModel(
                double.IsFinite(c.X) ? c.X : 0.0,
                double.IsFinite(c.Y) ? c.Y : 0.0,
                double.IsFinite(c.Z) ? c.Z : 0.0));

            return _geometryConversionService.ToAssimpVectors(coords, planetoid, yUp, srcProjection, dstProjection)
                .Select(c => new Vector3D((float)c[0], (float)c[1], (float)c[2]))
                .ToArray();
        }
    }
}
