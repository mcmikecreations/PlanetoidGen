using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Dtos;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Generic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Services.Abstractions
{
    public interface IOverpassApiService
    {
        /// <summary>
        /// WGS84 geographic CRS.
        /// </summary>
        const int WGS84Srid = 4326;

        /// <summary>
        /// Google Maps projected CRS.
        /// </summary>
        const int WebMercatorSrid = 3857;

        string HighwayKeyword { get; }

        string RailwayKeyword { get; }

        string BuildingKeyword { get; }

        Task<Result<OverpassResponseDto>> GetBuildings(BoundingBoxDto bbox, CancellationToken token);

        Task<Result<OverpassResponseDto>> GetHighways(BoundingBoxDto bbox, CancellationToken token);

        Task<Result<OverpassResponseDto>> GetRailways(BoundingBoxDto bbox, CancellationToken token);

        IReadOnlyList<HighwayEntity> ToHighwayEntityList(OverpassResponseDto overpassResponse, int? srid = null);

        IReadOnlyList<RailwayEntity> ToRailwayEntityList(OverpassResponseDto overpassResponse, int? srid = null);

        IReadOnlyList<BuildingEntity> ToBuildingEntityList(OverpassResponseDto overpassResponse, int? srid = null);

        Task<Result<OverpassResponseDto>> SendRequest(string request, CancellationToken token);

        BoundingBoxDto GetBoundingBox(CoordinateDto coordinate, int zoom);

        /// <summary>
        /// Map PlanetoidGen bounding box to Overpass bounding box.
        /// </summary>
        BoundingBoxDto GetBoundingBox(AxisAlignedBoundingBoxCoordinateModel model);

        /// <param name="lat">Latitude in degrees.</param>
        /// <param name="lon">Longitude in degrees.</param>
        /// <param name="zoom">Zoom value.</param>
        /// <returns>Pair of tile X, tile Y indices.</returns>
        (int, int) LonLat2Tile(double lon, double lat, int zoom);

        /// <returns>Pair of Lon, Lat of NW point of tile in degrees.</returns>
        (double, double) Tile2LonLat(int xtile, int ytile, int zoom);

        /// <returns>Pair of West Lon, South Lat, East Lon, North Lat in degrees..</returns>
        (double, double, double, double) LonLat2BBox(double lon, double lat, int zoom);

        /// <summary>
        /// Map PlanetoidGen <seealso cref="SphericalCoordinateModel"/> zoom to Overpass zoom.
        /// Final zoom is calculated as follows:
        /// <code>Min(zoom + increment, MaxShortInt)</code>
        /// </summary>
        short GetZoom(short zoom, int increment);
    }
}
