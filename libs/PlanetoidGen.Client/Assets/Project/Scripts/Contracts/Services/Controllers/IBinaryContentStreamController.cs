using PlanetoidGen.Client.Contracts.Models.Args;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Contracts.Services.Controllers
{
    public interface IBinaryContentStreamController : IStreamControllerBase<FileEventArgs>
    {
        Task SendFileContentRequest(string id);
    }
}
