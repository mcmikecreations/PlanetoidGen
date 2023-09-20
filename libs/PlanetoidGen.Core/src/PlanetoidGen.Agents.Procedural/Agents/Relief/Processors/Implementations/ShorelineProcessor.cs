using PlanetoidGen.Agents.Procedural.Agents.Relief.Models;
using PlanetoidGen.Agents.Procedural.Agents.Relief.Processors.Abstractions;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Procedural.Agents.Relief.Processors.Implementations
{
    internal class ShorelineProcessor : IReliefProcessor
    {
        private readonly int _tileSizePixels;
        private readonly ReliefAgentSettings _settings;
        private readonly (float x, float y) _p1 = (0.0f, 0.0f);
        private readonly (float x, float y) _p2 = (0.01f, 1.094f);
        private readonly (float x, float y) _p3 = (0.636f, 0.973f);
        private readonly (float x, float y) _p4 = (1.0f, 1.0f);

        public ShorelineProcessor(ReliefAgentSettings settings)
        {
            _tileSizePixels = settings.TileSizeInPixels;
            _settings = settings;
        }

        public ValueTask<Result> Execute(float[,] heightmap, CancellationToken token)
        {
            if (heightmap == null || heightmap.Length != _tileSizePixels * _tileSizePixels)
            {
                return new ValueTask<Result>(Result.CreateFailure(GeneralStringMessages.ObjectNotInitialized));
            }

            for (var i = 0; i < _tileSizePixels; ++i)
            {
                for (var j = 0; j < _tileSizePixels; ++j)
                {
                    if (heightmap[i, j] >= _settings.MinShorelineAltitude && heightmap[i, j] <= _settings.MaxShorelineAltitude)
                    {
                        heightmap[i, j] = heightmap[i, j] * GetBezierCurveCoef(heightmap[i, j]);
                    }
                }
            }

            return new ValueTask<Result>(Result.CreateSuccess());
        }

        private float GetBezierCurveCoef(float x)
        {
            var min = _settings.MinShorelineAltitude;
            var max = _settings.MaxShorelineAltitude;
            var t = (x - min) / (max - min);

            return (float)(Math.Pow(1 - t, 3) * _p1.y + 3 * Math.Pow(1 - t, 2) * t * _p2.y + 3 * (1 - t) * Math.Pow(t, 2) * _p3.y + Math.Pow(t, 3) * _p4.y);
        }
    }
}
