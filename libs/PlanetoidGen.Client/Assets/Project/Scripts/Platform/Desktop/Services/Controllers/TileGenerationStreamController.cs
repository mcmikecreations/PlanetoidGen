using Grpc.Core;
using PlanetoidGen.API;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Client.Contracts.Models.Args;
using PlanetoidGen.Client.Contracts.Services.Controllers;
using PlanetoidGen.Client.Platform.Desktop.Services.Context.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Platform.Desktop.Services.Controllers
{
    public class TileGenerationStreamController : StreamControllerBase<TileGenerationArrayModel, TileArrayModel, TileEventArgs>, ITileGenerationStreamController
    {
        private readonly TileGeneration.TileGenerationClient _client;

        public TileGenerationStreamController(IConnectionContext context)
        {
            _client = new TileGeneration.TileGenerationClient(context.Channel);
        }

        public async Task SendTileGenerationRequest(int planetoidId, IEnumerable<SphericalLODCoordinateModel> models)
        {
            var request = new TileGenerationArrayModel
            {
                PlanetoidId = planetoidId,
            };

            request.TileGenerationInfos.AddRange(models.Select(m => new TileGenerationModel
            {
                Latitude = m.Latitude,
                Longtitude = m.Longtitude,
                LOD = m.LOD
            }));

            await SendStreamRequest(request);
        }

        protected override AsyncDuplexStreamingCall<TileGenerationArrayModel, TileArrayModel> OpenDuplexStream()
        {
            return _client.QueueTilesGeneration();
        }

        protected override Task RunResponseStreamReadingTask(CancellationToken token)
        {
            return base.RunResponseStreamReadingTask((tiles) =>
            {
                return new TileEventArgs
                {
                    TileInfos = tiles.TileInfos.Select(t => new Domain.Models.Info.TileInfoModel(t.PlanetoidId, (short)t.Z, t.X, t.Y, t.LastAgent, t.Id, default, default))
                };
            },
            token);
        }
    }
}
