using PlanetoidGen.BusinessLogic.Agents.Models.Agents;
using System.ComponentModel.DataAnnotations;

namespace PlanetoidGen.Agents.Osm.Agents.Viewing.Models.Settings
{
    public class BuildingInformationSeedingAgentSettings : BaseAgentSettings<BuildingInformationSeedingAgentSettings>
    {
        public long Seed { get; set; } = 0L;

        public double DefaultFloorHeight { get; set; } = 2.7;

        /// <summary>
        /// The database table schema. A default value "dyn" is used if null.
        /// </summary>
        [RegularExpression(@"^[a-zA-Z0-9]+$")]
        public string? BuildingTableSchema { get; set; }

        /// <summary>
        /// The database table name. A default value is used if null.
        /// </summary>
        [RegularExpression(@"^[a-zA-Z0-9]+$")]
        public string? BuildingTableName { get; set; }
    }
}
