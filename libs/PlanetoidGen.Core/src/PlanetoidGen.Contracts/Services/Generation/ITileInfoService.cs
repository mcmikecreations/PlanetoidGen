using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Services.Generation
{
    public interface ITileInfoService
    {
        /// <summary>
        /// Select a tile from the system.
        /// </summary>
        /// <param name="model">Tile model including planet id, z, x, y.</param>
        /// <returns><see cref="Result{TileInfoModel}"/> which contains selected <see cref="TileInfoModel"/> or created if not present before.</returns>
        ValueTask<Result<TileInfoModel>> SelectTile(PlanarCoordinateModel planarModel, CancellationToken token);

        /// <summary>
        /// Updates <see cref="TileInfoModel.ModifiedDate"/> to a date specified.
        /// </summary>
        /// <param name="tile">Tile info.</param>
        /// <param name="modifiedDate">Modified date.</param>
        /// <returns><see cref="Result{T}"/> which contains updated <see cref="TileInfoModel"/>.</returns>
        ValueTask<Result<TileInfoModel>> UpdateTileLastModifiedDate(TileInfoModel tile, DateTimeOffset? modifiedDate, CancellationToken token);

        /// <summary>
        /// Updates <see cref="TileInfoModel.ModifiedDate"/> to a current timestamp.
        /// </summary>
        /// <param name="tile">Tile info.</param>
        /// <returns><see cref="Result{T}"/> which contains updated <see cref="TileInfoModel"/>.</returns>
        ValueTask<Result<TileInfoModel>> UpdateTileLastModifiedDateSetCurrentTimestamp(TileInfoModel tile, CancellationToken token);

        /// <summary>
        /// Updates a tile <see cref="TileInfoModel.LastAgent"/> and
        /// sets <see cref="TileInfoModel.ModifiedDate"/> to <see langword="null"/>.
        /// </summary>
        /// <param name="tileId">Tile identifier.</param>
        /// <param name="lastAgent">Last agent index.</param>
        /// <returns><see cref="Result{T}"/> which contains updated <see cref="TileInfoModel"/>.</returns>
        ValueTask<Result<TileInfoModel>> UpdateTileLastAgentResetLastModifiedDate(string tileId, int lastAgent, CancellationToken token);
    }
}
