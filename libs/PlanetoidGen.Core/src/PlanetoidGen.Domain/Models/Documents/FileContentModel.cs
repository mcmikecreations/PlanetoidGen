using System.Collections.Generic;

namespace PlanetoidGen.Domain.Models.Documents
{
    public class FileContentModel : DocumentBase
    {
        /// <summary>
        /// File name with extension
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Local path to the file without <see cref="FileName"/>
        /// </summary>
        public string LocalPath { get; set; }

        /// <summary>
        /// List of additionals attributes if such are required
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; }

        /// <summary>
        /// External ID to the GridFS file
        /// If file content is bigger than MongoDb document size (16MB), then <see cref="LargeContentId"/> is assigned
        /// </summary>
        public string LargeContentId { get; set; }

        /// <summary>
        /// Binary content
        /// </summary>
        public byte[] Content { get; set; }
    }
}
