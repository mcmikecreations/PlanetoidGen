namespace PlanetoidGen.Domain.Models.Descriptions
{
    public class BaseDescriptionModel
    {
        /// <summary>
        /// The height of the building, including the roof, in meters.
        /// </summary>
        public double? Height { get; set; }

        /// <summary>
        /// The amount of skipped height, after which the overhang begins, in meters.
        /// </summary>
        public double? MinHeight { get; set; }

        /// <summary>
        /// The description of the purpose of the building.
        /// </summary>
        public string Description { get; set; }
    }
}
