using PlanetoidGen.Client.Contracts.Services.Loaders;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace PlanetoidGen.Client.BusinessLogic.Services.Loaders
{
    public class TextureLoadingService : ITextureLoadingService
    {
        public Texture2D Load(string filePath)
        {
            var tex = new Texture2D(0, 0);

            tex.LoadImage(File.ReadAllBytes(filePath));

            return tex;
        }

        public async Task<Texture2D> LoadAsync(string filePath)
        {
            var tex = new Texture2D(0, 0);

            tex.LoadImage(await File.ReadAllBytesAsync(filePath));

            return tex;
        }

        public Texture2D Load(byte[] bytes)
        {
            var tex = new Texture2D(0, 0);

            tex.LoadImage(bytes);

            return tex;
        }
    }
}
