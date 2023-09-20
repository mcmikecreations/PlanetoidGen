using PlanetoidGen.BusinessLogic.Agents.Models.Agents;

namespace PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Settings
{
    public class OpenStreetMapLoadingAgentSettings : BaseAgentSettings<OpenStreetMapLoadingAgentSettings>
    {
        public bool PushToGeoServer { get; set; }

        public string? OverpassBaseUrl { get; set; }

        /// <summary>
        /// The database table schema. A default value "dyn" is used if null.
        /// </summary>
        public string? EntityTableSchema { get; set; }

        /// <summary>
        /// The database table name. A default value is used if null.
        /// </summary>
        public string? EntityTableName { get; set; }
    }
}
