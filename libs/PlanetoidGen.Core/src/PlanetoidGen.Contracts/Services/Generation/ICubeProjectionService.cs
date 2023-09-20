using PlanetoidGen.Contracts.Models.Coordinates;

namespace PlanetoidGen.Contracts.Services.Generation
{
    public interface ICubeProjectionService
    {
        enum FaceSide : int
        {
            FaceFront = 0,
            /// <summary>
            /// +lon, east.
            /// </summary>
            FaceRight = 1,
            FaceBack = 2,
            /// <summary>
            /// -lon, west.
            /// </summary>
            FaceLeft = 3,
            FaceTop = 4,
            FaceBottom = 5,
        }

        /// <summary>
        /// Convert geocentric geographic coordinates to the
        /// Quadrilateralized Spherical Cube projection.
        /// </summary>
        /// <param name="lon">Geocentric longitude in radians.</param>
        /// <param name="lat">Geocentric latitude in radians.</param>
        /// <returns>A tuple of face, x, y.</returns>
        (FaceSide, double, double) Forward(double lon, double lat);

        /// <summary>
        /// Convert Quadrilateralized Spherical Cube projection coordinates
        /// to the geocentric geographic coordinate system.
        /// </summary>
        /// <param name="face">Cube face on which the point lies.</param>
        /// <param name="x">Horizontal coordinate.</param>
        /// <param name="y">Vertical coordinate.</param>
        /// <returns>A tuple of lon, lat in radians.</returns>
        (double, double) Inverse(FaceSide face, double x, double y);

        /// <summary>
        /// Convert from a spherical coordinate model to a bounding box model
        /// containing the four corners for the tile.
        /// </summary>
        /// <param name="model">Geodesic spherical coordinate model of a point inside the bounding box.</param>
        /// <returns>Bounding box coordinates.</returns>
        BoundingBoxCoordinateModel ToBoundingBox(CubicCoordinateModel point, ICoordinateMappingService parent);
    }
}
