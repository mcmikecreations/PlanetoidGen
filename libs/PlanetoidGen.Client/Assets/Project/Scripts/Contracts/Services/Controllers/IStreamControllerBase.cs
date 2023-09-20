using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Contracts.Services.Controllers
{
    public interface IStreamControllerBase<TArgs> where TArgs : EventArgs
    {
        Task StartStream(CancellationToken token);
        Task StopStreamIfExists();
        void Subscribe(EventHandler<TArgs> handler);
        void Unsubscribe(EventHandler<TArgs> handler);
    }
}
