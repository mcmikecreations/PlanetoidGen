using Assimp;
using System.Collections.Generic;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Collections
{
    public class VertexRing
    {
        public List<Vector3D> Vertices { get; set; }

        public List<int> Indices { get; set; }
    }
}
