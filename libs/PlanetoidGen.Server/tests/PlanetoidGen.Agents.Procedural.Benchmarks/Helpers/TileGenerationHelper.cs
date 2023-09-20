using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Enums;

namespace PlanetoidGen.Agents.Procedural.Benchmarks.Helpers
{
    public static class TileGenerationHelper
    {
        public static IEnumerable<SphericalLODCoordinateModel> GenerateTilesForRequest(int tileCountRow, SphericalCoordinateModel baseTile, ICoordinateMappingService coordinateMappingService)
        {
            var baseTilePivot = coordinateMappingService.ToCubic(/*coordinateMappingService.ToPlanar(*/baseTile/*)*/);
            //var tileSizeCubic = coordinateMappingService.TileSizeCubic(baseTile.Zoom);

            // Get the center of the tile to avoid tile boundary rounding errors
            //baseTilePivot = new CubicCoordinateModel(baseTilePivot.PlanetoidId, baseTilePivot.Face, baseTilePivot.Z, baseTilePivot.X + tileSizeCubic / 2.0, baseTilePivot.Y / 2.0);

            var tiles = new List<CubicCoordinateModel>() { baseTilePivot };

            // For 4 tile row add 1 tile to the right
            for (int i = 0; i < Convert.ToInt32(Math.Ceiling(tileCountRow / 2f)) - 1; ++i)
            {
                tiles.Add(coordinateMappingService.RelativeTile(tiles.Last(), RelativeTileDirectionType.Right));
            }

            // For 4 tile row add 2 tiles to the left
            for (int i = 0; i < Convert.ToInt32(Math.Floor(tileCountRow / 2f)); ++i)
            {
                tiles.Insert(0, coordinateMappingService.RelativeTile(tiles.First(), RelativeTileDirectionType.Left));
            }

            tiles = tiles
                .SelectMany(x =>
                {
                    var tilesLocal = new List<CubicCoordinateModel>() { x };

                    // For 4 tile row add 1 tile up
                    for (int i = 0; i < Convert.ToInt32(Math.Ceiling(tileCountRow / 2f)) - 1; ++i)
                    {
                        tilesLocal.Add(coordinateMappingService.RelativeTile(tilesLocal.Last(), RelativeTileDirectionType.Up));
                    }

                    // For 4 tile row add 2 tiles down
                    for (int i = 0; i < Convert.ToInt32(Math.Floor(tileCountRow / 2f)); ++i)
                    {
                        tilesLocal.Insert(0, coordinateMappingService.RelativeTile(tilesLocal.First(), RelativeTileDirectionType.Down));
                    }

                    return tilesLocal;
                })
                .Distinct()
                .ToList();

            return tiles.Select(x =>
            {
                var spherical = coordinateMappingService.ToSpherical(x);
                return new SphericalLODCoordinateModel(spherical.Longtitude, spherical.Latitude, spherical.Zoom);
            });
        }
    }
}
