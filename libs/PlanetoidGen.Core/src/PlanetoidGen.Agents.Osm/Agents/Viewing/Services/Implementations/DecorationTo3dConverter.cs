using Assimp;
using PlanetoidGen.Agents.Osm.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations
{
    internal class DecorationTo3dConverter
    {
        private readonly Dictionary<string, Scene> _loadedScenes;
        private readonly AssimpContext _context;

        public DecorationTo3dConverter()
        {
            _loadedScenes = new Dictionary<string, Scene>();
            _context = new AssimpContext();
        }

        /// <summary>
        /// Ensures a file with decorations is loaded.
        /// </summary>
        /// <param name="decorationFileNameExt">The file name with extension of the decoration to load.</param>
        /// <returns>True if it was loaded now or before, false if file doesn't exist.</returns>
        public bool EnsureLoaded(string decorationFileNameExt)
        {
            if (_loadedScenes.ContainsKey(decorationFileNameExt)) return true;

            var filePath = "Data/Models/" + decorationFileNameExt;
            if (!File.Exists(filePath)) return false;

            var scene = _context.ImportFile(filePath);
            _loadedScenes[decorationFileNameExt] = scene;

            return true;
        }

        /// <summary>
        /// Add a decoration node to the parent node.
        /// Decoration source transformation is used.
        /// </summary>
        /// <param name="decorationFileNameExt"></param>
        /// <param name="scene"></param>
        /// <param name="parentNode"></param>
        /// <returns>The child node, already added to the parent, if added successfully; null otherwise.</returns>
        public Node? AddDecoration(string decorationFileNameExt, string decorationName, Scene scene, Node parentNode)
        {
            if (!EnsureLoaded(decorationFileNameExt)) return null;

            var decorationScene = _loadedScenes[decorationFileNameExt];
            var decorationNode = decorationScene.RootNode.FindNode(decorationName);
            if (decorationNode == null) return null;

            var childNode = new Node("", parentNode)
            {
                Transform = decorationNode.Transform,
            };
            parentNode.Children.Add(childNode);

            for (var i = 0; i < decorationNode.MeshCount; ++i)
            {
                int decorationMeshIndex = decorationNode.MeshIndices[i];
                var decorationMesh = decorationScene.Meshes[decorationMeshIndex];

                // A simple check for matching meshes
                var matchingMesh = scene.Meshes.FirstOrDefault(x =>
                    x.Name == decorationMesh.Name &&
                    x.FaceCount == decorationMesh.FaceCount &&
                    x.VertexCount == decorationMesh.VertexCount);

                if (matchingMesh == null)
                {
                    matchingMesh = AssimpHelpers.Clone(decorationMesh);
                    scene.Meshes.Add(matchingMesh);

                    // Check material
                    var decorationMaterial = decorationScene.Materials[decorationMesh.MaterialIndex];
                    var matchingMaterial = scene.Materials.FirstOrDefault(x =>
                        x.Name == decorationMaterial.Name &&
                        x.HasColorDiffuse == decorationMaterial.HasColorDiffuse &&
                        x.HasTextureDiffuse == decorationMaterial.HasTextureDiffuse);

                    if (matchingMaterial == null)
                    {
                        matchingMaterial = AssimpHelpers.Clone(decorationMaterial);

                        var textureDiffuse = matchingMaterial.TextureDiffuse;
                        if (!string.IsNullOrWhiteSpace(textureDiffuse.FilePath))
                        {
                            string path = textureDiffuse.FilePath;
                            var matchingTextureIndex = path.LastIndexOf('/');
                            if (matchingTextureIndex != -1) path = path.Substring(matchingTextureIndex + 1);
                            textureDiffuse.FilePath = "Data/Textures/" + path;
                        }
                        matchingMaterial.TextureDiffuse = textureDiffuse;

                        scene.Materials.Add(matchingMaterial);
                    }

                    var matchingMaterialIndex = scene.Materials.IndexOf(matchingMaterial);
                    matchingMesh.MaterialIndex = matchingMaterialIndex;
                }

                var matchingMeshIndex = scene.Meshes.IndexOf(matchingMesh);
                childNode.MeshIndices.Add(matchingMeshIndex);
            }

            foreach (var key in decorationNode.Metadata.Keys)
            {
                childNode.Metadata[key] = decorationNode.Metadata[key];
            }

            return childNode;
        }
    }
}
