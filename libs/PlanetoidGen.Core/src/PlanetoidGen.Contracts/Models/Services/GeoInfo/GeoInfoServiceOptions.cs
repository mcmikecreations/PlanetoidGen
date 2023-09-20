namespace PlanetoidGen.Contracts.Models.Services.GeoInfo
{
    public class GeoInfoServiceOptions
    {
        public static string DefaultConfigurationSectionName = nameof(GeoInfoServiceOptions);

        /// <summary>
        /// A connection url to the Overpass server interpreter endpoint
        /// in the form <code>http[s]://address[:port]/api/interpreter</code>
        /// </summary>
        public string? OverpassConnectionString { get; set; }

        /// <summary>
        /// Should geodetic coordinates, that account for planet deformation,
        /// be converted to geocentric coordinates, which assume a perfect sphere?
        /// </summary>
        public bool? TransformGeodeticToGeocentric { get; set; }

        /// <summary>
        /// Start of the vailable integer range of ids for the spatial_ref_sys PostGIS table.
        /// </summary>
        public int? AvailableMinSrid { get; set; }

        /// <summary>
        /// Exclusive end of the vailable integer range of ids for the spatial_ref_sys PostGIS table.
        /// </summary>
        public int? AvailableMaxSrid { get; set; }
    }
}
