using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Repositories.Info;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Constants.StringMessages;
using PlanetoidGen.DataAccess.Helpers.Extensions;
using PlanetoidGen.DataAccess.Repositories.Generic;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Info
{
    public class TileInfoRepository : RepositoryAccessWrapper<TileInfoModel>, ITileInfoRepository
    {
        private static readonly Func<IDataReader, TileInfoModel> _reader = (r) => new TileInfoModel(
                (int)r[nameof(TileInfoModel.PlanetoidId)],
                (short)r[nameof(TileInfoModel.Z)],
                (long)r[nameof(TileInfoModel.X)],
                (long)r[nameof(TileInfoModel.Y)],
                r.GetNullableValue<int>(nameof(TileInfoModel.LastAgent)),
                (string)r[nameof(TileInfoModel.Id)],
                (DateTimeOffset)r[nameof(TileInfoModel.CreatedDate)],
                (DateTimeOffset?)r[nameof(TileInfoModel.ModifiedDate)]
                );

        public TileInfoRepository(DbConnectionStringBuilder connection, IMetaProcedureRepository meta)
            : base(connection, meta)
        {
        }

        public override string Name => TableStringMessages.TileInfo;

        public override Func<IDataReader, TileInfoModel>? Reader => _reader;

        public async ValueTask<Result<string>> InsertTile(TileInfoModel model, CancellationToken token)
        {
            return await RunSingleFunction<string>(
                StoredProcedureStringMessages.TileInfoInsert,
                new { model.PlanetoidId, model.Z, model.X, model.Y },
                token);
        }

        public async ValueTask<Result<TileInfoModel>> UpdateTileLastModfiedInfo(
            string tileId,
            int? lastAgentIndex,
            DateTimeOffset? modifiedDate,
            CancellationToken token)
        {
            var updateResult = await RunSingleFunction<TileInfoModel>(
                StoredProcedureStringMessages.TileInfoLastModfiedInfoUpdate,
                new
                {
                    tileId,
                    lastAgentIndex,
                    lastModifiedDate = modifiedDate.HasValue ? modifiedDate.Value.UtcDateTime : (DateTime?)null
                },
                token);

            return updateResult.Success && string.IsNullOrEmpty(updateResult.Data?.Id)
                ? Result<TileInfoModel>.CreateFailure($"Tile with id '{tileId}' does not exist.")
                : updateResult;
        }

        public async ValueTask<Result<TileInfoModel>> SelectTile(PlanarCoordinateModel planarModel, CancellationToken token)
        {
            return await RunSingleFunction<TileInfoModel>(
                StoredProcedureStringMessages.TileInfoSelect,
                new { dplanetoidId = planarModel.PlanetoidId, dz = planarModel.Z, dx = planarModel.X, dy = planarModel.Y },
                token);
        }
    }
}
