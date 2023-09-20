using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Enums;
using System;

namespace PlanetoidGen.BusinessLogic.Common.Services.Generation
{
    public class CoordinateMappingService : ICoordinateMappingService
    {
        private readonly ICubeProjectionService _cubeProjectionService;

        public CoordinateMappingService(ICubeProjectionService cubeProjectionService)
        {
            _cubeProjectionService = cubeProjectionService;
        }

        public SphericalCoordinateModel RelativeTile(
            SphericalCoordinateModel model,
            RelativeTileDirectionType relative)
        {
            if (relative == RelativeTileDirectionType.Current)
            {
                return new SphericalCoordinateModel(model);
            }

            var oldCubic = ToCubic(model);

            var newCubic = RelativeTileCalculation.GetRelativeTile(oldCubic, relative);

            return ToSpherical(newCubic);
        }

        public CubicCoordinateModel RelativeTile(
            CubicCoordinateModel model,
            RelativeTileDirectionType relative)
        {
            return relative == RelativeTileDirectionType.Current
                ? new CubicCoordinateModel(model)
                : RelativeTileCalculation.GetRelativeTile(model, relative);
        }

        public double TileSizeCubic(short zoom)
        {
            return 2.0 / (1L << zoom);
        }

        public double SphericalTileSizeRadians(short zoom)
        {
            // (1 << zoom) tiles per cube face, 4 cube faces per 2 PI radians
            return Math.PI / (1L << (zoom + 1));
        }

        public CubicCoordinateModel ToCubic(PlanarCoordinateModel model)
        {
            double max = 1L << model.Z;
            var face = model.Y % 6L;
            var lx = model.X;
            var ly = (model.Y - face) / 6L;
            var dx = lx / max;
            var dy = ly / max;
            var x = dx * 2.0 - 1.0;
            var y = dy * 2.0 - 1.0;

            return new CubicCoordinateModel(model.PlanetoidId, (short)face, model.Z, x, y);
        }

        public CubicCoordinateModel ToCubic(SphericalCoordinateModel model)
        {
            (var face, var x, var y) = _cubeProjectionService.Forward(model.Longtitude, model.Latitude);

            return new CubicCoordinateModel(model.PlanetoidId, (short)face, model.Zoom, x, y);
        }

        public PlanarCoordinateModel ToPlanar(CubicCoordinateModel model)
        {
            double max = 1L << model.Z;
            var dx = (model.X + 1.0) * 0.5;
            var dy = (model.Y + 1.0) * 0.5;
            var x = (long)(dx * max);
            var y = (long)(dy * max) * 6L + model.Face;

            return new PlanarCoordinateModel(model.PlanetoidId, model.Z, x, y);
        }

        public PlanarCoordinateModel ToPlanar(SphericalCoordinateModel model)
        {
            var cubicModelResult = ToCubic(model);

            return ToPlanar(cubicModelResult);
        }

        public SphericalCoordinateModel ToSpherical(CubicCoordinateModel model)
        {
            (var lon, var lat) = _cubeProjectionService.Inverse((ICubeProjectionService.FaceSide)model.Face, model.X, model.Y);

            return new SphericalCoordinateModel(model.PlanetoidId, lon, lat, model.Z);
        }

        public SphericalCoordinateModel ToSpherical(PlanarCoordinateModel model)
        {
            var cubicModelResult = ToCubic(model);

            return ToSpherical(cubicModelResult);
        }

        public AxisAlignedBoundingBoxCoordinateModel ToAxisAlignedBoundingBox(SphericalCoordinateModel point)
        {
            return ToAxisAlignedBoundingBox(ToBoundingBox(point));
        }

        public AxisAlignedBoundingBoxCoordinateModel ToAxisAlignedBoundingBox(BoundingBoxCoordinateModel model)
        {
            var coordinates = model.GetCoordinateArray();

            var minLon = Math.Min(Math.Min(coordinates[0].X, coordinates[1].X), Math.Min(coordinates[2].X, coordinates[3].X));
            var maxLon = Math.Max(Math.Max(coordinates[0].X, coordinates[1].X), Math.Max(coordinates[2].X, coordinates[3].X));
            var minLat = Math.Min(Math.Min(coordinates[0].Y, coordinates[1].Y), Math.Min(coordinates[2].Y, coordinates[3].Y));
            var maxLat = Math.Min(Math.Max(coordinates[0].Y, coordinates[1].Y), Math.Max(coordinates[2].Y, coordinates[3].Y));

            return new AxisAlignedBoundingBoxCoordinateModel(model.PlanetoidId, minLon, maxLon, minLat, maxLat);
        }

        public BoundingBoxCoordinateModel ToBoundingBox(SphericalCoordinateModel point)
        {
            return ToBoundingBox(ToCubic(point));
        }

        public BoundingBoxCoordinateModel ToBoundingBox(CubicCoordinateModel point)
        {
            return _cubeProjectionService.ToBoundingBox(point, this);
        }

        private class RelativeTileCalculation
        {
            public static CubicCoordinateModel GetRelativeTile(CubicCoordinateModel model, RelativeTileDirectionType relative)
            {
                var z = model.Z;
                var max = (1L << z);
                var tileSize = 2.0 / max;

                int face = model.Face;

                long nface = face;
                var nx = model.X;
                var ny = model.Y;

                switch ((ICubeProjectionService.FaceSide)face)
                {
                    case ICubeProjectionService.FaceSide.FaceTop:
                        RelativeTileFaceTop(ref nface, ref nx, ref ny, tileSize, relative);
                        break;
                    case ICubeProjectionService.FaceSide.FaceBottom:
                        RelativeTileFaceBottom(ref nface, ref nx, ref ny, tileSize, relative);
                        break;
                    case ICubeProjectionService.FaceSide.FaceFront:
                        RelativeTileFaceFront(ref nface, ref nx, ref ny, tileSize, relative);
                        break;
                    case ICubeProjectionService.FaceSide.FaceLeft:
                        RelativeTileFaceWest(ref nface, ref nx, ref ny, tileSize, relative);
                        break;
                    case ICubeProjectionService.FaceSide.FaceBack:
                        RelativeTileFaceBack(ref nface, ref nx, ref ny, tileSize, relative);
                        break;
                    case ICubeProjectionService.FaceSide.FaceRight:
                        RelativeTileFaceEast(ref nface, ref nx, ref ny, tileSize, relative);
                        break;
                    default:
                        return model;
                }

                return new CubicCoordinateModel(model.PlanetoidId, (short)nface, z, nx, ny);
            }

            private static void RelativeTileFaceTop(ref long nface, ref double nx, ref double ny, double tileSize, RelativeTileDirectionType relative)
            {
                switch (relative)
                {
                    case RelativeTileDirectionType.Current:
                        break;
                    case RelativeTileDirectionType.Up:
                        if (ny < 1.0 - tileSize)
                        {
                            ny += tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceBack;
                            nx = -nx;
                            ny = 2.0 - ny - tileSize;
                        }

                        break;
                    case RelativeTileDirectionType.Right:
                        if (nx < 1.0 - tileSize)
                        {
                            nx += tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceRight;
                            (nx, ny) = (ny, 2.0 - nx - tileSize);
                        }

                        break;
                    case RelativeTileDirectionType.Down:
                        if (ny > -1.0 + tileSize)
                        {
                            ny -= tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceFront;
                            ny = 2.0 + ny - tileSize;
                        }

                        break;
                    case RelativeTileDirectionType.Left:
                        if (nx > -1.0 + tileSize)
                        {
                            nx -= tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceLeft;
                            (nx, ny) = (-ny, 2.0 + nx - tileSize);
                        }

                        break;
                }
            }

            private static void RelativeTileFaceBottom(ref long nface, ref double nx, ref double ny, double tileSize, RelativeTileDirectionType relative)
            {
                switch (relative)
                {
                    case RelativeTileDirectionType.Current:
                        break;
                    case RelativeTileDirectionType.Up:
                        if (ny < 1.0 - tileSize)
                        {
                            ny += tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceFront;
                            ny = -2.0 + ny + tileSize;
                        }

                        break;
                    case RelativeTileDirectionType.Right:
                        if (nx < 1.0 - tileSize)
                        {
                            nx += tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceRight;
                            (nx, ny) = (-ny, -2.0 + nx + tileSize);
                        }

                        break;
                    case RelativeTileDirectionType.Down:
                        if (ny > -1.0 + tileSize)
                        {
                            ny -= tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceBack;
                            nx = -nx;
                            ny = -2.0 - ny + tileSize;
                        }

                        break;
                    case RelativeTileDirectionType.Left:
                        if (nx > -1.0 + tileSize)
                        {
                            nx -= tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceLeft;
                            (nx, ny) = (ny, -2.0 - nx + tileSize);
                        }

                        break;
                }
            }

            private static void RelativeTileFaceFront(ref long nface, ref double nx, ref double ny, double tileSize, RelativeTileDirectionType relative)
            {
                switch (relative)
                {
                    case RelativeTileDirectionType.Current:
                        break;
                    case RelativeTileDirectionType.Up:
                        if (ny < 1.0 - tileSize)
                        {
                            ny += tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceTop;
                            ny = -2.0 + ny + tileSize;
                        }

                        break;
                    case RelativeTileDirectionType.Right:
                        if (nx < 1.0 - tileSize)
                        {
                            nx += tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceRight;
                            nx = -2.0 + nx + tileSize;
                        }

                        break;
                    case RelativeTileDirectionType.Down:
                        if (ny > -1.0 + tileSize)
                        {
                            ny -= tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceBottom;
                            ny = 2.0 + ny - tileSize;
                        }

                        break;
                    case RelativeTileDirectionType.Left:
                        if (nx > -1.0 + tileSize)
                        {
                            nx -= tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceLeft;
                            nx = 2.0 + nx - tileSize;
                        }

                        break;
                }
            }

            private static void RelativeTileFaceWest(ref long nface, ref double nx, ref double ny, double tileSize, RelativeTileDirectionType relative)
            {
                switch (relative)
                {
                    case RelativeTileDirectionType.Current:
                        break;
                    case RelativeTileDirectionType.Up:
                        if (ny < 1.0 - tileSize)
                        {
                            ny += tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceTop;
                            (nx, ny) = (-2.0 + ny + tileSize, -nx);
                        }

                        break;
                    case RelativeTileDirectionType.Right:
                        if (nx < 1.0 - tileSize)
                        {
                            nx += tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceFront;
                            nx = -2.0 + nx + tileSize;
                        }

                        break;
                    case RelativeTileDirectionType.Down:
                        if (ny > -1.0 + tileSize)
                        {
                            ny -= tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceBottom;
                            (nx, ny) = (-2.0 - ny + tileSize, nx);
                        }

                        break;
                    case RelativeTileDirectionType.Left:
                        if (nx > -1.0 + tileSize)
                        {
                            nx -= tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceBack;
                            nx = 2.0 + nx - tileSize;
                        }

                        break;
                }
            }

            private static void RelativeTileFaceEast(ref long nface, ref double nx, ref double ny, double tileSize, RelativeTileDirectionType relative)
            {
                switch (relative)
                {
                    case RelativeTileDirectionType.Current:
                        break;
                    case RelativeTileDirectionType.Up:
                        if (ny < 1.0 - tileSize)
                        {
                            ny += tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceTop;
                            (nx, ny) = (2.0 - ny - tileSize, nx);
                        }

                        break;
                    case RelativeTileDirectionType.Right:
                        if (nx < 1.0 - tileSize)
                        {
                            nx += tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceBack;
                            nx = -2.0 + nx + tileSize;
                        }

                        break;
                    case RelativeTileDirectionType.Down:
                        if (ny > -1.0 + tileSize)
                        {
                            ny -= tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceBottom;
                            (nx, ny) = (2.0 + ny - tileSize, -nx);
                        }

                        break;
                    case RelativeTileDirectionType.Left:
                        if (nx > -1.0 + tileSize)
                        {
                            nx -= tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceFront;
                            nx = 2.0 + nx - tileSize;
                        }

                        break;
                }
            }

            private static void RelativeTileFaceBack(ref long nface, ref double nx, ref double ny, double tileSize, RelativeTileDirectionType relative)
            {
                switch (relative)
                {
                    case RelativeTileDirectionType.Current:
                        break;
                    case RelativeTileDirectionType.Up:
                        if (ny < 1.0 - tileSize)
                        {
                            ny += tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceTop;
                            nx = -nx;
                            ny = 2.0 - ny - tileSize;
                        }

                        break;
                    case RelativeTileDirectionType.Right:
                        if (nx < 1.0 - tileSize)
                        {
                            nx += tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceLeft;
                            nx = -2.0 + nx + tileSize;
                        }

                        break;
                    case RelativeTileDirectionType.Down:
                        if (ny > -1.0 + tileSize)
                        {
                            ny -= tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceBottom;
                            nx = -nx;
                            ny = -2.0 - ny + tileSize;
                        }

                        break;
                    case RelativeTileDirectionType.Left:
                        if (nx > -1.0 + tileSize)
                        {
                            nx -= tileSize;
                        }
                        else
                        {
                            nface = (long)ICubeProjectionService.FaceSide.FaceRight;
                            nx = 2.0 + nx - tileSize;
                        }

                        break;
                }
            }
        }
    }
}
