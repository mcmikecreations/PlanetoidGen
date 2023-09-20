using Grpc.Core;
using System;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Platform.Desktop.Services.Controllers
{
    public abstract class ControllerBase
    {
        public async Task<T> HandleRequest<T>(Func<Task<T>> func)
        {
            try
            {
                return await func();
            }
            catch (RpcException ex)
            {
                throw new InvalidOperationException($"RPC exception: {ex}", ex);
            }
        }

    }
}
