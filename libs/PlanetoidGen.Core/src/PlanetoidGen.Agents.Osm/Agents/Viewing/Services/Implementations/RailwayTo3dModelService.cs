using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations
{
    public class RailwayTo3dModelService : IRailwayTo3dModelService
    {
        private readonly IMaterialTo3dConverter _materialTo3DConverter;
        private readonly DecorationTo3dConverter _decorationTo3DConverter;

        private const float RailwayWidth = 1.52f + 2.6f;

        public RailwayTo3dModelService()
        {
            _materialTo3DConverter = new MaterialTo3dConverter();
            _decorationTo3DConverter = new DecorationTo3dConverter();
        }

        public void ProcessEntity(
            RailwayEntity entity,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            IList<Vector3D[]> lines,
            Vector3D pivot,
            Scene scene,
            Node parent,
            int z)
        {
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }
            if (planetoid == null)
            {
                throw new ArgumentNullException(nameof(planetoid));
            }

            for (var i = 0; i < lines.Count; ++i)
            {
                var line = lines[i];
                var lineFirst = line.First();
                line = line.Select(x => x - lineFirst).ToArray();

                var node = new Node($"Railway_{entity.GID}_{i}", parent);
                node.Transform = Matrix4x4.FromTranslation(lineFirst - pivot);

                scene.Meshes.Add(BuildRailway(entity, i, options, planetoid, scene, node, line));

                node.MeshIndices.Add(scene.MeshCount - 1);

                parent.Children.Add(node);
            }
        }

        public Scene ProcessEntity(
            RailwayEntity entity,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            IList<Vector3D[]> lines,
            Vector3D pivot,
            int z)
        {
            var scene = new Scene();

            var rootNode = new Node("Root", null);

            scene.RootNode = rootNode;

            ProcessEntity(entity, options, planetoid, lines, pivot, scene, rootNode, z);

            return scene;
        }

        private Mesh BuildRailway(RailwayEntity entity, int lineIndex, ConvertTo3dModelAgentSettings options, PlanetoidInfoModel planetoid, Scene scene, Node node, Vector3D[] line)
        {
            var entityKind = entity.Kind ?? string.Empty;

            return BuildRailwayMesh(
                entity.GID,
                lineIndex,
                options.YUp,
                options.FoundationHeight.HasValue ? (float)options.FoundationHeight.Value : (float?)null,
                RailwayWidth,
                entityKind,
                scene,
                node,
                line);
        }

        private Mesh BuildRailwayMesh(long gid, int lineIndex, bool yUp, float? foundationHeight, float width, string surface, Scene scene, Node node, Vector3D[] line)
        {
            var mesh = new Mesh($"Mesh_Railway_{gid}_{lineIndex}", PrimitiveType.Polygon);

            BuildRailwayMeshFaces(width, yUp, foundationHeight, mesh, line);

            mesh.UVComponentCount[0] = 2;
            var (matInd, matObj) = _materialTo3DConverter.GetRailwayMaterialIndex(surface, scene);
            mesh.MaterialIndex = matInd;

            return mesh;
        }

        private void BuildRailwayMeshFaces(float width, bool yUp, float? foundationHeight, Mesh mesh, Vector3D[] line)
        {
            // First and last perpendicular edges are taken as normals to the line
            // Others are medians between line segment normals
            const float foundationFactor = 1.15f;
            var foundation = foundationHeight ?? 0f;
            var halfW = width * 0.5f;

            Vector3D up, forward, right0, right1, right2, averageRight1, averageRight2;

            up = yUp ? new Vector3D(0f, 1f, 0f) : new Vector3D(0f, 0f, 1f);

            forward = line[1] - line[0];
            forward.Normalize();
            right0 = Vector3D.Cross(forward, up);

            var faceIndex = 0;
            float length;

            for (var i = 1; i < line.Length; ++i)
            {
                forward = line[i] - line[i - 1];
                length = forward.Length();
                forward.X /= length;
                forward.Y /= length;
                forward.Z /= length;
                length /= width;

                right1 = Vector3D.Cross(forward, up);

                if (i < line.Length - 1)
                {
                    forward = line[i + 1] - line[i];
                    forward.Normalize();
                    right2 = Vector3D.Cross(forward, up);
                }
                else
                {
                    right2 = right1;
                }

                averageRight1 = (right0 + right1) * 0.5f;
                mesh.Vertices.Add(line[i - 1] - averageRight1 * halfW);
                mesh.Vertices.Add(line[i - 1] + averageRight1 * halfW);

                averageRight2 = (right1 + right2) * 0.5f;
                mesh.Vertices.Add(line[i] + averageRight2 * halfW);
                mesh.Vertices.Add(line[i] - averageRight2 * halfW);

                mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(1f, 0f, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(1f, length, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, length, 0f));

                mesh.Faces.Add(new Face(new int[] { faceIndex, faceIndex + 1, faceIndex + 2, faceIndex + 3 }));
                faceIndex += 4;

                if (foundationHeight.HasValue)
                {
                    mesh.Vertices.Add(line[i - 1] + averageRight1 * halfW);
                    mesh.Vertices.Add(line[i - 1] + averageRight1 * (halfW * foundationFactor) - up * foundation);
                    mesh.Vertices.Add(line[i] + averageRight2 * (halfW * foundationFactor) - up * foundation);
                    mesh.Vertices.Add(line[i] + averageRight2 * halfW);

                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(.15f, 0f, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(.15f, length, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, length, 0f));

                    mesh.Faces.Add(new Face(new int[] { faceIndex, faceIndex + 1, faceIndex + 2, faceIndex + 3 }));
                    faceIndex += 4;

                    mesh.Vertices.Add(line[i - 1] - averageRight1 * (halfW * foundationFactor) - up * foundation);
                    mesh.Vertices.Add(line[i - 1] - averageRight1 * halfW);
                    mesh.Vertices.Add(line[i] - averageRight2 * halfW);
                    mesh.Vertices.Add(line[i] - averageRight2 * (halfW * foundationFactor) - up * foundation);

                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(.15f, 0f, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, length, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(.15f, length, 0f));

                    mesh.Faces.Add(new Face(new int[] { faceIndex, faceIndex + 1, faceIndex + 2, faceIndex + 3 }));
                    faceIndex += 4;
                }

                right0 = right1;
            }

            if (foundationHeight.HasValue)
            {
                mesh.Vertices.Add(mesh.Vertices[faceIndex - 9]);
                mesh.Vertices.Add(mesh.Vertices[faceIndex - 10]);
                mesh.Vertices.Add(mesh.Vertices[faceIndex - 6]);
                mesh.Vertices.Add(mesh.Vertices[faceIndex - 1]);

                mesh.TextureCoordinateChannels[0].Add(new Vector3D(1f, 0f, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(1f - foundationFactor, foundation / width, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(foundationFactor, foundation / width, 0f));

                mesh.Faces.Add(new Face(new int[] { faceIndex, faceIndex + 1, faceIndex + 2, faceIndex + 3 }));
                faceIndex += 4;

                mesh.Vertices.Add(mesh.Vertices[1]);
                mesh.Vertices.Add(mesh.Vertices[0]);
                mesh.Vertices.Add(mesh.Vertices[8]);
                mesh.Vertices.Add(mesh.Vertices[5]);

                mesh.TextureCoordinateChannels[0].Add(new Vector3D(1f, 0f, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(1f - foundationFactor, foundation / width, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(foundationFactor, foundation / width, 0f));

                mesh.Faces.Add(new Face(new int[] { faceIndex, faceIndex + 1, faceIndex + 2, faceIndex + 3 }));
            }
        }
    }
}
