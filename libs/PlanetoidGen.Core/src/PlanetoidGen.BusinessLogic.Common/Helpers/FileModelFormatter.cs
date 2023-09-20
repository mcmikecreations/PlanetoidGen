using PlanetoidGen.Contracts.Models.Documents;
using PlanetoidGen.Domain.Models.Documents;
using System;
using System.Linq;

namespace PlanetoidGen.BusinessLogic.Common.Helpers
{
    public static class FileModelFormatter
    {
        public const string StaticDataFolder = "Data";

        /// <summary>
        /// Formats a <see cref="FileContentModel.LocalPath"/> string based on input parameters.
        /// </summary>
        /// <param name="planetoidId">Planetoid Id.</param>
        /// <param name="style">A style of tile (e.g., "Satelite", "Heightmap").</param>
        /// <param name="z">Planar coordinate Z index.</param>
        /// <param name="x">Planar coordinate X index.</param>
        /// <returns>Formatted local path. For example, "Planetoid_2/Satelite/12/20".</returns>
        /// <exception cref="ArgumentException"></exception>
        public static string FormatLocalPath(int planetoidId, string style, short z, long x)
        {
            return string.IsNullOrWhiteSpace(style)
                ? throw new ArgumentException($"'{nameof(style)}' cannot be null or whitespace.", nameof(style))
                : $"Planetoid_{planetoidId}/{style.Trim()}/{z}/{x}";
        }

        /// <summary>
        /// Formats a <see cref="FileContentModel.FileName"/> string based on input parameters.
        /// </summary>
        /// <param name="y">Planar coordinate Y index.</param>
        /// <param name="extension">File extension without leading dot.</param>
        /// <returns>Formatted file name. For example, "20.png".</returns>
        /// <exception cref="ArgumentException"></exception>
        public static string FormatFileName(long y, string extension)
        {
            return string.IsNullOrWhiteSpace(extension) || extension.Any(char.IsWhiteSpace)
                ? throw new ArgumentException($"'{nameof(extension)}' cannot be null or contain whitespace.", nameof(extension))
                : $"{y}.{extension}";
        }

        /// <summary>
        /// Formats a <see cref="FileModel.FileId"/> string based on input parameters.
        /// </summary>
        /// <param name="planetoidId">Planetoid Id.</param>
        /// <param name="style">A style of tile (e.g., "Satelite", "Heightmap").</param>
        /// <param name="z">Planar coordinate Z index.</param>
        /// <param name="x">Planar coordinate X index.</param>
        /// <param name="y">Planar coordinate Y index.</param>
        /// <returns>Formatted file id. For example, "Planetoid_2/com.PlanetoidGen.HeightmapRgbaEncoded/Heightmap/12/20/20".</returns>
        /// <exception cref="ArgumentException"></exception>
        public static string FormatFileId(int planetoidId, string style, short z, long x, long y)
        {
            return string.IsNullOrWhiteSpace(style)
                ? throw new ArgumentException($"'{nameof(style)}' cannot be null or whitespace.", nameof(style))
                : $"Planetoid_{planetoidId}/{style.Trim()}/{z}/{x}/{y}";
        }
    }
}
