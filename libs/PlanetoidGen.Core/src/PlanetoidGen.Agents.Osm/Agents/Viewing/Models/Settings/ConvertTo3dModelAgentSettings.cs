using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Services.Abstractions;
using PlanetoidGen.BusinessLogic.Agents.Models.Agents;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings
{
    public class ConvertTo3dModelAgentSettings : BaseAgentSettings<ConvertTo3dModelAgentSettings>
    {
        /// <summary>
        /// Is the Y coordinate up? Else Z is up.
        /// </summary>
        public bool YUp { get; set; } = true;

        public bool MergeModels { get; set; } = true;

        public double BestLODSize { get; set; } = 800.0;

        public double WorstLODSize { get; set; } = 3200.0;

        /// <summary>
        /// The database table schema. A default value "dyn" is used if null.
        /// </summary>
        public string? EntityTableSchema { get; set; }

        /// <summary>
        /// The database table name. A default value is used if null.
        /// </summary>
        public string? EntityTableName { get; set; }

        /// <summary>
        /// If null or non-positive, no foundation is added.
        /// Otherwise, the original foundation is extruded downwards
        /// to touch or go underground.
        /// </summary>
        public double? FoundationHeight { get; set; } = 5.0;

        public int SourceProjection { get; set; } = IOverpassApiService.WGS84Srid;

        public int DestinationProjection { get; set; } = IOverpassApiService.WebMercatorSrid;
    }
}
