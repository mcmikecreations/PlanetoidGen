using PlanetoidGen.Client.Contracts.Services.Procedural;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using System;
using System.Collections.Generic;
using UnityEngine;
using static PlanetoidGen.Contracts.Services.Generation.ICubeProjectionService;

namespace PlanetoidGen.Client.BusinessLogic
{
    public class SphericalTileService : ISphericalTileService
    {
        private readonly ICoordinateMappingService _coordinateMapping;

        public SphericalTileService(ICoordinateMappingService coordinateMapping)
        {
            _coordinateMapping = coordinateMapping;
        }

        public Mesh GenerateTile(
            CubicCoordinateModel coordinates,
            double radius,
            int tesselation)
        {
            var result = new Mesh();

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            var edge = _coordinateMapping.ToCubic(_coordinateMapping.ToPlanar(coordinates));

            ////var size = radius * Math.PI * 0.5 * Math.Pow(2.0, -coordinates.Z);
            var sizeCubic = _coordinateMapping.TileSizeCubic(coordinates.Z);
            int steps = 1 << tesselation;

            for (int d = 0; d <= steps; d++)
            {
                for (int w = 0; w <= steps; w++)
                {
                    var xCubic = edge.X + sizeCubic * (w / (double)steps);
                    var yCubic = edge.Y + sizeCubic * (d / (double)steps);
                    var spherical = _coordinateMapping.ToSpherical(new CubicCoordinateModel(
                        edge.PlanetoidId, edge.Face, edge.Z, xCubic, yCubic));

                    var uvx = w / (float)steps;
                    var uvy = d / (float)steps;

                    var height = (float)radius;

                    vertices.Add(new Vector3(
                        (float)(height * Math.Cos(spherical.Latitude) * Math.Cos(spherical.Longtitude)),
                        (float)(height * Math.Sin(spherical.Latitude)),
                        (float)(height * Math.Cos(spherical.Latitude) * Math.Sin(spherical.Longtitude))
                        ));
                    uvs.Add(new Vector2(uvx, uvy));

                    if (d < steps && w < steps)
                    {
                        // quad triangles index.
                        triangles.Add((d * (steps + 1)) + w);
                        triangles.Add(((d + 1) * (steps + 1)) + w);
                        triangles.Add(((d + 1) * (steps + 1)) + w + 1);
                        // Second triangle
                        triangles.Add((d * (steps + 1)) + w);
                        triangles.Add(((d + 1) * (steps + 1)) + w + 1);
                        triangles.Add((d * (steps + 1)) + w + 1);
                    }
                }
            }

            result.SetVertices(vertices);
            result.SetUVs(0, uvs);
            result.SetTriangles(triangles, 0);

            return result;
        }

        /// <summary>
        /// Not working properly. Issue with face index.
        /// </summary>
        public Mesh GenerateTile(FaceSide faceSide, float radius, int tesselation)
        {
            var result = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            var steps = 1 << tesselation;

            for (var y = 0; y <= steps; y++)
            {
                for (var x = 0; x <= steps; x++)
                {
                    var xCubic = -1f + 2 * (x / (float)steps);
                    var yCubic = -1f + 2 * (y / (float)steps);

                    var pointOnUnitCube = faceSide switch
                    {
                        FaceSide.FaceTop => new Vector3(xCubic, 1f, yCubic),
                        FaceSide.FaceBottom => new Vector3(xCubic, -1f, yCubic),

                        FaceSide.FaceLeft => new Vector3(-1f, yCubic, xCubic),
                        FaceSide.FaceRight => new Vector3(1f, yCubic, xCubic),

                        FaceSide.FaceBack => new Vector3(xCubic, yCubic, -1f),
                        FaceSide.FaceFront => new Vector3(xCubic, yCubic, 1f),
                        _ => Vector3.zero,
                    };
                    var pointOnUnitSphere = pointOnUnitCube.normalized;

                    var uvx = x / (float)steps;
                    var uvy = y / (float)steps;

                    vertices.Add(pointOnUnitSphere * radius);
                    uvs.Add(new Vector2(uvx, uvy));

                    if (y < steps && x < steps)
                    {
                        // Determine the winding order based on faceSide
                        if (faceSide == FaceSide.FaceBottom || faceSide == FaceSide.FaceLeft || faceSide == FaceSide.FaceFront)
                        {
                            // First triangle (reversed order)
                            triangles.Add((y * (steps + 1)) + x);
                            triangles.Add(((y + 1) * (steps + 1)) + x + 1);
                            triangles.Add(((y + 1) * (steps + 1)) + x);

                            // Second triangle (reversed order)
                            triangles.Add((y * (steps + 1)) + x);
                            triangles.Add((y * (steps + 1)) + x + 1);
                            triangles.Add(((y + 1) * (steps + 1)) + x + 1);
                        }
                        else
                        {
                            // First triangle
                            triangles.Add((y * (steps + 1)) + x);
                            triangles.Add(((y + 1) * (steps + 1)) + x);
                            triangles.Add(((y + 1) * (steps + 1)) + x + 1);

                            // Second triangle
                            triangles.Add((y * (steps + 1)) + x);
                            triangles.Add(((y + 1) * (steps + 1)) + x + 1);
                            triangles.Add((y * (steps + 1)) + x + 1);
                        }
                    }

                }
            }

            result.SetVertices(vertices);
            result.SetUVs(0, uvs);
            result.SetTriangles(triangles, 0);

            return result;
        }
    }
}
