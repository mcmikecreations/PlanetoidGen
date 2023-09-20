using NetTopologySuite.Geometries;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using System.Collections.Generic;

namespace PlanetoidGen.Agents.Osm.Models.Entities
{
    public class RailwayEntity : BaseEntity
    {
        /// <summary>
        /// Type of the railway, e.g. rail, monorail.
        /// </summary>
        public string? Kind { get; }

        /// <summary>
        /// Number of passenger lines this track corresponds to.
        /// </summary>
        public int? PassengerLines { get; }

        /// <summary>
        /// Is the railway electrified, e.g. contact_line, rail, yes, no.
        /// </summary>
        public string? Electrified { get; }

        /// <summary>
        /// Real shape of the railway. The coords are (lon,lat) in degrees.
        /// </summary>
        public MultiLineString Path => (MultiLineString)Geom;

        public RailwayEntity(
            long gid,
            string? kind,
            int? passengerLines,
            string? electrified,
            Geometry geom) : base(gid, (MultiLineString)geom)
        {
            Kind = kind;
            PassengerLines = passengerLines;
            Electrified = electrified;
        }

        public static TableSchema GetSchema(string schema, string tableName, int? srid = null)
        {
            var geomDict = new Dictionary<string, string>()
            {
                { ColumnSchema.PropertyKeys.GeometryType, nameof(MultiLineString) },
            };

            if (srid.HasValue)
            {
                geomDict[ColumnSchema.PropertyKeys.SpatialRefSys] = srid!.ToString();
            }

            return new TableSchema(
                schema,
                tableName,
                new List<ColumnSchema>()
                {
                    new ColumnSchema(nameof(GID), ColumnSchema.ColumnType.Int64, null, false, true, true, true, true),
                    new ColumnSchema(nameof(Kind), ColumnSchema.ColumnType.String, null, null, true, false, false, false),
                    new ColumnSchema(nameof(PassengerLines), ColumnSchema.ColumnType.Int32, null, null, true, false, false, false),
                    new ColumnSchema(nameof(Electrified), ColumnSchema.ColumnType.String, null, null, true, false, false, false),
                    new ColumnSchema(nameof(Geom), ColumnSchema.ColumnType.Geometry, geomDict, false, true, false, false, false),
                },
                new List<IndexSchema>()
                {
                    new IndexSchema(IndexSchema.IndexKind.PrimaryKey, new List<string>() { nameof(GID) }, null),
                    new IndexSchema(IndexSchema.IndexKind.Gist, new List<string>() { nameof(Geom) }, null),
                });
        }

        public static TableSchema GetSchema(string schema = "dyn", int? srid = null)
        {
            return GetSchema(schema, $"{nameof(RailwayEntity)}Collection", srid);
        }
    }
}
