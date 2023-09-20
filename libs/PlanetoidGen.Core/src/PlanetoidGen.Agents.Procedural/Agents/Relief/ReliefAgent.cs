using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlanetoidGen.Agents.Procedural.Agents.Relief.Models;
using PlanetoidGen.Agents.Procedural.Agents.Relief.Processors.Abstractions;
using PlanetoidGen.Agents.Procedural.Agents.Relief.Processors.Implementations;
using PlanetoidGen.BusinessLogic.Agents.Helpers;
using PlanetoidGen.BusinessLogic.Common.Constants;
using PlanetoidGen.BusinessLogic.Common.Helpers;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Documents;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Documents;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Documents;
using PlanetoidGen.Domain.Models.Info;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Procedural.Agents.Relief
{
    public class ReliefAgent : ITypedAgent<ReliefAgentSettings>
    {
        private const string ImageExtension = "png";

        private static readonly IImageEncoder _imageEncoder = new PngEncoder
        {
            BitDepth = PngBitDepth.Bit8,
            ColorType = PngColorType.RgbWithAlpha,
        };

        private ICoordinateMappingService? _coordinateMappingService;
        private IPlanetoidService? _planetoidService;
        private IFileContentService? _fileContentService;
        private ILogger<ReliefAgent>? _logger;

        private bool _initialized = false;
        private ReliefAgentSettings? _settings;

        public string Title => $"{nameof(PlanetoidGen)}.{nameof(Procedural)}.{nameof(ReliefAgent)}";

        public string Description => string.Empty;

        public bool IsVisibleToClient => true;

        public async ValueTask<Result> Execute(GenerationJobMessage job, CancellationToken token)
        {
            if (!_initialized)
            {
                return Result.CreateFailure(GeneralStringMessages.ObjectNotInitialized);
            }

            var planetoidResult = await _planetoidService!.GetPlanetoid(job.PlanetoidId, token);

            if (!planetoidResult.Success)
            {
                return Result.CreateFailure(planetoidResult.ErrorMessage!);
            }

            var heightmap = new float[_settings!.TileSizeInPixels, _settings!.TileSizeInPixels];
            var processors = new IReliefProcessor[]
            {
                new TerrainProcessor(
                    (int)planetoidResult.Data!.Seed,
                    job.Z,
                    _settings!,
                    tileStart: _coordinateMappingService!.ToCubic(new PlanarCoordinateModel(job.PlanetoidId, job.Z, job.X, job.Y)),
                    _coordinateMappingService),
                new ShorelineProcessor(_settings!),
                new SmoothProcessor(_settings!),
            };

            foreach (var processor in processors)
            {
                var result = await processor.Execute(heightmap, token);

                if (!result.Success)
                {
                    return result;
                }
            }

            return await SaveHeightmapAsync(job, heightmap, token);
        }

        public ReliefAgentSettings GetTypedDefaultSettings()
        {
            return new ReliefAgentSettings
            {
                TileSizeInPixels = 512,
                MaxMaskAltitude = 50f,
                MaskEdgeThresholdNegativePercentage = -0.32f,
                MaskEdgeThresholdPositivePercentage = 0.16f,
                MaxMountainAltittude = 1000f,
                MinMountainThreshold = 15f,
                MaxHillAltittude = 250f,
                MinHillThreshold = 25f,
                MinShorelineAltitude = 0f,
                MaxShorelineAltitude = 4f,
                GaussianKernelSize = 5,
            };
        }

        public ValueTask<string> GetDefaultSettings()
        {
            return GetTypedDefaultSettings().Serialize();
        }

        public ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies(int z)
        {
            return GetDependencies();
        }

        public ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies()
        {
            return new ValueTask<IEnumerable<AgentDependencyModel>>(Array.Empty<AgentDependencyModel>());
        }

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs(int z)
        {
            return GetOutputs();
        }

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs()
        {
            return new ValueTask<IEnumerable<DataTypeInfoModel>>(new DataTypeInfoModel[]
            {
                new DataTypeInfoModel(DataTypes.HeightMapRgba32Encoded, isRaster: true),
            });
        }

        public async ValueTask<Result> Initialize(string settings, IServiceProvider serviceProvider)
        {
            try
            {
                var deserializationResult = await GetTypedDefaultSettings().Deserialize(settings);

                if (!deserializationResult.Success)
                {
                    return Result.CreateFailure(deserializationResult);
                }

                _settings = deserializationResult.Data;
                _coordinateMappingService = serviceProvider.GetRequiredService<ICoordinateMappingService>();
                _planetoidService = serviceProvider.GetRequiredService<IPlanetoidService>();
                _fileContentService = serviceProvider.GetRequiredService<IFileContentService>();
                _logger = serviceProvider.GetRequiredService<ILogger<ReliefAgent>>();

                _initialized = true;
            }
            catch (Exception ex)
            {
                return Result.CreateFailure(ex);
            }

            return Result.CreateSuccess();
        }

        private async ValueTask<Result> SaveHeightmapAsync(
            GenerationJobMessage job,
            float[,] heightmap,
            CancellationToken token)
        {
            var conversionResult = await ConvertHeightmapToBytes(heightmap, token);

            if (!conversionResult.Success)
            {
                return Result.CreateFailure(conversionResult);
            }

            var fileId = FileModelFormatter.FormatFileId(
                job.PlanetoidId,
                DataTypes.HeightMapRgba32Encoded,
                job.Z,
                job.X,
                job.Y);

            return Result.Convert(await _fileContentService!.SaveFileContentWithDependencies(
                new FileModel
                {
                    FileId = fileId,
                    Content = new FileContentModel
                    {
                        Id = fileId,
                        FileName = FileModelFormatter.FormatFileName(job.Y, ImageExtension),
                        LocalPath = FileModelFormatter.FormatLocalPath(
                            job.PlanetoidId,
                            DataTypes.HeightMapRgba32Encoded,
                            job.Z,
                            job.X),
                        Content = conversionResult.Data,
                    },
                    TileBasedFileInfo = new TileBasedFileInfoModel(
                        fileId,
                        job.PlanetoidId,
                        job.Z,
                        job.X,
                        job.Y),
                },
                token));
        }

        private async ValueTask<Result<byte[]>> ConvertHeightmapToBytes(float[,] heightmap, CancellationToken token)
        {
            var tileSizePixels = _settings!.TileSizeInPixels;

            try
            {
                using (var stream = new MemoryStream())
                using (var image = new Image<Rgba32>(tileSizePixels, tileSizePixels))
                {
                    var pixel = new Rgba32();

                    for (var i = 0; i < tileSizePixels; ++i)
                    {
                        for (var j = 0; j < tileSizePixels; ++j)
                        {
                            var (r, g, b, a) = Utils.EncodeNoiseToRGBA32(heightmap[i, j]);
                            pixel.R = r;
                            pixel.G = g;
                            pixel.B = b;
                            pixel.A = a;

                            image[i, j] = pixel;
                        }
                    }

                    await image.SaveAsync(stream, _imageEncoder, token);
                    return Result<byte[]>.CreateSuccess(stream.ToArray());
                }
            }
            catch (Exception ex)
            {
                return Result<byte[]>.CreateFailure(ex);
            }
        }
    }
}
