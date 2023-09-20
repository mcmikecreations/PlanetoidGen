using PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Services.Abstractions;
using PlanetoidGen.BusinessLogic.Agents.Models.Agents;

namespace PlanetoidGen.Agents.Osm.Agents.OpenStreetMap.Models.Settings
{
    public class OpenStreetMapTileLoadingAgentSettings : BaseAgentSettings<OpenStreetMapTileLoadingAgentSettings>
    {
        /// <summary>
        /// Url should include placeholders for {Style}, {Z}, {X}, {Y},
        /// {AccessToken}, {ImageFormatExtension}
        /// which will be replaced by the actual values.
        /// If style or access token is not applicable, the placeholder can be ignored.
        /// </summary>
        /// <remarks>
        /// Subdomains are supported through [a,b] format, e.g.,
        /// http://[a,b,c].tile.openstreetmap.org
        /// </remarks>
        public string? Url { get; set; }

        /// <summary>
        /// Style of the tiles to fetch.
        /// </summary>
        public string? Style { get; set; }

        /// <summary>
        /// Access token required to fetch the tiles.
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// Max zoom supported by the provider.
        /// </summary>
        public int? MaxZoom { get; set; }

        /// <summary>
        /// Final zoom is calculated as follows:
        /// <code>Min(MaxZoom, request + ZoomIncrement)</code>
        /// </summary>
        public int? ZoomIncrement { get; set; } = 2;

        /// <summary>
        /// The file extension of tiles, e.g. png, jpg.
        /// </summary>
        public string? ImageFormatExtension { get; set; }

        public int? SourceProjection { get; set; } = IOverpassApiService.WGS84Srid;
    }
}
