using Grpc.Core;
using PlanetoidGen.API;
using PlanetoidGen.Client.Contracts.Models.Args;
using PlanetoidGen.Client.Contracts.Services.Controllers;
using PlanetoidGen.Client.Platform.Desktop.Services.Context.Abstractions;
using PlanetoidGen.Client.Platform.Desktop.Services.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Platform.Desktop.Services.Controllers
{
    public class BinaryContentStreamController : StreamControllerBase<StringIdModel, FileContentModel, FileEventArgs>, IBinaryContentStreamController
    {
        private readonly BinaryContent.BinaryContentClient _client;

        public BinaryContentStreamController(IConnectionContext context)
        {
            _client = new BinaryContent.BinaryContentClient(context.Channel);
        }

        public async Task SendFileContentRequest(string id)
        {

            await SendStreamRequest(new StringIdModel { Id = id });
        }

        protected override AsyncDuplexStreamingCall<StringIdModel, FileContentModel> OpenDuplexStream()
        {
            return _client.GetFileContent();
        }

        protected override Task RunResponseStreamReadingTask(CancellationToken token)
        {
            return base.RunResponseStreamReadingTask((file) =>
            {
                return new FileEventArgs
                {
                    File = file.ToResponseModel()
                };
            },
            token);
        }
    }
}
