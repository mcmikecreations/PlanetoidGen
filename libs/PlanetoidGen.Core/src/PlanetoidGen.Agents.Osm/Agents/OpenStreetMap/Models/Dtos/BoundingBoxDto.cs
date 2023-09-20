namespace PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Dtos
{
    public class BoundingBoxDto
    {
        /// <summary>
        /// South latitude in degrees.
        /// </summary>
        public double South { get; set; }

        /// <summary>
        /// West longitude in degrees.
        /// </summary>
        public double West { get; set; }

        /// <summary>
        /// North latitude in degrees.
        /// </summary>
        public double North { get; set; }

        /// <summary>
        /// East longitude in degrees.
        /// </summary>
        public double East { get; set; }

        public override string ToString()
        {
            return $"({South},{West},{North},{East})";
        }
    }
}
