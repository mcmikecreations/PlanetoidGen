namespace PlanetoidGen.Contracts.Models.Coordinates
{
    public class BoundingBoxCoordinateModel
    {
        public int PlanetoidId { get; }

        /// <summary>
        /// Lower left corner in radians. Usually SW.
        /// </summary>
        public CoordinateModel LowerLeftCorner { get; }

        /// <summary>
        /// Upper left corner in radians. Usually NW.
        /// </summary>
        public CoordinateModel UpperLeftCorner { get; }

        /// <summary>
        /// Upper right corner in radians. Usually NE.
        /// </summary>
        public CoordinateModel UpperRightCorner { get; }

        /// <summary>
        /// Lower right corner in radians. Usually SE.
        /// </summary>
        public CoordinateModel LowerRightCorner { get; }

        /// <summary>
        /// Creates a new instance of bounding box coordinates.
        /// </summary>
        /// <param name="planetoidId">Id of the planetoid in which the coordinates are set.</param>
        /// <param name="lowerLeftCorner">Lower left corner in radians. Usually SW.</param>
        /// <param name="upperLeftCorner">Upper left corner in radians. Usually NW.</param>
        /// <param name="upperRightCorner">Upper right corner in radians. Usually NE.</param>
        /// <param name="lowerRightCorner">Lower right corner in radians. Usually SE.</param>
        public BoundingBoxCoordinateModel(
            int planetoidId,
            CoordinateModel lowerLeftCorner,
            CoordinateModel upperLeftCorner,
            CoordinateModel upperRightCorner,
            CoordinateModel lowerRightCorner)
        {
            PlanetoidId = planetoidId;
            LowerLeftCorner = lowerLeftCorner;
            UpperLeftCorner = upperLeftCorner;
            UpperRightCorner = upperRightCorner;
            LowerRightCorner = lowerRightCorner;
        }

        public override string ToString()
        {
            return $"P={PlanetoidId}, LoLeft={LowerLeftCorner}, UpLeft={UpperLeftCorner}, UpRight={UpperRightCorner}, LoRight={LowerRightCorner}";
        }

        /// <summary>
        /// Get an array of coordinates in order of
        /// lower left, upper left, upper right, and lower right.
        /// </summary>
        public CoordinateModel[] GetCoordinateArray()
        {
            return new[]
            {
                LowerLeftCorner,
                UpperLeftCorner,
                UpperRightCorner,
                LowerRightCorner,
            };
        }
    }
}
