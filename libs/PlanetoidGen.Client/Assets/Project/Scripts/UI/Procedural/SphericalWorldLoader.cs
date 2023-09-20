using PlanetoidGen.BusinessLogic.Common.Constants;
using PlanetoidGen.BusinessLogic.Common.Helpers;
using PlanetoidGen.Client.BusinessLogic.Managers;
using PlanetoidGen.Client.Contracts.Services.Loaders;
using PlanetoidGen.Client.Contracts.Services.Procedural;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using UnityEngine;
using static PlanetoidGen.Contracts.Services.Generation.ICubeProjectionService;

public class SphericalWorldLoader : MonoBehaviour
{
    private ICoordinateMappingService _coordinateMapping;
    private ISphericalTileService _sphericalTileService;
    private ITextureLoadingService _textureLoadingService;

    [Range(0, 10)]
    public int tesselation = 0;

    [Range(1, 100)]
    public float radius = 1;

    [Range(0, 5)]
    public short zoom = 0;

    [Min(0)]
    public int planetoidId = 0;

    public Shader shader;

    public Material testMaterial;

    public bool useTestMaterial;

    public float minHeight = -0.1f;

    public float maxHeight = 0.1f;

    public float rotationSpeed = 10f;

    void Start()
    {
        _coordinateMapping = ServiceManager.Instance.GetService<ICoordinateMappingService>();
        _sphericalTileService = ServiceManager.Instance.GetService<ISphericalTileService>();
        _textureLoadingService = ServiceManager.Instance.GetService<ITextureLoadingService>();

        var tilesCountPerCubeSide = 1L << zoom;
        var step = 1.0 / tilesCountPerCubeSide;

        for (short f = 0; f < 6; ++f)
        {
            for (int i = 0; i < tilesCountPerCubeSide; ++i)
            {
                for (int j = 0; j < tilesCountPerCubeSide; ++j)
                {
                    var coords = new CubicCoordinateModel(
                        planetoidId,
                        f,
                        zoom,
                        x: (-1 + step) + 2 * i * step,
                        y: (-1 + step) + 2 * j * step);

                    var planar = _coordinateMapping.ToPlanar(coords);
                    var fileId = FileModelFormatter.FormatFileId(
                        planetoidId,
                        DataTypes.HeightMapGrayscaleEncoded,
                        planar.Z,
                        planar.X,
                        planar.Y);

                    var texture = _textureLoadingService.Load($"Assets/Resources/Streamed/{fileId}.png");
                    var material = new Material(shader);
                    material.SetTexture("_BaseMap", texture);
                    material.SetFloat("_Radius", radius);
                    material.SetFloat("_Min_Height", minHeight);
                    material.SetFloat("_Max_Height", maxHeight);

                    coords = RemapTopBottom(coords);

                    var child = new GameObject($"Tile_{(FaceSide)coords.Face}_{planar.X}_{planar.Y}");
                    child.transform.parent = transform;

                    AdjustPositionAndScale(coords, child);

                    var filter = child.AddComponent<MeshFilter>();
                    filter.mesh = _sphericalTileService.GenerateTile(
                        coords,
                        radius,
                        tesselation);

                    var renderer = child.AddComponent<MeshRenderer>();
                    renderer.material = useTestMaterial ? testMaterial : material;
                }
            }
        }
    }

    private void Update()
    {
        if (rotationSpeed > 0f)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Workaround due to a known issue with tile top/bottom face index.
    /// </summary>
    private static CubicCoordinateModel RemapTopBottom(CubicCoordinateModel model)
    {
        var face = (FaceSide)model.Face;

        if (face == FaceSide.FaceTop)
        {
            face = FaceSide.FaceBottom;
        }
        else if (face == FaceSide.FaceBottom)
        {
            face = FaceSide.FaceTop;
        }

        return new CubicCoordinateModel(model.PlanetoidId, (short)face, model.Z, model.X, model.Y);
    }

    /// <summary>
    /// Workaround due to unknown issue.
    /// </summary>
    private static void AdjustPositionAndScale(CubicCoordinateModel coords, GameObject child)
    {
        if ((FaceSide)coords.Face == FaceSide.FaceTop)
        {
            child.transform.position = new Vector3(-0.06f, -1.05f, 0f);
            child.transform.localScale = new Vector3(1.03f, 1f, 1.02f);
        }
        else if ((FaceSide)coords.Face == FaceSide.FaceBottom)
        {
            ////child.transform.position = new Vector3(0.0044f, 0.0369f, 0.0005f);
            child.transform.localScale = new Vector3(1.02f, 0.98f, 1f);
        }
    }
}
