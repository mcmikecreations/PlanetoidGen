using System;

namespace PlanetoidGen.Contracts.Models.Coordinates
{
    public class AxisAlignedBoundingBoxCoordinateModel
    {
        public int PlanetoidId { get; }

        /// <summary>
        /// Minimum longtitute in radians.
        /// </summary>
        public double MinLongtitude { get; }

        /// <summary>
        /// Maximum longtitute in radians.
        /// </summary>
        public double MaxLongtitude { get; }

        /// <summary>
        /// Minimum latitude in radians.
        /// </summary>
        public double MinLatitude { get; }

        /// <summary>
        /// Maximum latitude in radians.
        /// </summary>
        public double MaxLatitude { get; }

        /// <summary>
        /// Creates a new instance of bounding box coordinates.
        /// </summary>
        /// <param name="planetoidId">Id of the planetoid in which the coordinates are set.</param>
        /// <param name="minLongtitude">Minimum longtitude in radians.</param>
        /// <param name="maxLongtitude">Maximum longtitude in radians.</param>
        /// <param name="minLatitude">Minimum latitude in radians.</param>
        /// <param name="maxLatitude">Maximum latitude in radians.</param>
        public AxisAlignedBoundingBoxCoordinateModel(int planetoidId, double minLongtitude, double maxLongtitude, double minLatitude, double maxLatitude)
        {
            PlanetoidId = planetoidId;
            MinLongtitude = minLongtitude;
            MaxLongtitude = maxLongtitude;
            MinLatitude = minLatitude;
            MaxLatitude = maxLatitude;
        }

        public override string ToString()
        {
            return $"({MinLatitude * 180.0 / Math.PI},{MinLongtitude * 180.0 / Math.PI},{MaxLatitude * 180.0 / Math.PI},{MaxLongtitude * 180.0 / Math.PI})";
        }

        /// <summary>
        /// Get an array of coordinates in order of
        /// lower left, upper left, upper right, and lower right.
        /// </summary>
        public CoordinateModel[] GetCoordinateArray()
        {
            return new[]
            {
                new CoordinateModel(MinLongtitude, MinLatitude, 0.0),
                new CoordinateModel(MinLongtitude, MaxLatitude, 0.0),
                new CoordinateModel(MaxLongtitude, MaxLatitude, 0.0),
                new CoordinateModel(MaxLongtitude, MinLatitude, 0.0),
            };
        }
    }
}
