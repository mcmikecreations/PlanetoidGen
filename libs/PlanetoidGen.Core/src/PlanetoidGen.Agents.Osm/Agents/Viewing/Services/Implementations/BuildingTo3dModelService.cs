using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Collections;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations.Builders.Roofs;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations.Builders.SurfaceParts;
using PlanetoidGen.Agents.Osm.Constants.KindValues;
using PlanetoidGen.Agents.Osm.Helpers;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Domain.Models.Descriptions.Building;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations
{
    public class BuildingTo3dModelService : IBuildingTo3dModelService
    {
        private readonly IMaterialTo3dConverter _materialTo3DConverter;
        private readonly DecorationTo3dConverter _decorationTo3DConverter;
        private readonly Dictionary<string, ISurfacePartTo3dModelBuilder> _partBuilders;
        private readonly Dictionary<string, IRoofTo3dModelBuilder> _roofBuilders;

        public BuildingTo3dModelService()
        {
            _materialTo3DConverter = new MaterialTo3dConverter();
            _decorationTo3DConverter = new DecorationTo3dConverter();
            _partBuilders = new Dictionary<string, ISurfacePartTo3dModelBuilder>()
            {
                { LevelSidePartKindValues.PartWall, new Wall3dModelBuilder() },
                { LevelSidePartKindValues.PartWindow, new Window3dModelBuilder(_decorationTo3DConverter) },
                { LevelSidePartKindValues.PartBalcony, new Balcony3dModelBuilder(_decorationTo3DConverter) },
                { LevelSidePartKindValues.PartPorch, new Porch3dModelBuilder(_decorationTo3DConverter) },
                { LevelSidePartKindValues.PartInsulation, new Insulation3dModelBuilder(_decorationTo3DConverter) },
            };
            _roofBuilders = new Dictionary<string, IRoofTo3dModelBuilder>()
            {
                { RoofKindValues.RoofFlat, new FlatRoof3dModelBuilder(_materialTo3DConverter) },
                { RoofKindValues.RoofSkillion, new SkillionRoof3dModelBuilder(_materialTo3DConverter) },
                { RoofKindValues.RoofGabled, new GabledRoof3dModelBuilder(_materialTo3DConverter) },
                { RoofKindValues.RoofHipped, new HippedRoof3dModelBuilder(_materialTo3DConverter) },
            };
        }

        public void ProcessEntity(
            BuildingEntity entity,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            IList<BuildingModel> descriptions,
            IList<Vector3D[]> outerRings,
            Vector3D pivot,
            Scene scene,
            Node parent,
            int z)
        {
            if (descriptions == null)
            {
                throw new ArgumentNullException(nameof(descriptions));
            }
            if (outerRings == null)
            {
                throw new ArgumentNullException(nameof(outerRings));
            }
            if (planetoid == null)
            {
                throw new ArgumentNullException(nameof(planetoid));
            }
            if (descriptions.Count != outerRings.Count)
            {
                throw new ArgumentException($"{nameof(descriptions)} count {descriptions.Count} doesn't match {outerRings} count {outerRings.Count}.");
            }

            var up = options.YUp ? new Vector3D(0f, 1f, 0f) : new Vector3D(0f, 0f, 1f);

            for (var i = 0; i < descriptions.Count; ++i)
            {
                var description = descriptions[i];
                var ring = outerRings[i];
                var ringFirst = ring.First();

                var lodCount = GetSupportedLODCount(description);
                var lod = GetLODFromZ(z, planetoid.Radius, lodCount, options);

                ring = GetOuterRing(ring, options.YUp, up);

                var node = new Node($"Building_{entity.GID}_{i}", parent);
                node.Transform = Matrix4x4.FromTranslation(ringFirst - pivot);

                scene.Meshes.Add(BuildBuilding(entity, options, planetoid, i, description, scene, node, ring, lod, GetSupportedLODCount(description)));

                node.MeshIndices.Add(scene.MeshCount - 1);

                parent.Children.Add(node);
            }
        }

        private Vector3D[] GetOuterRing(Vector3D[] initialRing, bool isYUp, Vector3D up)
        {
            IEnumerable<Vector3D> ring = initialRing;
            var ringFirst = initialRing[0];

            if (initialRing[initialRing.Length - 1] == ringFirst)
            {
                ring = ring.Take(initialRing.Length - 1).Select(x => x - ringFirst);
            }
            else
            {
                ring = ring.Select(x => x - ringFirst);
            }

            // https://stackoverflow.com/a/1165943
            var ringList = ring.ToList();
            var sum = 0f;

            if (isYUp)
            {
                for (var i = 0; i < ringList.Count; ++i)
                {
                    var a = ringList[i];
                    var b = ringList[(i + 1) % ringList.Count];
                    sum += (b.X - a.X) * (b.Z + a.Z);
                }
            }
            else
            {
                for (var i = 0; i < ringList.Count; ++i)
                {
                    var a = ringList[i];
                    var b = ringList[(i + 1) % ringList.Count];
                    sum += (b.X - a.X) * (b.Y + a.Y);
                }
            }

            if (sum > 0f)
            {
                ring = ring.Reverse();
            }

            return ring.ToArray();
        }

        public Scene ProcessEntity(
            BuildingEntity entity,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            IList<BuildingModel> descriptions,
            IList<Vector3D[]> outerRings,
            Vector3D pivot,
            int z)
        {
            var scene = new Scene();

            var rootNode = new Node("Root", null);

            scene.RootNode = rootNode;

            ProcessEntity(entity, options, planetoid, descriptions, outerRings, pivot, scene, rootNode, z);

            return scene;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="planetoid"></param>
        /// <param name="description"></param>
        /// <param name="outerRing">Outer ring of the building shape, last != first.</param>
        /// <returns></returns>
        private Mesh BuildBuilding(
            BuildingEntity entity,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            int descriptionIndex,
            BuildingModel description,
            Scene scene,
            Node buildingNode,
            Vector3D[] outerRing,
            int lod,
            int globalLODCount)
        {
            var totalHeight = (float)description.Height;
            // mesh.PrimitiveType
            var mesh = new Mesh($"Mesh_Building_{entity.GID}_{descriptionIndex}", PrimitiveType.Polygon);

            // mesh.BiTangents
            // mesh.BoneCount
            // mesh.Bones
            // mesh.BoundingBox
            // mesh.MeshAnimationAttachmentCount
            // mesh.MeshAnimationAttachments
            // mesh.MorphMethod
            // mesh.Tangents
            // mesh.VertexColorChannelCount
            // mesh.VertexColorChannels

            //mesh.Name
            //mesh.TextureCoordinateChannelCount
            //mesh.TextureCoordinateChannels
            //mesh.UVComponentCount

            //mesh.VertexCount
            //mesh.Vertices
            //mesh.FaceCount;
            //mesh.Faces

            var startIndex = 0;

            var bottomHeight = (float)((description.Height! - (description.Roof?.Height ?? 0.0)) / (description.Levels! - description.MinLevel!) * description.MinLevel!);
            LevelRing bottomRing;

            // Floor
            BuildFoundation(ref startIndex, options, description, mesh, outerRing);

            // Redo the bottom ring to have separate UVs.
            {
                bottomRing = BuildRing(description.LevelCollection[0], description.LevelCollection[0], startIndex, outerRing);

                BuildLevelUV(bottomRing, bottomHeight, mesh);

                IEnumerable<Vector3D> vertices;
                if (options.YUp)
                {
                    vertices = bottomRing.Sides.SelectMany(x => x.Vertices, (ring, c) => new Vector3D(c.X, bottomHeight, c.Z));
                }
                else
                {
                    vertices = bottomRing.Sides.SelectMany(x => x.Vertices, (ring, c) => new Vector3D(c.X, c.Y, bottomHeight));
                }

                mesh.Vertices.AddRange(vertices);
                startIndex += vertices.Count();
            }

            for (var i = 0; i < description.LevelCollection.Count; ++i)
            {
                var topRing = BuildRing(
                    description.LevelCollection[i],
                    description.LevelCollection[Math.Min(description.LevelCollection.Count - 1, i + 1)],
                    startIndex,
                    outerRing);

                PrepareLevel(topRing, ref startIndex, bottomHeight, description.LevelCollection[i], options, mesh);

                BuildLevel(entity,
                    bottomRing, topRing,
                    ref startIndex, bottomHeight,
                    description.LevelCollection[i],
                    options, planetoid,
                    description,
                    scene, buildingNode,
                    mesh, outerRing,
                    lod, globalLODCount);

                bottomRing = topRing;
                bottomHeight += (float)description.LevelCollection[i].Height!;
            }

            BuildRoof(
                new VertexRing() { Vertices = outerRing.ToList(), },
                ref startIndex,
                bottomHeight,
                description.LevelCollection.Last(),
                options, planetoid,
                description,
                scene, buildingNode,
                mesh, outerRing,
                lod, globalLODCount);

            mesh.UVComponentCount[0] = 2;

            //mesh.Normals
            //mesh.Normals.AddRange(outerRing.Take(3).Select(_ => new Vector3D(0f, 0f, 1f)));

            //mesh.MaterialIndex
            var (matInd, matObj) = _materialTo3DConverter.GetWallMaterialIndex(description.Material, scene);
            mesh.MaterialIndex = matInd;

            return mesh;
        }

        private void BuildFoundation(ref int startIndex, ConvertTo3dModelAgentSettings options, BuildingModel description, Mesh mesh, Vector3D[] outerRing)
        {
            IList<Vector3D> vertices;
            var bottomHeight = (float)((description.Height! - (description.Roof?.Height ?? 0.0)) / (description.Levels! - description.MinLevel!) * description.MinLevel!);

            if (options.YUp)
            {
                vertices = outerRing.Select(x => new Vector3D(x.X, bottomHeight, x.Z)).ToList();
            }
            else
            {
                vertices = outerRing.Select(x => new Vector3D(x.X, x.Y, bottomHeight)).ToList();
            }

            if (options.FoundationHeight.HasValue)
            {
                var foundation = -(float)options.FoundationHeight!;
                var down = options.YUp ? new Vector3D(0f, foundation, 0f) : new Vector3D(0f, 0f, foundation);
                var downVertices = vertices.Select(x => x + down).ToList();

                // Walls
                for (var i = 0; i < outerRing.Length; ++i)
                {
                    var start = vertices[i];
                    var end = vertices[(i + 1) % outerRing.Length];
                    var length = AssimpHelpers.Length(start, end);

                    mesh.Vertices.Add(downVertices[i]);
                    mesh.Vertices.Add(start);
                    mesh.Vertices.Add(end);
                    mesh.Vertices.Add(downVertices[(i + 1) % outerRing.Length]);

                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, foundation, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(length, 0f, 0f));
                    mesh.TextureCoordinateChannels[0].Add(new Vector3D(length, foundation, 0f));

                    mesh.Faces.Add(new Face(new int[] { startIndex, startIndex + 1, startIndex + 2, startIndex + 3, }));
                    startIndex += 4;
                }

                // Bottom
                mesh.Vertices.AddRange(downVertices);
            }
            else
            {
                // Bottom
                mesh.Vertices.AddRange(vertices);
            }

            mesh.Faces.Add(new Face(Enumerable.Range(startIndex, outerRing.Length).ToArray()));

            if (options.YUp)
            {
                mesh.TextureCoordinateChannels[0].AddRange(outerRing.Select(x => new Vector3D(x.X, x.Z, 0f)));
            }
            else
            {
                mesh.TextureCoordinateChannels[0].AddRange(outerRing.Select(x => new Vector3D(x.X, x.Y, 0f)));
            }

            startIndex += outerRing.Length;
        }

        private void BuildRoof(
            VertexRing bottomRing,
            ref int startIndex,
            float bottomHeight,
            LevelModel topLevel,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            BuildingModel description,
            Scene scene,
            Node buildingNode,
            Mesh mesh,
            Vector3D[] outerRing,
            int lod,
            int globalLODCount)
        {
            var roofBuilder = GetRoofBuilder(description.Roof);

            if (roofBuilder != null)
            {
                var localCount = roofBuilder.GetSupportedLODCount();

                roofBuilder.BuildRoof(
                    bottomRing,
                    ref startIndex,
                    bottomHeight,
                    topLevel,
                    options,
                    planetoid,
                    description!,
                    scene,
                    buildingNode,
                    mesh,
                    outerRing,
                    MapLOD(lod, localCount, globalLODCount));
            }
        }

        private void PrepareLevel(
            LevelRing topRing,
            ref int startIndex,
            float bottomHeight,
            LevelModel level,
            ConvertTo3dModelAgentSettings options,
            Mesh mesh)
        {
            float topHeight = bottomHeight + (float)level.Height!;

            IEnumerable<Vector3D> vertices;
            if (options.YUp)
            {
                vertices = topRing.Sides.SelectMany(x => x.Vertices, (ring, c) => new Vector3D(c.X, topHeight, c.Z));
            }
            else
            {
                vertices = topRing.Sides.SelectMany(x => x.Vertices, (ring, c) => new Vector3D(c.X, c.Y, topHeight));
            }

            mesh.Vertices.AddRange(vertices);
            startIndex += topRing.Sides.Select(x => x.Vertices.Count).Sum();

            BuildLevelUV(topRing, topHeight, mesh);
        }

        private void BuildLevel(
            BuildingEntity entity,
            LevelRing bottomRing,
            LevelRing topRing,
            ref int startIndex,
            float bottomHeight,
            LevelModel level,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            BuildingModel description,
            Scene scene,
            Node buildingNode,
            Mesh mesh,
            Vector3D[] outerRing,
            int lod,
            int globalLODCount)
        {
            /// Split top and bottom ring based on the side parts.
            /// Don't forget to reorder the top pair of points.
            /// Process each part separately, only add newly added vertices to the mesh.
            /// Only add top ring vertices to the mesh.
            float topHeight = bottomHeight + (float)level.Height!;
            var vectorComp = new AssimpHelpers.Vector3DEqualityComparer();

            for (var i = 0; i < level.Sides.Count; ++i)
            {
                /// Find start and end vertices for each part on bottom and top ring.
                /// Collect all other vertices between them.
                var side = level.Sides[i];
                var outerRingStart = outerRing[i];
                var outerRingEnd = outerRing[(i + 1) % outerRing.Length];

                var width = 0.0;
                int bottomSideOffset = 0, topSideOffset = 0;
                for (var j = 0; j < side.Parts.Count; ++j)
                {
                    width += side.Parts[j].Width;

                    /// Add all bottom vertices to the partRing.
                    var partRing = new VertexRing()
                    {
                        Vertices = new List<Vector3D>(),
                        Indices = new List<int>(),
                    };

                    var sourceVertices = bottomRing.Sides[i].Vertices;
                    var sourceIndices = bottomRing.Sides[i].Indices;
                    var destVertices = new List<Vector3D>();
                    var destIndices = new List<int>();

                    var newVertex = AssimpHelpers.Lerp(outerRingStart, outerRingEnd, (float)(width / side.Width));
                    Vector3D processedVertex;
                    int processedIndex;

                    do
                    {
                        /// May need to take first point from next side.
                        processedVertex = sourceVertices[bottomSideOffset];
                        processedIndex = sourceIndices[bottomSideOffset];

                        destIndices.Add(processedIndex);

                        if (options.YUp)
                        {
                            destVertices.Add(new Vector3D(processedVertex.X, bottomHeight, processedVertex.Z));
                        }
                        else
                        {
                            destVertices.Add(new Vector3D(processedVertex.X, processedVertex.Y, bottomHeight));
                        }

                        ++bottomSideOffset;
                    }
                    while (!vectorComp.Equals(newVertex, processedVertex));

                    partRing.Vertices.AddRange(destVertices.Reverse<Vector3D>());
                    partRing.Indices.AddRange(destIndices.Reverse<int>());

                    /// Add all top vertices to the partRing.
                    sourceVertices = topRing.Sides[i].Vertices;
                    sourceIndices = topRing.Sides[i].Indices;
                    destVertices = new List<Vector3D>();
                    destIndices = new List<int>();

                    do
                    {
                        /// May need to take first point from next side.
                        processedVertex = sourceVertices[topSideOffset];
                        processedIndex = sourceIndices[topSideOffset];

                        destIndices.Add(processedIndex);


                        if (options.YUp)
                        {
                            destVertices.Add(new Vector3D(processedVertex.X, topHeight, processedVertex.Z));
                        }
                        else
                        {
                            destVertices.Add(new Vector3D(processedVertex.X, processedVertex.Y, topHeight));
                        }

                        ++topSideOffset;
                    }
                    while (!vectorComp.Equals(newVertex, processedVertex));

                    /// To have the top in the reversed order.
                    partRing.Vertices.AddRange(destVertices);
                    partRing.Indices.AddRange(destIndices);

                    /// To include these vertices in the next part.
                    --bottomSideOffset;
                    --topSideOffset;

                    BuildPart(
                        entity,
                        partRing,
                        ref startIndex,
                        bottomHeight,
                        side,
                        side.Parts[j],
                        level,
                        options,
                        planetoid,
                        description,
                        scene,
                        buildingNode,
                        mesh,
                        outerRing,
                        lod,
                        globalLODCount);
                }
            }
        }

        private void BuildLevelUV(
            LevelRing ring,
            float height,
            Mesh mesh)
        {
            var uvs = new List<Vector3D>();

            for (var i = 0; i < ring.Sides.Count; ++i)
            {
                var width = 0.0;
                var side = ring.Sides[i];

                uvs.Add(new Vector3D(0f, height, 0.0f));
                var vert = side.Vertices[0];

                for (var j = 1; j < side.Vertices.Count; ++j)
                {
                    var newVert = side.Vertices[j];
                    width += (newVert - vert).Length();

                    uvs.Add(new Vector3D((float)width, height, 0.0f));
                    vert = newVert;
                }
            }

            mesh.TextureCoordinateChannels[0].AddRange(uvs);
        }

        private void BuildPart(
            BuildingEntity entity,
            VertexRing partRing,
            ref int startIndex,
            float bottomHeight,
            SurfaceSideModel sideModel,
            SurfacePartModel partModel,
            LevelModel level,
            ConvertTo3dModelAgentSettings options,
            PlanetoidInfoModel planetoid,
            BuildingModel description,
            Scene scene,
            Node buildingNode,
            Mesh mesh,
            Vector3D[] outerRing,
            int lod,
            int globalLODCount)
        {
            var partBuilder = GetPartBuilder(partModel);

            if (partBuilder != null)
            {
                var localLODCount = partBuilder.GetSupportedLODCount();

                partBuilder.BuildPart(
                    entity,
                    partRing,
                    ref startIndex,
                    bottomHeight,
                    sideModel,
                    partModel,
                    level,
                    options,
                    planetoid,
                    description,
                    scene,
                    buildingNode,
                    mesh,
                    outerRing,
                    MapLOD(lod, localLODCount, globalLODCount));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bottom"></param>
        /// <param name="top"></param>
        /// <param name="outerRing"></param>
        /// <returns>
        /// A 0 meter high "level vertex ring" for the connection between two levels.
        /// Each side contains parts from both levels, including the last vertex,
        /// which is the a duplicate of the first for the next side.
        /// </returns>
        private LevelRing BuildRing(LevelModel bottom, LevelModel top, int startIndex, Vector3D[] outerRing)
        {
            if (bottom == null) bottom = top;
            if (top == null) top = bottom;

            var comparer = new AssimpHelpers.Vector3DEqualityComparer();

            var currentStartIndex = startIndex;

            var result = new LevelRing()
            {
                Sides = new List<VertexRing>(),
            };

            for (var i = 0; i < bottom.Sides.Count; ++i)
            {
                var botParts = bottom.Sides[i];
                var topParts = top.Sides[i];

                var totalParts = new List<(double, Vector3D)>();

                // Process bottom parts
                totalParts.Add((0.0, outerRing[i]));
                for (var j = 0; j < botParts.Parts.Count - 1; ++j)
                {
                    var width = totalParts.Last().Item1 + botParts.Parts[j].Width;
                    totalParts.Add((width, AssimpHelpers.Lerp(outerRing[i], outerRing[(i + 1) % outerRing.Length], (float)(width / botParts.Width))));
                }

                // Process top parts
                totalParts.Add((0.0, outerRing[i]));
                for (var j = 0; j < topParts.Parts.Count - 1; ++j)
                {
                    var width = totalParts.Last().Item1 + topParts.Parts[j].Width;
                    totalParts.Add((width, AssimpHelpers.Lerp(outerRing[i], outerRing[(i + 1) % outerRing.Length], (float)(width / topParts.Width))));
                }

                totalParts.Add((botParts.Width, outerRing[(i + 1) % outerRing.Length]));

                var vertices = totalParts.OrderBy(x => x.Item1).Select(x => x.Item2).Distinct(comparer).ToList();
                result.Sides.Add(new VertexRing()
                {
                    Vertices = vertices,
                    Indices = Enumerable.Range(currentStartIndex, vertices.Count).ToList(),
                });
                currentStartIndex += vertices.Count;
            }

            //startIndex = currentStartIndex;
            return result;
        }

        private IRoofTo3dModelBuilder? GetRoofBuilder(RoofModel roofModel)
        {
            var roofType = roofModel?.Shape?.Trim()?.ToLowerInvariant() ?? string.Empty;

            var key = _roofBuilders.Keys.FirstOrDefault(x => roofType.StartsWith(x));

            if (key != null)
            {
                return _roofBuilders[key];
            }
            else
            {
                return _roofBuilders[RoofKindValues.RoofFlat];
            }
        }

        private ISurfacePartTo3dModelBuilder? GetPartBuilder(SurfacePartModel partModel)
        {
            var partType = partModel.Kind?.Trim()?.ToLowerInvariant() ?? string.Empty;

            var key = _partBuilders.Keys.FirstOrDefault(x => partType.StartsWith(x));

            if (key != null)
            {
                return _partBuilders[key];
            }
            else
            {
                return null;
            }
        }

        public int GetSupportedLODCount(BuildingModel description)
        {
            var roofBuilder = GetRoofBuilder(description.Roof);
            var roofLODCount = roofBuilder?.GetSupportedLODCount() ?? 0;
            int partLODCount = description.LevelCollection
                .SelectMany(level => level.Sides, (level, side) => side.Parts)
                .SelectMany(parts => parts, (parts, part) => GetPartBuilder(part))
                .Distinct()
                .Select(b => b?.GetSupportedLODCount() ?? 0)
                .DefaultIfEmpty(1)
                .Max();
            return Math.Max(roofLODCount, partLODCount);
        }

        private static int MapLOD(int lod, int localCount, int globalCount)
        {
            if (lod == 0) return 0;
            else if (localCount == 1) return 0;

            float maxLocal = localCount - 1;
            float maxGlobal = globalCount - 1;

            return (int)MathF.Ceiling(lod / maxGlobal * maxLocal);
        }

        private static int GetLODFromZ(int z, double planetoidRadius, int lodCount, ConvertTo3dModelAgentSettings options)
        {
            var lodSize = planetoidRadius / (1 << z);

            if (lodSize < options.BestLODSize)
            {
                return 0;
            }
            else if (lodSize >= options.WorstLODSize)
            {
                return lodCount - 1;
            }
            else
            {
                var factor = (lodSize - options.BestLODSize) / (options.WorstLODSize - options.BestLODSize);
                return (int)Math.Round(factor * (lodCount - 1));
            }
        }
    }
}
