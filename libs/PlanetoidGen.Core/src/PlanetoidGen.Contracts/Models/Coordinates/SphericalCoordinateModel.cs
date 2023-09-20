using System;

namespace PlanetoidGen.Contracts.Models.Coordinates
{
    public class SphericalCoordinateModel
    {
        public int PlanetoidId { get; }

        /// <summary>
        /// Longtitute in radians.
        /// </summary>
        public double Longtitude { get; }

        /// <summary>
        /// Latitude in radians.
        /// </summary>
        public double Latitude { get; }

        public short Zoom { get; }

        /// <summary>
        /// Creates a new instance of spherical coordinates.
        /// </summary>
        /// <param name="planetoidId">Id of the planetoid in which the coordinates are set.</param>
        /// <param name="longtitude">Longtitude in radians.</param>
        /// <param name="latitude">Latitude in radians.</param>
        /// <param name="zoom">Zoom value, 0 covers entire planetoid.</param>
        public SphericalCoordinateModel(int planetoidId, double longtitude, double latitude, short zoom)
        {
            PlanetoidId = planetoidId;
            Longtitude = longtitude;
            Latitude = latitude;
            Zoom = zoom;
        }

        public SphericalCoordinateModel(SphericalCoordinateModel other)
        {
            PlanetoidId = other.PlanetoidId;
            Longtitude = other.Longtitude;
            Latitude = other.Latitude;
            Zoom = other.Zoom;
        }

        public override string ToString()
        {
            return $"P={PlanetoidId}, Zoom={Zoom}, Lat={Latitude}, Lon={Longtitude}";
        }

        public CoordinateModel ToCoordinatesRadians()
        {
            return new CoordinateModel(Longtitude, Latitude, 0.0);
        }

        public CoordinateModel ToCoordinatesDegrees()
        {
            return new CoordinateModel(Longtitude * 180.0 / Math.PI, Latitude * 180.0 / Math.PI, 0.0);
        }
    }
}
