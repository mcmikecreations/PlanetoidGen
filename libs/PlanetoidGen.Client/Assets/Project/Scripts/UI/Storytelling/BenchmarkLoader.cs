using PlanetoidGen.Client.BusinessLogic.Managers;
using PlanetoidGen.Client.Contracts.Services.Controllers;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Enums;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class BenchmarkLoader : MonoBehaviour
{
    public int tileCountRow = 4;
    public int seed = 42;
    public double lon = 38.6321;
    public double lat = 48.7249;

    private ITileGenerationStreamController _tileGenerationStreamController;
    private IBinaryContentStreamController _binaryContentStreamController;
    private System.Diagnostics.Stopwatch _stopwatch;

    async void Start()
    {
        // Examples of the GRPC controllers usage

        try
        {
            var serviceManager = ServiceManager.Instance;
            var planetoidController = serviceManager.GetService<IPlanetoidController>();
            var lodsController = serviceManager.GetService<IGenerationLODController>();
            var agentsController = serviceManager.GetService<IAgentController>();
            _tileGenerationStreamController = serviceManager.GetService<ITileGenerationStreamController>();
            var binaryContentController = serviceManager.GetService<IBinaryContentController>();
            _binaryContentStreamController = serviceManager.GetService<IBinaryContentStreamController>();

            var coordinateMappingService = serviceManager.GetService<ICoordinateMappingService>();

            const short zoom = 12;
            const short lod = 12;
            var planetoid = new PlanetoidInfoModel(0, "Earth", seed, 6_371_000);
            // Rough coordinates of Партизанська вулиця, Novotoshkivske, Гірська міська громада, Sievierodonetsk Raion, Luhansk Oblast, 93891, Ukraine
            var baseTile = new SphericalCoordinateModel(0, lon / 180.0 * Math.PI, lat / 180.0 * Math.PI, zoom);
            var agents = new List<AgentInfoModel>
            {
                new AgentInfoModel(0, 0, "PlanetoidGen.Osm.TileLoadingAgent", "{\"Url\":null,\"Style\":null,\"AccessToken\":\"pk.eyJ1IjoibS11cmlqIiwiYSI6ImNsOW9qZnJ2bjAyanQzcXFwZTU4enJrZHAifQ.PdOD7o7Kw43590-qdnvWuQ\",\"MaxZoom\":null,\"ZoomIncrement\":2,\"ImageFormatExtension\":null,\"SourceProjection\":4326}", false),
                new AgentInfoModel(0, 0, "PlanetoidGen.Osm.BuildingLoadingAgent", "{\"PushToGeoServer\":false,\"OverpassBaseUrl\":\"http://overpass.overpass:9096/api/interpreter\",\"EntityTableSchema\":null,\"EntityTableName\":null}", false),
                new AgentInfoModel(0, 0, "PlanetoidGen.Osm.HighwayLoadingAgent", "{\"PushToGeoServer\":false,\"OverpassBaseUrl\":\"http://overpass.overpass:9096/api/interpreter\",\"EntityTableSchema\":null,\"EntityTableName\":null}", false),
                new AgentInfoModel(0, 0, "PlanetoidGen.Osm.RailwayLoadingAgent", "{\"PushToGeoServer\":false,\"OverpassBaseUrl\":\"http://overpass.overpass:9096/api/interpreter\",\"EntityTableSchema\":null,\"EntityTableName\":null}", false),
                new AgentInfoModel(0, 0, "PlanetoidGen.Osm.BuildingInformationSeedingAgent", "{\"Seed\":0,\"DefaultFloorHeight\":2.7,\"BuildingTableSchema\":null,\"BuildingTableName\":null}", false),
                new AgentInfoModel(0, 0, "PlanetoidGen.Osm.BuildingTo3dModelAgent", "{\"YUp\":true,\"MergeModels\":true,\"BestLODSize\":800,\"WorstLODSize\":3200,\"EntityTableSchema\":null,\"EntityTableName\":null,\"FoundationHeight\":5,\"SourceProjection\":4326,\"DestinationProjection\":3857}", false),
                new AgentInfoModel(0, 0, "PlanetoidGen.Osm.HighwayTo3dModelAgent", "{\"YUp\":true,\"MergeModels\":true,\"BestLODSize\":800,\"WorstLODSize\":3200,\"EntityTableSchema\":null,\"EntityTableName\":null,\"FoundationHeight\":5,\"SourceProjection\":4326,\"DestinationProjection\":3857}", false),
                new AgentInfoModel(0, 0, "PlanetoidGen.Osm.RailwayTo3dModelAgent", "{\"YUp\":true,\"MergeModels\":true,\"BestLODSize\":800,\"WorstLODSize\":3200,\"EntityTableSchema\":null,\"EntityTableName\":null,\"FoundationHeight\":5,\"SourceProjection\":4326,\"DestinationProjection\":3857}", false),
                new AgentInfoModel(0, 0, "PlanetoidGen.DataReportingAgent", "{}", true),
            };

            _stopwatch = new System.Diagnostics.Stopwatch();

            _stopwatch.Start();

            var planetoidId = await planetoidController.AddPlanetoid(planetoid);

            var insertLODsResult = await lodsController.InsertLODs(new List<GenerationLODModel> { new GenerationLODModel(planetoidId, lod, zoom), });

            var setAgentsResult = await agentsController.SetAgents(planetoidId, agents);

            _stopwatch.Stop();

            Debug.LogWarning($"Planetoid setup: {_stopwatch.ElapsedMilliseconds}");

            _stopwatch.Reset();

            Debug.LogWarning($"Seed {seed}");
            planetoid = new PlanetoidInfoModel(planetoidId, planetoid.Title, planetoid.Seed, planetoid.Radius);
            baseTile = new SphericalCoordinateModel(planetoidId, baseTile.Longtitude, baseTile.Latitude, baseTile.Zoom);

            var tiles = GenerateTilesForRequest(baseTile, coordinateMappingService);
            int tilesLeftToGenerate = tiles.Count();

            Debug.LogWarning($"Starting tile generation {tilesLeftToGenerate}...");

            if (tilesLeftToGenerate < 1)
            {
                return;
            }

            _tileGenerationStreamController.Subscribe(async (sender, args) =>
            {
                Debug.Log("Tiles " + string.Join('\n', args.TileInfos.Select(x => x.ToString())));
                int res = Interlocked.Add(ref tilesLeftToGenerate, -args.TileInfos.Count());

                if (res <= 0)
                {
                    _stopwatch.Stop();

                    await _tileGenerationStreamController.StopStreamIfExists();
                }
            });

            await _tileGenerationStreamController.StartStream(default);

            _stopwatch.Start();

            await _tileGenerationStreamController.SendTileGenerationRequest(planetoidId, tiles);

            var dataLoadTask = Task.Run(async () =>
            {
                while (_stopwatch.IsRunning) { }

                Debug.LogWarning($"Tile generation: {_stopwatch.ElapsedMilliseconds}");

                _stopwatch.Reset();

                Debug.Log("Starting file gathering...");

                _stopwatch.Start();

                var allFileIdTasks = await Task.WhenAll(tiles
                    .Select(async (x) =>
                    {
                        var planarTile = coordinateMappingService.ToPlanar(new SphericalCoordinateModel(planetoidId, x.Longtitude, x.Latitude, x.LOD));
                        var tileModel = new PlanetoidGen.Contracts.Models.Documents.GenericTileInfo()
                        {
                            PlanetoidId = planetoidId,
                            Z = planarTile.Z,
                            X = planarTile.X,
                            Y = planarTile.Y,
                        };
                        return await binaryContentController.GetFileContentIdsByTile(tileModel, false, false, default);
                    }));

                _stopwatch.Stop();

                var allFileIds = allFileIdTasks.SelectMany(x => x).Distinct();

                int filesLeftToDownload = allFileIds.Count();

                Debug.LogWarning($"Started file loading {filesLeftToDownload}...");

                if (filesLeftToDownload < 1)
                {
                    return;
                }

                _binaryContentStreamController.Subscribe(async (sender, args) =>
                {
                    Debug.Log($"File {args.File.FileId}");
                    int res = Interlocked.Decrement(ref filesLeftToDownload);

                    if (res <= 0)
                    {
                        _stopwatch.Stop();

                        await _binaryContentStreamController.StopStreamIfExists();

                        Debug.LogWarning($"File loading: {_stopwatch.ElapsedMilliseconds}");
                    }
                });

                await _binaryContentStreamController.StartStream(default);

                _stopwatch.Start();

                foreach (var fileId in allFileIds)
                {
                    await _binaryContentStreamController.SendFileContentRequest(fileId);
                }
            });

            await dataLoadTask;
        }
        catch (InvalidOperationException ex)
        {
            Debug.Log(ex.ToString());
        }
    }

    private async void OnApplicationQuit()
    {
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Stop();
        }

        await _tileGenerationStreamController.StopStreamIfExists();
        await _binaryContentStreamController.StopStreamIfExists();
    }

    private IEnumerable<SphericalLODCoordinateModel> GenerateTilesForRequest(SphericalCoordinateModel baseTile, ICoordinateMappingService coordinateMappingService)
    {
        var baseTilePivot = coordinateMappingService.ToCubic(/*coordinateMappingService.ToPlanar(*/baseTile/*)*/);
        //var tileSizeCubic = coordinateMappingService.TileSizeCubic(baseTile.Zoom);

        // Get the center of the tile to avoid tile boundary rounding errors
        //baseTilePivot = new CubicCoordinateModel(baseTilePivot.PlanetoidId, baseTilePivot.Face, baseTilePivot.Z, baseTilePivot.X + tileSizeCubic / 2.0, baseTilePivot.Y / 2.0);

        var tiles = new List<CubicCoordinateModel>() { baseTilePivot };

        // For 4 tile row add 1 tile to the right
        for (int i = 0; i < Mathf.CeilToInt(tileCountRow / 2f) - 1; ++i)
        {
            tiles.Add(coordinateMappingService.RelativeTile(tiles.Last(), RelativeTileDirectionType.Right));
        }

        // For 4 tile row add 2 tiles to the left
        for (int i = 0; i < Mathf.FloorToInt(tileCountRow / 2f); ++i)
        {
            tiles.Insert(0, coordinateMappingService.RelativeTile(tiles.First(), RelativeTileDirectionType.Left));
        }

        tiles = tiles
            .SelectMany(x =>
            {
                var tilesLocal = new List<CubicCoordinateModel>() { x };

                // For 4 tile row add 1 tile up
                for (int i = 0; i < Mathf.CeilToInt(tileCountRow / 2f) - 1; ++i)
                {
                    tilesLocal.Add(coordinateMappingService.RelativeTile(tilesLocal.Last(), RelativeTileDirectionType.Up));
                }

                // For 4 tile row add 2 tiles down
                for (int i = 0; i < Mathf.FloorToInt(tileCountRow / 2f); ++i)
                {
                    tilesLocal.Insert(0, coordinateMappingService.RelativeTile(tilesLocal.First(), RelativeTileDirectionType.Down));
                }

                return tilesLocal;
            })
            .Distinct()
            .ToList();

        return tiles.Select(x =>
        {
            var spherical = coordinateMappingService.ToSpherical(x);
            return new SphericalLODCoordinateModel(spherical.Longtitude, spherical.Latitude, spherical.Zoom);
        });
    }
}
