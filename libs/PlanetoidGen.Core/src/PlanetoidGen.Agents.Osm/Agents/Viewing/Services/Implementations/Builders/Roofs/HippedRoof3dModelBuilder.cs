using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Collections;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Constants;
using PlanetoidGen.Agents.Osm.Constants.KindValues;
using PlanetoidGen.Agents.Osm.Helpers;
using PlanetoidGen.Agents.Standard.Helpers;
using PlanetoidGen.Domain.Models.Descriptions.Building;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;
using System.Linq;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations.Builders.Roofs
{
    internal class HippedRoof3dModelBuilder : IRoofTo3dModelBuilder
    {
        private readonly IMaterialTo3dConverter _materialTo3dConverter;

        public HippedRoof3dModelBuilder(IMaterialTo3dConverter materialTo3DConverter)
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
            if (outerRing.Length % 2 == 1)
            {
                // To have at least something in the corner case scenario
                var flatRoofBuilder = new FlatRoof3dModelBuilder(_materialTo3dConverter);
                flatRoofBuilder.BuildRoof(
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
                    outerRing,
                    0);
                return;
            }

            /// o-------o-------o
            /// |B      |P      |C
            /// o-------o-------o
            /// |M      |O      |N
            /// o-------o-------o
            ///  A       R       D
            /// The algorithm goes as follows:
            /// Find the shortest side
            /// Set upper pointer to side end
            /// Set lower pointer to side start
            /// Go along the sides by moving the pointers
            /// For each move of pointers, add the center of the segment between pointers to the polyline
            /// End the loop when pointers cross
            /// This algorithm doesn't work with building shapes with an odd number of vertices
            var roofMesh = new Mesh(PrimitiveType.Polygon);
            var roofHeight = (float)description.Roof.Height!;

            var smallestSideIndex = 0;
            var smallestSideLength = float.PositiveInfinity;
            for (var i = 0; i < outerRing.Length; ++i)
            {
                var p1 = outerRing[i];
                var p2 = outerRing[(i + 1) % outerRing.Length];
                var length = AssimpHelpers.LengthSquared(p1, p2);
                if (length < smallestSideLength)
                {
                    smallestSideIndex = i;
                    smallestSideLength = length;
                }
            }

            var upperIndex = (smallestSideIndex + 1) % outerRing.Length;
            var lowerIndex = smallestSideIndex;
            Vector3D lowerPoint, upperPoint, centerPoint, up;
            List<Vector3D> roofVertices;
            var spine = new List<Vector3D>();

            if (options.YUp)
            {
                up = new Vector3D(0f, 1f, 0f);
                roofVertices = outerRing.Select(x => new Vector3D(x.X, bottomHeight, x.Z)).ToList();
            }
            else
            {
                up = new Vector3D(0f, 0f, 1f);
                roofVertices = outerRing.Select(x => new Vector3D(x.X, x.Y, bottomHeight)).ToList();
            }

            var roofTop = up * (roofHeight + bottomHeight);
            var offsetSpine = up * Measurements.InsulationOffset;
            var offsetRoof = up * Measurements.RoofLedgeThickness;

            while (true)
            {
                upperPoint = outerRing[upperIndex];
                lowerPoint = outerRing[lowerIndex];
                centerPoint = new Vector3D(
                    (lowerPoint.X + upperPoint.X) * 0.5f,
                    (lowerPoint.Y + upperPoint.Y) * 0.5f,
                    (lowerPoint.Z + upperPoint.Z) * 0.5f);
                centerPoint.Y += roofTop.Y; // To avoid memory allocations and if statements
                centerPoint.Z += roofTop.Z;
                spine.Add(centerPoint);

                if (upperIndex == lowerIndex) break;
                lowerIndex = MathHelpers.Modulo(lowerIndex - 1, outerRing.Length);
                if (upperIndex == lowerIndex) break;
                upperIndex = (upperIndex + 1) % outerRing.Length;
            }

            var spineHorizontalOffset = spine[1] - spine[0];
            spineHorizontalOffset *= roofHeight / spineHorizontalOffset.Length();
            spine[0] += spineHorizontalOffset;
            spineHorizontalOffset = spine[spine.Count - 2] - spine[spine.Count - 1];
            spineHorizontalOffset *= roofHeight / spineHorizontalOffset.Length();
            spine[spine.Count - 1] += spineHorizontalOffset;

            int buildingLengthIndex = buildingMesh.VertexCount;

            for (var i = 0; i < outerRing.Length; ++i)
            {
                var sideLength = AssimpHelpers.Length(roofVertices[(i + 1) % outerRing.Length], roofVertices[i]);
                buildingMesh.Vertices.Add(roofVertices[(i + 1) % outerRing.Length]);
                buildingMesh.Vertices.Add(roofVertices[i]);
                buildingMesh.Vertices.Add(roofVertices[i] + offsetSpine);
                buildingMesh.Vertices.Add(roofVertices[(i + 1) % outerRing.Length] + offsetSpine);
                buildingMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, bottomHeight, 0f));
                buildingMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, bottomHeight, 0f));
                buildingMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, bottomHeight + Measurements.InsulationOffset, 0f));
                buildingMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, bottomHeight + Measurements.InsulationOffset, 0f));
                buildingMesh.Faces.Add(new Face(new int[]
                {
                    buildingLengthIndex + i * 4,
                    buildingLengthIndex + 1 + i * 4,
                    buildingLengthIndex + 2 + i * 4,
                    buildingLengthIndex + 3 + i * 4,
                }));
            }

            Vector3D planeP, planeN, e1, e2, offsetStart, offsetEnd;
            Vector3D[] planeVerts;
            var spineIndex = 0;
            int faceOffset = roofMesh.VertexCount;

            {
                // Front side
                offsetStart = roofVertices[smallestSideIndex] - spine[spineIndex];
                offsetStart *= 2f * Measurements.InsulationOffset / offsetStart.Length();
                offsetStart += offsetSpine;
                offsetEnd = roofVertices[(smallestSideIndex + 1) % outerRing.Length] - spine[spineIndex];
                offsetEnd *= 2f * Measurements.InsulationOffset / offsetEnd.Length();
                offsetEnd += offsetSpine;

                // Slope part
                planeVerts = new Vector3D[]
                {
                    roofVertices[smallestSideIndex] + offsetStart + offsetRoof,
                    spine[spineIndex] + offsetSpine + offsetRoof,
                    roofVertices[(smallestSideIndex + 1) % outerRing.Length] + offsetEnd + offsetRoof,
                };
                var sideLength = AssimpHelpers.Length(planeVerts[0], planeVerts[2]);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(bottomHeight, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(bottomHeight + MathHelpers.Sqrt2 * roofHeight, sideLength * 0.5f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(bottomHeight, sideLength, 0f));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                }));
                faceOffset += 3;

                // Underside part
                planeVerts = new Vector3D[]
                {
                    roofVertices[smallestSideIndex] + offsetStart,
                    roofVertices[(smallestSideIndex + 1) % outerRing.Length] + offsetEnd,
                    roofVertices[(smallestSideIndex + 1) % outerRing.Length] + offsetSpine,
                    roofVertices[smallestSideIndex] + offsetSpine,
                };
                sideLength = AssimpHelpers.Length(planeVerts[0], planeVerts[1]);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D());
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, 2f * Measurements.InsulationOffset, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 2f * Measurements.InsulationOffset, 0f));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                    faceOffset + 3,
                }));
                faceOffset += 4;

                // Vertical part
                planeVerts = new Vector3D[]
                {
                    roofVertices[(smallestSideIndex + 1) % outerRing.Length] + offsetEnd,
                    roofVertices[smallestSideIndex] + offsetStart,
                    roofVertices[smallestSideIndex] + offsetStart + offsetRoof,
                    roofVertices[(smallestSideIndex + 1) % outerRing.Length] + offsetEnd + offsetRoof,
                };
                sideLength = AssimpHelpers.Length(planeVerts[0], planeVerts[1]);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D());
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, Measurements.RoofLedgeThickness, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, Measurements.RoofLedgeThickness, 0f));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                    faceOffset + 3,
                }));
                faceOffset += 4;

                // Back side
                lowerIndex = (upperIndex + 1) % outerRing.Length;
                spineIndex = spine.Count - 1;
                offsetStart = roofVertices[upperIndex] - spine[spineIndex];
                offsetStart *= 2f * Measurements.InsulationOffset / offsetStart.Length();
                offsetStart += offsetSpine;
                offsetEnd = roofVertices[lowerIndex] - spine[spineIndex];
                offsetEnd *= 2f * Measurements.InsulationOffset / offsetEnd.Length();
                offsetEnd += offsetSpine;

                // Slope part
                planeVerts = new Vector3D[]
                {
                    roofVertices[upperIndex] + offsetStart + offsetRoof,
                    spine[spineIndex] + offsetSpine + offsetRoof,
                    roofVertices[lowerIndex] + offsetEnd + offsetRoof,
                };
                sideLength = AssimpHelpers.Length(planeVerts[0], planeVerts[2]);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(bottomHeight, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(bottomHeight + MathHelpers.Sqrt2 * roofHeight, sideLength * 0.5f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(bottomHeight, sideLength, 0f));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                }));
                faceOffset += 3;

                // Underside part
                planeVerts = new Vector3D[]
                {
                    roofVertices[upperIndex] + offsetStart,
                    roofVertices[lowerIndex] + offsetEnd,
                    roofVertices[lowerIndex] + offsetSpine,
                    roofVertices[upperIndex] + offsetSpine,
                };
                sideLength = AssimpHelpers.Length(planeVerts[0], planeVerts[1]);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D());
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, 2f * Measurements.InsulationOffset, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 2f * Measurements.InsulationOffset, 0f));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                    faceOffset + 3,
                }));
                faceOffset += 4;

                // Vertical part
                planeVerts = new Vector3D[]
                {
                    roofVertices[lowerIndex] + offsetEnd,
                    roofVertices[upperIndex] + offsetStart,
                    roofVertices[upperIndex] + offsetStart + offsetRoof,
                    roofVertices[lowerIndex] + offsetEnd + offsetRoof,
                };
                sideLength = AssimpHelpers.Length(planeVerts[0], planeVerts[1]);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D());
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, Measurements.RoofLedgeThickness, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, Measurements.RoofLedgeThickness, 0f));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                    faceOffset + 3,
                }));
                faceOffset += 4;
            }

            spineIndex = 0;
            upperIndex = (smallestSideIndex + 1) % outerRing.Length;
            lowerIndex = smallestSideIndex;

            while (true)
            {
                // Front side
                offsetStart = roofVertices[lowerIndex] - spine[spineIndex];
                offsetEnd = roofVertices[MathHelpers.Modulo(lowerIndex - 1, outerRing.Length)] - spine[spineIndex + 1];
                offsetStart *= 2f * Measurements.InsulationOffset / offsetStart.Length();
                offsetEnd *= 2f * Measurements.InsulationOffset / offsetEnd.Length();
                offsetStart += offsetSpine;
                offsetEnd += offsetSpine;

                // Underside part
                planeVerts = new Vector3D[]
                {
                    roofVertices[MathHelpers.Modulo(lowerIndex - 1, outerRing.Length)] + offsetEnd,
                    roofVertices[lowerIndex] + offsetStart,
                    roofVertices[lowerIndex] + offsetSpine,
                    roofVertices[MathHelpers.Modulo(lowerIndex - 1, outerRing.Length)] + offsetSpine,
                };
                var sideLength = AssimpHelpers.Length(planeVerts[0], planeVerts[1]);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D());
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, 2f * Measurements.InsulationOffset, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 2f * Measurements.InsulationOffset, 0f));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                    faceOffset + 3,
                }));
                faceOffset += 4;

                // Vertical part
                planeVerts = new Vector3D[]
                {
                    roofVertices[lowerIndex] + offsetStart,
                    roofVertices[MathHelpers.Modulo(lowerIndex - 1, outerRing.Length)] + offsetEnd,
                    roofVertices[MathHelpers.Modulo(lowerIndex - 1, outerRing.Length)] + offsetEnd + offsetRoof,
                    roofVertices[lowerIndex] + offsetStart + offsetRoof,
                };
                sideLength = AssimpHelpers.Length(planeVerts[0], planeVerts[1]);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D());
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, Measurements.RoofLedgeThickness, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, Measurements.RoofLedgeThickness, 0f));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                    faceOffset + 3,
                }));
                faceOffset += 4;

                // Slope part
                planeVerts = new Vector3D[]
                {
                    planeVerts[3],
                    planeVerts[2],
                    spine[spineIndex + 1] + offsetSpine + offsetRoof,
                    spine[spineIndex] + offsetSpine + offsetRoof,
                };
                planeP = planeVerts[0];
                e1 = planeVerts[1] - planeVerts[0];
                e1.Normalize();
                e2 = planeVerts[3] - planeVerts[0];
                e2.Normalize();
                planeN = Vector3D.Cross(e1, e2);
                e2 = Vector3D.Cross(e1, planeN);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].AddRange(planeVerts.Select(x =>
                {
                    var projected = AssimpHelpers.ProjectOntoPlane(planeP, planeN, e2, e1, x);
                    projected.Z = 0f;
                    return projected;
                }));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                    faceOffset + 3,
                }));
                faceOffset += 4;

                // Back side
                offsetStart = roofVertices[(upperIndex + 1) % outerRing.Length] - spine[spineIndex + 1];
                offsetEnd = roofVertices[upperIndex] - spine[spineIndex];
                offsetStart *= 2f * Measurements.InsulationOffset / offsetStart.Length();
                offsetEnd *= 2f * Measurements.InsulationOffset / offsetEnd.Length();
                offsetStart += offsetSpine;
                offsetEnd += offsetSpine;

                // Underside part
                planeVerts = new Vector3D[]
                {
                    roofVertices[upperIndex] + offsetEnd,
                    roofVertices[(upperIndex + 1) % outerRing.Length] + offsetStart,
                    roofVertices[(upperIndex + 1) % outerRing.Length] + offsetSpine,
                    roofVertices[upperIndex] + offsetSpine,
                };
                sideLength = AssimpHelpers.Length(planeVerts[0], planeVerts[1]);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D());
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, 2f * Measurements.InsulationOffset, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 2f * Measurements.InsulationOffset, 0f));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                    faceOffset + 3,
                }));
                faceOffset += 4;

                // Vertical part
                planeVerts = new Vector3D[]
                {
                    roofVertices[(upperIndex + 1) % outerRing.Length] + offsetStart,
                    roofVertices[upperIndex] + offsetEnd,
                    roofVertices[upperIndex] + offsetEnd + offsetRoof,
                    roofVertices[(upperIndex + 1) % outerRing.Length] + offsetStart + offsetRoof,
                };
                sideLength = AssimpHelpers.Length(planeVerts[0], planeVerts[1]);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D());
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(sideLength, Measurements.RoofLedgeThickness, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, Measurements.RoofLedgeThickness, 0f));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                    faceOffset + 3,
                }));
                faceOffset += 4;

                // Slope part
                planeVerts = new Vector3D[]
                {
                    roofVertices[(upperIndex + 1) % outerRing.Length] + offsetStart + offsetRoof,
                    roofVertices[upperIndex] + offsetEnd + offsetRoof,
                    spine[spineIndex] + offsetSpine + offsetRoof,
                    spine[spineIndex + 1] + offsetSpine + offsetRoof,
                };
                planeP = planeVerts[0];
                e1 = planeVerts[1] - planeVerts[0];
                e1.Normalize();
                e2 = planeVerts[3] - planeVerts[0];
                e2.Normalize();
                planeN = Vector3D.Cross(e1, e2);
                e2 = Vector3D.Cross(e1, planeN);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].AddRange(planeVerts.Select(x =>
                {
                    var projected = AssimpHelpers.ProjectOntoPlane(planeP, planeN, e2, e1, x);
                    projected.Z = 0f;
                    return projected;
                }));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                    faceOffset + 3,
                }));
                faceOffset += 4;

                if (upperIndex == lowerIndex || spineIndex >= spine.Count - 2) break;
                lowerIndex = MathHelpers.Modulo(lowerIndex - 1, outerRing.Length);
                if (upperIndex == lowerIndex) break;
                upperIndex = (upperIndex + 1) % outerRing.Length;
            }

            roofMesh.UVComponentCount[0] = 2;

            var (matInd, matObj) = _materialTo3dConverter.GetRoofMaterialIndex(description.Roof.Material ?? RoofMaterialKindValues.BuildingRoofMaterialBitumen, scene);
            roofMesh.MaterialIndex = matInd;

            scene.Meshes.Add(roofMesh);
            var roofNode = new Node(parentNode.Name + "_Roof", parentNode);
            roofNode.MeshIndices.Add(scene.MeshCount - 1);
            parentNode.Children.Add(roofNode);
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
            if (outerRing.Length % 2 == 1)
            {
                // To have at least something in the corner case scenario
                var flatRoofBuilder = new FlatRoof3dModelBuilder(_materialTo3dConverter);
                flatRoofBuilder.BuildRoof(
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
                    outerRing,
                    1);
                return;
            }

            /// o-------o-------o
            /// |B      |P      |C
            /// o-------o-------o
            /// |M      |O      |N
            /// o-------o-------o
            ///  A       R       D
            /// The algorithm goes as follows:
            /// Find the shortest side
            /// Set upper pointer to side end
            /// Set lower pointer to side start
            /// Go along the sides by moving the pointers
            /// For each move of pointers, add the center of the segment between pointers to the polyline
            /// End the loop when pointers cross
            /// This algorithm doesn't work with building shapes with an odd number of vertices
            var roofMesh = new Mesh(PrimitiveType.Polygon);
            var roofHeight = (float)description.Roof.Height!;

            var smallestSideIndex = 0;
            var smallestSideLength = float.PositiveInfinity;
            for (var i = 0; i < outerRing.Length; ++i)
            {
                var p1 = outerRing[i];
                var p2 = outerRing[(i + 1) % outerRing.Length];
                var length = AssimpHelpers.LengthSquared(p1, p2);
                if (length < smallestSideLength)
                {
                    smallestSideIndex = i;
                    smallestSideLength = length;
                }
            }

            var upperIndex = (smallestSideIndex + 1) % outerRing.Length;
            var lowerIndex = smallestSideIndex;
            Vector3D lowerPoint, upperPoint, centerPoint, up;
            List<Vector3D> roofVertices;
            var spine = new List<Vector3D>();

            if (options.YUp)
            {
                up = new Vector3D(0f, 1f, 0f);
                roofVertices = outerRing.Select(x => new Vector3D(x.X, bottomHeight, x.Z)).ToList();
            }
            else
            {
                up = new Vector3D(0f, 0f, 1f);
                roofVertices = outerRing.Select(x => new Vector3D(x.X, x.Y, bottomHeight)).ToList();
            }

            var roofTop = up * (roofHeight + bottomHeight);

            while (true)
            {
                upperPoint = outerRing[upperIndex];
                lowerPoint = outerRing[lowerIndex];
                centerPoint = new Vector3D(
                    (lowerPoint.X + upperPoint.X) * 0.5f,
                    (lowerPoint.Y + upperPoint.Y) * 0.5f,
                    (lowerPoint.Z + upperPoint.Z) * 0.5f);
                centerPoint.Y += roofTop.Y; // To avoid memory allocations and if statements
                centerPoint.Z += roofTop.Z;
                spine.Add(centerPoint);

                if (upperIndex == lowerIndex) break;
                lowerIndex = MathHelpers.Modulo(lowerIndex - 1, outerRing.Length);
                if (upperIndex == lowerIndex) break;
                upperIndex = (upperIndex + 1) % outerRing.Length;
            }

            var spineHorizontalOffset = spine[1] - spine[0];
            spineHorizontalOffset *= roofHeight / spineHorizontalOffset.Length();
            spine[0] += spineHorizontalOffset;
            spineHorizontalOffset = spine[spine.Count - 2] - spine[spine.Count - 1];
            spineHorizontalOffset *= roofHeight / spineHorizontalOffset.Length();
            spine[spine.Count - 1] += spineHorizontalOffset;

            // For hipped roofs
            // roofMesh.Faces.Add(new Face(new int[] { smallestSideIndex, (smallestSideIndex + 1) % outerRing.Length, outerRing.Length }));
            // roofMesh.Faces.Add(new Face(new int[] { lowerIndex, (lowerIndex + 1) % outerRing.Length, outerRing.Length + spine.Count - 1 }));
            int buildingLengthIndex = roofMesh.VertexCount;
            // Front
            roofMesh.Vertices.Add(roofVertices[smallestSideIndex]);
            roofMesh.Vertices.Add(roofVertices[(smallestSideIndex + 1) % outerRing.Length]);
            roofMesh.Vertices.Add(spine[0]);
            // Back
            roofMesh.Vertices.Add(roofVertices[lowerIndex]);
            roofMesh.Vertices.Add(roofVertices[(lowerIndex + 1) % outerRing.Length]);
            roofMesh.Vertices.Add(spine[spine.Count - 1]);

            var frontWidth = AssimpHelpers.Length(roofMesh.Vertices[buildingLengthIndex], roofMesh.Vertices[buildingLengthIndex + 1]);
            var backWidth = AssimpHelpers.Length(roofMesh.Vertices[buildingLengthIndex + 3], roofMesh.Vertices[buildingLengthIndex + 4]);

            // Texture coordinates
            roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(bottomHeight, 0f, 0f));
            roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(bottomHeight, frontWidth, 0f));
            roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(bottomHeight + MathHelpers.Sqrt2 * roofHeight, frontWidth * 0.5f, 0f));
            roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(bottomHeight, 0f, 0f));
            roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(bottomHeight, backWidth, 0f));
            roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(bottomHeight + MathHelpers.Sqrt2 * roofHeight, backWidth * 0.5f, 0f));

            roofMesh.Faces.Add(new Face(new int[] { buildingLengthIndex, buildingLengthIndex + 2, buildingLengthIndex + 1, }));
            roofMesh.Faces.Add(new Face(new int[] { buildingLengthIndex + 3, buildingLengthIndex + 5, buildingLengthIndex + 4, }));

            upperIndex = (smallestSideIndex + 1) % outerRing.Length;
            lowerIndex = smallestSideIndex;
            var spineIndex = 0;

            Vector3D planeP, planeN, e1, e2;
            Vector3D[] planeVerts;

            int faceOffset = roofMesh.VertexCount;
            while (true)
            {
                planeVerts = new Vector3D[]
                {
                    roofVertices[lowerIndex],
                    roofVertices[MathHelpers.Modulo(lowerIndex - 1, outerRing.Length)],
                    spine[spineIndex + 1],
                    spine[spineIndex],
                };
                planeP = planeVerts[0];
                e1 = planeVerts[1] - planeVerts[0];
                e1.Normalize();
                e2 = planeVerts[3] - planeVerts[0];
                e2.Normalize();
                planeN = Vector3D.Cross(e1, e2);
                e2 = Vector3D.Cross(e1, planeN);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].AddRange(planeVerts.Select(x =>
                {
                    var projected = AssimpHelpers.ProjectOntoPlane(planeP, planeN, e2, e1, x);
                    projected.Z = 0f;
                    return projected;
                }));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                    faceOffset + 3,
                }));
                faceOffset += 4;

                planeVerts = new Vector3D[]
                {
                    roofVertices[(upperIndex + 1) % outerRing.Length],
                    roofVertices[upperIndex],
                    spine[spineIndex],
                    spine[spineIndex + 1],
                };
                planeP = planeVerts[0];
                e1 = planeVerts[1] - planeVerts[0];
                e1.Normalize();
                e2 = planeVerts[3] - planeVerts[0];
                e2.Normalize();
                planeN = Vector3D.Cross(e1, e2);
                e2 = Vector3D.Cross(e1, planeN);
                roofMesh.Vertices.AddRange(planeVerts);
                roofMesh.TextureCoordinateChannels[0].AddRange(planeVerts.Select(x =>
                {
                    var projected = AssimpHelpers.ProjectOntoPlane(planeP, planeN, e2, e1, x);
                    projected.Z = 0f;
                    return projected;
                }));
                roofMesh.Faces.Add(new Face(new int[]
                {
                    faceOffset,
                    faceOffset + 1,
                    faceOffset + 2,
                    faceOffset + 3,
                }));
                faceOffset += 4;

                if (upperIndex == lowerIndex || spineIndex >= spine.Count - 2) break;
                lowerIndex = MathHelpers.Modulo(lowerIndex - 1, outerRing.Length);
                if (upperIndex == lowerIndex) break;
                upperIndex = (upperIndex + 1) % outerRing.Length;
            }

            roofMesh.UVComponentCount[0] = 2;

            var (matInd, matObj) = _materialTo3dConverter.GetRoofMaterialIndex(description.Roof.Material ?? RoofMaterialKindValues.BuildingRoofMaterialBitumen, scene);
            roofMesh.MaterialIndex = matInd;

            scene.Meshes.Add(roofMesh);
            var roofNode = new Node(parentNode.Name + "_Roof", parentNode);
            roofNode.MeshIndices.Add(scene.MeshCount - 1);
            parentNode.Children.Add(roofNode);
        }

        public int GetSupportedLODCount()
        {
            return 2;
        }
    }
}
