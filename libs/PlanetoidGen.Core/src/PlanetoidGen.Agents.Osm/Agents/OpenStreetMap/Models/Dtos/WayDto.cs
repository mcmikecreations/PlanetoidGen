using System.Collections.Generic;

namespace PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Dtos
{
    public class WayDto
    {
        public long Id { get; set; }

        public IList<long> References { get; set; }

        public IDictionary<string, string> Tags { get; set; }

        public WayDto()
        {
            References = new List<long>();
            Tags = new Dictionary<string, string>();
        }
    }
}
