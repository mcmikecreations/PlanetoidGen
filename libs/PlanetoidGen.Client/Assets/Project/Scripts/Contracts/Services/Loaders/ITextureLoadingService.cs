using System.Threading.Tasks;
using UnityEngine;

namespace PlanetoidGen.Client.Contracts.Services.Loaders
{
    public interface ITextureLoadingService
    {
        Texture2D Load(string filePath);

        Texture2D Load(byte[] bytes);

        Task<Texture2D> LoadAsync(string filePath);
    }
}
