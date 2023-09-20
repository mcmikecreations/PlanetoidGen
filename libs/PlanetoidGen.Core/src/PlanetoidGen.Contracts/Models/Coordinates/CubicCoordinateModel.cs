namespace PlanetoidGen.Contracts.Models.Coordinates
{
    /// <summary>
    /// The Quadrilateralized Spherical Cube coordinate model.
    /// </summary>
    public class CubicCoordinateModel
    {
        public int PlanetoidId { get; }

        /// <summary>
        /// Cube face index.
        /// </summary>
        public short Face { get; }

        /// <summary>
        /// Zoom value.
        /// </summary>
        public short Z { get; }

        /// <summary>
        /// X relative offset.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Y relative offset.
        /// </summary>
        public double Y { get; }

        public CubicCoordinateModel(int planetoidId, short face, short z, double x, double y)
        {
            PlanetoidId = planetoidId;
            Face = face;
            Z = z;
            X = x;
            Y = y;
        }

        public CubicCoordinateModel(CubicCoordinateModel other)
        {
            PlanetoidId = other.PlanetoidId;
            Face = other.Face;
            Z = other.Z;
            X = other.X;
            Y = other.Y;
        }

        public override string ToString()
        {
            return $"P={PlanetoidId}, F={Face}, Z={Z}, X={X}, Y={Y}";
        }
    }
}
