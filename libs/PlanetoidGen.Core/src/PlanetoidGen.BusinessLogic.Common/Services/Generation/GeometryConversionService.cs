using DotSpatial.Projections;
using DotSpatial.Projections.Transforms;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Repositories.Generation;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.BusinessLogic.Common.Services.Generation
{
    public class GeometryConversionService : IGeometryConversionService
    {
        private readonly ISpatialReferenceSystemRepository _srsRepo;

        public GeometryConversionService(ISpatialReferenceSystemRepository srsRepo)
        {
            _srsRepo = srsRepo;
        }

        public async ValueTask<IEnumerable<double[]>> ToAssimpVectors(
            IEnumerable<CoordinateModel> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            CancellationToken token,
            int? srcProjection = null,
            int? dstProjection = null)
        {
            var (srcProjInfo, dstProjInfo) = await GetProjections(planetoid, srcProjection, dstProjection, token);
            return ToAssimpVectors(coordinates, planetoid, yUp, srcProjInfo, dstProjInfo);
        }

        public async ValueTask<IList<IEnumerable<double[]>>> ToAssimpVectors(
            IEnumerable<CoordinateModel[]> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            CancellationToken token,
            int? srcProjection = null,
            int? dstProjection = null)
        {
            var (srcProjInfo, dstProjInfo) = await GetProjections(planetoid, srcProjection, dstProjection, token);
            return coordinates
                    .Select(x => ToAssimpVectors(x, planetoid, yUp, srcProjInfo, dstProjInfo))
                    .ToList();
        }

        /// <returns>A pair of src and dst projections.</returns>
        private (ProjectionInfo, ProjectionInfo) GetProjections(SpatialReferenceSystemModel srcProjection, SpatialReferenceSystemModel dstProjection)
        {
            var srcProjInfo = ProjectionInfo.FromEsriString(string.IsNullOrWhiteSpace(srcProjection.WktString)
                    ? srcProjection.Proj4String
                    : srcProjection.WktString);

            var dstProjInfo = ProjectionInfo.FromEsriString(string.IsNullOrWhiteSpace(dstProjection.WktString)
                    ? dstProjection.Proj4String
                    : dstProjection.WktString);

            return (srcProjInfo, dstProjInfo);
        }

        /// <exception cref="ArgumentNullException">If srcProjection or dstProjection is missing in the db.</exception>
        private async ValueTask<(ProjectionInfo, ProjectionInfo)> GetProjections(
            PlanetoidInfoModel planetoid,
            int? srcProjection,
            int? dstProjection,
            CancellationToken token)
        {
            ProjectionInfo? srcProjInfo, dstProjInfo;

            if (srcProjection.HasValue)
            {
                var srs = await _srsRepo.GetSRS(srcProjection.Value, token);
                if (!srs.Success)
                {
                    throw new ArgumentNullException(nameof(srcProjection));
                }

                srcProjInfo = ProjectionInfo.FromEsriString(string.IsNullOrWhiteSpace(srs.Data.WktString)
                    ? srs.Data.Proj4String
                    : srs.Data.WktString);
            }
            else
            {
                var wkt = GenerateWKTStringGeographic(planetoid);
                srcProjInfo = ProjectionInfo.FromEsriString(wkt);
            }

            if (dstProjection.HasValue)
            {
                var srs = await _srsRepo.GetSRS(dstProjection.Value, token);
                if (!srs.Success)
                {
                    throw new ArgumentNullException(nameof(dstProjection));
                }

                dstProjInfo = ProjectionInfo.FromEsriString(string.IsNullOrWhiteSpace(srs.Data.WktString)
                    ? srs.Data.Proj4String
                    : srs.Data.WktString);
            }
            else
            {
                var wkt = GenerateWKTStringProjected(planetoid);
                dstProjInfo = ProjectionInfo.FromEsriString(wkt);
            }

            return (srcProjInfo, dstProjInfo);
        }

        private IEnumerable<double[]> ToAssimpVectors(
            IEnumerable<CoordinateModel> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            ProjectionInfo srcProjection,
            ProjectionInfo dstProjection)
        {
            var coordList = coordinates.ToList();
            var coordCount = coordList.Count;

            double[] coordsXY = new double[2 * coordCount];
            double[] coordsZ = new double[coordCount];

            int i = 0;
            foreach (var c in coordList)
            {
                coordsXY[2 * i] = c.X; // Longtitude
                coordsXY[2 * i + 1] = c.Y; // Latitude
                coordsZ[i] = c.Z; // Height
                ++i;
            }

            Reproject.ReprojectPoints(coordsXY, coordsZ, srcProjection, dstProjection, 0, coordCount);

            var result = new List<double[]>();
            // Flip lon and lat to match the correct orientation (+Z north, +X east).
            if (yUp)
            {
                for (i = 0; i < coordCount; ++i)
                {
                    result.Add(new[] { coordsXY[2 * i], coordsZ[i], coordsXY[2 * i + 1] });
                }
            }
            else
            {
                for (i = 0; i < coordCount; ++i)
                {
                    result.Add(new[] { coordsXY[2 * i], coordsXY[2 * i + 1], coordsZ[i] });
                }
            }

            return result;
        }

        public string GenerateWKTStringGeographic(PlanetoidInfoModel planetoid)
        {
            // Based on KnownCoordinateSystems.Geographic.World.WGS1984;
            // From a preset:
            /*return KnownCoordinateSystems.Geographic.World.WGS1984.ToEsriString();*/

            // Based on GCS_WGS_1984_Major_Auxiliary_Sphere 104199
            // From a correct Esri string:
            /*return $@"GEOGCRS[""GCS_{planetoid.Title}_Major_Auxiliary_Sphere"",
    DATUM[""D_{planetoid.Title}_Major_Auxiliary_Sphere"",
        ELLIPSOID[""{planetoid.Title} Visualisation Sphere"",{planetoid.Radius},0,
            LENGTHUNIT[""metre"",1]]],
    PRIMEM[""Reference_Meridian"",0,
        ANGLEUNIT[""degree"",0.0174532925199433]],
    CS[ellipsoidal,2],
        AXIS[""geodetic latitude (Lat)"",north,
            ORDER[1],
            ANGLEUNIT[""degree"",0.0174532925199433]],
        AXIS[""geodetic longitude (Lon)"",east,
            ORDER[2],
            ANGLEUNIT[""degree"",0.0174532925199433]],
    USAGE[
        SCOPE[""Usage within the {nameof(PlanetoidGen)} system.""],
        AREA[""World.""],
        BBOX[-90,-180,90,180]]
]";*/

            var crs = GenerateSRSModelGeographic(planetoid);
            return crs.WktString;
        }

        public string GenerateWKTStringProjected(PlanetoidInfoModel planetoid)
        {
            // Based on KnownCoordinateSystems.Projected.World.WebMercator; Google Maps 900913
            // ANGLEUNIT = 2pi / 360, how many radians in a degree
            // From a preset:
            /*return KnownCoordinateSystems.Projected.World.WebMercator.ToEsriString();*/

            // Based on Sphere_Mercator; 53004
            // From a correct Esri string:
            /*return @$"PROJCRS[""{planetoid.Title}_Mercator"",
    BASEGEOGCRS[""Unknown datum based upon the {planetoid.Title} planetoid sphere"",
        DATUM[""Not specified (based on planetoid sphere)"",
            ELLIPSOID[""{planetoid.Title}_Sphere"",{planetoid.Radius},0,
                LENGTHUNIT[""metre"",1]]],
        PRIMEM[""Reference_Meridian"",0,
            ANGLEUNIT[""Degree"",0.0174532925199433]]],
    CS[Cartesian,2],
        AXIS[""(E)"",east,
            ORDER[1],
            LENGTHUNIT[""metre"",1]],
        AXIS[""(N)"",north,
            ORDER[2],
            LENGTHUNIT[""metre"",1]],
    USAGE[
        SCOPE[""Usage within the {nameof(PlanetoidGen)} system.""],
        AREA[""World.""],
        BBOX[-90,-180,90,180]]
]";*/

            var crs = GenerateSRSModelProjected(planetoid);
            return crs.WktString;
        }

        public string GenerateProj4StringGeographic(PlanetoidInfoModel planetoid)
        {
            // +rf (reverse flattening) is calculated as (a-b)/a,
            // where a is the equatorial semimajor axis
            // and b is the polar semiminor axis.
            // From a preset:
            /*return $"+proj=longlat +a={planetoid.Radius} +rf=0.0 +no_defs";*/

            // From a correct Proj string:
            /*return $"+proj=longlat +R={planetoid.Radius} +no_defs";*/

            var crs = GenerateSRSModelGeographic(planetoid);
            return crs.Proj4String;
        }

        public string GenerateProj4StringProjected(PlanetoidInfoModel planetoid)
        {
            // a is the equatorial semimajor axis
            // and b is the polar semiminor axis.
            // k_0 is the scaling factor for output,
            // lat_ts defines the latitude where scale is not distorted or aliases lat_0,
            // lon_0 is the longtitude of the projection center or central meridian,
            // wktext is "embed the entire PROJ string in the WKT and use it literally when converting back to PROJ format" from https://github.com/OSGeo/gdal/issues/2392.
            // From a preset:
            /*return $"+proj=merc +a={planetoid.Radius} +b={planetoid.Radius} +lat_ts=0 +lon_0=0 +x_0=0 +y_0=0 +k_0=1 +units=m +nadgrids=@null +wktext +no_defs";*/

            // From a correct Proj string:
            /*return $"+proj=merc +lat_ts=0 +lon_0=0 +x_0=0 +y_0=0 +R={planetoid.Radius} +units=m +no_defs";*/

            var crs = GenerateSRSModelProjected(planetoid);
            return crs.Proj4String;
        }

        public SpatialReferenceSystemModel GenerateSRSModelGeographic(PlanetoidInfoModel planetoid)
        {
            var crs = new ProjectionInfo
            {
                Authority = nameof(PlanetoidGen),
                AuthorityCode = planetoid.Id * 2,
                AuxiliarySphereType = AuxiliarySphereType.Authalic,
                IsLatLon = true,
                NoDefs = true
            };

            var name = $"GCS_{planetoid.Title}_Major_Auxiliary_Sphere";
            crs.Name = name;

            crs.ScaleFactor = 1.0;
            crs.FalseEasting = 0.0; // x_0
            crs.FalseNorthing = 0.0; // y_0
            crs.lat_ts = 0.0;
            crs.StandardParallel1 = 0.0; // lat_ts
            crs.CentralMeridian = 0.0; // lon_0
            crs.LatitudeOfOrigin = 0.0; // lat_0

            // Re-sets the info from the default constructor, explicit for documentation purposes
            crs.Unit.Meters = 1.0;
            crs.Unit.Name = "Meter";

            crs.GeographicInfo.Meridian.Name = "Reference_Meridian";
            crs.GeographicInfo.Meridian.Longitude = 0.0;
            crs.GeographicInfo.Meridian.Code = 8901; // Greenwich GDAL code.

            crs.GeographicInfo.Name = name;

            // Re-sets the info from the default constructor, explicit for documentation purposes
            crs.GeographicInfo.Unit.Radians = Math.PI / 180.0;
            crs.GeographicInfo.Unit.Name = "Degree";

            crs.GeographicInfo.Datum.Name = $"D_{planetoid.Title}_Major_Auxiliary_Sphere";
            crs.GeographicInfo.Datum.DatumType = DatumType.Unknown;

            crs.GeographicInfo.Datum.Spheroid = new Spheroid(planetoid.Radius)
            {
                Name = $"{planetoid.Title} Visualisation Sphere",
                EquatorialRadius = planetoid.Radius,
                PolarRadius = planetoid.Radius, // InverseFlattening calculated via formula.
                KnownEllipsoid = Proj4Ellipsoid.Custom
            };

            crs.Transform = new LongLat();
            crs.Transform.Init(crs);

            return new SpatialReferenceSystemModel(default, crs.Authority, crs.AuthorityCode, crs.ToEsriString(), crs.ToProj4String());
        }

        public SpatialReferenceSystemModel GenerateSRSModelProjected(PlanetoidInfoModel planetoid)
        {
            var crs = new ProjectionInfo
            {
                Authority = nameof(PlanetoidGen),
                AuthorityCode = planetoid.Id * 2 + 1,
                AuxiliarySphereType = AuxiliarySphereType.Authalic,
                IsLatLon = false,
                NoDefs = true
            };

            var name = $"{planetoid.Title}_Mercator";
            crs.Name = name;

            crs.ScaleFactor = 1.0;
            crs.FalseEasting = 0.0; // x_0
            crs.FalseNorthing = 0.0; // y_0
            crs.lat_ts = 0.0;
            crs.StandardParallel1 = 0.0; // lat_ts
            crs.CentralMeridian = 0.0; // lon_0
            crs.LatitudeOfOrigin = 0.0; // lat_0

            // Re-sets the info from the default constructor, explicit for documentation purposes
            crs.Unit.Meters = 1.0;
            crs.Unit.Name = "Meter";

            crs.GeographicInfo.Meridian.Name = "Reference_Meridian";
            crs.GeographicInfo.Meridian.Longitude = 0.0;
            crs.GeographicInfo.Meridian.Code = 8901; // Greenwich GDAL code.

            crs.GeographicInfo.Name = name;

            // Re-sets the info from the default constructor, explicit for documentation purposes
            crs.GeographicInfo.Unit.Radians = Math.PI / 180.0;
            crs.GeographicInfo.Unit.Name = "Degree";

            crs.GeographicInfo.Datum.Name = $"Unknown datum based upon the {planetoid.Title} planetoid sphere";
            crs.GeographicInfo.Datum.Description = $"Not specified (based on the {planetoid.Title} planetoid sphere)";
            crs.GeographicInfo.Datum.DatumType = DatumType.Unknown;

            crs.GeographicInfo.Datum.Spheroid = new Spheroid(planetoid.Radius)
            {
                Name = $"{planetoid.Title}_Sphere",
                EquatorialRadius = planetoid.Radius,
                PolarRadius = planetoid.Radius, // InverseFlattening calculated via formula.
                KnownEllipsoid = Proj4Ellipsoid.Custom
            };

            crs.Transform = new MercatorAuxiliarySphere();
            crs.Transform.Init(crs);

            return new SpatialReferenceSystemModel(default, crs.Authority, crs.AuthorityCode, crs.ToEsriString(), crs.ToProj4String());
        }

        public IEnumerable<double[]> ToAssimpVectors(
            IEnumerable<CoordinateModel> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            SpatialReferenceSystemModel srcProjection,
            SpatialReferenceSystemModel dstProjection)
        {
            var (srcProjInfo, dstProjInfo) = GetProjections(srcProjection, dstProjection);
            return ToAssimpVectors(coordinates, planetoid, yUp, srcProjInfo, dstProjInfo);
        }

        public IList<IEnumerable<double[]>> ToAssimpVectors(
            IEnumerable<CoordinateModel[]> coordinates,
            PlanetoidInfoModel planetoid,
            bool yUp,
            SpatialReferenceSystemModel srcProjection,
            SpatialReferenceSystemModel dstProjection)
        {
            var (srcProjInfo, dstProjInfo) = GetProjections(srcProjection, dstProjection);
            return coordinates
                    .Select(x => ToAssimpVectors(x, planetoid, yUp, srcProjInfo, dstProjInfo))
                    .ToList();
        }
    }
}
