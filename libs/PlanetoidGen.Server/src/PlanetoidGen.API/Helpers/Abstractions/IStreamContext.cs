using System.Collections.Concurrent;

namespace PlanetoidGen.API.Helpers.Abstractions
{
    public interface IStreamContext<T>
    {
        ConcurrentDictionary<string, ConcurrentBag<T>>? StreamMessages { get; }
    }
}
