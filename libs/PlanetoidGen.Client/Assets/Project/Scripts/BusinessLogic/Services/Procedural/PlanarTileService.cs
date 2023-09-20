using PlanetoidGen.Client.Contracts.Services.Procedural;
using PlanetoidGen.Contracts.Models.Coordinates;
using System.Collections.Generic;
using System;
using UnityEngine;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Enums;
using System.Linq;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using System.Runtime.InteropServices;
using UnityEngine.Windows;

namespace PlanetoidGen.Client.BusinessLogic
{
    public class PlanarTileService : IPlanarTileService
    {
        private readonly ICoordinateMappingService _coordinateMapping;
        private readonly IGeometryConversionService _geometryConversion;

        public PlanarTileService(
            ICoordinateMappingService coordinateMapping,
            IGeometryConversionService geometryConversion)
        {
            _coordinateMapping = coordinateMapping;
            _geometryConversion = geometryConversion;
        }

        /// <inheritdoc/>
        public Mesh GenerateTile(
            CubicCoordinateModel coordinates,
            PlanetoidInfoModel planetoid,
            SpatialReferenceSystemModel from,
            SpatialReferenceSystemModel to,
            int tesselation)
        {
            var result = new Mesh();

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            var edge = _coordinateMapping.ToCubic(_coordinateMapping.ToPlanar(coordinates));
            var edgeTop = _coordinateMapping.RelativeTile(edge, RelativeTileDirectionType.Up);
            var edgeRight = _coordinateMapping.RelativeTile(edge, RelativeTileDirectionType.Right);
            var edgeOpposite = _coordinateMapping.RelativeTile(edgeRight, RelativeTileDirectionType.Up);

            var edgesCubic = new CubicCoordinateModel[]
            {
                edge,
                edgeTop,
                edgeOpposite,
                edgeRight,
            };
            var edgesSpherical = edgesCubic.Select(x => _coordinateMapping.ToSpherical(x));
            var edgesModels = edgesSpherical.Select(x => new CoordinateModel(
                x.Longtitude * 180.0 / Math.PI,
                x.Latitude * 180.0 / Math.PI,
                0.0));
            var edgesProjected = _geometryConversion.ToAssimpVectors(edgesModels, planetoid, true, from, to);
            var edgesRelative = edgesProjected.Select(x => new double[]
            {
                x[0] - edgesProjected.First()[0],
                x[1] - edgesProjected.First()[1],
                x[2] - edgesProjected.First()[2],
            }).ToList();

            var edgeP = edgesRelative[0];
            var edgeTopP = edgesRelative[1];
            var edgeOppositeP = edgesRelative[2];
            var edgeRightP = edgesRelative[3];

            int steps = 1 << tesselation;

            for (int l = 0; l <= steps; l++)
            {
                for (int w = 0; w <= steps; w++)
                {
                    var xFactor = 1.0 / steps * w;
                    var zFactor = 1.0 / steps * l;

                    var uvx = w / (float)steps;
                    var uvy = l / (float)steps;

                    // t--b----o  e = edge
                    // ^  |    |  t = edgeTop
                    // |  |    |  r = edgeRight
                    // c--p----d  o = edgeOpposite
                    // ^  |    |
                    // e->a--->r 

                    var a = new double[]
                    {
                        (1.0 - xFactor) * edgeP[0] + xFactor * edgeRightP[0],
                        (1.0 - xFactor) * edgeP[1] + xFactor * edgeRightP[1],
                        (1.0 - xFactor) * edgeP[2] + xFactor * edgeRightP[2],
                    };

                    var b = new double[]
                    {
                        (1.0 - xFactor) * edgeTopP[0] + xFactor * edgeOppositeP[0],
                        (1.0 - xFactor) * edgeTopP[1] + xFactor * edgeOppositeP[1],
                        (1.0 - xFactor) * edgeTopP[2] + xFactor * edgeOppositeP[2],
                    };

                    var c = new double[]
                    {
                        (1.0 - zFactor) * edgeP[0] + zFactor * edgeTopP[0],
                        (1.0 - zFactor) * edgeP[1] + zFactor * edgeTopP[1],
                        (1.0 - zFactor) * edgeP[2] + zFactor * edgeTopP[2],
                    };

                    var d = new double[]
                    {
                        (1.0 - zFactor) * edgeRightP[0] + zFactor * edgeOppositeP[0],
                        (1.0 - zFactor) * edgeRightP[1] + zFactor * edgeOppositeP[1],
                        (1.0 - zFactor) * edgeRightP[2] + zFactor * edgeOppositeP[2],
                    };

                    double px, pz;
                    if (!LineLineIntersect(
                        a[0], a[2], b[0], b[2],
                        c[0], c[2], d[0], d[2],
                        out px, out pz))
                    {
                        return null;
                    }

                    vertices.Add(new Vector3(
                        (float)px,
                        0.0f,
                        (float)pz));
                    uvs.Add(new Vector2(uvx, uvy));

                    if (l < steps && w < steps)
                    {
                        // quad triangles index.
                        triangles.Add((l * (steps + 1)) + w);
                        triangles.Add(((l + 1) * (steps + 1)) + w);
                        triangles.Add(((l + 1) * (steps + 1)) + w + 1);
                        // Second triangle
                        triangles.Add((l * (steps + 1)) + w);
                        triangles.Add(((l + 1) * (steps + 1)) + w + 1);
                        triangles.Add((l * (steps + 1)) + w + 1);
                    }
                }
            }

            result.SetVertices(vertices);
            result.SetUVs(0, uvs);
            result.SetTriangles(triangles, 0);

            return result;
        }

        // https://gist.github.com/TimSC/47203a0f5f15293d2099507ba5da44e6
        /// <summary>
        /// Calculate determinant of matrix:
        /// <code>
        /// [a b]
        /// [c d]
        /// </code>
        /// </summary>
        private static double Det(double a, double b, double c, double d)
        {
            return a * d - b * c;
        }

        /// <summary>
        /// Calculate intersection of two lines.
        /// </summary>
        /// <param name="x1">Line 1 start X.</param>
        /// <param name="y1">Line 1 start Y.</param>
        /// <param name="x2">Line 1 end X.</param>
        /// <param name="y2">Line 1 end Y.</param>
        /// <param name="x3">Line 2 start X.</param>
        /// <param name="y3">Line 2 start Y.</param>
        /// <param name="x4">Line 2 end X.</param>
        /// <param name="y4">Line 2 end Y.</param>
        /// <param name="ixOut">Output X.</param>
        /// <param name="iyOut">Output Y.</param>
        /// <returns>True if found, false if not found or error.</returns>
        bool LineLineIntersect(
            double x1, double y1,
            double x2, double y2,
            double x3, double y3,
            double x4, double y4,
            out double ixOut, out double iyOut)
        {
            // http://mathworld.wolfram.com/Line-LineIntersection.html

            double detL1 = Det(x1, y1, x2, y2);
            double detL2 = Det(x3, y3, x4, y4);
            double x1mx2 = x1 - x2;
            double x3mx4 = x3 - x4;
            double y1my2 = y1 - y2;
            double y3my4 = y3 - y4;

            double xnom = Det(detL1, x1mx2, detL2, x3mx4);
            double ynom = Det(detL1, y1my2, detL2, y3my4);
            double denom = Det(x1mx2, y1my2, x3mx4, y3my4);
            if (denom == 0.0) //Lines don't seem to cross.
            {
                ixOut = double.NaN;
                iyOut = double.NaN;
                return false;
            }

            ixOut = xnom / denom;
            iyOut = ynom / denom;
            if (!double.IsFinite(ixOut) || !double.IsFinite(iyOut)) //Probably a numerical issue.
            {
                return false;
            }

            return true; //All OK.
        }
    }
}
