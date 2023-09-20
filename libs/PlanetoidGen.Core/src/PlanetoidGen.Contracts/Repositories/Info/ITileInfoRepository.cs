using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Info
{
    public interface ITileInfoRepository : INamedRepository<TileInfoModel>
    {
        /// <summary>
        /// Insert an empty tile into the system.
        /// </summary>
        /// <param name="model">Tile model including planet id, z, x, y.</param>
        /// <returns>Guid of the created tile.</returns>
        ValueTask<Result<string>> InsertTile(TileInfoModel model, CancellationToken token);

        /// <summary>
        /// Updates a tile <see cref="TileInfoModel.LastAgent"/>
        /// and <see cref="TileInfoModel.ModifiedDate"/> value in the system.
        /// </summary>
        /// <param name="tileId">Tile identifier.</param>
        /// <param name="lastAgentIndex">The agent index that last processed this segment.</param>
        /// <param name="modifiedDate">Last modified date.</param>
        /// <returns><see cref="Result{TileInfoModel}"/> which contains updated <see cref="TileInfoModel"/>.</returns>
        ValueTask<Result<TileInfoModel>> UpdateTileLastModfiedInfo(
            string tileId,
            int? lastAgentIndex,
            DateTimeOffset? modifiedDate,
            CancellationToken token);

        /// <summary>
        /// Select a tile from the system.
        /// </summary>
        /// <param name="model">Tile model including planet id, z, x, y.</param>
        /// <returns>Selected tile, created if not present before.</returns>
        ValueTask<Result<TileInfoModel>> SelectTile(PlanarCoordinateModel planarModel, CancellationToken token);
    }
}
