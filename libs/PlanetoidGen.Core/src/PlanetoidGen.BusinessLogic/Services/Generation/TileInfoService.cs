using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Repositories.Info;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.BusinessLogic.Services.Generation
{
    public class TileInfoService : ITileInfoService
    {
        private readonly ITileInfoRepository _tileRepository;

        /// <summary>
        /// Creates an instance of <see cref="TileInfoService"/>.
        /// </summary>
        public TileInfoService(ITileInfoRepository tileRepository)
        {
            _tileRepository = tileRepository ?? throw new ArgumentNullException(nameof(tileRepository));
        }

        /// <inheritdoc/>
        public async ValueTask<Result<TileInfoModel>> SelectTile(PlanarCoordinateModel planarModel, CancellationToken token)
        {
            return await _tileRepository.SelectTile(planarModel, token);
        }

        /// <inheritdoc/>
        public async ValueTask<Result<TileInfoModel>> UpdateTileLastModifiedDateSetCurrentTimestamp(TileInfoModel tile, CancellationToken token)
        {
            return await _tileRepository.UpdateTileLastModfiedInfo(
                tile.Id,
                tile.LastAgent,
                DateTime.UtcNow,
                token);
        }

        /// <inheritdoc/>
        public async ValueTask<Result<TileInfoModel>> UpdateTileLastModifiedDate(TileInfoModel tile, DateTimeOffset? modifiedDate, CancellationToken token)
        {
            return await _tileRepository.UpdateTileLastModfiedInfo(
                tile.Id,
                tile.LastAgent,
                modifiedDate,
                token);
        }

        /// <inheritdoc/>
        public async ValueTask<Result<TileInfoModel>> UpdateTileLastAgentResetLastModifiedDate(string tileId, int lastAgent, CancellationToken token)
        {
            return await _tileRepository.UpdateTileLastModfiedInfo(
                tileId,
                lastAgent,
                modifiedDate: null,
                token);
        }
    }
}
