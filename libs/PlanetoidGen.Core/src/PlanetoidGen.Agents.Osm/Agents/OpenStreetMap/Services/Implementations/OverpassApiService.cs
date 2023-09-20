using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Dtos;
using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Services.Abstractions;
using PlanetoidGen.Agents.Osm.Models.Entities;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Services.GeoInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Services.Implementations
{
    public class OverpassApiService : IOverpassApiService
    {
        private delegate double TransformLatitudeDelegate(double latitude);

        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private readonly string _baseUrl;

        private readonly bool _transformGeodetic;
        private readonly TransformLatitudeDelegate _transformLatitude;
        private readonly TransformLatitudeDelegate _transformLatitudeInverse;
        private const double EarthEccentricity = 1 - 6.69437999014e-3;
        private const double EarthEccentricityInverse = 1.0 / EarthEccentricity;

        public string HighwayKeyword => "highway";

        public string RailwayKeyword => "railway";

        public string BuildingKeyword => "building";

        protected const string BuildingLevelsKeyword = "building:levels";
        protected const string BuildingMaterialKeyword = "building:material";
        protected const string BuildingRoofKindKeyword = "roof:shape";
        protected const string BuildingRoofMaterialKeyword = "roof:material";
        protected const string AmenityKeyword = "amenity";
        protected const string ReligionKeyword = "religion";
        protected const string OfficeKeyword = "office";
        protected const string AbandonedKeyword = "abandoned";

        protected const string HighwayLanesKeyword = "lanes";
        protected const string HighwayWidthKeyword = "width";
        protected const string HighwaySurfaceKeyword = "surface";

        protected const string RailwayPassengerLinesKeyword = "passenger_lines";
        protected const string RailwayElectrifiedKeyword = "electrified";

        public OverpassApiService(GeoInfoServiceOptions geoInfoServiceOptions, ILogger<OverpassApiService> logger)
        {
            _client = new HttpClient();
            _baseUrl = geoInfoServiceOptions.OverpassConnectionString
                ?? throw new ArgumentNullException(nameof(geoInfoServiceOptions.OverpassConnectionString));
            _transformGeodetic = geoInfoServiceOptions.TransformGeodeticToGeocentric ?? false;
            _transformLatitude = _transformGeodetic ? (TransformLatitudeDelegate)TransformLatitude : KeepLatitude;
            _transformLatitudeInverse = _transformGeodetic ? (TransformLatitudeDelegate)TransformLatitudeInverse : KeepLatitude;
            _logger = logger;
        }

        #region Private Members

        #region Node Parsing

        private NodeDto ParseNode(XmlElement element)
        {
            try
            {
                var tags = element
                        .SelectNodes("./tag")
                        .OfType<XmlElement>()
                        .ToDictionary(x => x.GetAttribute("k"), x => x.GetAttribute("v"));

                return new NodeDto()
                {
                    Id = long.Parse(element.GetAttribute("id")),
                    Latitude = double.Parse(element.GetAttribute("lat")),
                    Longitude = double.Parse(element.GetAttribute("lon")),
                    Tags = tags,
                };
            }
            catch (Exception e)
            {
                throw new ArgumentException(element.ToString(), e);
            }
        }

        private WayDto ParseWay(XmlElement element)
        {
            try
            {
                var references = element
                        .SelectNodes("./nd")
                        .OfType<XmlElement>()
                        .Select(x => long.Parse(x.GetAttribute("ref")))
                        .ToList();

                var tags = element
                        .SelectNodes("./tag")
                        .OfType<XmlElement>()
                        .ToDictionary(x => x.GetAttribute("k"), x => x.GetAttribute("v"));

                return new WayDto()
                {
                    Id = long.Parse(element.GetAttribute("id")),
                    References = references,
                    Tags = tags,
                };
            }
            catch (Exception e)
            {
                throw new ArgumentException(element.ToString(), e);
            }
        }

        #endregion

        #region Bounding Box

        private double Deg2Rad(double degrees) => degrees / 180.0 * Math.PI;

        private double Rad2Deg(double radians) => radians * 180.0 / Math.PI;

        private double Sec(double x) => 1.0 / Math.Cos(x);

        public (int, int) LonLat2Tile(double lon, double lat, int zoom)
        {
            var xtile = (int)((lon + 180.0) / 360.0 * (1 << zoom));
            var ytile = (int)((1 - Math.Log(Math.Tan(Deg2Rad(lat)) + Sec(Deg2Rad(lat))) / Math.PI) / 2.0 * (1 << zoom));
            return (xtile, ytile);
        }

        public (double, double) Tile2LonLat(int xtile, int ytile, int zoom)
        {
            double n = 1 << zoom;
            var lon_deg = xtile / n * 360.0 - 180.0;
            var lat_rad = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * ytile / n)));
            var lat_deg = Rad2Deg(lat_rad);

            return (lon_deg, lat_deg);
        }

        public (double, double, double, double) LonLat2BBox(double lon, double lat, int zoom)
        {
            var (xtile, ytile) = LonLat2Tile(lon, lat, zoom);

            var n = 1 << zoom;
            var (nwLon, nwLat) = Tile2LonLat(xtile, ytile, zoom);
            var (seLon, seLat) = Tile2LonLat((xtile + 1) % n, (ytile + 1) % n, zoom);

            return (nwLon, seLat, seLon, nwLat);
        }

        public BoundingBoxDto GetBoundingBox(CoordinateDto coordinate, int zoom)
        {
            var (west, south, east, north) = LonLat2BBox(lon: coordinate.Longitude, lat: coordinate.Latitude, zoom);

            return new BoundingBoxDto()
            {
                East = east,
                North = north,
                West = west,
                South = south,
            };
        }

        public BoundingBoxDto GetBoundingBox(AxisAlignedBoundingBoxCoordinateModel model)
        {
            return new BoundingBoxDto()
            {
                South = _transformLatitudeInverse(model.MinLatitude * 180.0 / Math.PI),
                West = model.MinLongtitude * 180.0 / Math.PI,
                North = _transformLatitudeInverse(model.MaxLatitude * 180.0 / Math.PI),
                East = model.MaxLongtitude * 180.0 / Math.PI,
            };
        }

        public short GetZoom(short zoom, int increment)
        {
            // Multiplies tile count by 2^increment.
            return (short)Math.Min(zoom + increment, short.MaxValue);
        }

        #endregion

        #endregion

        public async Task<Result<OverpassResponseDto>> SendRequest(string request, CancellationToken token)
        {
            Stream resultStream;
            try
            {
                var response = await _client.GetAsync(request, token);
                resultStream = await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException e)
            {
                return Result<OverpassResponseDto>.CreateFailure(IOStringMessages.RequestFailed, e);
            }

            var doc = new XmlDocument();
            doc.Load(resultStream);

            if (doc.ChildNodes.Count != 2)
            {
                return Result<OverpassResponseDto>.CreateFailure(IOStringMessages.FormatIncorrect);
            }

            var nodes = doc.SelectNodes("/osm/node").OfType<XmlElement>();
            var ways = doc.SelectNodes("/osm/way").OfType<XmlElement>();

            try
            {
                return Result<OverpassResponseDto>.CreateSuccess(new OverpassResponseDto()
                {
                    Nodes = nodes.Select(x => ParseNode(x)).ToList(),
                    Ways = ways.Select(x => ParseWay(x)).ToList(),
                });
            }
            catch (ArgumentException e)
            {
                _logger.LogError(e, "Failed to parse Overpass response.");

                return Result<OverpassResponseDto>.CreateFailure(IOStringMessages.FormatIncorrect, e);
            }
        }

        public async Task<Result<OverpassResponseDto>> GetBuildings(BoundingBoxDto bbox, CancellationToken token)
        {
            return await SendRequest(_baseUrl + $"?data=(way[\"{BuildingKeyword}\"]({bbox.South},{bbox.West},{bbox.North},{bbox.East});>;);out;", token);
        }

        public async Task<Result<OverpassResponseDto>> GetHighways(BoundingBoxDto bbox, CancellationToken token)
        {
            return await SendRequest(_baseUrl + $"?data=(way[\"{HighwayKeyword}\"]({bbox.South},{bbox.West},{bbox.North},{bbox.East});>;);out;", token);
        }

        public async Task<Result<OverpassResponseDto>> GetRailways(BoundingBoxDto bbox, CancellationToken token)
        {
            return await SendRequest(_baseUrl + $"?data=(way[\"{RailwayKeyword}\"]({bbox.South},{bbox.West},{bbox.North},{bbox.East});>;);out;", token);
        }

        public IReadOnlyList<HighwayEntity> ToHighwayEntityList(OverpassResponseDto overpassResponse, int? srid = null)
        {
            var nodeDict = overpassResponse.Nodes
                .ToDictionary(x => x.Id, x => x);

            var highways = overpassResponse.Ways
                .Where(x => x.Tags.ContainsKey(HighwayKeyword));

            var highwayTags = highways
                .SelectMany(x => x.Tags.Keys)
                .ToHashSet();
            var nodeTags = highways
                .SelectMany(x => x.References, (_, nodeRef) => nodeDict[nodeRef])
                .SelectMany(x => x.Tags.Keys)
                .ToHashSet();
            var unsupportedHighways = highways
                .Where(x => x.References.FirstOrDefault() == x.References.LastOrDefault())
                .Count();

            _logger.LogDebug("Found {Count} way tags: {Tags}.", highwayTags.Count, string.Join(", ", highwayTags));
            _logger.LogDebug("Found {Count} node tags: {Tags}.", nodeTags.Count, string.Join(", ", nodeTags));
            _logger.LogDebug("Found {Count} unsupported highways.", unsupportedHighways);

            var factory = CreateGeometryFactory(srid);

            return highways
                .Where(x => x.References.FirstOrDefault() != x.References.LastOrDefault()) // Filter out area highways
                .Select(x => new HighwayEntity(
                    gid: x.Id,
                    kind: x.Tags.TryGetValue(HighwayKeyword, out var kind) ? kind : null,
                    surface: x.Tags.TryGetValue(HighwaySurfaceKeyword, out var surface) ? surface : null,
                    width: x.Tags.TryGetValue(HighwayWidthKeyword, out var width) ? double.TryParse(width, out var widthValue) ? widthValue : (double?)null : null,
                    lanes: x.Tags.TryGetValue(HighwayLanesKeyword, out var lanes) ? int.TryParse(lanes, out var lanesValue) ? lanesValue : (int?)null : null,
                    factory.CreateMultiLineString(new LineString[]
                    {
                        factory.CreateLineString(x.References.Select(y =>
                        {
                            var node = nodeDict[y];
                            return new Coordinate(node.Longitude, _transformLatitude(node.Latitude));
                        }).ToArray()),
                    })))
                .ToList();
        }

        public IReadOnlyList<RailwayEntity> ToRailwayEntityList(OverpassResponseDto overpassResponse, int? srid = null)
        {
            var nodeDict = overpassResponse.Nodes
                .ToDictionary(x => x.Id, x => x);

            var railways = overpassResponse.Ways
                .Where(x => x.Tags.ContainsKey(RailwayKeyword));

            var railwayTags = railways
                .SelectMany(x => x.Tags.Keys)
                .ToHashSet();
            var nodeTags = railways
                .SelectMany(x => x.References, (_, nodeRef) => nodeDict[nodeRef])
                .SelectMany(x => x.Tags.Keys)
                .ToHashSet();
            var unsupportedRailways = railways
                .Where(x => x.References.FirstOrDefault() == x.References.LastOrDefault())
                .Count();

            _logger.LogDebug("Found {Count} way tags: {Tags}.", railwayTags.Count, string.Join(", ", railwayTags));
            _logger.LogDebug("Found {Count} node tags: {Tags}.", nodeTags.Count, string.Join(", ", nodeTags));
            _logger.LogDebug("Found {Count} unsupported railways.", unsupportedRailways);

            var factory = CreateGeometryFactory(srid);

            return railways
                .Where(x => x.References.FirstOrDefault() != x.References.LastOrDefault()) // Filter out area railways
                .Select(x => new RailwayEntity(
                    gid: x.Id,
                    kind: x.Tags.TryGetValue(RailwayKeyword, out var kind) ? kind : null,
                    passengerLines: x.Tags.TryGetValue(RailwayPassengerLinesKeyword, out var passengerLines)
                        ? int.TryParse(passengerLines, out var passengerLinesValue) ? passengerLinesValue : (int?)null
                        : null,
                    electrified: x.Tags.TryGetValue(RailwayElectrifiedKeyword, out var electrified) ? electrified : null,
                    factory.CreateMultiLineString(new LineString[]
                    {
                        factory.CreateLineString(x.References.Select(y =>
                        {
                            var node = nodeDict[y];
                            return new Coordinate(node.Longitude, _transformLatitude(node.Latitude));
                        }).ToArray()),
                    })))
                .ToList();
        }

        public IReadOnlyList<BuildingEntity> ToBuildingEntityList(OverpassResponseDto overpassResponse, int? srid = null)
        {
            var nodeDict = overpassResponse.Nodes
                .ToDictionary(x => x.Id, x => x);

            var buildings = overpassResponse.Ways
                .Where(x => x.Tags.ContainsKey(BuildingKeyword));

            var buildingTags = buildings
                .SelectMany(x => x.Tags.Keys)
                .ToHashSet();
            var nodeTags = buildings
                .SelectMany(x => x.References, (_, nodeRef) => nodeDict[nodeRef])
                .SelectMany(x => x.Tags.Keys)
                .ToHashSet();
            _logger.LogDebug("Found {Count} way tags: {Tags}.", buildingTags.Count, string.Join(", ", buildingTags));
            _logger.LogDebug("Found {Count} node tags: {Tags}.", nodeTags.Count, string.Join(", ", nodeTags));

            var factory = CreateGeometryFactory(srid);

            return buildings
                .Select(x => new BuildingEntity(
                    gid: x.Id,
                    kind: x.Tags.TryGetValue(BuildingKeyword, out var kind) ? kind : null,
                    material: x.Tags.TryGetValue(BuildingMaterialKeyword, out var material) ? material : null,
                    roofKind: x.Tags.TryGetValue(BuildingRoofKindKeyword, out var roofKind) ? roofKind : null,
                    roofMaterial: x.Tags.TryGetValue(BuildingRoofMaterialKeyword, out var roofMaterial) ? roofMaterial : null,
                    levels: x.Tags.TryGetValue(BuildingLevelsKeyword, out var levels) ? int.TryParse(levels, out var levelsInt) ? levelsInt : (int?)null : null,
                    amenity: x.Tags.TryGetValue(AmenityKeyword, out var amenity) ? amenity : null,
                    religion: x.Tags.TryGetValue(ReligionKeyword, out var religion) ? religion : null,
                    office: x.Tags.TryGetValue(OfficeKeyword, out var office) ? office : null,
                    abandoned: x.Tags.TryGetValue(AbandonedKeyword, out var abandoned) ? abandoned : null,
                    null,
                    factory.CreateMultiPolygon(new Polygon[] { factory.CreatePolygon(x.References.Select(y =>
                    {
                        var node = nodeDict[y];
                        return new Coordinate(node.Longitude, _transformLatitude(node.Latitude));
                    }).ToArray()) })))
                .ToList();
        }

        private double TransformLatitude(double latitude)
        {
            // Geographic latitudes are normally geodetic; we convert this to
            // geocentric because we want spherical coordinates.  The magic
            // number below is the Earth's eccentricity, squared, using the WGS84
            // ellipsoid.
            return Math.Atan(EarthEccentricity * Math.Tan(latitude));
        }

        private double TransformLatitudeInverse(double latitude)
        {
            // Geographic latitudes are normally geodetic; we convert this to
            // geocentric because we want spherical coordinates.  The magic
            // number below is the Earth's eccentricity, squared, using the WGS84
            // ellipsoid.
            return Math.Atan(EarthEccentricityInverse * Math.Tan(latitude));
        }

        private double KeepLatitude(double latitude) => latitude;

        private GeometryFactory CreateGeometryFactory(int? srid)
        {
            var precision = PrecisionModel.Floating.Value;

            return srid == null ? new GeometryFactory(precision) : new GeometryFactory(precision, srid.Value);
        }
    }
}
