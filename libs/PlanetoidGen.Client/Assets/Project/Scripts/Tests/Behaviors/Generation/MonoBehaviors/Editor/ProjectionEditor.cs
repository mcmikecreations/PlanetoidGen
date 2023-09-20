using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Enums;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PlanetoidGen.Client.Tests.Behaviors
{
    [CustomEditor(typeof(ProjectionDebugger))]
    public class ProjectionEditor : Editor
    {
        private const float CubeSize = 40.0f;

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
            var debugger = target as ProjectionDebugger;
            var story = debugger.story;

            if (debugger == null || story == null)
            {
                return;
            }

            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            if (debugger.drawBoundingBoxes)
            {
                DrawGrid(debugger);
            }

            if (debugger.drawRelativeTiles && Physics.Raycast(ray, out var hit) && debugger.transform == hit.transform)
            {
                DrawRelativeTiles(hit.point, debugger);
            }

        }

        private void DrawRelativeTiles(Vector3 point, ProjectionDebugger debugger)
        {
            var coordMapping = debugger.CoordinateMapping;
            var projMapping = debugger.GeometryConversion;
            var story = debugger.story;
            var planetoid = debugger.Planetoid;
            var srcSRS = debugger.SrsGeographic;
            var dstSRS = debugger.SrsProjected;

            var storyCenter = new SphericalCoordinateModel(
                planetoid.Id,
                story.LocationLongitude / 180.0 * Math.PI,
                story.LocationLatitude / 180.0 * Math.PI,
                (short)story.LocationZoom);

            var storyPivot = coordMapping.ToSpherical(coordMapping.ToPlanar(storyCenter));

            var storyPivotProjected = GetPivot(
                planetoid,
                storyPivot,
                projMapping, srcSRS, dstSRS);

            var pointProjected = new CoordinateModel(storyPivotProjected.x + point.x, storyPivotProjected.z + point.z, storyPivotProjected.y + point.y);

            var pointGeographic = projMapping.ToAssimpVectors(new[] { pointProjected }, planetoid, false, dstSRS, srcSRS).First();

            var pointSpherical = new SphericalCoordinateModel(planetoid.Id, pointGeographic[0] / 180.0 * Math.PI, pointGeographic[1] / 180.0 * Math.PI, storyCenter.Zoom);

            var relativeTiles = debugger.relativeTiles.Select(k =>
            (
                k, coordMapping.ToSpherical(coordMapping.ToPlanar(coordMapping.RelativeTile(pointSpherical, k)))
            ));

            var relativeTilesProjected = projMapping.ToAssimpVectors(relativeTiles.Select(v =>
                new CoordinateModel(v.Item2.Longtitude * 180.0 / Math.PI, v.Item2.Latitude * 180.0 / Math.PI, 0.0)),
                planetoid, true, srcSRS, dstSRS);

            var oldColor = Handles.color;

            Handles.DrawWireCube(point, CubeSize * Vector3.one);

            Handles.color = Color.cyan;
            foreach (var tile in relativeTilesProjected)
            {
                Handles.DrawWireCube(new Vector3((float)tile[0], (float)tile[1], (float)tile[2]) - storyPivotProjected, CubeSize * Vector3.one);
            }

            Handles.color = oldColor;
        }

        private void DrawGrid(ProjectionDebugger debugger)
        {
            var coordMapping = debugger.CoordinateMapping;
            var projMapping = debugger.GeometryConversion;
            var story = debugger.story;
            var planetoid = debugger.Planetoid;
            var srcSRS = debugger.SrsGeographic;
            var dstSRS = debugger.SrsProjected;

            var storyCenter = new SphericalCoordinateModel(
                planetoid.Id,
                story.LocationLongitude / 180.0 * Math.PI,
                story.LocationLatitude / 180.0 * Math.PI,
                (short)story.LocationZoom);

            var storyPivot = coordMapping.ToSpherical(coordMapping.ToPlanar(storyCenter));

            Func<ICoordinateMappingService, SphericalCoordinateModel, IEnumerable<CoordinateModel>> boundingFunction;

            switch (debugger.selectedFunctionType)
            {
                case ProjectionRelativeType.Service:
                    boundingFunction = GetBoundingBoxService;
                    break;
                case ProjectionRelativeType.Relative:
                    boundingFunction = GetBoundingBoxRelative;
                    break;
                case ProjectionRelativeType.Simplified:
                    boundingFunction = GetBoundingBoxSimplified;
                    break;
                case ProjectionRelativeType.Cubic:
                    boundingFunction = GetBoundingBoxCubic;
                    break;
                default:
                    boundingFunction = GetBoundingBoxService;
                    break;
            }

            // Maps to planar and back, adds size in radians
            var bbox = boundingFunction(coordMapping, storyPivot);

            var storyPivotProjected = GetPivot(
                planetoid,
                storyPivot,
                projMapping, srcSRS, dstSRS);

            var storyCenterProjected = GetPivot(
                planetoid,
                storyCenter,
                projMapping, srcSRS, dstSRS);

            var relativeTiles = debugger.relativeTiles.Select(k =>
            (
                k, boundingFunction(coordMapping, coordMapping.ToSpherical(coordMapping.ToPlanar(coordMapping.RelativeTile(storyPivot, k))))
            ));

            var oldColor = Handles.color;

            Handles.color = Color.red;
            DrawBoundingBox(planetoid, coordMapping, projMapping, bbox, srcSRS, dstSRS, storyPivotProjected);

            Handles.color = Color.blue;
            foreach (var relBbox in relativeTiles)
            {
                DrawBoundingBox(planetoid, coordMapping, projMapping, relBbox.Item2, srcSRS, dstSRS, storyPivotProjected);
            }

            Handles.color = Color.green;
            Handles.DrawSolidDisc(storyCenterProjected - storyPivotProjected, Vector3.up, CubeSize);

            Handles.color = oldColor;

            Handles.BeginGUI();

            DrawUI(storyPivot.Zoom, coordMapping, storyCenter, bbox, relativeTiles);

            Handles.EndGUI();
        }

        private void DrawUI(
            short zoom,
            ICoordinateMappingService coordMapping,
            SphericalCoordinateModel storyCoords,
            IEnumerable<CoordinateModel> bbox,
            IEnumerable<(RelativeTileDirectionType, IEnumerable<CoordinateModel>)> relBboxes)
        {
            var pivot = coordMapping.ToSpherical(coordMapping.ToCubic(storyCoords));

            var centerDeg = storyCoords.ToCoordinatesDegrees();
            var pivotDeg = pivot.ToCoordinatesDegrees();
            var diffDeg = new CoordinateModel(centerDeg.X - pivotDeg.X, centerDeg.Y - pivotDeg.Y, centerDeg.Z - pivotDeg.Z);

            GUILayout.Window(3, new Rect(300, 20, 150, 60), (id) =>
            {
                GUILayout.BeginVertical();

                GUILayout.Label($"Current: {centerDeg}");
                GUILayout.Label($"Pivot  : {pivotDeg}");
                GUILayout.Label($"Diff   : {diffDeg}");

                GUILayout.EndVertical();

            }, "Stats");
        }

        Vector3 GetPivot(
            PlanetoidInfoModel planetoid,
            SphericalCoordinateModel spherical,
            IGeometryConversionService geometryConversionService,
            SpatialReferenceSystemModel srcSRS,
            SpatialReferenceSystemModel dstSRS)
        {
            return GetPivot(
                planetoid,
                new CoordinateModel(spherical.Longtitude * 180.0 / Math.PI, spherical.Latitude * 180.0 / Math.PI, 0.0),
                geometryConversionService,
                srcSRS, dstSRS);
        }

        Vector3 GetPivot(
            PlanetoidInfoModel planetoid,
            CoordinateModel geographic,
            IGeometryConversionService geometryConversionService,
            SpatialReferenceSystemModel srcSRS,
            SpatialReferenceSystemModel dstSRS)
        {
            var mappedPoint = geometryConversionService.ToAssimpVectors(
                new[] { geographic },
                planetoid,
                true,
                srcSRS,
                dstSRS).ToArray();

            return new Vector3((float)mappedPoint[0][0], (float)mappedPoint[0][1], (float)mappedPoint[0][2]);
        }

        IEnumerable<CoordinateModel> GetBoundingBoxService(ICoordinateMappingService coordinateMapping, SphericalCoordinateModel point)
        {
            var bbox = coordinateMapping.ToBoundingBox(point);

            return bbox.GetCoordinateArray().Select(coord => new CoordinateModel(coord.X * 180.0 / Math.PI, coord.Y * 180.0 / Math.PI, coord.Z));
        }

        private IEnumerable<CoordinateModel> GetBoundingBoxCubic(ICoordinateMappingService coordMapping, SphericalCoordinateModel point)
        {
            var pointC = coordMapping.ToCubic(point);

            int face = pointC.Face;
            // Map x,y index + zoom to [-1,1]
            var max = coordMapping.TileSizeCubic(point.Zoom);
            var x = pointC.X;
            var y = pointC.Y;

            var mx = x + max;
            var my = y + max;

            var upC = new CubicCoordinateModel(pointC.PlanetoidId, pointC.Face, pointC.Z, x, my);
            var oppositeC = new CubicCoordinateModel(pointC.PlanetoidId, pointC.Face, pointC.Z, mx, my);
            var rightC = new CubicCoordinateModel(pointC.PlanetoidId, pointC.Face, pointC.Z, mx, y);

            var bbox = new BoundingBoxCoordinateModel(
                point.PlanetoidId,
                coordMapping.ToSpherical(coordMapping.ToPlanar(pointC)).ToCoordinatesRadians(),
                coordMapping.ToSpherical(coordMapping.ToPlanar(upC)).ToCoordinatesRadians(),
                coordMapping.ToSpherical(coordMapping.ToPlanar(oppositeC)).ToCoordinatesRadians(),
                coordMapping.ToSpherical(coordMapping.ToPlanar(rightC)).ToCoordinatesRadians()
                );

            return bbox.GetCoordinateArray().Select(p => p.Clone(p.X * 180.0 / Math.PI, p.Y * 180.0 / Math.PI));
        }

        IEnumerable<CoordinateModel> GetBoundingBoxSimplified(ICoordinateMappingService coordinateMapping, SphericalCoordinateModel point)
        {
            var currentCornerC = coordinateMapping.ToCubic(point);
            var rightCornerC = coordinateMapping.RelativeTile(currentCornerC, RelativeTileDirectionType.Right);
            var upCornerC = coordinateMapping.RelativeTile(currentCornerC, RelativeTileDirectionType.Up);
            var oppositeCornerC = coordinateMapping.RelativeTile(rightCornerC, RelativeTileDirectionType.Up);

            var currentCorner = coordinateMapping.ToSpherical(
                //coordinateMapping.ToPlanar(
                    currentCornerC
                //    )
                );
            var rightCorner = coordinateMapping.ToSpherical(
                //coordinateMapping.ToPlanar(
                    rightCornerC
                //    )
                );
            var upCorner = coordinateMapping.ToSpherical(
                //coordinateMapping.ToPlanar(
                    upCornerC
                //    )
                );
            var oppositeCorner = coordinateMapping.ToSpherical(
                //coordinateMapping.ToPlanar(
                    oppositeCornerC
                //    )
                );

            return new[]
            {
                new CoordinateModel(currentCorner.Longtitude * 180.0 / Math.PI, currentCorner.Latitude * 180.0 / Math.PI, 0.0),
                new CoordinateModel(upCorner.Longtitude * 180.0 / Math.PI, upCorner.Latitude * 180.0 / Math.PI, 0.0),
                new CoordinateModel(oppositeCorner.Longtitude * 180.0 / Math.PI, oppositeCorner.Latitude * 180.0 / Math.PI, 0.0),
                new CoordinateModel(rightCorner.Longtitude * 180.0 / Math.PI, rightCorner.Latitude * 180.0 / Math.PI, 0.0),
            };
        }

        IEnumerable<CoordinateModel> GetBoundingBoxRelative(ICoordinateMappingService coordinateMapping, SphericalCoordinateModel point)
        {
            var currentCorner = point;
            var rightCorner = coordinateMapping.RelativeTile(point, RelativeTileDirectionType.Right);
            var upCorner = coordinateMapping.RelativeTile(point, RelativeTileDirectionType.Up);
            var oppositeCorner = coordinateMapping.RelativeTile(rightCorner, RelativeTileDirectionType.Up);

            currentCorner = coordinateMapping.ToSpherical(coordinateMapping.ToPlanar(currentCorner));
            rightCorner = coordinateMapping.ToSpherical(coordinateMapping.ToPlanar(rightCorner));
            upCorner = coordinateMapping.ToSpherical(coordinateMapping.ToPlanar(upCorner));
            oppositeCorner = coordinateMapping.ToSpherical(coordinateMapping.ToPlanar(oppositeCorner));

            return new[]
            {
                new CoordinateModel(currentCorner.Longtitude * 180.0 / Math.PI, currentCorner.Latitude * 180.0 / Math.PI, 0.0),
                new CoordinateModel(upCorner.Longtitude * 180.0 / Math.PI, upCorner.Latitude * 180.0 / Math.PI, 0.0),
                new CoordinateModel(oppositeCorner.Longtitude * 180.0 / Math.PI, oppositeCorner.Latitude * 180.0 / Math.PI, 0.0),
                new CoordinateModel(rightCorner.Longtitude * 180.0 / Math.PI, rightCorner.Latitude * 180.0 / Math.PI, 0.0),
            };
        }

        void DrawBoundingBox(
            PlanetoidInfoModel planetoid,
            ICoordinateMappingService coordinateMapping,
            IGeometryConversionService geometryConversion,
            IEnumerable<CoordinateModel> bbox,
            SpatialReferenceSystemModel srcSRS,
            SpatialReferenceSystemModel dstSRS,
            Vector3 pivot)
        {
            var mappedPoints = geometryConversion.ToAssimpVectors(
                bbox,
                planetoid,
                true,
                srcSRS,
                dstSRS);

            var mappedVectors = mappedPoints.Select(p => new Vector3(
                (float)(p[0] - pivot[0]),
                (float)(p[1] - pivot[1]),
                (float)(p[2] - pivot[2])
                ));
            //mappedVectors = mappedVectors.Append(mappedVectors.First());

            Handles.DrawAAConvexPolygon(mappedVectors.ToArray());
        }
    }
}
