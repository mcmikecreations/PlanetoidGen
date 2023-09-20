using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using UnityEngine;

namespace PlanetoidGen.Client.Contracts.Services.Procedural
{
    public interface IPlanarTileService
    {
        /// <summary>
        /// Generate the tile mesh on a plane.
        /// </summary>
        /// <param name="coordinates">Coordinates of the tile to generate.</param>
        /// <param name="from">Geographic reference system to convert from.</param>
        /// <param name="to">Projected reference system to convert to.</param>
        /// <param name="tileScale">Size factor of the tile.</param>
        /// <param name="tesselation">Power of polygon divisions.</param>
        /// <returns>A 3D mesh of the tile.</returns>
        Mesh GenerateTile(CubicCoordinateModel coordinates,
            PlanetoidInfoModel planetoid,
            SpatialReferenceSystemModel from,
            SpatialReferenceSystemModel to,
            int tesselation);
    }
}
