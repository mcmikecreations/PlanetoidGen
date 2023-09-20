using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Domain.Enums;

namespace PlanetoidGen.Contracts.Services.Generation
{
    public interface ICoordinateMappingService
    {
        double TileSizeCubic(short zoom);

        double SphericalTileSizeRadians(short zoom);

        CubicCoordinateModel ToCubic(PlanarCoordinateModel model);

        CubicCoordinateModel ToCubic(SphericalCoordinateModel model);

        PlanarCoordinateModel ToPlanar(CubicCoordinateModel model);

        SphericalCoordinateModel ToSpherical(CubicCoordinateModel model);

        /// <summary>
        /// Convert from geodesic spherical coords to flat projected ones bypassing the cubic coords.
        /// </summary>
        /// <param name="model">Geodesic spherical coordinate model.</param>
        /// <returns>Flat plane coordinates.</returns>
        PlanarCoordinateModel ToPlanar(SphericalCoordinateModel model);

        /// <summary>
        /// Convert from flat projected coords to geodesic spherical ones bypassing the cubic coords.
        /// </summary>
        /// <param name="model">Flat plane coordinate model.</param>
        /// <returns>Geodesic spherical coordinates.</returns>
        SphericalCoordinateModel ToSpherical(PlanarCoordinateModel model);

        /// <summary>
        /// Convert from a spherical coordinate model to a bounding box model
        /// containing the min and max longtitude and latitude for the tile.
        /// </summary>
        /// <param name="model">Geodesic spherical coordinate model of a point inside the bounding box.</param>
        /// <returns>Bounding box coordinates.</returns>
        AxisAlignedBoundingBoxCoordinateModel ToAxisAlignedBoundingBox(SphericalCoordinateModel point);

        AxisAlignedBoundingBoxCoordinateModel ToAxisAlignedBoundingBox(BoundingBoxCoordinateModel model);

        /// <summary>
        /// Convert from a spherical coordinate model to a bounding box model
        /// containing the four corners for the tile.
        /// </summary>
        /// <param name="model">Geodesic spherical coordinate model of a point inside the bounding box.</param>
        /// <returns>Bounding box coordinates.</returns>
        BoundingBoxCoordinateModel ToBoundingBox(SphericalCoordinateModel point);

        /// <summary>
        /// Convert from a Quadrilateralized Spherical Cube coordinate model to a bounding box model
        /// containing the four corners for the tile.
        /// </summary>
        /// <param name="model">Quadrilateralized Spherical Cube coordinate model of a point inside the bounding box.</param>
        /// <returns>Bounding box coordinates.</returns>
        BoundingBoxCoordinateModel ToBoundingBox(CubicCoordinateModel point);

        SphericalCoordinateModel RelativeTile(SphericalCoordinateModel model, RelativeTileDirectionType relative);

        CubicCoordinateModel RelativeTile(CubicCoordinateModel model, RelativeTileDirectionType relative);
    }
}
