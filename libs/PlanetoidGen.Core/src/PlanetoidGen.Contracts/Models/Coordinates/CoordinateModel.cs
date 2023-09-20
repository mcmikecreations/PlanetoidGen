namespace PlanetoidGen.Contracts.Models.Coordinates
{
    public class CoordinateModel
    {
        /// <summary>
        /// Typically longitude in degrees.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Typically latitude in degrees.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Typically height in meters.
        /// </summary>
        public double Z { get; }

        /// <param name="x">Typically longitude in degrees.</param>
        /// <param name="y">Typically latitude in degrees.</param>
        /// <param name="z">Typically height in meters.</param>
        public CoordinateModel(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public CoordinateModel Clone(double? x = null, double? y = null, double? z = null)
        {
            double nx = x ?? X;
            double ny = y ?? Y;
            double nz = z ?? Z;

            return new CoordinateModel(nx, ny, nz);
        }

        public override string ToString()
        {
            return $"X={X}, Y={Y}, Z={Z}";
        }
    }
}
