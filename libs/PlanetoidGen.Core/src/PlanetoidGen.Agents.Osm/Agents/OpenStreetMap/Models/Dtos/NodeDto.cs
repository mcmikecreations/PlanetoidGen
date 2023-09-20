using System.Collections.Generic;

namespace PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Dtos
{
    public class NodeDto
    {
        public long Id { get; set; }

        /// <summary>
        /// Latitude in degrees.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Longitude in degrees.
        /// </summary>
        public double Longitude { get; set; }

        public IDictionary<string, string> Tags { get; set; }

        public NodeDto()
        {
            Tags = new Dictionary<string, string>();
        }
    }
}
