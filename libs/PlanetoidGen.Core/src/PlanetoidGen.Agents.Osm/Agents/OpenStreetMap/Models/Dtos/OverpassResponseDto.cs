using System.Collections.Generic;

namespace PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Dtos
{
    public class OverpassResponseDto
    {
        public IList<NodeDto> Nodes { get; set; }

        public IList<WayDto> Ways { get; set; }

        public OverpassResponseDto()
        {
            Nodes = new List<NodeDto>();
            Ways = new List<WayDto>();
        }
    }
}
