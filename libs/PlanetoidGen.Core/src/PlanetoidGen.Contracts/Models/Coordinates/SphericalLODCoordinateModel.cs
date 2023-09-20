namespace PlanetoidGen.Contracts.Models.Coordinates
{
    public class SphericalLODCoordinateModel
    {
        /// <summary>
        /// Longtitute in radians.
        /// </summary>
        public double Longtitude { get; }

        /// <summary>
        /// Latitude in radians.
        /// </summary>
        public double Latitude { get; }

        public short LOD { get; }

        public SphericalLODCoordinateModel(double longtitude, double latitude, short lod)
        {
            Longtitude = longtitude;
            Latitude = latitude;
            LOD = lod;
        }
    }
}
