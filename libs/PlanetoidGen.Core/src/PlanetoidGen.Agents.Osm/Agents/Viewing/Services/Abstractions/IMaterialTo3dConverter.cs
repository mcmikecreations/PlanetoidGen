using Assimp;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Services.Abstractions
{
    internal interface IMaterialTo3dConverter
    {
        /// <summary>
        /// Try to get the material based on name, then by texture.
        /// </summary>
        /// <param name="name">Material name to find.</param>
        /// <param name="textureName">Texture path to find mentions of.</param>
        /// <param name="scene">Scene to find the material in.</param>
        /// <returns>A pair of material index and material itself. If missing, returns <c>(-1, null)</c>.</returns>
        (int, Material) GetMaterial(string? name, string? textureName, Scene scene);

        /// <summary>
        /// Get or add the roof material based on name.
        /// </summary>
        /// <param name="matName"></param>
        /// <param name="scene"></param>
        /// <returns>A pair of material index and material itself.</returns>
        (int, Material) GetRoofMaterialIndex(string matName, Scene scene);

        /// <summary>
        /// Get or add the wall material based on name.
        /// </summary>
        /// <param name="matName"></param>
        /// <param name="scene"></param>
        /// <returns>A pair of material index and material itself.</returns>
        (int, Material) GetWallMaterialIndex(string matName, Scene scene);

        /// <summary>
        /// Get or add the highway material based on name.
        /// </summary>
        /// <param name="matName"></param>
        /// <param name="scene"></param>
        /// <returns>A pair of material index and material itself.</returns>
        (int, Material) GetHighwayMaterialIndex(string matName, Scene scene);

        /// <summary>
        /// Get or add the railway material based on name.
        /// </summary>
        /// <param name="matName"></param>
        /// <param name="scene"></param>
        /// <returns>A pair of material index and material itself.</returns>
        (int, Material) GetRailwayMaterialIndex(string matName, Scene scene);
    }
}
