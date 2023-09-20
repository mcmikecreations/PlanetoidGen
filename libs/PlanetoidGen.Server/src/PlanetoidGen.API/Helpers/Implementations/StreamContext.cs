using PlanetoidGen.API.Helpers.Abstractions;
using System.Collections.Concurrent;

namespace PlanetoidGen.API.Helpers.Implementations
{
    public class StreamContext<T> : IStreamContext<T>
    {
        public ConcurrentDictionary<string, ConcurrentBag<T>>? StreamMessages { get; }

        public StreamContext()
        {
            StreamMessages = new ConcurrentDictionary<string, ConcurrentBag<T>>();
        }
    }
}
