using PlanetoidGen.BusinessLogic.Common.Services.Generation;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CartesianEulerDebugger))]
public class CartesianEulerEditor : Editor
{
    private const float CubeSize = 0.025f;
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
        var debugger = target as CartesianEulerDebugger;
        var coordinateMapping = debugger?.CoordinateMapping;
        var geometryConversion = debugger?.GeometryConversion;
        if (debugger == null || coordinateMapping == null || debugger!.enabled == false) return;

        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if (Physics.Raycast(ray, out var hit))
        {
            if (debugger.transform == hit.transform)
            {
                var localPos = hit.point - hit.transform.position;
                var radius = localPos.magnitude;

                DrawGridCartesian(hit.point, radius, coordinateMapping, geometryConversion, debugger);
            }
        }

        // Do your drawing here using Handles.
        Handles.BeginGUI();

        DrawUI(debugger);

        // Do your drawing here using GUI.
        Handles.EndGUI();
    }

    private void DrawGridCartesian(Vector3 hitPoint, float radius, CoordinateMappingService service, GeometryConversionService projection, CartesianEulerDebugger debugger)
    {
        Handles.DrawWireCube(hitPoint, CubeSize * radius * Vector3.one);

        short tileZ = (short)debugger.zoomValue;
        //var sphericalTileSizeResult = Math.PI / (2.0 * (1L << tileZ));
        var cubicTileSizeRelativeResult = 2.0 / (1L << tileZ);

        // Needs to have lon [-pi;pi] or [0;2pi] and lat [-pi/2,pi/2]
        var lonLatResult = Utils.ToSpherical2(hitPoint.x / radius, hitPoint.y / radius, hitPoint.z / radius);

        var tileSphericalCoordOriginal = new SphericalCoordinateModel(
            0,
            lonLatResult.lon/* - Utils.Mod(lonLatResult.lon, sphericalTileSizeResult)*/,
            lonLatResult.lat/* - Utils.Mod(lonLatResult.lat, sphericalTileSizeResult)*/,
            tileZ);

        var projectedCoord = projection.ToAssimpVectors(new CoordinateModel[]
            {
                new CoordinateModel(tileSphericalCoordOriginal.Longtitude, tileSphericalCoordOriginal.Latitude, 0.0)
            }, new PlanetoidInfoModel(0, "Earth", 42, 6_371_000), true, default).Result.First();
        //Debug.Log($"Using {new Vector3((float)projectedCoord[0], (float)projectedCoord[1], (float)projectedCoord[2])}");

        var tileCubicCoord = service.ToCubic(tileSphericalCoordOriginal);

        tileCubicCoord = new CubicCoordinateModel(tileCubicCoord.PlanetoidId, tileCubicCoord.Face, tileCubicCoord.Z,
            tileCubicCoord.X - Utils.Mod(tileCubicCoord.X + 1.0, cubicTileSizeRelativeResult),
            tileCubicCoord.Y - Utils.Mod(tileCubicCoord.Y + 1.0, cubicTileSizeRelativeResult)
            );
        //Debug.Log($"Using {tileCubicCoord.X} {tileCubicCoord.Y}");

        int tileSizePixels = 32;

        //var noiseP = new Perlin(
        //    seed: 47,
        //    noiseQuality: Helpers.LibNoise.Helpers.NoiseQuality.QUALITY_BEST);

        var step = cubicTileSizeRelativeResult / tileSizePixels;
        var relativeX = tileCubicCoord.X;

        for (int i = 0; i < tileSizePixels; ++i)
        {
            var relativeY = tileCubicCoord.Y;

            for (int j = 0; j < tileSizePixels; ++j)
            {
                var tileSphericalCoord = service.ToSpherical(new CubicCoordinateModel(
                    tileCubicCoord.PlanetoidId,
                    tileCubicCoord.Face,
                    tileCubicCoord.Z,
                    relativeX,
                    relativeY));

                var coords = Utils.ToCartesian2(tileSphericalCoord.Latitude, tileSphericalCoord.Longtitude);
                var x = (float)coords.x;
                var y = (float)coords.y;
                var z = (float)coords.z;

                Handles.color = Color.red;
                Handles.DrawWireCube(new Vector3(x, y, z) * radius, CubeSize * radius * Vector3.one);

                relativeY += step;
            }

            relativeX += step;
        }

    }

    void DrawUI(CartesianEulerDebugger debugger)
    {
        GUILayout.Window(3, new Rect(300, 20, 150, 60), (id) =>
        {

            GUILayout.BeginVertical();

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

    private static class Utils
    {
        public static (double x, double y, double z) ToCartesian(double lat, double lon)
        {
            var cos = Math.Cos(lat);

            return (x: cos * Math.Cos(lon), y: cos * Math.Sin(lon), z: Math.Sin(lat));
        }

        public static (double x, double y, double z) ToCartesian2(double lat, double lon)
        {
            var cos = Math.Cos(lat);

            return (cos * Math.Cos(lon), Math.Sin(lat), cos * Math.Sin(lon));
        }

        public static double Mod(double x, double m)
        {
            double r = x % m;
            return r < 0.0 ? r + m : r;
        }

        public static (double lat, double lon) ToSpherical(double x, double y, double z)
        {
            double lat = Math.Asin(z);
            double lon = Math.Atan2(y, x);

            return (lat, lon);
        }

        public static (double lat, double lon) ToSpherical2(double x, double y, double z)
        {
            double lat = Math.Asin(y);
            double lon = Math.Atan2(z, x);

            return (lat, lon);
        }

        public static (byte r, byte g, byte b, byte a) EncodeNoiseToRGBA32(float noise)
        {
            var encoded = BitConverter.GetBytes(noise);

            return (encoded[0], encoded[1], encoded[2], encoded[3]);
        }

        public static float DecodeNoiseFromRGBA32(byte r, byte g, byte b, byte a)
        {
            return BitConverter.ToSingle(new[] { r, g, b, a });
        }
    }
}
