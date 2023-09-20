using NetTopologySuite.Geometries;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using System.Collections.Generic;

namespace PlanetoidGen.Agents.Osm.Models.Entities
{
    public class HighwayEntity : BaseEntity
    {
        /// <summary>
        /// Type of the highway, e.g. motorway, footway.
        /// </summary>
        public string? Kind { get; }

        /// <summary>
        /// Surface material of the highway, e.g. paving_stones, asphalt
        /// </summary>
        public string? Surface { get; }

        /// <summary>
        /// Total width of highway in meters.
        /// </summary>
        public double? Width { get; }

        /// <summary>
        /// Total number of lines, including bus lanes, parking-abled lanes, etc.
        /// Count excludes cycle lanes and motorcycle lanes that do not permit a motor vehicle.
        /// </summary>
        public int? Lanes { get; }

        /// <summary>
        /// Real shape of the highway. The coords are (lon,lat) in degrees.
        /// </summary>
        public MultiLineString Path => (MultiLineString)Geom;

        public HighwayEntity(
            long gid,
            string? kind,
            string? surface,
            double? width,
            int? lanes,
            Geometry geom) : base(gid, (MultiLineString)geom)
        {
            Kind = kind;
            Surface = surface;
            Width = width;
            Lanes = lanes;
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
                    new ColumnSchema(nameof(Surface), ColumnSchema.ColumnType.String, null, null, true, false, false, false),
                    new ColumnSchema(nameof(Width), ColumnSchema.ColumnType.Float64, null, null, true, false, false, false),
                    new ColumnSchema(nameof(Lanes), ColumnSchema.ColumnType.Int32, null, null, true, false, false, false),
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
            return GetSchema(schema, $"{nameof(HighwayEntity)}Collection", srid);
        }
    }
}
