using PlanetoidGen.Agents.Procedural.Agents.Relief.Models;
using PlanetoidGen.Agents.Procedural.Agents.Relief.Processors.Abstractions;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Procedural.Agents.Relief.Processors.Implementations
{
    internal class SmoothProcessor : IReliefProcessor
    {
        private readonly int _tileSizePixels;
        private readonly ReliefAgentSettings _settings;
        private readonly float[,] _gaussianBlur;

        public SmoothProcessor(ReliefAgentSettings settings)
        {
            _tileSizePixels = settings.TileSizeInPixels;
            _settings = settings;
            _gaussianBlur = GaussianBlur(_settings.GaussianKernelSize, 1f);
        }

        public ValueTask<Result> Execute(float[,] heightmap, CancellationToken token)
        {
            var kernelSize = _settings.GaussianKernelSize;
            var radius = (kernelSize - 1) / 2;

            if (heightmap == null || heightmap.Length != _tileSizePixels * _tileSizePixels)
            {
                return new ValueTask<Result>(Result.CreateFailure(GeneralStringMessages.ObjectNotInitialized));
            }

            for (var i = radius; i < _tileSizePixels - radius; ++i)
            {
                for (var j = radius; j < _tileSizePixels - radius; ++j)
                {
                    heightmap[i, j] = OperatePointWithGaussianBlur(i, j, radius, heightmap);
                }
            }

            return new ValueTask<Result>(Result.CreateSuccess());
        }

        private float OperatePointWithGaussianBlur(int x, int y, int radius, float[,] heightmap)
        {
            var value = 0f;

            for (var i = -radius; i <= radius; ++i)
            {
                for (var j = -radius; j <= radius; ++j)
                {
                    value += heightmap[x + i, y + j] * _gaussianBlur[i + radius, j + radius];
                }
            }

            return value;
        }

        public static float[,] GaussianBlur(int length, float weight)
        {
            var kernel = new float[length, length];
            float kernelSum = 0;
            var radius = (length - 1) / 2;
            float distance = 0;
            var constant = 1f / (2f * (float)Math.PI * weight * weight);

            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    distance = (y * y + x * x) / (2 * weight * weight);
                    kernel[y + radius, x + radius] = constant * (float)Math.Exp(-distance);
                    kernelSum += kernel[y + radius, x + radius];
                }
            }

            for (var y = 0; y < length; y++)
            {
                for (var x = 0; x < length; x++)
                {
                    kernel[y, x] = kernel[y, x] / kernelSum;
                }
            }

            return kernel;
        }
    }
}
