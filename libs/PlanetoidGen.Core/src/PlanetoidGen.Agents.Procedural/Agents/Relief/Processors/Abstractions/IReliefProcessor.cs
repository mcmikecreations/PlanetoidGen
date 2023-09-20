using PlanetoidGen.Contracts.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Procedural.Agents.Relief.Processors.Abstractions
{
    internal interface IReliefProcessor
    {
        ValueTask<Result> Execute(float[,] heightmap, CancellationToken token);
    }
}
