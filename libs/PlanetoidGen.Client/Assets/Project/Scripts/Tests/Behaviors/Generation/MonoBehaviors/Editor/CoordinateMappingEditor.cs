using PlanetoidGen.BusinessLogic.Common.Services.Generation;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CoordinateMappingDebugger))]
public class CoordinateMappingEditor : Editor
{
    private void OnEnable()
    {
        // Remove delegate listener if it has previously
        // been assigned.
        SceneView.duringSceneGui -= OnCustomSceneGUI;
        // Add (or re-add) the delegate.
        SceneView.duringSceneGui += OnCustomSceneGUI;
    }

    private void OnDisable()
    {
        // When the window is destroyed, remove the delegate
        // so that it will no longer do any drawing.
        SceneView.duringSceneGui -= OnCustomSceneGUI;
    }

    void OnCustomSceneGUI(SceneView sceneView)
    {
        var debugger = target as CoordinateMappingDebugger;
        var coordinateMapping = debugger?.CoordinateMapping;
        if (debugger == null || coordinateMapping == null || debugger!.enabled == false) return;

        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if (Physics.Raycast(ray, out var hit))
        {
            if (debugger.transform == hit.transform)
            {
                switch (debugger.mode)
                {
                    case 0:
                        DrawTile(coordinateMapping, hit, (short)debugger.zoomValue);
                        break;
                    case 1:
                        DrawAllTiles(coordinateMapping, hit, (short)debugger.zoomValue);
                        break;
                    case 2:
                        DrawAdjacent(debugger, coordinateMapping, hit, (short)debugger.zoomValue);
                        break;
                    default:
                        break;
                }
            }
        }

        // Do your drawing here using Handles.
        Handles.BeginGUI();

        DrawUI(debugger);

        // Do your drawing here using GUI.
        Handles.EndGUI();
    }

    void DrawAllTiles(CoordinateMappingService coordinateMapping, RaycastHit hit, short zoom)
    {
        var oldColor = Handles.color;
        Handles.color = Color.red;

        var localPos = hit.point - hit.transform.position;
        var radius = localPos.magnitude;

        const int meridian = 36;
        const int parallel = 18;
        const float size = 100f;
        var delta = Mathf.PI / (2f * size * Mathf.Sqrt(2.0f));

        for (var i = 0; i < meridian; i++)
        {
            var lon = -Mathf.PI + i * Mathf.PI * 2 / meridian;
            for (var j = -Mathf.PI / 2; j <= Mathf.PI / 2; j += delta)
            {
                var cubic = coordinateMapping.ToCubic(new SphericalCoordinateModel(0, lon, j, zoom));
                cubic = coordinateMapping.ToCubic(coordinateMapping.ToPlanar(cubic));
                DrawCubic(cubic, hit.transform.position, radius);
            }
        }

        for (var j = 0; j < parallel; j++)
        {
            var lat = -Mathf.PI / 2.0f + j * Mathf.PI / parallel;
            for (var i = -Mathf.PI; i <= Mathf.PI; i += delta)
            {
                var cubic = coordinateMapping.ToCubic(new SphericalCoordinateModel(0, i, lat, zoom));
                cubic = coordinateMapping.ToCubic(coordinateMapping.ToPlanar(cubic));
                DrawCubic(cubic, hit.transform.position, radius);
            }
        }

        Handles.color = oldColor;
    }

    void DrawTile(CoordinateMappingService coordinateMapping, RaycastHit hit, short zoom)
    {
        var oldColor = Handles.color;
        Handles.color = Color.red;

        var localPos = hit.point - hit.transform.position;
        var radius = localPos.magnitude;
        localPos /= radius;

        CartesianToSpherical(localPos.x, localPos.z, localPos.y, out var lat, out var lon);
        //Debug.Log($"Lon: {lon * Mathf.Rad2Deg} Lat: {lat * Mathf.Rad2Deg}");
        var oldSphericalModel = new SphericalCoordinateModel(0, lon, lat, zoom);

        var cubicModel = coordinateMapping.ToCubic(oldSphericalModel);
        cubicModel = coordinateMapping.ToCubic(coordinateMapping.ToPlanar(cubicModel));
        //Debug.Log(cubicModel);

        var faces = new Dictionary<short, Color>()
        {
            { (short)ICubeProjectionService.FaceSide.FaceFront, Color.cyan },
            { (short)ICubeProjectionService.FaceSide.FaceLeft, Color.magenta },
            { (short)ICubeProjectionService.FaceSide.FaceRight, Color.grey },
            { (short)ICubeProjectionService.FaceSide.FaceBack, Color.white },
            { (short)ICubeProjectionService.FaceSide.FaceTop, Color.red },
            { (short)ICubeProjectionService.FaceSide.FaceBottom, Color.green },
        };

        Handles.color = faces[cubicModel.Face];
        DrawCubic(cubicModel, hit.transform.position, radius);

        var newSphericalModel = coordinateMapping.ToSpherical(cubicModel);

        Handles.color = Color.green;
        DrawSpherical(newSphericalModel, hit.transform.position, radius);

        Handles.color = oldColor;
    }

    void DrawAdjacent(CoordinateMappingDebugger debugger, CoordinateMappingService coordinateMapping, RaycastHit hit, short zoom)
    {
        var oldColor = Handles.color;
        Handles.color = Color.red;

        var localPos = hit.point - hit.transform.position;
        var radius = localPos.magnitude;
        localPos /= radius;

        CartesianToSpherical(localPos.x, localPos.z, localPos.y, out var lat, out var lon);
        //Debug.Log($"Lon: {lon * Mathf.Rad2Deg} Lat: {lat * Mathf.Rad2Deg}");
        var oldSphericalModel = new SphericalCoordinateModel(0, lon, lat, zoom);

        var relativeDirections = debugger.relativeTiles;
        var adjacentQuery = relativeDirections
            .Select(x => new { d = x, s = coordinateMapping.RelativeTile(oldSphericalModel, x) });

        var story = debugger.story;
        if (story != null)
        {
            var storySpherical = new SphericalCoordinateModel(
                oldSphericalModel.PlanetoidId,
                story.LocationLongitude / 180.0 * Math.PI,
                story.LocationLatitude / 180.0 * Math.PI,
                oldSphericalModel.Zoom);

            adjacentQuery = adjacentQuery.Concat(relativeDirections.Select(x => new { d = x, s = coordinateMapping.RelativeTile(storySpherical, x) }));
        }

        var adjacent = adjacentQuery
            .Select(x =>
            {
                var oldCubic = coordinateMapping.ToCubic(x.s);
                var newCubic = coordinateMapping.ToCubic(coordinateMapping.ToPlanar(oldCubic));

                DrawCubic(newCubic, hit.transform.position, radius);
                DrawCubicPoint(oldCubic, hit.transform.position, radius);

                return x;
            })
            .ToArray();

        Handles.color = oldColor;

        Handles.BeginGUI();

        GUILayout.Window(3, new Rect(300, 20, 150, 30), (id) =>
        {

            GUILayout.BeginVertical();

            GUILayout.Label(oldSphericalModel.ToString());
            GUILayout.Label(coordinateMapping.ToCubic(oldSphericalModel).ToString());
            GUILayout.Label(coordinateMapping.ToSpherical(coordinateMapping.ToCubic(oldSphericalModel)).ToString());

            GUILayout.EndVertical();

        }, $"Cubic");

        Handles.EndGUI();
    }

    void DrawCubicPoint(CubicCoordinateModel model, Vector3 transformPosition, float radius)
    {
        int face = model.Face;
        // Map x,y index + zoom to [-1,1]
        var max = 2.0 / Mathf.Pow(2.0f, model.Z);
        var x = (float)model.X;
        var y = (float)model.Y;

        var nx = (float)(x + max);
        var ny = (float)(y + max);

        Vector3 v1;
        if (face == (int)ICubeProjectionService.FaceSide.FaceTop)
        {
            v1 = new Vector3(-y, 1.0f, x);
        }
        else if (face == (int)ICubeProjectionService.FaceSide.FaceFront)
        {
            v1 = new Vector3(1.0f, y, x);
        }
        else if (face == (int)ICubeProjectionService.FaceSide.FaceLeft)
        {
            v1 = new Vector3(x, y, -1.0f);
        }
        else if (face == (int)ICubeProjectionService.FaceSide.FaceBack)
        {
            v1 = new Vector3(-1.0f, y, -x);
        }
        else if (face == (int)ICubeProjectionService.FaceSide.FaceRight)
        {
            v1 = new Vector3(-x, y, 1.0f);
        }
        else if (face == (int)ICubeProjectionService.FaceSide.FaceBottom)
        {
            v1 = new Vector3(y, -1.0f, x);
        }
        else
        {
            v1 = Vector3.zero;
        }

        v1 = v1 * radius + transformPosition;

        Handles.DrawWireCube(v1, Vector3.one * radius * 0.05f);
    }

    void DrawCubic(CubicCoordinateModel model, Vector3 transformPosition, float radius)
    {
        int face = model.Face;
        // Map x,y index + zoom to [-1,1]
        var max = 2.0 / Mathf.Pow(2.0f, model.Z);
        var x = (float)model.X;
        var y = (float)model.Y;

        var nx = (float)(x + max);
        var ny = (float)(y + max);

        Vector3 v1, v2, v3, v4;
        if (face == (int)ICubeProjectionService.FaceSide.FaceTop)
        {
            v1 = new Vector3(-y, 1.0f, x);
            v2 = new Vector3(-ny, 1.0f, x);
            v3 = new Vector3(-ny, 1.0f, nx);
            v4 = new Vector3(-y, 1.0f, nx);
        }
        else if (face == (int)ICubeProjectionService.FaceSide.FaceFront)
        {
            v1 = new Vector3(1.0f, y, x);
            v2 = new Vector3(1.0f, ny, x);
            v3 = new Vector3(1.0f, ny, nx);
            v4 = new Vector3(1.0f, y, nx);
        }
        else if (face == (int)ICubeProjectionService.FaceSide.FaceLeft)
        {
            v1 = new Vector3(x, y, -1.0f);
            v2 = new Vector3(x, ny, -1.0f);
            v3 = new Vector3(nx, ny, -1.0f);
            v4 = new Vector3(nx, y, -1.0f);
        }
        else if (face == (int)ICubeProjectionService.FaceSide.FaceBack)
        {
            v1 = new Vector3(-1.0f, y, -x);
            v2 = new Vector3(-1.0f, ny, -x);
            v3 = new Vector3(-1.0f, ny, -nx);
            v4 = new Vector3(-1.0f, y, -nx);
        }
        else if (face == (int)ICubeProjectionService.FaceSide.FaceRight)
        {
            v1 = new Vector3(-x, y, 1.0f);
            v2 = new Vector3(-x, ny, 1.0f);
            v3 = new Vector3(-nx, ny, 1.0f);
            v4 = new Vector3(-nx, y, 1.0f);
        }
        else if (face == (int)ICubeProjectionService.FaceSide.FaceBottom)
        {
            v1 = new Vector3(y, -1.0f, x);
            v2 = new Vector3(ny, -1.0f, x);
            v3 = new Vector3(ny, -1.0f, nx);
            v4 = new Vector3(y, -1.0f, nx);
        }
        else
        {
            v1 = v2 = v3 = v4 = Vector3.zero;
        }

        v1 = v1 * radius + transformPosition;
        v2 = v2 * radius + transformPosition;
        v3 = v3 * radius + transformPosition;
        v4 = v4 * radius + transformPosition;

        Handles.DrawAAPolyLine(v1, v2, v3, v4, v1);
    }

    void DrawSpherical(SphericalCoordinateModel model, Vector3 transformPosition, float radius)
    {
        var lat = (float)model.Latitude;
        var lon = (float)model.Longtitude;

        SphericalToCartesian(lat, lon, out var nx, out var nz, out var ny);

        var newPos = new Vector3(nx, ny, nz) * radius;

        Handles.DrawWireCube(newPos + transformPosition, Vector3.one * 0.1f * radius);
    }

    void CartesianToSpherical(float x, float z, float y, out float lat, out float lon)
    {
        var xzLen = new Vector2(x, z).magnitude;
        lat = Mathf.Atan2(y, xzLen); //theta
        lon = Mathf.Atan2(z, x); //phi
    }

    void SphericalToCartesian(float lat, float lon, out float x, out float z, out float y)
    {
        x = Mathf.Cos(lat) * Mathf.Cos(lon);
        z = Mathf.Cos(lat) * Mathf.Sin(lon);
        y = Mathf.Sin(lat);
    }

    void DrawUI(CoordinateMappingDebugger debugger)
    {
        GUILayout.Window(2, new Rect(60, 20, 150, 60), (id) =>
        {

            GUILayout.BeginVertical();

            debugger.mode = GUILayout.SelectionGrid(debugger.mode, new string[]
            {
                "Current",
                "All",
                "Adjacent"
            }, 3);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Zoom out"))
            {
                debugger.zoomValue = Mathf.Max(debugger.zoomValue - 1, 0);
            }

            if (GUILayout.Button("Zoom in"))
            {
                debugger.zoomValue = Mathf.Min(debugger.zoomValue + 1, 30);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

        }, $"Zoom: {debugger.zoomValue}");
    }
}
