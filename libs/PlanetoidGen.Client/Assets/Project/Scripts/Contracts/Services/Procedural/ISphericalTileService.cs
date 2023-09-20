using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Services.Generation;
using UnityEngine;

namespace PlanetoidGen.Client.Contracts.Services.Procedural
{
    public interface ISphericalTileService
    {
        Mesh GenerateTile(CubicCoordinateModel coordinates, double radius, int tesselation);

        Mesh GenerateTile(ICubeProjectionService.FaceSide faceSide, float radius, int tesselation);
    }
}
