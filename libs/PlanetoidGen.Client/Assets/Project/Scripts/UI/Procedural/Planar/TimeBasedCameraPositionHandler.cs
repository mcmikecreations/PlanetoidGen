using PlanetoidGen.BusinessLogic.Common.Constants;
using PlanetoidGen.BusinessLogic.Common.Helpers;
using PlanetoidGen.Client.BusinessLogic.Managers;
using PlanetoidGen.Client.Contracts.Models.Args;
using PlanetoidGen.Client.Contracts.Services.Controllers;
using PlanetoidGen.Client.Contracts.Services.Loaders;
using PlanetoidGen.Client.Contracts.Services.Procedural;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Enums;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class TimeBasedCameraPositionHandler : MonoBehaviour
{
    [Min(0)]
    public int planetoidId = 0;

    public float radius = 6_371_000.0f;

    [Range(0, 61)]
    public short zoom = 0;

    [Range(1.0f, 10.0f)]
    public float logIntervalSeconds = 1.0f;

    public float tileScale = 100.0f;

    public float minHeight = -0.1f;

    public float maxHeight = 0.1f;

    public int tesselation = 5;

    public int tileCountRow = 2;

    public Shader shader;

    public GameObject world;

    private ICoordinateMappingService _coordinateMappingService;
    private ITileGenerationStreamController _tileGenerationStreamController;
    private IBinaryContentStreamController _binaryContentStreamController;
    private IBinaryContentController _binaryContentController;
    private ITextureLoadingService _textureLoadingService;
    private IGeometryConversionService _geometryConversionService;
    private IPlanarTileService _planarTileService;

    private Vector3 _referencePoint = new(0, 0, 0);
    private Vector3 _currentTile;
    private PlanarCoordinateModel _currentTilePlanar;

    private int _tileSize;

    private PlanetoidInfoModel _planetoid;
    private SpatialReferenceSystemModel _wgs1984;
    private SpatialReferenceSystemModel _webMercator;

    private bool _initDebug = false;

    private readonly ConcurrentQueue<FileEventArgs> _filesQueue = new();

    async void Start()
    {
        InitServices();

        _planetoid = new PlanetoidInfoModel(planetoidId, string.Empty, 0, radius);
        _wgs1984 = _geometryConversionService.GenerateSRSModelGeographic(_planetoid);
        _webMercator = _geometryConversionService.GenerateSRSModelProjected(_planetoid);
        _tileSize = (int)(_coordinateMappingService.SphericalTileSizeRadians(zoom) * radius / tileScale);

        _tileGenerationStreamController.Subscribe(async (sender, args) =>
        {
            await RequestFileContent(args);
        });

        _binaryContentStreamController.Subscribe((sender, args) =>
        {
            _filesQueue.Enqueue(args);
        });

        await _binaryContentStreamController.StartStream(default);
        await _tileGenerationStreamController.StartStream(default);

        InvokeRepeating(nameof(HandleCameraPositionUpdate), 0f, logIntervalSeconds);
        InvokeRepeating(nameof(ProcessFileQueue), 0f, logIntervalSeconds);
    }

    private void ProcessFileQueue()
    {
        while (_filesQueue.TryDequeue(out var args))
        {
            RenderTile(args);
        }
    }

    private async Task HandleCameraPositionUpdate()
    {
        if (!_initDebug)
        {
            var cameraPosition = transform.position;

            Debug.Log("Time-based camera position: " + cameraPosition);

            // TODO: check if this conversion is properly configured
            var coords = _geometryConversionService.ToAssimpVectors(
                new CoordinateModel[]
                {
                    new CoordinateModel(cameraPosition.x, cameraPosition.z, 0.0f),
                },
                _planetoid,
                yUp: false,
                _webMercator,
                _wgs1984);
            var coord = coords.First();
            var spherical = new SphericalCoordinateModel(_planetoid.Id, coord[0], coord[1], zoom);

            // TODO: check if this condition will be enough
            var newCurrentTilePlanar = _coordinateMappingService.ToPlanar(spherical);

            if (_currentTilePlanar == null || newCurrentTilePlanar.X != _currentTilePlanar.X || newCurrentTilePlanar.Y != _currentTilePlanar.Y)
            {
                _currentTilePlanar = newCurrentTilePlanar;

                await _tileGenerationStreamController.SendTileGenerationRequest(
                    planetoidId,
                    GenerateTilesForRequest(spherical, tileCountRow));
            }

            _initDebug = true;
        }
    }

    private async Task RequestFileContent(TileEventArgs args)
    {
        var tileInfo = args.TileInfos.First();
        var fileId = FileModelFormatter.FormatFileId(
            _planetoid.Id,
            DataTypes.HeightMapGrayscaleEncoded,
            tileInfo.Z,
            tileInfo.X,
            tileInfo.Y);

        Debug.Log($"Requesting file content {fileId}");

        await _binaryContentStreamController.SendFileContentRequest(fileId);
    }

    private void RenderTile(FileEventArgs args)
    {
        var gameObjectId = args.File.FileId;
        var existing = GameObject.Find(gameObjectId);

        if (existing != null)
        {
            return;
        }

        Debug.Log($"Rendering file content {args.File.FileId}");

        var info = args.File.TileBasedFileInfo;
        var spherical = _coordinateMappingService.ToSpherical(new PlanarCoordinateModel(_planetoid.Id, info.Z, info.X, info.Y));

        var coords = _geometryConversionService.ToAssimpVectors(
            new CoordinateModel[]
            {
                new CoordinateModel(spherical.Longtitude * Mathf.Rad2Deg, spherical.Latitude * Mathf.Rad2Deg, 0),
            },
            _planetoid,
            yUp: true,
            _wgs1984,
            _webMercator);

        var coord = coords.First();
        var tileCoordinates = new Vector3(
            (float)(coord[0] - _referencePoint.x),
            (float)(coord[1] - _referencePoint.y),
            (float)(coord[2] - _referencePoint.z)) / tileScale;

        var texture = _textureLoadingService.Load(args.File.Content.Content);
        var material = new Material(shader);
        material.SetTexture("_BaseMap", texture);
        material.SetFloat("_Min_Height", minHeight);
        material.SetFloat("_Max_Height", maxHeight);

        var child = new GameObject(gameObjectId);
        child.transform.parent = world.transform;
        child.transform.position = tileCoordinates;
        child.transform.localScale = new Vector3(1 / tileScale, 1 / tileScale, 1 / tileScale);

        // Creates mesh for tile in **meters**.
        // To convert units use transform.localScale and tileScale.
        // Then set transform.localPosition which should align the tiles correctly.
        // Line intersection calculation inside the service may be replaced by simpler vector math.
        // Also removed normal map mapping from pixel shader due to visual artifacts.
        var filter = child.AddComponent<MeshFilter>();
        filter.mesh = _planarTileService.GenerateTile(
            _coordinateMappingService.ToCubic(spherical),
            _planetoid,
            _wgs1984,
            _webMercator,
            tesselation);

        var renderer = child.AddComponent<MeshRenderer>();
        renderer.material = material;
    }

    private void OnDestroy()
    {
        _tileGenerationStreamController.StopStreamIfExists();
        _binaryContentStreamController.StopStreamIfExists();
    }

    private IEnumerable<SphericalLODCoordinateModel> GenerateTilesForRequest(SphericalCoordinateModel baseTile, int tileCountRow)
    {
        var baseTilePivot = _coordinateMappingService.ToCubic(baseTile);

        var tiles = new List<CubicCoordinateModel>() { baseTilePivot };

        // For 4 tile row add 1 tile to the right
        for (int i = 0; i < Mathf.CeilToInt(tileCountRow / 2f) - 1; ++i)
        {
            tiles.Add(_coordinateMappingService.RelativeTile(tiles.Last(), RelativeTileDirectionType.Right));
        }

        // For 4 tile row add 2 tiles to the left
        for (int i = 0; i < Mathf.FloorToInt(tileCountRow / 2f); ++i)
        {
            tiles.Insert(0, _coordinateMappingService.RelativeTile(tiles.First(), RelativeTileDirectionType.Left));
        }

        tiles = tiles
            .SelectMany(x =>
            {
                var tilesLocal = new List<CubicCoordinateModel>() { x };

                // For 4 tile row add 1 tile up
                for (int i = 0; i < Mathf.CeilToInt(tileCountRow / 2f) - 1; ++i)
                {
                    tilesLocal.Add(_coordinateMappingService.RelativeTile(tilesLocal.Last(), RelativeTileDirectionType.Up));
                }

                // For 4 tile row add 2 tiles down
                for (int i = 0; i < Mathf.FloorToInt(tileCountRow / 2f); ++i)
                {
                    tilesLocal.Insert(0, _coordinateMappingService.RelativeTile(tilesLocal.First(), RelativeTileDirectionType.Down));
                }

                return tilesLocal;
            })
            .Distinct()
            .ToList();

        return tiles.Select(x =>
        {
            var spherical = _coordinateMappingService.ToSpherical(x);
            return new SphericalLODCoordinateModel(spherical.Longtitude, spherical.Latitude, spherical.Zoom);
        });
    }

    private void InitServices()
    {
        var serviceManager = ServiceManager.Instance;

        _coordinateMappingService = serviceManager.GetService<ICoordinateMappingService>();
        _tileGenerationStreamController = serviceManager.GetService<ITileGenerationStreamController>();
        _binaryContentController = serviceManager.GetService<IBinaryContentController>();
        _binaryContentStreamController = serviceManager.GetService<IBinaryContentStreamController>();
        _textureLoadingService = serviceManager.GetService<ITextureLoadingService>();
        _geometryConversionService = serviceManager.GetService<IGeometryConversionService>();
        _planarTileService = serviceManager.GetService<IPlanarTileService>();
    }
}
