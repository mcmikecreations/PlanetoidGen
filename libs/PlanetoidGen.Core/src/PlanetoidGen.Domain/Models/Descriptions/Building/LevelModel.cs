using System.Collections.Generic;

namespace PlanetoidGen.Domain.Models.Descriptions.Building
{
    public class LevelModel
    {
        /// <summary>
        /// The height of the level in meters.
        /// </summary>
        public double? Height { get; set; }

        /// <summary>
        /// A collection of side descriptions for a specific building floor.
        /// </summary>
        public IList<SurfaceSideModel> Sides { get; set; }
    }
}
