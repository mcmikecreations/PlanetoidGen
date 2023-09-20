using PlanetoidGen.BusinessLogic.Common.Services.Generation;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode()]
public class CartesianEulerDebugger : MonoBehaviour
{
    private CoordinateMappingService coordinateMapping;
    private GeometryConversionService geometryConversion;
    private bool init = false;

    public CoordinateMappingService CoordinateMapping
    {
        get
        {
            if (coordinateMapping == null && init)
            {
                init = false;
                Init();
            }

            return coordinateMapping;
        }
    }

    public GeometryConversionService GeometryConversion
    {
        get
        {
            if (geometryConversion == null && init)
            {
                init = false;
                Init();
            }

            return geometryConversion;
        }
    }

    public int zoomValue = 0;

    private void Init()
    {
        if (init)
        {
            return;
        }

        coordinateMapping = new CoordinateMappingService(new ProjCubeProjectionService());
        geometryConversion = new GeometryConversionService(null);
        init = true;
    }

    private void Awake()
    {
        Init();
    }

    private void Update()
    {
        Init();
    }
}
#endif
