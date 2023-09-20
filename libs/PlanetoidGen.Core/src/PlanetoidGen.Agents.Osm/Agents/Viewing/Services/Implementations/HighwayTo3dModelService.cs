using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Constants.KindValues;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations
{
    public class HighwayTo3dModelService : IHighwayTo3dModelService
    {
        private readonly IMaterialTo3dConverter _materialTo3DConverter;
        private readonly DecorationTo3dConverter _decorationTo3DConverter;

        private readonly Dictionary<string, float> _highwayWidths;

        public HighwayTo3dModelService()
        {
            _materialTo3DConverter = new MaterialTo3dConverter();
            _decorationTo3DConverter = new DecorationTo3dConverter();
            _highwayWidths = new Dictionary<string, float>()
            {
                { HighwayKindValues.HighwayMotorway, 3.5f },
                { HighwayKindValues.HighwayTrunk, 3.5f },
                { HighwayKindValues.HighwayPrimary, 3.5f },
                { HighwayKindValues.HighwaySecondary, 2.7f },
                { HighwayKindValues.HighwayResidential, 2.7f },
                { string.Empty, 2.7f },
            };
        }

        public void ProcessEntity(
            HighwayEntity entity,
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

                var node = new Node($"Highway_{entity.GID}_{i}", parent);
                node.Transform = Matrix4x4.FromTranslation(lineFirst - pivot);

                scene.Meshes.Add(BuildHighway(entity, i, options, planetoid, scene, node, line));

                node.MeshIndices.Add(scene.MeshCount - 1);

                parent.Children.Add(node);
            }
        }

        public Scene ProcessEntity(
            HighwayEntity entity,
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

        private Mesh BuildHighway(HighwayEntity entity, int lineIndex, ConvertTo3dModelAgentSettings options, PlanetoidInfoModel planetoid, Scene scene, Node node, Vector3D[] line)
        {
            var entityKind = entity.Kind ?? string.Empty;
            var width = entity.Width.HasValue ? (float)entity.Width.Value : (float?)null;

            if (!width.HasValue)
            {
                var lanes = entity.Lanes;

                if (lanes == null)
                {
                    lanes = 2;
                }

                var widthDictValue = _highwayWidths.ContainsKey(entityKind)
                    ? _highwayWidths[entityKind]
                    : _highwayWidths[string.Empty];

                width = widthDictValue * lanes;
            }

            var surface = entity.Surface ?? HighwaySurfaceKindValues.HighwaySurfaceAsphalt;

            return BuildHighwayMesh(
                entity.GID,
                lineIndex,
                options.YUp,
                options.FoundationHeight.HasValue ? (float)options.FoundationHeight.Value : (float?)null,
                width.Value,
                surface,
                scene,
                node,
                line);
        }

        private Mesh BuildHighwayMesh(long gid, int lineIndex, bool yUp, float? foundationHeight, float width, string surface, Scene scene, Node node, Vector3D[] line)
        {
            var mesh = new Mesh($"Mesh_Highway_{gid}_{lineIndex}", PrimitiveType.Polygon);

            BuildHighwayMeshFaces(width, yUp, foundationHeight, mesh, line);

            mesh.UVComponentCount[0] = 2;
            var (matInd, matObj) = _materialTo3DConverter.GetHighwayMaterialIndex(surface, scene);
            mesh.MaterialIndex = matInd;

            return mesh;
        }

        private void BuildHighwayMeshFaces(float width, bool yUp, float? foundationHeight, Mesh mesh, Vector3D[] line)
        {
            // First and last perpendicular edges are taken as normals to the line
            // Others are medians between line segment normals
            const float foundationFactor = 1.15f;
            var foundation = foundationHeight ?? 0f;
            var halfW = width * 0.5f;
            var foundationTextureWidth = MathF.Sqrt(foundation * foundation + foundationFactor * foundationFactor * halfW * halfW);

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

                mesh.TextureCoordinateChannels[0].Add(new Vector3D(width, 0f, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, length, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(width, length, 0f));

                mesh.Faces.Add(new Face(new int[] { faceIndex, faceIndex + 1, faceIndex + 2, faceIndex + 3 }));
                faceIndex += 4;

                if (foundationHeight.HasValue)
                {
                    mesh.Vertices.Add(line[i - 1] + averageRight1 * halfW);
                    mesh.Vertices.Add(line[i - 1] + averageRight1 * (halfW * foundationFactor) - up * foundation);
                    mesh.Vertices.Add(line[i] + averageRight2 * (halfW * foundationFactor) - up * foundation);
                    mesh.Vertices.Add(line[i] + averageRight2 * halfW);

                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(foundationTextureWidth, 0f, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(foundationTextureWidth, length, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, length, 0f));

                    mesh.Faces.Add(new Face(new int[] { faceIndex, faceIndex + 1, faceIndex + 2, faceIndex + 3 }));
                    faceIndex += 4;

                    mesh.Vertices.Add(line[i - 1] - averageRight1 * (halfW * foundationFactor) - up * foundation);
                    mesh.Vertices.Add(line[i - 1] - averageRight1 * halfW);
                    mesh.Vertices.Add(line[i] - averageRight2 * halfW);
                    mesh.Vertices.Add(line[i] - averageRight2 * (halfW * foundationFactor) - up * foundation);

                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(foundationTextureWidth, 0f, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, length, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(foundationTextureWidth, length, 0f));

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

                mesh.TextureCoordinateChannels[0].Add(new Vector3D(width, 0f, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(1f - foundationFactor * halfW, foundation, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(foundationFactor * halfW, foundation, 0f));

                mesh.Faces.Add(new Face(new int[] { faceIndex, faceIndex + 1, faceIndex + 2, faceIndex + 3 }));
                faceIndex += 4;

                mesh.Vertices.Add(mesh.Vertices[1]);
                mesh.Vertices.Add(mesh.Vertices[0]);
                mesh.Vertices.Add(mesh.Vertices[8]);
                mesh.Vertices.Add(mesh.Vertices[5]);

                mesh.TextureCoordinateChannels[0].Add(new Vector3D(width, 0f, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(1f - foundationFactor * halfW, foundation, 0f));
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(foundationFactor * halfW, foundation, 0f));

                mesh.Faces.Add(new Face(new int[] { faceIndex, faceIndex + 1, faceIndex + 2, faceIndex + 3 }));
            }
        }
    }
}
