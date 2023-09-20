using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Repositories.Documents;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Constants.StringMessages;
using PlanetoidGen.DataAccess.Repositories.Generic;
using PlanetoidGen.Domain.Models.Documents;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Generation
{
    public class TileBasedFileInfoRepository : RepositoryAccessWrapper<TileBasedFileInfoModel>, ITileBasedFileInfoRepository
    {
        private static readonly Func<IDataReader, TileBasedFileInfoModel> _reader = (r) => new TileBasedFileInfoModel(
            (string)r[nameof(TileBasedFileInfoModel.FileId)],
            (int)r[nameof(TileBasedFileInfoModel.PlanetoidId)],
            (short)r[nameof(TileBasedFileInfoModel.Z)],
            (long)r[nameof(TileBasedFileInfoModel.X)],
            (long)r[nameof(TileBasedFileInfoModel.Y)],
            ((Array)r[nameof(TileBasedFileInfoModel.Position)]).OfType<double>().ToArray(),
            ((Array)r[nameof(TileBasedFileInfoModel.Rotation)]).OfType<double>().ToArray(),
            ((Array)r[nameof(TileBasedFileInfoModel.Scale)]).OfType<double>().ToArray()
            );

        public TileBasedFileInfoRepository(DbConnectionStringBuilder connection, IMetaProcedureRepository meta) : base(connection, meta)
        {
        }

        public override string Name => TableStringMessages.TileBasedFileInfo;

        public override Func<IDataReader, TileBasedFileInfoModel>? Reader => _reader;

        public async ValueTask<Result<string>> InsertTileBasedFileInfo(
            TileBasedFileInfoModel model,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunSingleFunction<string>(
                StoredProcedureStringMessages.TileBasedFileInfoInsert,
                new
                {
                    dfileId = model.FileId,
                    dplanetoidId = model.PlanetoidId,
                    dz = model.Z,
                    dx = model.X,
                    dy = model.Y,
                    dposition = string.Join(";", model.Position),
                    drotation = string.Join(";", model.Rotation),
                    dscale = string.Join(";", model.Scale),
                },
                token,
                connection: connection);
        }

        public async ValueTask<Result<TileBasedFileInfoModel>> SelectTileBasedFileInfo(
            string fileId,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunSingleFunction<TileBasedFileInfoModel>(
                StoredProcedureStringMessages.TileBasedFileInfoSelectById,
                new { dfileId = fileId },
                token,
                connection: connection,
                constructor: Reader);
        }

        public async ValueTask<Result<IReadOnlyList<TileBasedFileInfoModel>>> SelectTileBasedFileInfosByTile(
            int planetoidId,
            short z,
            long x,
            long y,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunMultipleFunction<TileBasedFileInfoModel>(
                StoredProcedureStringMessages.TileBasedFileInfoSelectAllByTile,
                new { dplanetoidId = planetoidId, dz = z, dx = x, dy = y },
                token,
                connection: connection,
                constructor: Reader);
        }

        public async ValueTask<Result<bool>> RemoveTileBasedFileInfo(
            string fileId,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunSingleFunction<bool>(
                StoredProcedureStringMessages.TileBasedFileInfoDelete,
                new { dfileId = fileId },
                token,
                connection: connection);
        }

        public async ValueTask<Result<bool>> RemoveTileBasedFileInfosByTile(
            int planetoidId,
            short z,
            long x,
            long y,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunSingleFunction<bool>(
                StoredProcedureStringMessages.TileBasedFileInfoDeleteAllByTile,
                new { dplanetoidId = planetoidId, dz = z, dx = x, dy = y },
                token,
                connection: connection);
        }
    }
}
