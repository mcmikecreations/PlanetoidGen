using PlanetoidGen.BusinessLogic.Common.Services.Generation;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using System;
using Xunit;

namespace PlanetoidGen.BusinessLogic.Tests.Services.Generation
{
    public class CoordinateMappingServiceTests
    {
        private const int PlanetoidId = 0;
        private const double ToleranceSpherical = 1E-4;
        private const double TolerancePlanar = 1E-8;

        [Theory]
        [InlineData(37.5497, 47.0946, 14, typeof(QuadSphereCubeProjectionService))]
        [InlineData(37.5497, 47.0946, 14, typeof(ProjCubeProjectionService))]
        [InlineData(38.6321, 48.7249, 12, typeof(QuadSphereCubeProjectionService))]
        [InlineData(38.6321, 48.7249, 12, typeof(ProjCubeProjectionService))]
        public void GivenDefaultOptions_EnsureCubicErrorMargin(double lonDegrees, double latDegrees, short zoom, Type projectionServiceType)
        {
            var projInstance = Activator.CreateInstance(projectionServiceType) as ICubeProjectionService;
            var coordMapping = new CoordinateMappingService(projInstance!);

            var sphericalCoordinates = new SphericalCoordinateModel(
                PlanetoidId,
                lonDegrees / 180.0 * Math.PI,
                latDegrees / 180.0 * Math.PI,
                zoom);

            var cubicCoordinates = coordMapping.ToCubic(sphericalCoordinates);

            var projectedCoordinates = coordMapping.ToSpherical(cubicCoordinates);

            Assert.InRange(projectedCoordinates.Longtitude, sphericalCoordinates.Longtitude - ToleranceSpherical, sphericalCoordinates.Longtitude + ToleranceSpherical);
            Assert.InRange(projectedCoordinates.Latitude, sphericalCoordinates.Latitude - ToleranceSpherical, sphericalCoordinates.Latitude + ToleranceSpherical);
        }

        [Theory]
        [InlineData(37.5497, 47.0946, 14, typeof(QuadSphereCubeProjectionService))]
        [InlineData(37.5497, 47.0946, 14, typeof(ProjCubeProjectionService))]
        [InlineData(38.6321, 48.7249, 12, typeof(QuadSphereCubeProjectionService))]
        [InlineData(38.6321, 48.7249, 12, typeof(ProjCubeProjectionService))]
        public void GivenDefaultOptions_EnsureMatchingPlanar(double lonDegrees, double latDegrees, short zoom, Type projectionServiceType)
        {
            var projInstance = Activator.CreateInstance(projectionServiceType) as ICubeProjectionService;
            var coordMapping = new CoordinateMappingService(projInstance!);

            var sphericalCoordinates = new SphericalCoordinateModel(
                PlanetoidId,
                lonDegrees / 180.0 * Math.PI,
                latDegrees / 180.0 * Math.PI,
                zoom);

            var planarCoordinates = coordMapping.ToPlanar(sphericalCoordinates);
            var cubicCoordinates = coordMapping.ToCubic(planarCoordinates);
            var size = coordMapping.TileSizeCubic(zoom) * 0.5;
            var projectedCoordinates = coordMapping.ToPlanar(new CubicCoordinateModel(
                PlanetoidId,
                cubicCoordinates.Face,
                cubicCoordinates.Z,
                cubicCoordinates.X + size,
                cubicCoordinates.Y + size));

            Assert.InRange(projectedCoordinates.X, planarCoordinates.X - TolerancePlanar, planarCoordinates.X + TolerancePlanar);
            Assert.InRange(projectedCoordinates.Y, planarCoordinates.Y - TolerancePlanar, planarCoordinates.Y + TolerancePlanar);
        }

        [Theory]
        [InlineData(typeof(QuadSphereCubeProjectionService))]
        [InlineData(typeof(ProjCubeProjectionService))]
        public void GivenDefaultOptions_EnsureCubicRanges(Type projectionServiceType)
        {
            var projInstance = Activator.CreateInstance(projectionServiceType) as ICubeProjectionService;
            var coordMapping = new CoordinateMappingService(projInstance!);

            for (double lon = Math.PI * 0.75; lon > -Math.PI; lon -= Math.PI * 0.125)
            {
                for (double lat = /*-Math.PI * 0.5*/0; lat <= Math.PI * 0.5; lat += Math.PI * 0.125)
                {
                    var lonDeg = lon * 180.0 / Math.PI;
                    var latDeg = lat * 180.0 / Math.PI;

                    var sphericalCoordinates = new SphericalCoordinateModel(
                        PlanetoidId,
                        lon,
                        lat,
                        2);

                    var cubicCoordinates = coordMapping.ToCubic(sphericalCoordinates);

                    Assert.InRange(cubicCoordinates.X, -1.0, 1.0);
                    Assert.InRange(cubicCoordinates.Y, -1.0, 1.0);
                }
            }
        }

        [Theory]
        [InlineData(typeof(QuadSphereCubeProjectionService))]
        [InlineData(typeof(ProjCubeProjectionService))]
        public void GivenDefaultOptions_EnsurePlaneErrorMargin(Type projectionServiceType)
        {
            var projInstance = Activator.CreateInstance(projectionServiceType) as ICubeProjectionService;
            var coordMapping = new CoordinateMappingService(projInstance!);

            short zoom = 14;
            var tileSize = coordMapping.TileSizeCubic(zoom);
            long max = (1L << zoom) - 1;

            var planarCoordinates = new[]
            {
                (new PlanarCoordinateModel(0, zoom, 0, 0), (-1.0, -1.0)),
                (new PlanarCoordinateModel(0, zoom, max, 0), (1.0 - tileSize, -1.0)),
                (new PlanarCoordinateModel(0, zoom, max, max * 6L), (1.0 - tileSize, 1.0 - tileSize)),
                (new PlanarCoordinateModel(0, zoom, 0, max * 6L), (-1.0, 1.0 - tileSize)),
                (new PlanarCoordinateModel(0, zoom, (max + 1L) / 2L, (max + 1L) * 3L), (0.0, 0.0)),
            };

            foreach (var planarCoord in planarCoordinates)
            {
                var cubicCoord = coordMapping.ToCubic(planarCoord.Item1);
                Assert.InRange(cubicCoord.X, planarCoord.Item2.Item1 - TolerancePlanar, planarCoord.Item2.Item1 + TolerancePlanar);
                Assert.InRange(cubicCoord.Y, planarCoord.Item2.Item2 - TolerancePlanar, planarCoord.Item2.Item2 + TolerancePlanar);
            }
        }
    }
}
