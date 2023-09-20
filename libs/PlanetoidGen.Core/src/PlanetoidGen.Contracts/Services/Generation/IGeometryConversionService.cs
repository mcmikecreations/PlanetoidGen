using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Services.Generation
{
    public interface IGeometryConversionService
    {
        ValueTask<IEnumerable<double[]>> ToAssimpVectors(
            IEnumerable<CoordinateModel> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            CancellationToken token,
            int? srcProjection = null,
            int? dstProjection = null);

        ValueTask<IList<IEnumerable<double[]>>> ToAssimpVectors(
            IEnumerable<CoordinateModel[]> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            CancellationToken token,
            int? srcProjection = null,
            int? dstProjection = null);

        IEnumerable<double[]> ToAssimpVectors(
            IEnumerable<CoordinateModel> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            SpatialReferenceSystemModel srcProjection,
            SpatialReferenceSystemModel dstProjection);

        IList<IEnumerable<double[]>> ToAssimpVectors(
            IEnumerable<CoordinateModel[]> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            SpatialReferenceSystemModel srcProjection,
            SpatialReferenceSystemModel dstProjection);

        /// <returns>A partially filled SRS model with srid and authority set to defaults.</returns>
        SpatialReferenceSystemModel GenerateSRSModelGeographic(PlanetoidInfoModel planetoid);

        SpatialReferenceSystemModel GenerateSRSModelProjected(PlanetoidInfoModel planetoid);

        string GenerateWKTStringGeographic(PlanetoidInfoModel planetoid);

        string GenerateWKTStringProjected(PlanetoidInfoModel planetoid);

        string GenerateProj4StringGeographic(PlanetoidInfoModel planetoid);

        string GenerateProj4StringProjected(PlanetoidInfoModel planetoid);
    }
}
