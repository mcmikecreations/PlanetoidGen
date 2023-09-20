using System;
using System.Collections.Generic;

namespace PlanetoidGen.Domain.Models.Descriptions.Building
{
    public class RoofModel
    {
        /// <summary>
        /// The number of floors inside the roof.
        /// </summary>
        public int? Levels { get; set; }

        /// <summary>
        /// The shape of the roof. For example, flat, gabled, round.
        /// </summary>
        public string Shape { get; set; }

        /// <summary>
        /// Does the roof go along the long side of the building or across.
        /// </summary>
        public string Orientation { get; set; }

        /// <summary>
        /// The height of the roof in meters.
        /// </summary>
        public double? Height { get; set; }

        /// <summary>
        /// Inclination of the roof sides.
        /// </summary>
        [Obsolete("Not supported.")]
        public double? Angle { get; set; }

        /// <summary>
        /// The compass direction (0,N; 90,E; 180,S; 270,W) from back side of roof to front,
        /// i.e. the direction towards which the main face of the roof is looking.
        /// </summary>
        [Obsolete("Not supported.")]
        public double? Direction { get; set; }

        /// <summary>
        /// The color of the roof.
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// The material of the roof.
        /// </summary>
        public string Material { get; set; }

        /// <summary>
        /// A list of descriptions for each level.
        /// </summary>
        public IList<LevelModel> LevelCollection { get; set; }
    }
}
