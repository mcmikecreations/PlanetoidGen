using System;
using System.Collections.Generic;

namespace PlanetoidGen.Domain.Models.Descriptions.Building
{
    public class BuildingModel : BaseDescriptionModel
    {
        /// <summary>
        /// The kind of the building, the value of the building tag.
        /// For example, apartments, cabin, residential.
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Number of floors in a building, excluding the roof and underground floors.
        /// </summary>
        public int? Levels { get; set; }

        /// <summary>
        /// Number of skipped levels, after which the overhang begins.
        /// </summary>
        public int? MinLevel { get; set; }

        /// <summary>
        /// Number of basement floors in a building.
        /// </summary>
        public int? UndergroundLevels { get; set; }

        /// <summary>
        /// The number of flats in the building.
        /// </summary>
        public int? Flats { get; set; }

        /// <summary>
        /// One level is significantly more flexible (less stiff) than those above and below it.
        /// </summary>
        [Obsolete("Not supported.")]
        public string SoftStorey { get; set; }

        /// <summary>
        /// The color of the building exterior, if any.
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// The material for the frame of the building.
        /// </summary>
        public string Material { get; set; }

        /// <summary>
        /// The material of the exterior surface of the building.
        /// </summary>
        public string Cladding { get; set; }

        /// <summary>
        /// The material of the walls of the building.
        /// </summary>
        public string Walls { get; set; }

        /// <summary>
        /// The construction method of the building.
        /// </summary>
        public string Structure { get; set; }

        /// <summary>
        /// The purpose of the part of the building.
        /// </summary>
        public string Part { get; set; }

        /// <summary>
        /// Is the building fireproof.
        /// </summary>
        public bool? Fireproof { get; set; }

        [Obsolete("Not supported.")]
        public string Entrance { get; set; }

        [Obsolete("Not supported.")]
        public string Access { get; set; }

        /// <summary>
        /// The date when the building finished construction.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// The description of the roof.
        /// </summary>
        public RoofModel Roof { get; set; }

        /// <summary>
        /// A list of descriptions for each level.
        /// </summary>
        public IList<LevelModel> LevelCollection { get; set; }
    }
}
