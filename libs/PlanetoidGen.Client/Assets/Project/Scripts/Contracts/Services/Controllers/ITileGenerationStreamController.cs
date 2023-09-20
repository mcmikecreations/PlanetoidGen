using PlanetoidGen.Client.Contracts.Models.Args;
using PlanetoidGen.Contracts.Models.Coordinates;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Contracts.Services.Controllers
{
    public interface ITileGenerationStreamController : IStreamControllerBase<TileEventArgs>
    {
        Task SendTileGenerationRequest(int planetoidId, IEnumerable<SphericalLODCoordinateModel> models);
    }
}
