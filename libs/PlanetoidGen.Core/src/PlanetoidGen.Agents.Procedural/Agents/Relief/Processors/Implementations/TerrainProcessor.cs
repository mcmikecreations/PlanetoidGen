using PlanetoidGen.Agents.Procedural.Agents.Relief.Models;
using PlanetoidGen.Agents.Procedural.Agents.Relief.Processors.Abstractions;
using PlanetoidGen.Agents.Procedural.Helpers.LibNoise;
using PlanetoidGen.BusinessLogic.Agents.Helpers;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Procedural.Agents.Relief.Processors.Implementations
{
    internal class TerrainProcessor : IReliefProcessor
    {
        private const double ConstMaskScalingFactor = 3.5;
        private const double NormalDistributionHeight = 0.15;
        private const float ComparisonEps = 1e-5f;

        private readonly short _zoom;
        private readonly int _tileSizePixels;
        private readonly ReliefAgentSettings _settings;
        private readonly CubicCoordinateModel _tileStart;
        private readonly ICoordinateMappingService _mapper;

        private readonly Perlin _maskNoise, _maskDetailsNoise;
        private readonly Perlin _mountainNoise;
        private readonly Perlin _hillNoise;

        public TerrainProcessor(
            int seed,
            short zoom,
            ReliefAgentSettings settings,
            CubicCoordinateModel tileStart,
            ICoordinateMappingService mapper)
        {
            _zoom = zoom;
            _tileSizePixels = settings.TileSizeInPixels;
            _settings = settings;
            _tileStart = tileStart;
            _mapper = mapper;

            _maskNoise = new Perlin(
                frequency: 1L << zoom,
                seed: seed,
                noiseQuality: NoiseQuality.QUALITY_FAST);
            _maskDetailsNoise = new Perlin(
                frequency: 2.0 * (1L << zoom),
                octaveCount: 8,
                seed: seed + 4323,
                noiseQuality: NoiseQuality.QUALITY_BEST);
            _mountainNoise = new Perlin(
                frequency: 1L << zoom,
                seed: seed + 564645465,
                noiseQuality: NoiseQuality.QUALITY_BEST);
            _hillNoise = new Perlin(
                frequency: 4.0 * (1L << zoom),
                seed: seed + 45644,
                noiseQuality: NoiseQuality.QUALITY_BEST);
        }

        public ValueTask<Result> Execute(float[,] heightmap, CancellationToken token)
        {
            if (heightmap == null || heightmap.Length != _tileSizePixels * _tileSizePixels)
            {
                return new ValueTask<Result>(Result.CreateFailure(GeneralStringMessages.ObjectNotInitialized));
            }

            var cubicTileSize = _mapper.TileSizeCubic(_zoom);
            var step = cubicTileSize / (_tileSizePixels + 1);
            var relativeX = _tileStart.X;
            double relativeY;

            var maxMountainAltittude = _settings.MaxMountainAltittude * 0.5f;
            var maxHillAltittude = _settings.MaxHillAltittude * 0.5f;
            var mountainSmoothingFactor = _settings.MaxMaskAltitude - _settings.MinMountainThreshold;
            var hillSmoothingFactor = _settings.MaxMaskAltitude - _settings.MinHillThreshold;

            // The constant used to map mask beaches to [0;1]
            var constMaskFactor = Math.Exp(-0.5 * ConstMaskScalingFactor * ConstMaskScalingFactor);

            for (var i = 0; i < _tileSizePixels; ++i)
            {
                relativeY = _tileStart.Y;

                for (var j = 0; j < _tileSizePixels; ++j)
                {
                    var sphericalCoords = _mapper.ToSpherical(
                        new CubicCoordinateModel(
                            _tileStart.PlanetoidId,
                            _tileStart.Face,
                            _tileStart.Z,
                            relativeX,
                            relativeY));

                    var (x, y, z) = Utils.ToCartesian(sphericalCoords.Latitude, sphericalCoords.Longtitude);

                    var maskValue = (float)_maskNoise.GetValue(x, y, z);
                    var maskDetailValue = (float)_maskDetailsNoise.GetValue(x, y, z) * _settings.MaxMaskAltitude;

                    if (maskValue > _settings.MaskEdgeThresholdNegativePercentage &&
                        maskValue < _settings.MaskEdgeThresholdPositivePercentage)
                    {
                        var deltaX = maskValue.Remap(
                            _settings.MaskEdgeThresholdNegativePercentage, -1.0f,
                            _settings.MaskEdgeThresholdPositivePercentage, 1.0f);

                        // 0 on the sides, 1 in the middle
                        var normalHeightValue = Math.Exp(-0.5 * ConstMaskScalingFactor * ConstMaskScalingFactor * deltaX * deltaX);
                        var maskFactor = -NormalDistributionHeight * normalHeightValue +
                            NormalDistributionHeight * constMaskFactor + 1.0;

                        maskValue *= (float)maskFactor;
                    }

                    // Uncomment to see only the landmass
                    //if (maskValue < 0f) maskValue = -1f;

                    var maskHeightmapValue = maskValue * _settings.MaxMaskAltitude;
                    heightmap[i, j] = maskHeightmapValue;

                    if (maskHeightmapValue > _settings.MinMountainThreshold && maskDetailValue > _settings.MinMountainThreshold)
                    {
                        var mountainAlt = ((float)_mountainNoise.GetValue(x, y, z) + 1f) * maxMountainAltittude;
                        var a = maskHeightmapValue - _settings.MinMountainThreshold;
                        var b = maskDetailValue - _settings.MinMountainThreshold;

                        heightmap[i, j] += mountainAlt * Math.Clamp(Math.Min(a, b) / mountainSmoothingFactor, 0f, 1f);
                    }

                    if (maskHeightmapValue > _settings.MinHillThreshold)
                    {
                        var hillAlt = ((float)_hillNoise.GetValue(x, y, z) + 1f) * maxHillAltittude;
                        var a = maskHeightmapValue - _settings.MinHillThreshold;

                        heightmap[i, j] += hillAlt * Math.Clamp(a / hillSmoothingFactor, 0f, 1f);
                    }

                    relativeY += step;
                }

                relativeX += step;
            }

            return new ValueTask<Result>(Result.CreateSuccess());
        }
    }
}
