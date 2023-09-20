using PlanetoidGen.Domain.Models.Documents;
using System.Collections.Generic;

namespace PlanetoidGen.Contracts.Models.Documents
{
    public class FileModel
    {
        public string? FileId { get; set; }
        public FileContentModel? Content { get; set; }
        public TileBasedFileInfoModel? TileBasedFileInfo { get; set; }
        public IEnumerable<FileDependencyModel>? DependentFiles { get; set; }

        public FileModel()
        { }

        public FileModel(string fileId)
        {
            FileId = fileId;
        }
    }
}
