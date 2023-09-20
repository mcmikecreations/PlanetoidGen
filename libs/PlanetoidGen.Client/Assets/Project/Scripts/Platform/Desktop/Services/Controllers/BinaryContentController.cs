using PlanetoidGen.API;
using PlanetoidGen.Client.Contracts.Services.Controllers;
using PlanetoidGen.Client.Platform.Desktop.Services.Context.Abstractions;
using PlanetoidGen.Client.Platform.Desktop.Services.Extensions;
using PlanetoidGen.Contracts.Models.Documents;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Platform.Desktop.Services.Controllers
{
    public class BinaryContentController : ControllerBase, IBinaryContentController
    {
        private readonly BinaryContent.BinaryContentClient _client;

        public BinaryContentController(IConnectionContext context)
        {
            _client = new BinaryContent.BinaryContentClient(context.Channel);
        }

        public async Task<FileModel> GetFileContentByPath(string fileName, string localPath, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.GetFileContentByPathAsync(new GetFileContentByPathModel
                {
                    FileName = fileName,
                    LocalPath = localPath
                }, cancellationToken: token);

                return result.ToResponseModel();
            });
        }

        public async Task<IEnumerable<string>> GetFileContentIdsByTile(GenericTileInfo tileInfo, bool isRequiredOnly, bool isDynamicOnly, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.GetFileContentIdsByTileAsync(
                new GetFileContentByTile
                {
                    PlanetoidId = tileInfo.PlanetoidId,
                    Z = tileInfo.Z,
                    X = tileInfo.X,
                    Y = tileInfo.Y,
                    IsDynamicOnly = isDynamicOnly,
                    IsRequiredOnly = isRequiredOnly
                }, cancellationToken: token);

                return result.Ids.AsEnumerable();
            });
        }

        public async Task<bool> SaveFileContent(FileModel model, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.SaveFileContentAsync(model.ToRequestModel(), cancellationToken: token);
                return result.Success;
            });
        }

        public async Task<bool> DeleteAllFileContentByTile(GenericTileInfo tileInfo, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.DeleteAllFileContentByTileAsync(new GenericTileModel
                {
                    PlanetoidId = tileInfo.PlanetoidId,
                    X = tileInfo.X,
                    Y = tileInfo.Y,
                    Z = tileInfo.Z,
                }, cancellationToken: token);

                return result.Success;
            });
        }

        public async Task<bool> DeleteFileContent(string id, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.DeleteFileContentAsync(new StringIdModel
                {
                    Id = id
                }, cancellationToken: token);

                return result.Success;
            });
        }
    }
}
