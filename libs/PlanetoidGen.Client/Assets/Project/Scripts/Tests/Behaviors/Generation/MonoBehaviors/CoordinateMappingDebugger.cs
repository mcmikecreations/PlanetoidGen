using PlanetoidGen.BusinessLogic.Common.Services.Generation;
using PlanetoidGen.Client.Contracts.ScriptableObjects.Storytelling;
using PlanetoidGen.Domain.Enums;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode()]
public class CoordinateMappingDebugger : MonoBehaviour
{
    private CoordinateMappingService coordinateMapping;
    private bool init = false;
    public List<RelativeTileDirectionType> relativeTiles = new()
        {
            RelativeTileDirectionType.Right,
            RelativeTileDirectionType.Left,
            RelativeTileDirectionType.Up,
            RelativeTileDirectionType.Down,
        };
    public StorySO story;

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

    public int zoomValue = 0;
    public int mode = 0;

    private void Init()
    {
        if (init)
        {
            return;
        }

        coordinateMapping = new CoordinateMappingService(new QuadSphereCubeProjectionService());
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
