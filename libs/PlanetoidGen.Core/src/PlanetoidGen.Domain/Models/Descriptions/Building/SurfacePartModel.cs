namespace PlanetoidGen.Domain.Models.Descriptions.Building
{
    public class SurfacePartModel
    {
        /// <summary>
        /// The width of the building part.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// The kind of the part, used to differentiate different part types.
        /// </summary>
        public string Kind { get; set; }
    }
}
