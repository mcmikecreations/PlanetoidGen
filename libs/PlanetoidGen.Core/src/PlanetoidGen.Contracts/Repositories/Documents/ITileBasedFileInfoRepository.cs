using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Documents;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Documents
{
    public interface ITileBasedFileInfoRepository : INamedRepository<TileBasedFileInfoModel>
    {
        ValueTask<Result<string>> InsertTileBasedFileInfo(
            TileBasedFileInfoModel model,
            CancellationToken token,
            IDbConnection? connection = null);

        ValueTask<Result<bool>> RemoveTileBasedFileInfo(
            string fileId,
            CancellationToken token,
            IDbConnection? connection = null);

        ValueTask<Result<bool>> RemoveTileBasedFileInfosByTile(
            int planetoidId,
            short z,
            long x,
            long y,
            CancellationToken token,
            IDbConnection? connection = null);

        ValueTask<Result<TileBasedFileInfoModel>> SelectTileBasedFileInfo(
            string fileId,
            CancellationToken token,
            IDbConnection? connection = null);

        ValueTask<Result<IReadOnlyList<TileBasedFileInfoModel>>> SelectTileBasedFileInfosByTile(
            int planetoidId,
            short z,
            long x,
            long y,
            CancellationToken token,
            IDbConnection? connection = null);
    }
}
