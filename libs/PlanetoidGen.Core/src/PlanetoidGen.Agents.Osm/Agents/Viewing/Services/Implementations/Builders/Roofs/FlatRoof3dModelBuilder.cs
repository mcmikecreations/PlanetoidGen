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
    internal class FlatRoof3dModelBuilder : IRoofTo3dModelBuilder
    {
        private readonly IMaterialTo3dConverter _materialTo3dConverter;

        public FlatRoof3dModelBuilder(IMaterialTo3dConverter materialTo3DConverter)
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
            var roofMesh = new Mesh(PrimitiveType.Polygon);

            Vector3D planeP1, planeP2, planeN;
            List<Vector3D> roofVertices;

            if (options.YUp)
            {
                planeN = new Vector3D(0f, 1f, 0f);
                roofVertices = outerRing.Select(x => new Vector3D(x.X, bottomHeight, x.Z)).ToList();
            }
            else
            {
                planeN = new Vector3D(0f, 0f, 1f);
                roofVertices = outerRing.Select(x => new Vector3D(x.X, x.Y, bottomHeight)).ToList();
            }

            planeP1 = roofVertices[0];
            planeP2 = roofVertices[1];

            var e1 = planeP2 - planeP1;
            e1.Normalize();
            var e2 = Vector3D.Cross(e1, planeN);

            roofMesh.Vertices.AddRange(roofVertices);
            roofMesh.TextureCoordinateChannels[0].AddRange(roofVertices.Select(x =>
            {
                var projected = AssimpHelpers.ProjectOntoPlane(planeP1, planeN, e2, e1, x);
                projected.Z = 0f;
                return projected;
            }));
            roofMesh.Faces.Add(new Face(Enumerable.Range(0, bottomRing.Vertices.Count).ToArray()));
            roofMesh.UVComponentCount[0] = 2;

            var (matInd, matObj) = _materialTo3dConverter.GetRoofMaterialIndex(description.Roof.Material ?? RoofMaterialKindValues.BuildingRoofMaterialBitumen, scene);
            roofMesh.MaterialIndex = matInd;

            scene.Meshes.Add(roofMesh);
            var roofNode = new Node(parentNode.Name + "_Roof", parentNode);
            roofNode.MeshIndices.Add(scene.MeshCount - 1);
            parentNode.Children.Add(roofNode);
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
            var roofMesh = new Mesh(PrimitiveType.Polygon);

            Vector3D planeP1, planeP2, planeN;
            List<Vector3D> roofVertices;

            if (options.YUp)
            {
                planeN = new Vector3D(0f, 1f, 0f);
                roofVertices = outerRing.Select(x => new Vector3D(x.X, bottomHeight, x.Z)).ToList();
            }
            else
            {
                planeN = new Vector3D(0f, 0f, 1f);
                roofVertices = outerRing.Select(x => new Vector3D(x.X, x.Y, bottomHeight)).ToList();
            }

            planeP1 = roofVertices[0];
            planeP2 = roofVertices[1];

            var e1 = planeP2 - planeP1;
            e1.Normalize();
            var e2 = Vector3D.Cross(e1, planeN);

            var raiseHeight = Measurements.WindowSillHeight * 0.5f;
            var raiseHeightNormal = planeN * raiseHeight;
            var raisedVertices = roofVertices.Select(x => x + raiseHeightNormal).ToList();
            var insetVertices = AssimpHelpers.InsetRing(roofVertices, Measurements.RoofLedgeThickness, planeN);
            var insetRaisedVertices = insetVertices.Select(x => x + raiseHeightNormal).ToList();

            roofMesh.Vertices.AddRange(raisedVertices);
            roofMesh.Vertices.AddRange(insetRaisedVertices);

            var roofUVs = roofVertices.Select(x =>
            {
                var projected = AssimpHelpers.ProjectOntoPlane(planeP1, planeN, e2, e1, x);
                projected.Z = 0f;
                return projected;
            });
            var insetUVs = insetVertices.Select(x =>
            {
                var projected = AssimpHelpers.ProjectOntoPlane(planeP1, planeN, e2, e1, x);
                projected.Z = 0f;
                return projected;
            });

            roofMesh.TextureCoordinateChannels[0].AddRange(roofUVs);
            roofMesh.TextureCoordinateChannels[0].AddRange(insetUVs);

            for (var i = 0; i < outerRing.Length; ++i)
            {
                roofMesh.Faces.Add(new Face(new int[] { i + outerRing.Length, (i + 1) % outerRing.Length + outerRing.Length, (i + 1) % outerRing.Length, i }));
            }

            roofMesh.Vertices.AddRange(insetVertices);
            roofMesh.TextureCoordinateChannels[0].AddRange(insetUVs);
            roofMesh.Faces.Add(new Face(Enumerable.Range(outerRing.Length * 2, outerRing.Length).Reverse().ToArray()));

            // Roof sides
            var roofSideStartIndex = outerRing.Length * 3;
            for (var i = 0; i < outerRing.Length; ++i)
            {
                roofMesh.Vertices.Add(roofVertices[(i + 1) % outerRing.Length]);
                roofMesh.Vertices.Add(roofVertices[i]);
                roofMesh.Vertices.Add(raisedVertices[i]);
                roofMesh.Vertices.Add(raisedVertices[(i + 1) % outerRing.Length]);
                var uvLen = (roofVertices[(i + 1) % outerRing.Length] - roofVertices[i]).Length();
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(uvLen, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D());
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, raiseHeight, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(uvLen, raiseHeight, 0f));
                roofMesh.Faces.Add(new Face(Enumerable.Range(roofSideStartIndex + i * 8, 4).ToArray()));

                roofMesh.Vertices.Add(insetVertices[i]);
                roofMesh.Vertices.Add(insetVertices[(i + 1) % outerRing.Length]);
                roofMesh.Vertices.Add(insetRaisedVertices[(i + 1) % outerRing.Length]);
                roofMesh.Vertices.Add(insetRaisedVertices[i]);
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(Measurements.RoofLedgeThickness, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(uvLen - Measurements.RoofLedgeThickness * 2, 0f, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(uvLen - Measurements.RoofLedgeThickness * 2, raiseHeight, 0f));
                roofMesh.TextureCoordinateChannels[0].Add(new Vector3D(Measurements.RoofLedgeThickness, raiseHeight, 0f));
                roofMesh.Faces.Add(new Face(Enumerable.Range(roofSideStartIndex + i * 8 + 4, 4).ToArray()));
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
