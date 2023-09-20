using Assimp;
using PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Constants.KindValues;
using System.Linq;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Implementations
{
    internal class MaterialTo3dConverter : IMaterialTo3dConverter
    {
        public (int, Material) GetMaterial(string? name, string? textureName, Scene scene)
        {
            if (name != null)
            {
                var material = scene.Materials.Where(x => x.Name == name).FirstOrDefault();
                if (material != null)
                {
                    return (scene.Materials.IndexOf(material), material);
                }
            }

            if (textureName != null)
            {
                var material = scene.Materials.Where(x => x.TextureDiffuse.FilePath == textureName).FirstOrDefault();
                if (material != null)
                {
                    return (scene.Materials.IndexOf(material), material);
                }
            }

            return (-1, null);
        }

        public (int, Material) GetHighwayMaterialIndex(string matName, Scene scene)
        {
            var fullMaterialName = "Mat_Highway_" + matName;

            var (index, material) = GetMaterial(fullMaterialName, null, scene);

            if (index == -1 || material == null)
            {
                index = scene.MaterialCount;
                material = new Material();
                scene.Materials.Add(material);

                switch (matName)
                {
                    case HighwaySurfaceKindValues.HighwaySurfaceConcrete:
                        material.Name = fullMaterialName;
                        material.IsTwoSided = false;
                        material.ShadingMode = ShadingMode.Phong;
                        material.ColorDiffuse = new Color4D(1f, 1f, 1f, 1f);
                        material.TextureDiffuse = new TextureSlot(
                            "Data/Textures/highway_concrete.jpg",
                            TextureType.Diffuse,
                            0, /* texture index */
                            TextureMapping.FromUV,
                            0, /* uv channel index */
                            0, /* blend factor */
                            TextureOperation.Add,
                            TextureWrapMode.Wrap,
                            TextureWrapMode.Wrap,
                            0 /* flags */);
                        break;
                    case HighwaySurfaceKindValues.HighwaySurfaceAsphalt:
                    default:
                        material.Name = fullMaterialName;
                        material.IsTwoSided = false;
                        material.ShadingMode = ShadingMode.Phong;
                        material.ColorDiffuse = new Color4D(1f, 1f, 1f, 1f);
                        material.TextureDiffuse = new TextureSlot(
                            "Data/Textures/highway_asphalt.jpg",
                            TextureType.Diffuse,
                            0, /* texture index */
                            TextureMapping.FromUV,
                            0, /* uv channel index */
                            0, /* blend factor */
                            TextureOperation.Add,
                            TextureWrapMode.Wrap,
                            TextureWrapMode.Wrap,
                            0 /* flags */);
                        break;
                }
            }

            return (index, material);
        }

        public (int, Material) GetRoofMaterialIndex(string matName, Scene scene)
        {
            var fullMaterialName = "Mat_Roof_" + matName;

            var (index, material) = GetMaterial(fullMaterialName, null, scene);

            if (index == -1 || material == null)
            {
                index = scene.MaterialCount;
                material = new Material();
                scene.Materials.Add(material);

                switch (matName)
                {
                    case RoofMaterialKindValues.BuildingRoofMaterialBitumen:
                    default:
                        material.Name = fullMaterialName;
                        material.IsTwoSided = false;
                        material.ShadingMode = ShadingMode.Phong;
                        material.ColorDiffuse = new Color4D(1f, 1f, 1f, 1f);
                        material.TextureDiffuse = new TextureSlot(
                            "Data/Textures/roof_roll_bitumen.jpg",
                            TextureType.Diffuse,
                            0, /* texture index */
                            TextureMapping.FromUV,
                            0, /* uv channel index */
                            0, /* blend factor */
                            TextureOperation.Add,
                            TextureWrapMode.Wrap,
                            TextureWrapMode.Wrap,
                            0 /* flags */);
                        break;
                }
            }

            return (index, material);
        }

        public (int, Material) GetWallMaterialIndex(string matName, Scene scene)
        {
            var fullMaterialName = "Mat_Wall_" + matName;

            var (index, material) = GetMaterial(fullMaterialName, null, scene);

            if (index == -1 || material == null)
            {
                index = scene.MaterialCount;
                material = new Material();
                scene.Materials.Add(material);

                switch (matName)
                {
                    case BuildingMaterialKindValues.BuildingMaterialBrick:
                        material.Name = fullMaterialName;
                        material.IsTwoSided = false;
                        material.ShadingMode = ShadingMode.Phong;
                        material.ColorDiffuse = new Color4D(1f, 1f, 1f, 1f);
                        material.TextureDiffuse = new TextureSlot(
                            "Data/Textures/wall_brick.jpg",
                            TextureType.Diffuse,
                            0, /* texture index */
                            TextureMapping.FromUV,
                            0, /* uv channel index */
                            0, /* blend factor */
                            TextureOperation.Add,
                            TextureWrapMode.Wrap,
                            TextureWrapMode.Wrap,
                            0 /* flags */);
                        break;
                    case BuildingMaterialKindValues.BuildingMaterialPlaster:
                        material.Name = fullMaterialName;
                        material.IsTwoSided = false;
                        material.ShadingMode = ShadingMode.Phong;
                        material.ColorDiffuse = new Color4D(1f, 1f, 1f, 1f);
                        material.TextureDiffuse = new TextureSlot(
                            "Data/Textures/wall_decorative_plaster.jpg",
                            TextureType.Diffuse,
                            0, /* texture index */
                            TextureMapping.FromUV,
                            0, /* uv channel index */
                            0, /* blend factor */
                            TextureOperation.Add,
                            TextureWrapMode.Wrap,
                            TextureWrapMode.Wrap,
                            0 /* flags */);
                        break;
                    case BuildingMaterialKindValues.BuildingMaterialConcrete:
                    default:
                        material.Name = fullMaterialName;
                        material.IsTwoSided = false;
                        material.ShadingMode = ShadingMode.Phong;
                        material.ColorDiffuse = new Color4D(1f, 1f, 1f, 1f);
                        material.TextureDiffuse = new TextureSlot(
                            "Data/Textures/wall_concrete.jpg",
                            TextureType.Diffuse,
                            0, /* texture index */
                            TextureMapping.FromUV,
                            0, /* uv channel index */
                            0, /* blend factor */
                            TextureOperation.Add,
                            TextureWrapMode.Wrap,
                            TextureWrapMode.Wrap,
                            0 /* flags */);
                        break;
                }
            }

            return (index, material);
        }

        public (int, Material) GetRailwayMaterialIndex(string matName, Scene scene)
        {
            var fullMaterialName = "Mat_Railway_" + matName;

            var (index, material) = GetMaterial(fullMaterialName, null, scene);

            if (index == -1 || material == null)
            {
                index = scene.MaterialCount;
                material = new Material();
                scene.Materials.Add(material);

                switch (matName)
                {
                    default:
                        material.Name = fullMaterialName;
                        material.IsTwoSided = false;
                        material.ShadingMode = ShadingMode.Phong;
                        material.ColorDiffuse = new Color4D(1f, 1f, 1f, 1f);
                        material.TextureDiffuse = new TextureSlot(
                            "Data/Textures/railway_gravel.jpg",
                            TextureType.Diffuse,
                            0, /* texture index */
                            TextureMapping.FromUV,
                            0, /* uv channel index */
                            0, /* blend factor */
                            TextureOperation.Add,
                            TextureWrapMode.Wrap,
                            TextureWrapMode.Wrap,
                            0 /* flags */);
                        break;
                }
            }

            return (index, material);
        }
    }
}
