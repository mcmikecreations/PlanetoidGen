namespace PlanetoidGen.Contracts.Models.Repositories.Documents
{
    public class DocumentDbOptions
    {
        public static string DefaultConfigurationSectionName = nameof(DocumentDbOptions);

        public string? ConnectionString { get; set; }

        public string? DatabaseName { get; set; }

        public string? CollectionName { get; set; }

        public string? BucketName { get; set; }

        public int? MaxDocumentSizeInBytes { get; set; }
    }
}
