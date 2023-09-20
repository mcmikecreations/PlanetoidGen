namespace PlanetoidGen.Domain.Models.Documents
{
    public class FileDependencyModel
    {
        /// <summary>
        /// Id of the file, which is dependent on the <seealso cref="ReferencedFileId"/>.
        /// </summary>
        public string FileId { get; set; }

        /// <summary>
        /// Id of the dependency file.
        /// </summary>
        public string ReferencedFileId { get; set; }

        /// <summary>
        /// Is the referenced file required for the main one to make sense.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Is the file dynamically generated.
        /// </summary>
        public bool IsDynamic { get; set; }

        public FileDependencyModel(string fileId, string referencedFileId, bool isRequired, bool isDynamic)
        {
            FileId = fileId;
            ReferencedFileId = referencedFileId;
            IsRequired = isRequired;
            IsDynamic = isDynamic;
        }
    }
}
