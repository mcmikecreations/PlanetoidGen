namespace PlanetoidGen.Contracts.Models.Coordinates
{
    public class PlanarCoordinateModel
    {
        public int PlanetoidId { get; }

        /// <summary>
        /// Zoom value.
        /// </summary>
        public short Z { get; }

        /// <summary>
        /// X index of the tile with embedded face info.
        /// </summary>
        public long X { get; }

        /// <summary>
        /// Y index of the tile with embedded face info.
        /// </summary>
        public long Y { get; }

        public PlanarCoordinateModel(int planetoidId, short z, long x, long y)
        {
            PlanetoidId = planetoidId;
            Z = z;
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"P={PlanetoidId}, Z={Z}, X={X}, Y={Y}";
        }
    }
}
