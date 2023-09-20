using Grpc.Core;

namespace PlanetoidGen.Client.Platform.Desktop.Services.Context.Abstractions
{
    public interface IConnectionContext
    {
        Channel Channel { get; }
    }
}
