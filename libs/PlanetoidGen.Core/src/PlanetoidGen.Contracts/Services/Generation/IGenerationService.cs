using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Coordinates;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Services.Generation
{
    public interface IGenerationService
    {
        /// <summary>
        /// Schedules generation job.
        /// </summary>
        /// <param name="tileInfo">Tile coordinates to generate.</param>
        /// <param name="connectionId">Connection identifier.</param>
        /// <returns></returns>
        ValueTask<Result> QueueTileGeneration(SphericalCoordinateModel tileInfo, string connectionId, CancellationToken token);

        /// <summary>
        /// Schedules generation jobs.
        /// </summary>
        /// <param name="tileGenerationInfos">List of tile coordinates to generate.</param>
        /// <param name="connectionId">Connection identifier.</param>
        /// <returns></returns>
        ValueTask<Result> QueueTilesGeneration(IEnumerable<SphericalCoordinateModel> tileGenerationInfos, string connectionId, CancellationToken token);
    }
}
