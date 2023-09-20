using PlanetoidGen.Contracts.Models.Documents;
using PlanetoidGen.Contracts.Models.Generic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Services.Documents
{
    public interface IFileContentService
    {
        ValueTask<Result<bool>> DeleteFileContent(string fileId, CancellationToken token);
        ValueTask<Result<bool>> DeleteFileContentByTile(GenericTileInfo tileInfo, CancellationToken token);
        ValueTask<Result<FileModel>> GetFileContent(string fileId, CancellationToken token);
        ValueTask<Result<FileModel>> GetFileContentByPath(string fileName, string localPath, CancellationToken token);
        ValueTask<Result<IEnumerable<FileModel>>> GetFileContentByTile(GenericTileInfo tileInfo, bool isRequiredOnly, bool isDynamicOnly, CancellationToken token);
        ValueTask<Result<IEnumerable<string>>> GetFileContentIdsByTile(GenericTileInfo tileInfo, bool isRequiredOnly, bool isDynamicOnly, CancellationToken token);
        ValueTask<Result<bool>> SaveFileContentWithDependencies(FileModel file, CancellationToken token);

        /// <summary>
        /// Check if a file with a specific id exists in the file database.
        /// </summary>
        /// <param name="fileId">File id to look for.</param>
        /// <returns>True if exists, false if not, error on exceptions.</returns>
        ValueTask<Result<bool>> FileIdExists(string fileId, CancellationToken token);
    }
}
