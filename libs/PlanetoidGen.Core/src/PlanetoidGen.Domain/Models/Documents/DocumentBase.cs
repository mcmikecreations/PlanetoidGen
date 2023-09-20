namespace PlanetoidGen.Domain.Models.Documents
{
    public class DocumentBase
    {
        /// <summary>
        /// Default ID property that will match Mongo's '_id' with a unique index
        /// </summary>
        public string Id { get; set; }
    }
}
