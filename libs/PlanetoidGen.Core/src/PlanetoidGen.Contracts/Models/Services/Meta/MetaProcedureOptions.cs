using System.ComponentModel.DataAnnotations;

namespace PlanetoidGen.Contracts.Models.Services.Meta
{
    public class MetaProcedureOptions
    {
        public static string DefaultConfigurationSectionName = nameof(MetaProcedureOptions);

        /// <summary>
        /// Should the tables be recreated on initialization.
        /// </summary>
        [Required]
        public bool RecreateTables { get; set; }

        /// <summary>
        /// Should the tables created during runtime be recreated on initialization.
        /// </summary>
        [Required]
        public bool RecreateDynamicTables { get; set; }

        /// <summary>
        /// Should the schemas (e.g. public, dyn) be recreated on initialization.
        /// </summary>
        [Required]
        public bool RecreateSchemas { get; set; }

        /// <summary>
        /// Should the extensions (e.g. postgis, uuid) be recreated on initialization.
        /// </summary>
        [Required]
        public bool RecreateExtensions { get; set; }

        /// <summary>
        /// Should the extensions (e.g. PlanetoidInfoCreate function) be recreated on initialization.
        /// </summary>
        [Required]
        public bool RecreateProcedures { get; set; }

        public bool RecreateAny =>
            RecreateTables ||
            RecreateDynamicTables ||
            RecreateProcedures ||
            RecreateSchemas ||
            RecreateExtensions;
    }
}
