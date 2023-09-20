using System;

namespace PlanetoidGen.Domain.Models.Documents
{
    public class FileInfoModel
    {
        public string FileId { get; set; }
        public DateTime ModifiedOn { get; set; }

        public FileInfoModel(string fileId, DateTime modifiedOn)
        {
            FileId = fileId;
            ModifiedOn = modifiedOn;
        }
    }
}
