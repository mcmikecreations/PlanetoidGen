using PlanetoidGen.Contracts.Models.Documents;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Contracts.Services.Controllers
{
    public interface IBinaryContentController
    {
        Task<bool> DeleteAllFileContentByTile(GenericTileInfo tileInfo, CancellationToken token = default);
        Task<bool> DeleteFileContent(string id, CancellationToken token = default);
        Task<FileModel> GetFileContentByPath(string fileName, string localPath, CancellationToken token = default);
        Task<IEnumerable<string>> GetFileContentIdsByTile(GenericTileInfo tileInfo, bool isRequiredOnly, bool isDynamicOnly, CancellationToken token = default);
        Task<bool> SaveFileContent(FileModel model, CancellationToken token = default);
    }
}
