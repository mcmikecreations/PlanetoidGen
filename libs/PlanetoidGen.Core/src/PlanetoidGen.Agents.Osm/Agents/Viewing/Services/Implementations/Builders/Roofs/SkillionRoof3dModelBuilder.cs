using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Collections;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Constants;
using PlanetoidGen.Agents.Osm.Constants.KindValues;
using PlanetoidGen.Agents.Osm.Helpers;
using PlanetoidGen.Domain.Models.Descriptions.Building;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations.Builders.Roofs
{
    internal class SkillionRoof3dModelBuilder : IRoofTo3dModelBuilder
    {
        private readonly IMaterialTo3dConverter _materialTo3dConverter;

        public SkillionRoof3dModelBuilder(IMaterialTo3dConverter materialTo3DConverter)
        {
            _materialTo3dConverter = materialTo3DConverter;
        }

        public void BuildRoof(
            VertexRing bottomRing,
            ref int startIndex,
            float bottomHeight,
            LevelModel topLevel,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            BuildingModel description,
            Scene scene,
            Node parentNode,
            Mesh buildingMesh,
            Vector3D[] outerRing,
            int lod)
        {
            if (lod == 0)
            {
                BuildRoofLOD0(
                    bottomRing,
                    ref startIndex,
                    bottomHeight,
                    topLevel,
                    options,
                    planetoid,
                    description,
                    scene,
                    parentNode,
                    buildingMesh,
                    outerRing);
            }
            else
            {
                BuildRoofLOD1(
                    bottomRing,
                    ref startIndex,
                    bottomHeight,
                    topLevel,
                    options,
                    planetoid,
                    description,
                    scene,
                    parentNode,
                    buildingMesh,
                    outerRing);
            }
        }

        public void BuildRoofLOD1(
            VertexRing bottomRing,
            ref int startIndex,
            float bottomHeight,
            LevelModel topLevel,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            BuildingModel description,
            Scene scene,
            Node parentNode,
            Mesh buildingMesh,
            Vector3D[] outerRing)
        {
            var roof = description.Roof;
            var maxSideIndex = 0;
            float maxSideLength = (outerRing[1] - outerRing[0]).LengthSquared();
            for (var i = 1; i < outerRing.Length; ++i)
            {
                float sideLength = (outerRing[(i + 1) % outerRing.Length] - outerRing[i]).LengthSquared();
                if (sideLength > maxSideLength)
                {
                    maxSideIndex = i;
                    maxSideLength = sideLength;
                }
            }

            var minVertexIndex = 0;
            var maxVertexLength = 0f;
            {
                var maxSideIndex2 = (maxSideIndex + 1) % outerRing.Length;
                var a = outerRing[maxSideIndex];
                var b = outerRing[maxSideIndex2];

                for (var i = 0; i < outerRing.Length; ++i)
                {
                    if (i == maxSideIndex || i == maxSideIndex2)
                    {
                        continue;
                    }

                    var v = outerRing[i];
                    var vertexLength = AssimpHelpers.LinePointDistance(v, a, b);
                    if (vertexLength > maxVertexLength)
                    {
                        minVertexIndex = i;
                        maxVertexLength = vertexLength;
                    }
                }
            }

            var roofMesh = new Mesh(PrimitiveType.Polygon);

            Vector3D plane1, plane2, planeP, planeN;
            List<Vector3D> roofVertices;
            var roofHeight = GetRoofHeight(roof, topLevel, outerRing);

            if (options.YUp)
            {
                var p1I = maxSideIndex;
                var p2I = (maxSideIndex + 1) % outerRing.Length;
                plane1 = new Vector3D(outerRing[p1I].X, bottomHeight + roofHeight, outerRing[p1I].Z);
                plane2 = new Vector3D(outerRing[p2I].X, bottomHeight + roofHeight, outerRing[p2I].Z);
                var plane3 = new Vector3D(outerRing[minVertexIndex].X, bottomHeight, outerRing[minVertexIndex].Z);

                (planeP, planeN) = AssimpHelpers.PlanePointNormal(plane1, plane2, plane3, true);

                roofVertices = outerRing.Select(x =>
                {
                    var p1 = new Vector3D(x.X, bottomHeight, x.Z);
                    return AssimpHelpers.PlaneLineIntersect(x, p1, planeP, planeN, out var fac) ?? p1;
                }).ToList();
                roofMesh.Vertices.AddRange(roofVertices);
            }
            else
            {
                var p1I = maxSideIndex;
                var p2I = (maxSideIndex + 1) % outerRing.Length;
                plane1 = new Vector3D(outerRing[p1I].X, outerRing[p1I].Y, bottomHeight + roofHeight);
                plane2 = new Vector3D(outerRing[p2I].X, outerRing[p2I].Y, bottomHeight + roofHeight);
                var plane3 = new Vector3D(outerRing[minVertexIndex].X, outerRing[minVertexIndex].Y, bottomHeight);

                (planeP, planeN) = AssimpHelpers.PlanePointNormal(plane1, plane2, plane3, false);

                roofVertices = outerRing.Select(x =>
                {
                    var p1 = new Vector3D(x.X, x.Y, bottomHeight);
                    return AssimpHelpers.PlaneLineIntersect(x, p1, planeP, planeN, out var fac) ?? p1;
                }).ToList();
                roofMesh.Vertices.AddRange(roofVertices);
            }

            /// Calculate texture coordinates:
            /// Get a coordinate system by upper edge and a perpendicular vector
            /// Ensure coordinate system still works in absolute units
            /// Express every bottom ring vertex in this coordinate system
            var e1 = plane2 - plane1;
            e1.Normalize();
            var e2 = Vector3D.Cross(e1, planeN); // In theory, both e1 and planeN are normalized, so e2 is normalized
            roofMesh.TextureCoordinateChannels[0].AddRange(roofVertices.Select(x =>
            {
                var projected = AssimpHelpers.ProjectOntoPlane(planeP, planeN, e2, e1, x);
                projected.Z = 0f;
                return projected;
            }));

            roofMesh.Faces.Add(new Face(Enumerable.Range(0, outerRing.Length).Reverse().ToArray()));
            roofMesh.UVComponentCount[0] = 2;

            var (matInd, matObj) = _materialTo3dConverter.GetRoofMaterialIndex(description.Roof.Material ?? RoofMaterialKindValues.BuildingRoofMaterialBitumen, scene);
            roofMesh.MaterialIndex = matInd;

            scene.Meshes.Add(roofMesh);
            var roofNode = new Node(parentNode.Name + "_Roof", parentNode);
            roofNode.MeshIndices.Add(scene.MeshCount - 1);
            parentNode.Children.Add(roofNode);

            // Fill the hole with walls
            for (var i = 0; i < outerRing.Length; ++i)
            {
                /// p2 --- p3
                /// |       |
                /// p1 --- p4
                /// i       j
                var j = (i + 1) % outerRing.Length;
                var p2 = roofVertices[i];
                var p3 = roofVertices[j];
                var p1 = options.YUp
                    ? new Vector3D(outerRing[i].X, bottomHeight, outerRing[i].Z)
                    : new Vector3D(outerRing[i].X, outerRing[i].Y, bottomHeight);
                var p4 = options.YUp
                    ? new Vector3D(outerRing[j].X, bottomHeight, outerRing[j].Z)
                    : new Vector3D(outerRing[j].X, outerRing[j].Y, bottomHeight);
                float p1p4 = (p4 - p1).Length();
                float p1p2 = options.YUp ? p2.Y - p1.Y : p2.Z - p1.Z;
                float p3p4 = options.YUp ? p3.Y - p4.Y : p3.Z - p4.Z;
                var count = 2;
                var eps = 1e-6f;

                buildingMesh.Vertices.Add(p1);
                buildingMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, bottomHeight, 0f));
                if (p1p2 > eps)
                {
                    buildingMesh.Vertices.Add(p2);
                    buildingMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, bottomHeight + p1p2, 0f));
                    ++count;
                }
                if (p3p4 > eps)
                {
                    buildingMesh.Vertices.Add(p3);
                    buildingMesh.TextureCoordinateChannels[0].Add(new Vector3D(p1p4, bottomHeight + p3p4, 0f));
                    ++count;
                }
                buildingMesh.Vertices.Add(p4);
                buildingMesh.TextureCoordinateChannels[0].Add(new Vector3D(p1p4, bottomHeight, 0f));
                buildingMesh.Faces.Add(new Face(Enumerable.Range(startIndex, count).ToArray()));
                startIndex += count;
            }
        }

        public void BuildRoofLOD0(
            VertexRing bottomRing,
            ref int startIndex,
            float bottomHeight,
            LevelModel topLevel,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            BuildingModel description,
            Scene scene,
            Node parentNode,
            Mesh buildingMesh,
            Vector3D[] outerRing)
        {
            /// Essentially this is just a slope from widest wall downwards.
            /// The height is dictated by roof height property.
            /// Find biggest wall, get 2 edge points.
            /// Get 3rd point by finding the furthest house vertex from the biggest wall.
            /// Get plane equation based on 3 points.
            /// For each vertical wall edge, intersect it with the plane to find the new vertex height.
            /// Add new wall faces to the wall, roof face for the roof.

            var roof = description.Roof;
            var maxSideIndex = 0;
            float maxSideLength = (outerRing[1] - outerRing[0]).LengthSquared();
            for (var i = 1; i < outerRing.Length; ++i)
            {
                float sideLength = (outerRing[(i + 1) % outerRing.Length] - outerRing[i]).LengthSquared();
                if (sideLength > maxSideLength)
                {
                    maxSideIndex = i;
                    maxSideLength = sideLength;
                }
            }

            var minVertexIndex = 0;
            var maxVertexLength = 0f;
            {
                var maxSideIndex2 = (maxSideIndex + 1) % outerRing.Length;
                var a = outerRing[maxSideIndex];
                var b = outerRing[maxSideIndex2];

                for (var i = 0; i < outerRing.Length; ++i)
                {
                    if (i == maxSideIndex || i == maxSideIndex2)
                    {
                        continue;
                    }

                    var v = outerRing[i];
                    var vertexLength = AssimpHelpers.LinePointDistance(v, a, b);
                    if (vertexLength > maxVertexLength)
                    {
                        minVertexIndex = i;
                        maxVertexLength = vertexLength;
                    }
                }
            }

            var roofMesh = new Mesh(PrimitiveType.Polygon);

            Vector3D plane1, plane2, planeP, planeN;
            List<Vector3D> roofVertices;
            var roofHeight = GetRoofHeight(roof, topLevel, outerRing);

            if (options.YUp)
            {
                var p1I = maxSideIndex;
                var p2I = (maxSideIndex + 1) % outerRing.Length;
                plane1 = new Vector3D(outerRing[p1I].X, bottomHeight + roofHeight, outerRing[p1I].Z);
                plane2 = new Vector3D(outerRing[p2I].X, bottomHeight + roofHeight, outerRing[p2I].Z);
                var plane3 = new Vector3D(outerRing[minVertexIndex].X, bottomHeight, outerRing[minVertexIndex].Z);

                (planeP, planeN) = AssimpHelpers.PlanePointNormal(plane1, plane2, plane3, true);

                roofVertices = outerRing.Select(x =>
                {
                    var p1 = new Vector3D(x.X, bottomHeight, x.Z);
                    return AssimpHelpers.PlaneLineIntersect(x, p1, planeP, planeN, out var fac) ?? p1;
                }).ToList();
            }
            else
            {
                var p1I = maxSideIndex;
                var p2I = (maxSideIndex + 1) % outerRing.Length;
                plane1 = new Vector3D(outerRing[p1I].X, outerRing[p1I].Y, bottomHeight + roofHeight);
                plane2 = new Vector3D(outerRing[p2I].X, outerRing[p2I].Y, bottomHeight + roofHeight);
                var plane3 = new Vector3D(outerRing[minVertexIndex].X, outerRing[minVertexIndex].Y, bottomHeight);

                (planeP, planeN) = AssimpHelpers.PlanePointNormal(plane1, plane2, plane3, false);

                roofVertices = outerRing.Select(x =>
                {
                    var p1 = new Vector3D(x.X, x.Y, bottomHeight);
                    return AssimpHelpers.PlaneLineIntersect(x, p1, planeP, planeN, out var fac) ?? p1;
                }).ToList();
            }

            // Add roof top and bottom
            var insetHeightUp = options.YUp ? new Vector3D(0f, Measurements.RoofLedgeThickness, 0f) : new Vector3D(0f, 0f, Measurements.RoofLedgeThickness);
            var insetVertices = AssimpHelpers.InsetRing(roofVertices, -Measurements.RoofLedgeThickness, planeN);
            var raisedVertices = insetVertices.Select(x => x + insetHeightUp).ToArray();
            roofMesh.Vertices.AddRange(roofVertices);
            roofMesh.Vertices.AddRange(insetVertices);
            roofMesh.Vertices.AddRange(raisedVertices);

            /// Calculate texture coordinates:
            /// Get a coordinate system by upper edge and a perpendicular vector
            /// Ensure coordinate system still works in absolute units
            /// Express every bottom ring vertex in this coordinate system
            var e1 = plane2 - plane1;
            e1.Normalize();
            var e2 = Vector3D.Cross(e1, planeN); // In theory, both e1 and planeN are normalized, so e2 is normalized
            roofMesh.TextureCoordinateChannels[0].AddRange(roofVertices.Select(x =>
            {
                var projected = AssimpHelpers.ProjectOntoPlane(planeP, planeN, e2, e1, x);
                projected.Z = 0f;
                return projected;
            }));
            var insetUVs = insetVertices.Select(x =>
            {
                var projected = AssimpHelpers.ProjectOntoPlane(planeP, planeN, e2, e1, x);
                projected.Z = 0f;
                return projected;
            });
            roofMesh.TextureCoordinateChannels[0].AddRange(insetUVs);
            roofMesh.TextureCoordinateChannels[0].AddRange(insetUVs);

            for (var i = 0; i < outerRing.Length; ++i)
            {
                roofMesh.Faces.Add(new Face(new int[] { i + outerRing.Length, (i + 1) % outerRing.Length + outerRing.Length, (i + 1) % outerRing.Length, i }));
            }

            roofMesh.Faces.Add(new Face(Enumerable.Range(outerRing.Length * 2, outerRing.Length).Reverse().ToArray()));

            // Roof sides
            var roofSideStartIndex = outerRing.Length * 3;
            for (var i = 0; i < outerRing.Length; ++i)
            {
                roofMesh.Vertices.Add(insetVertices[(i + 1) % outerRing.Length]);
                roofMesh.Vertices.Add(insetVertices[i]);
                roofMesh.Vertices.Add(raisedVertices[i]);
                roofMesh.Vertices.Add(raisedVertices[(i + 1) % outerRing.Length]);
                var uvLen = (insetVertices[(i + 1) % outerRing.Length] - insetVertices[i]).Length();
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(uvLen, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D());
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, Measurements.RoofLedgeThickness, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(uvLen, Measurements.RoofLedgeThickness, 0f));
                roofMesh.Faces.Add(new Face(Enumerable.Range(roofSideStartIndex + i * 4, 4).ToArray()));
            }

            roofMesh.UVComponentCount[0] = 2;

            var (matInd, matObj) = _materialTo3dConverter.GetRoofMaterialIndex(description.Roof.Material ?? RoofMaterialKindValues.BuildingRoofMaterialBitumen, scene);
            roofMesh.MaterialIndex = matInd;

            scene.Meshes.Add(roofMesh);
            var roofNode = new Node(parentNode.Name + "_Roof", parentNode);
            roofNode.MeshIndices.Add(scene.MeshCount - 1);
            parentNode.Children.Add(roofNode);

            // Fill the hole with walls
            for (var i = 0; i < outerRing.Length; ++i)
            {
                /// p2 --- p3
                /// |       |
                /// p1 --- p4
                /// i       j
                var j = (i + 1) % outerRing.Length;
                var p2 = roofVertices[i];
                var p3 = roofVertices[j];
                var p1 = options.YUp
                    ? new Vector3D(outerRing[i].X, bottomHeight, outerRing[i].Z)
                    : new Vector3D(outerRing[i].X, outerRing[i].Y, bottomHeight);
                var p4 = options.YUp
                    ? new Vector3D(outerRing[j].X, bottomHeight, outerRing[j].Z)
                    : new Vector3D(outerRing[j].X, outerRing[j].Y, bottomHeight);
                float p1p4 = (p4 - p1).Length();
                float p1p2 = options.YUp ? p2.Y - p1.Y : p2.Z - p1.Z;
                float p3p4 = options.YUp ? p3.Y - p4.Y : p3.Z - p4.Z;
                var count = 2;
                var eps = 1e-6f;

                buildingMesh.Vertices.Add(p1);
                buildingMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, bottomHeight, 0f));
                // Sometimes the vertical edge is so small it is better to merge it into a point
                if (p1p2 > eps)
                {
                    buildingMesh.Vertices.Add(p2);
                    buildingMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, bottomHeight + p1p2, 0f));
                    ++count;
                }
                if (p3p4 > eps)
                {
                    buildingMesh.Vertices.Add(p3);
                    buildingMesh.TextureCoordinateChannels[0].Add(new Vector3D(p1p4, bottomHeight + p3p4, 0f));
                    ++count;
                }
                buildingMesh.Vertices.Add(p4);
                buildingMesh.TextureCoordinateChannels[0].Add(new Vector3D(p1p4, bottomHeight, 0f));
                buildingMesh.Faces.Add(new Face(Enumerable.Range(startIndex, count).ToArray()));
                startIndex += count;
            }
        }

        public int GetSupportedLODCount()
        {
            return 2;
        }

        private float GetRoofHeight(RoofModel roof, LevelModel topLevel, Vector3D[] outerRing, float maxAngle = MathF.PI / 6f)
        {
            // Max angle in Ukraine is Pi/4 taken from https://budtraffic.net/ugol-naklona-krovli.html
            // but it looks unrealistic with tall narrow skillion buildings.

            var shortestLengthSquared = float.PositiveInfinity;

            for (var i = 0; i < outerRing.Length; ++i)
            {
                var a = outerRing[i];
                var b = outerRing[(i + 1) % outerRing.Length];

                var lengthSquared = AssimpHelpers.LengthSquared(a, b);
                if (shortestLengthSquared > lengthSquared)
                {
                    shortestLengthSquared = lengthSquared;
                }
            }

            var shortestLength = MathF.Sqrt(shortestLengthSquared);
            var presetHeight = (float)(roof.Height ?? (topLevel.Height ?? 1.0) / 2.0);

            var presetAngle = MathF.Abs(MathF.Atan(presetHeight / shortestLength));

            return presetAngle > maxAngle ? shortestLength * MathF.Tan(maxAngle) : presetHeight;
        }
    }
}
