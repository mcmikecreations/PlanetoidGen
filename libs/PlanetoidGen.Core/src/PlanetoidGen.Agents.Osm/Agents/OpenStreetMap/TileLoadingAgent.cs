using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Services.Implementations;
using PlanetoidGen.Agents.Standard.Constants.StringMessages;
using PlanetoidGen.BusinessLogic.Common.Helpers;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Documents;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Models.Services.GeoInfo;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Contracts.Services.Documents;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Documents;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static PlanetoidGen.Contracts.Constants.FileContentConstants;

namespace PlanetoidGen.Agents.Osm.Agents.OpenStreetMap
{
    public class TileLoadingAgent : ITypedAgent<OpenStreetMapTileLoadingAgentSettings>
    {
        private const string ImageWrapperExtension = "bin";

        private bool _initialized = false;
        private HttpClient? _httpClient;
        private OpenStreetMapTileLoadingAgentSettings? _settings;

        private IFileContentService? _fileContentService;
        private IOverpassApiService? _osmApi;
        private IPlanetoidService? _planetoidService;
        private ICoordinateMappingService? _coordinateMappingService;
        private ILogger? _logger;

        public string Title => $"{nameof(PlanetoidGen)}.{nameof(Osm)}.{nameof(TileLoadingAgent)}";

        public string Description => string.Empty;

        public bool IsVisibleToClient => true;

        public async ValueTask<Result> Execute(GenerationJobMessage job, CancellationToken token)
        {
            if (!_initialized)
            {
                _logger!.LogError("Failed to execute {JobId}, because {Agent} was not initialized.", job.Id, nameof(RailwayLoadingAgent));
                return Result.CreateFailure(GeneralStringMessages.ObjectNotInitialized);
            }

            var coordinatesSpherical = _coordinateMappingService!.ToSpherical(new PlanarCoordinateModel(job.PlanetoidId, job.Z, job.X, job.Y));

            var bbox = _osmApi!.GetBoundingBox(_coordinateMappingService!.ToAxisAlignedBoundingBox(coordinatesSpherical));

            var incrementedZoom = _osmApi!.GetZoom(coordinatesSpherical.Zoom, _settings!.ZoomIncrement ?? 0);

            var zoom = Math.Min(incrementedZoom, _settings!.MaxZoom ?? incrementedZoom);

            var (minTileX, minTileY) = _osmApi!.LonLat2Tile(bbox.West, bbox.North, zoom);
            var (maxTileX, maxTileY) = _osmApi!.LonLat2Tile(bbox.East, bbox.South, zoom);

            var domains = PrepareSubdomains(_settings!);

            var tileCount = 1 << zoom;
            int i, j, counter;
            var downloadTasks = new List<Task<Result<string>>>();

            counter = 0;
            i = minTileX - 1;
            do
            {
                i = (i + 1) % tileCount;
                j = minTileY - 1;
                do
                {
                    j = (j + 1) % tileCount;

                    downloadTasks.Add(DownloadTile(job.PlanetoidId, i, j, zoom, domains[counter], token));

                    counter = (counter + 1) % domains.Count;
                }
                while (j != maxTileY);
            }
            while (i != maxTileX);

            var results = await Task.WhenAll(downloadTasks);

            var failedResult = results.FirstOrDefault(x => !x.Success);

            if (failedResult != null)
            {
                return Result.Convert(failedResult);
            }

            return await SaveWrapperTile(job, results.Select(x => x.Data), token);
        }

        public OpenStreetMapTileLoadingAgentSettings GetTypedDefaultSettings()
        {
            return new OpenStreetMapTileLoadingAgentSettings();
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
            return new ValueTask<IEnumerable<DataTypeInfoModel>>(Array.Empty<DataTypeInfoModel>());
        }

        public async ValueTask<Result> Initialize(string settings, IServiceProvider serviceProvider)
        {
            try
            {
                _initialized = false;

                var deserializationResult = await GetTypedDefaultSettings().Deserialize(settings);

                if (!deserializationResult.Success)
                {
                    return Result.CreateFailure(deserializationResult);
                }

                _settings = deserializationResult.Data;

                ConfigureSettings(_settings!);

                _planetoidService = serviceProvider.GetService<IPlanetoidService>()
                    ?? throw new ArgumentNullException(nameof(IPlanetoidService));

                _coordinateMappingService = serviceProvider.GetService<ICoordinateMappingService>()
                    ?? throw new ArgumentNullException(nameof(ICoordinateMappingService));

                _fileContentService = serviceProvider.GetRequiredService<IFileContentService>()
                    ?? throw new ArgumentNullException(nameof(IFileContentService));

                _logger = serviceProvider.GetService<ILogger<RailwayLoadingAgent>>()
                    ?? throw new ArgumentNullException($"{nameof(ILogger<RailwayLoadingAgent>)}<{nameof(RailwayLoadingAgent)}>");

                var overpassLogger = serviceProvider.GetService<ILogger<OverpassApiService>>()
                    ?? throw new ArgumentNullException($"{nameof(ILogger<OverpassApiService>)}<{nameof(OverpassApiService)}>");
                var geoInfoOptions = serviceProvider.GetService<IOptions<GeoInfoServiceOptions>>()?.Value
                    ?? throw new ArgumentNullException($"{nameof(IOptions<GeoInfoServiceOptions>)}<{nameof(GeoInfoServiceOptions)}>");
                _osmApi = new OverpassApiService(geoInfoOptions, overpassLogger);

                _httpClient = new HttpClient();

                _initialized = true;
            }
            catch (Exception ex)
            {
                _initialized = false;
                return Result.CreateFailure(ex);
            }

            return Result.CreateSuccess();
        }

        private void ConfigureSettings(OpenStreetMapTileLoadingAgentSettings settings)
        {
            _settings = settings;
            _settings!.Url ??= "http://[a,b,c,d].tiles.mapbox.com/v4/{Style}/{Z}/{X}/{Y}.{ImageFormatExtension}?access_token={AccessToken}";
            _settings!.AccessToken ??= string.Empty;
            _settings!.ImageFormatExtension ??= "jpg";
            _settings!.Style ??= "mapbox.satellite";
            _settings!.MaxZoom ??= 19;
        }

        /// <summary>
        /// Prepares the subdomain list.
        /// </summary>
        /// <param name="options"></param>
        /// <returns>A list of domains with {0} as {Z}, {1} as {X}, {2} as {Y}.</returns>
        private static IList<string> PrepareSubdomains(OpenStreetMapTileLoadingAgentSettings options)
        {
            var url = options.Url!;
            var style = options.Style ?? string.Empty;
            var accessToken = options.AccessToken ?? string.Empty;
            var imageExt = options.ImageFormatExtension ?? string.Empty;

            var subdomainsStart = url.IndexOf('[');
            var subdomainsEnd = url.IndexOf(']');

            IEnumerable<string> domains;

            if (subdomainsStart != -1 && subdomainsEnd != -1)
            {
                var prefix = url.Substring(0, subdomainsStart);
                var suffix = url.Substring(subdomainsEnd + 1);
                domains = url
                    .Substring(subdomainsStart + 1, subdomainsEnd - subdomainsStart - 1)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => prefix + x + suffix);
            }
            else
            {
                domains = new string[1]
                {
                    url,
                };
            }

            domains = domains.Select(x => x
                .Replace("{Style}", style)
                .Replace("{ImageFormatExtension}", imageExt)
                .Replace("{AccessToken}", accessToken)
                .Replace("{Z}", "{0}")
                .Replace("{X}", "{1}")
                .Replace("{Y}", "{2}"));

            return domains.ToList();
        }

        private async Task<Result> SaveWrapperTile(GenerationJobMessage job, IEnumerable<string> images, CancellationToken token)
        {
            var fileId = FileModelFormatter.FormatFileId(
                job.PlanetoidId,
                Constants.DataTypes.OsmImage,
                job.Z,
                job.X,
                job.Y);

            return Result.Convert(await _fileContentService!.SaveFileContentWithDependencies(
                new FileModel
                {
                    FileId = fileId,
                    TileBasedFileInfo = new TileBasedFileInfoModel(
                        fileId,
                        job.PlanetoidId,
                        job.Z,
                        job.X,
                        job.Y),
                    Content = new FileContentModel
                    {
                        Id = fileId,
                        FileName = FileModelFormatter.FormatFileName(job.Y, ImageWrapperExtension),
                        LocalPath = FileModelFormatter.FormatLocalPath(
                            job.PlanetoidId,
                            Constants.DataTypes.OsmImage,
                            job.Z,
                            job.X),
                        Content = Array.Empty<byte>(),
                    },
                    DependentFiles = images.Select(x => new FileDependencyModel(fileId, x, true, false)),
                },
                token));
        }

        private async Task<Result<string>> DownloadTile(int planetoidId, int x, int y, int z, string baseUrl, CancellationToken token)
        {
            var fileExtension = _settings!.ImageFormatExtension ?? "png";
            var dataType = DataTypes.Image;
            var style = _settings!.Style ?? $"TilesRaw_{planetoidId}";

            var fileId = FileModelFormatter.FormatFileId(
                0,
                style,
                (short)z,
                x,
                y);

            var fileExists = await _fileContentService!.FileIdExists(fileId, token);

            if (!fileExists.Success)
            {
                return Result<string>.CreateFailure(fileExists);
            }
            else if (fileExists.Data)
            {
                return Result<string>.CreateSuccess(fileId);
            }
            else
            {
                var requestUrl = string.Format(baseUrl, z, x, y);

                var response = await _httpClient!.GetAsync(requestUrl, token);

                if (!response.IsSuccessStatusCode)
                {
                    var responseMessage = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"Tile loading request {requestUrl} failed with code {response.StatusCode} and message {responseMessage}.";

                    _logger!.LogError(errorMessage);
                    return Result<string>.CreateFailure(errorMessage);
                }

                var (lon, lat) = _osmApi!.Tile2LonLat(x, y, z);

                var fileModel = new FileModel()
                {
                    FileId = fileId,
                    Content = new FileContentModel()
                    {
                        Id = fileId,
                        FileName = $"{y}.{fileExtension}",
                        LocalPath = FileModelFormatter.FormatLocalPath(
                            0,
                            style,
                            (short)z,
                            x),
                        Content = await response.Content.ReadAsByteArrayAsync(),
                        Attributes = new Dictionary<string, string>
                        {
                            { TileMapAttributes.Srid, _settings!.SourceProjection.ToString() },
                            { TileMapAttributes.Location, $"{lon};{lat}" },
                            { CommonAttributes.ContentType, "image/" + fileExtension },
                        },
                    },
                };

                var addResult = await _fileContentService!.SaveFileContentWithDependencies(fileModel, token);

                return addResult.Success ? Result<string>.CreateSuccess(fileId) : Result<string>.CreateFailure(addResult);
            }
        }
    }
}
