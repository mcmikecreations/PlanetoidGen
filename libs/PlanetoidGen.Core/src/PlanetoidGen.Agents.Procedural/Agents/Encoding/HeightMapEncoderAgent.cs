using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlanetoidGen.Agents.Procedural.Agents.Encoding.Models;
using PlanetoidGen.BusinessLogic.Agents.Helpers;
using PlanetoidGen.BusinessLogic.Common.Constants;
using PlanetoidGen.BusinessLogic.Common.Helpers;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Documents;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Documents;
using PlanetoidGen.Domain.Enums;
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
using static PlanetoidGen.Contracts.Constants.FileContentConstants;

namespace PlanetoidGen.Agents.Procedural.Agents.Encoding
{
    public class HeightMapEncoderAgent : ITypedAgent<HeightMapEncoderAgentSettings>
    {
        private const string ImageExtension = "png";

        private static readonly IImageEncoder _imageEncoder = new PngEncoder
        {
            BitDepth = PngBitDepth.Bit16,
            ColorType = PngColorType.Grayscale,
        };

        private IFileContentService? _fileContentService;
        private ILogger<HeightMapEncoderAgent>? _logger;

        private bool _initialized = false;
        private HeightMapEncoderAgentSettings? _settings;

        public string Title => $"{nameof(PlanetoidGen)}.{nameof(Procedural)}.{nameof(HeightMapEncoderAgent)}";

        public string Description => string.Empty;

        public bool IsVisibleToClient => true;

        public async ValueTask<Result> Execute(GenerationJobMessage job, CancellationToken token)
        {
            if (!_initialized)
            {
                return Result.CreateFailure(GeneralStringMessages.ObjectNotInitialized);
            }

            var fileId = FileModelFormatter.FormatFileId(
                job.PlanetoidId,
                DataTypes.HeightMapRgba32Encoded,
                job.Z,
                job.X,
                job.Y);

            var getFileResult = await _fileContentService!.GetFileContent(fileId, token);
            var file = getFileResult.Data;

            if (!getFileResult.Success)
            {
                return Result.CreateFailure(getFileResult);
            }

            var content = file.Content?.Content;

            if (content == null || content.Length == 0)
            {
                return Result.CreateFailure($"File with id '{file.FileId}' has empty content.");
            }

            var encodingResult = await EncodeHeightMapAsync(content, token);

            var (heightmap, minHeight) = encodingResult.Data;

            return encodingResult.Success
                ? await SaveHeightmapAsync(job, heightmap, minHeight, token)
                : Result.CreateFailure(encodingResult);
        }

        public HeightMapEncoderAgentSettings GetTypedDefaultSettings()
        {
            return new HeightMapEncoderAgentSettings
            {
                MaxMaskAltitude = 50f,
                MaxAltitude = 1500f,
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
            return new ValueTask<IEnumerable<AgentDependencyModel>>(new AgentDependencyModel[]
            {
                new AgentDependencyModel(
                    RelativeTileDirectionType.Current,
                    new DataTypeInfoModel(DataTypes.HeightMapRgba32Encoded, isRaster: true))
            });
        }

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs(int z)
        {
            return GetOutputs();
        }

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs()
        {
            return new ValueTask<IEnumerable<DataTypeInfoModel>>(new DataTypeInfoModel[]
            {
                new DataTypeInfoModel(DataTypes.HeightMapGrayscaleEncoded, isRaster: true),
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
                _fileContentService = serviceProvider.GetRequiredService<IFileContentService>();
                _logger = serviceProvider.GetRequiredService<ILogger<HeightMapEncoderAgent>>();

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
            byte[] content,
            float minHeight,
            CancellationToken token)
        {
            var fileId = FileModelFormatter.FormatFileId(
                job.PlanetoidId,
                DataTypes.HeightMapGrayscaleEncoded,
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
                            DataTypes.HeightMapGrayscaleEncoded,
                            job.Z,
                            job.X),
                        Content = content,
                        Attributes = new Dictionary<string, string>
                        {
                            { HeightMapAttributes.MinHeight, minHeight.ToString() }
                        },
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

        private async ValueTask<Result<(byte[] heightmap, float minHeight)>> EncodeHeightMapAsync(byte[]? content, CancellationToken token)
        {
            try
            {
                float maxMaskHeight = _settings!.MaxMaskAltitude;
                float maxHeight = _settings!.MaxAltitude;

                using (var stream = new MemoryStream())
                using (var image = Image.Load<Rgba32>(content))
                using (var imageEncoded = new Image<L16>(image.Width, image.Height))
                {
                    var encodedPixel = new L16();
                    var minHeight = float.MaxValue;
                    var heightmap = new float[image.Width, image.Height];

                    for (var i = 0; i < image.Width; ++i)
                    {
                        for (var j = 0; j < image.Height; ++j)
                        {
                            var pixel = image[i, j];
                            heightmap[i, j] = Utils.DecodeNoiseFromRGBA32(pixel.R, pixel.G, pixel.B, pixel.A);

                            if (heightmap[i, j] < minHeight)
                            {
                                minHeight = heightmap[i, j];
                            }
                        }
                    }

                    for (var i = 0; i < image.Width; ++i)
                    {
                        for (var j = 0; j < image.Height; ++j)
                        {
                            var height = heightmap[i, j] - minHeight;

                            checked
                            {
                                float h = Math.Clamp(height + maxMaskHeight, 0f, maxHeight) / maxHeight;

                                encodedPixel.PackedValue = (ushort)(h * ushort.MaxValue);
                            }

                            imageEncoded[i, j] = encodedPixel;
                        }
                    }

                    await imageEncoded.SaveAsync(stream, _imageEncoder, token);

                    return Result<(byte[], float)>.CreateSuccess((stream.ToArray(), minHeight));
                }
            }
            catch (Exception ex)
            {
                return Result<(byte[], float)>.CreateFailure(ex);
            }
        }
    }
}
