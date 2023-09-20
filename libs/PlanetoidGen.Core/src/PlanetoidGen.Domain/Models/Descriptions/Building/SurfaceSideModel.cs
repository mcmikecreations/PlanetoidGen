using System.Collections.Generic;

namespace PlanetoidGen.Domain.Models.Descriptions.Building
{
    public class SurfaceSideModel
    {
        /// <summary>
        /// The width of the building size in meters.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// A collection of descriptions of individual parts of a wall.
        /// </summary>
        public IList<SurfacePartModel> Parts { get; set; }
    }
}
