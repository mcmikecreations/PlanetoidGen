using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Collections;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Constants;
using PlanetoidGen.Agents.Osm.Constants.KindValues;
using PlanetoidGen.Agents.Osm.Helpers;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Agents.Standard.Helpers;
using PlanetoidGen.Domain.Models.Descriptions.Building;
using PlanetoidGen.Domain.Models.Info;
using System.Linq;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations.Builders.SurfaceParts
{
    internal class Insulation3dModelBuilder : ISurfacePartTo3dModelBuilder
    {
        private readonly DecorationTo3dConverter _decoratorTo3dConverter;

        public Insulation3dModelBuilder(DecorationTo3dConverter decoratorTo3dConverter)
        {
            _decoratorTo3dConverter = decoratorTo3dConverter;
        }

        public void BuildPart(
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
            int lod)
        {
            if (lod == 0)
            {
                BuildPartLOD0(
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
                    outerRing);
            }
            else
            {
                mesh.Faces.Add(new Face(partRing.Indices.ToArray()));
                AddDecorations(
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
                    outerRing);
            }
        }

        void AddDecorations(
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
            Vector3D[] outerRing)
        {
            var insulationDetails = partModel.Kind.Substring(LevelSidePartKindValues.PartInsulation.Length);
            if (insulationDetails.StartsWith(LevelSidePartKindValues.PartWindow))
            {
                AddWindow(partRing, options, description, scene, buildingNode);
            }
            else if (insulationDetails.StartsWith(LevelSidePartKindValues.PartBalcony))
            {
                AddBalcony(partRing, entity, options, level, sideModel, partModel, description, scene, buildingNode);
            }
            else if (insulationDetails.StartsWith(LevelSidePartKindValues.PartPorch))
            {
                AddPorch(partRing, entity, options, level, sideModel, partModel, description, scene, buildingNode);
            }
        }

        public void BuildPartLOD0(
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
            Vector3D[] outerRing)
        {
            var up = options.YUp ? new Vector3D(0f, 1f, 0f) : new Vector3D(0f, 0f, 1f);
            var right = partRing.Vertices[0] - partRing.Vertices[1];
            right.Normalize();
            var forward = Vector3D.Cross(up, right) * Measurements.InsulationOffset;
            right *= Measurements.InsulationOffset;

            /// p3 --- p4
            /// |       |
            /// p2 --- p1
            var p1 = partRing.Vertices[0];
            var p4 = partRing.Vertices[partRing.Vertices.Count - 1];
            var p2 = options.YUp
                ? partRing.Vertices.Last(x => x.Y == p1.Y)
                : partRing.Vertices.Last(x => x.Z == p1.Z);
            var p3 = options.YUp
                ? partRing.Vertices.First(x => x.Y == p4.Y)
                : partRing.Vertices.First(x => x.Z == p4.Z);

            int sideIndex = level.Sides.IndexOf(sideModel);
            int partIndex = sideModel.Parts.IndexOf(partModel);
            // These checks avoid having an L-shaped cross-section at the edge of a building
            // Since both faces will be extruded independently
            // This will in theory give the insulation a 45-degree bevel
            bool isFirstOffset = partIndex == 0 &&
                level.Sides[MathHelpers.Modulo(sideIndex - 1, level.Sides.Count)].Parts.Last().Kind.StartsWith(LevelSidePartKindValues.PartInsulation);
            bool isLastOffset = partIndex == sideModel.Parts.Count - 1 &&
                level.Sides[(sideIndex + 1) % level.Sides.Count].Parts[0].Kind.StartsWith(LevelSidePartKindValues.PartInsulation);

            var p5 = isLastOffset ? p1 + forward - right : p1 + forward;
            var p6 = isFirstOffset ? p2 + forward + right : p2 + forward;
            var p7 = isFirstOffset ? p3 + forward + right : p3 + forward;
            var p8 = isLastOffset ? p4 + forward - right : p4 + forward;

            var height = (p4 - p1).Length();

            // Front
            var frontStart = mesh.TextureCoordinateChannels[0][partRing.Indices[0]].X;
            var frontEnd = frontStart + (p6 - p5).Length();
            mesh.Vertices.Add(p5);
            mesh.TextureCoordinateChannels[0].Add(new Vector3D(frontEnd, bottomHeight, 0f));
            mesh.Vertices.Add(p6);
            mesh.TextureCoordinateChannels[0].Add(new Vector3D(frontStart, bottomHeight, 0f));
            mesh.Vertices.Add(p7);
            mesh.TextureCoordinateChannels[0].Add(new Vector3D(frontStart, bottomHeight + height, 0f));
            mesh.Vertices.Add(p8);
            mesh.TextureCoordinateChannels[0].Add(new Vector3D(frontEnd, bottomHeight + height, 0f));
            mesh.Faces.Add(new Face(Enumerable.Range(startIndex, 4).ToArray()));
            startIndex += 4;

            // Right p1p4 side
            var rightLength = (p1 - p5).Length();
            mesh.Vertices.Add(p1);
            mesh.TextureCoordinateChannels[0].Add(new Vector3D(frontEnd + rightLength, bottomHeight, 0f));
            mesh.Vertices.Add(p5);
            mesh.TextureCoordinateChannels[0].Add(new Vector3D(frontEnd, bottomHeight, 0f));
            mesh.Vertices.Add(p8);
            mesh.TextureCoordinateChannels[0].Add(new Vector3D(frontEnd, bottomHeight + height, 0f));
            mesh.Vertices.Add(p4);
            mesh.TextureCoordinateChannels[0].Add(new Vector3D(frontEnd + rightLength, bottomHeight + height, 0f));
            mesh.Faces.Add(new Face(Enumerable.Range(startIndex, 4).ToArray()));
            startIndex += 4;

            // Left p2p3 side
            var leftLength = (p2 - p6).Length();
            mesh.Vertices.Add(p6);
            mesh.TextureCoordinateChannels[0].Add(new Vector3D(frontStart, bottomHeight, 0f));
            mesh.Vertices.Add(p2);
            mesh.TextureCoordinateChannels[0].Add(new Vector3D(frontStart + leftLength, bottomHeight, 0f));
            mesh.Vertices.Add(p3);
            mesh.TextureCoordinateChannels[0].Add(new Vector3D(frontStart + leftLength, bottomHeight + height, 0f));
            mesh.Vertices.Add(p7);
            mesh.TextureCoordinateChannels[0].Add(new Vector3D(frontStart, bottomHeight + height, 0f));
            mesh.Faces.Add(new Face(Enumerable.Range(startIndex, 4).ToArray()));
            startIndex += 4;

            // Bottom and top
            // p1-p2-p6-p5
            // p8-p7-p3-p4
            if (options.YUp)
            {
                mesh.Vertices.Add(p1);
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                mesh.Vertices.Add(p2);
                mesh.TextureCoordinateChannels[0].Add(AssimpHelpers.SwapYZInPlace(p2 - p1));
                mesh.Vertices.Add(p6);
                mesh.TextureCoordinateChannels[0].Add(AssimpHelpers.SwapYZInPlace(p6 - p1));
                mesh.Vertices.Add(p5);
                mesh.TextureCoordinateChannels[0].Add(AssimpHelpers.SwapYZInPlace(p5 - p1));

                mesh.Vertices.Add(p8);
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                mesh.Vertices.Add(p7);
                mesh.TextureCoordinateChannels[0].Add(AssimpHelpers.SwapYZInPlace(p7 - p8));
                mesh.Vertices.Add(p3);
                mesh.TextureCoordinateChannels[0].Add(AssimpHelpers.SwapYZInPlace(p3 - p8));
                mesh.Vertices.Add(p4);
                mesh.TextureCoordinateChannels[0].Add(AssimpHelpers.SwapYZInPlace(p4 - p8));
            }
            else
            {
                mesh.Vertices.Add(p1);
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                mesh.Vertices.Add(p2);
                mesh.TextureCoordinateChannels[0].Add(p2 - p1);
                mesh.Vertices.Add(p6);
                mesh.TextureCoordinateChannels[0].Add(p6 - p1);
                mesh.Vertices.Add(p5);
                mesh.TextureCoordinateChannels[0].Add(p5 - p1);

                mesh.Vertices.Add(p8);
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(0f, 0f, 0f));
                mesh.Vertices.Add(p7);
                mesh.TextureCoordinateChannels[0].Add(p7 - p8);
                mesh.Vertices.Add(p3);
                mesh.TextureCoordinateChannels[0].Add(p3 - p8);
                mesh.Vertices.Add(p4);
                mesh.TextureCoordinateChannels[0].Add(p4 - p8);
            }

            mesh.Faces.Add(new Face(Enumerable.Range(startIndex, 4).ToArray()));
            mesh.Faces.Add(new Face(Enumerable.Range(startIndex + 4, 4).ToArray()));
            startIndex += 8;

            AddDecorations(
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
                outerRing);
        }

        private void AddWindow(
            VertexRing partRing,
            ConvertTo3dModelAgentSettings options,
            BuildingModel description,
            Scene scene,
            Node buildingNode)
        {
            var decorationNode = _decoratorTo3dConverter.AddDecoration(
                "windows.obj", WindowKindValues.PickWindowType(description), scene, buildingNode);

            if (decorationNode == null)
            {
                return;
            }

            var decorationPosition = AssimpHelpers.VerticalPartRingBottomCenter(partRing, options.YUp);
            var up = options.YUp ? new Vector3D(0f, 1f, 0f) : new Vector3D(0f, 0f, 1f);
            var right = partRing.Vertices[0] - partRing.Vertices[1];
            right.Normalize();
            var forward = Vector3D.Cross(up, right);

            if (options.YUp)
            {
                decorationPosition += new Vector3D(0f, Measurements.WindowSillHeight, 0f) + forward * (Measurements.DecorationForwardOffset + Measurements.InsulationOffset);
            }
            else
            {
                decorationPosition += new Vector3D(0f, 0f, Measurements.WindowSillHeight) + forward * (Measurements.DecorationForwardOffset + Measurements.InsulationOffset);
            }

            var angle = AssimpHelpers.AngleBetweenVectors(new Vector3D(1f, 0f, 0f), right, up);

            decorationNode.Transform = Matrix4x4.FromAngleAxis(angle, up) * Matrix4x4.FromTranslation(decorationPosition);
        }

        private void AddBalcony(
            VertexRing partRing,
            BuildingEntity entity,
            ConvertTo3dModelAgentSettings options,
            LevelModel level,
            SurfaceSideModel sideModel,
            SurfacePartModel partModel,
            BuildingModel description,
            Scene scene,
            Node buildingNode)
        {
            var levelIndex = description.LevelCollection.IndexOf(level);
            var sideIndex = level.Sides.IndexOf(sideModel);
            var partIndex = sideModel.Parts.IndexOf(partModel);
            var partTotalIndex = description.LevelCollection.Take(levelIndex).Sum(x => x.Sides.Sum(y => y.Parts.Count)) +
                level.Sides.Take(sideIndex).Sum(x => x.Parts.Count) +
                partIndex;
            var decorationNode = _decoratorTo3dConverter.AddDecoration(
                "balconies.obj", BalconyKindValues.PickBalconyType(description, description.LevelCollection.IndexOf(level), (int)(entity.GID + partTotalIndex)), scene, buildingNode);

            if (decorationNode == null)
            {
                return;
            }

            var decorationPosition = AssimpHelpers.VerticalPartRingBottomCenter(partRing, options.YUp);
            var up = options.YUp ? new Vector3D(0f, 1f, 0f) : new Vector3D(0f, 0f, 1f);
            var right = partRing.Vertices[1] - partRing.Vertices[0];
            right.Normalize();
            var forward = Vector3D.Cross(right, up);

            decorationPosition += forward * Measurements.InsulationOffset;

            var angle = AssimpHelpers.AngleBetweenVectors(new Vector3D(1f, 0f, 0f), right, up);

            decorationNode.Transform = Matrix4x4.FromAngleAxis(angle, up) * Matrix4x4.FromTranslation(decorationPosition);
        }

        private void AddPorch(
            VertexRing partRing,
            BuildingEntity entity,
            ConvertTo3dModelAgentSettings options,
            LevelModel level,
            SurfaceSideModel sideModel,
            SurfacePartModel partModel,
            BuildingModel description,
            Scene scene,
            Node buildingNode)
        {
            var levelIndex = description.LevelCollection.IndexOf(level);
            var sideIndex = level.Sides.IndexOf(sideModel);
            var partIndex = sideModel.Parts.IndexOf(partModel);
            var partTotalIndex = description.LevelCollection.Take(levelIndex).Sum(x => x.Sides.Sum(y => y.Parts.Count)) +
                level.Sides.Take(sideIndex).Sum(x => x.Parts.Count) +
                partIndex;
            var decorationNode = _decoratorTo3dConverter.AddDecoration(
                "porches.obj", PorchKindValues.PickPorchType(description, (int)(entity.GID + partTotalIndex)), scene, buildingNode);

            if (decorationNode == null)
            {
                return;
            }

            var decorationPosition = AssimpHelpers.VerticalPartRingBottomCenter(partRing, options.YUp);
            var up = options.YUp ? new Vector3D(0f, 1f, 0f) : new Vector3D(0f, 0f, 1f);
            var right = partRing.Vertices[1] - partRing.Vertices[0];
            right.Normalize();
            var forward = Vector3D.Cross(right, up);

            decorationPosition += forward * Measurements.InsulationOffset;

            var angle = AssimpHelpers.AngleBetweenVectors(new Vector3D(1f, 0f, 0f), right, up);

            decorationNode.Transform = Matrix4x4.FromAngleAxis(angle, up) * Matrix4x4.FromTranslation(decorationPosition);
        }

        public int GetSupportedLODCount()
        {
            return 2;
        }
    }
}
