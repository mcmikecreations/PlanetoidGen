using NetTopologySuite.Geometries;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using System.Collections.Generic;

namespace PlanetoidGen.Agents.Osm.Models.Entities
{
    public class BuildingEntity : BaseEntity
    {
        /// <summary>
        /// Type of the building, e.g. garages, semidetached_house.
        /// </summary>
        public string? Kind { get; }

        /// <summary>
        /// Building material, e.g. brick, plaster.
        /// </summary>
        public string? Material { get; }

        /// <summary>
        /// Building roof shape, e.g. hipped, skillion.
        /// </summary>
        public string? RoofKind { get; }

        /// <summary>
        /// Building roof material, e.g. aluminum, bitumen.
        /// </summary>
        public string? RoofMaterial { get; }

        /// <summary>
        /// Number of floors in the building.
        /// </summary>
        public int? Levels { get; }

        /// <summary>
        /// Type of amenity in the building, e.g. bar, restaurant, library.
        /// </summary>
        public string? Amenity { get; }

        /// <summary>
        /// Type of religion in the building, e.g. buddhist, christian, scientologist.
        /// </summary>
        public string? Religion { get; }

        /// <summary>
        /// Type of office in the building, e.g. accountant, architect, charity.
        /// </summary>
        public string? Office { get; }

        /// <summary>
        /// Is the building abandoned? Can be yes or contain the old type/info about the building.
        /// </summary>
        public string? Abandoned { get; }

        /// <summary>
        /// The floor-based description of the building.
        /// </summary>
        public string? DetailedDescription { get; }

        /// <summary>
        /// Real shape of the building. The coords are (lon,lat) in degrees.
        /// </summary>
        public MultiPolygon Path => (MultiPolygon)Geom;

        public BuildingEntity(
            long gid,
            string? kind,
            string? material,
            string? roofKind,
            string? roofMaterial,
            int? levels,
            string? amenity,
            string? religion,
            string? office,
            string? abandoned,
            string? detailedDescription,
            Geometry geom) : base(gid, (MultiPolygon)geom)
        {
            Kind = kind;
            Material = material;
            RoofKind = roofKind;
            RoofMaterial = roofMaterial;
            Levels = levels;
            Amenity = amenity;
            Religion = religion;
            Office = office;
            Abandoned = abandoned;
            DetailedDescription = detailedDescription;
        }

        public static TableSchema GetSchema(string schema, string tableName, int? srid = null)
        {
            var geomDict = new Dictionary<string, string>()
            {
                { ColumnSchema.PropertyKeys.GeometryType, nameof(MultiPolygon) },
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
                    new ColumnSchema(nameof(Material), ColumnSchema.ColumnType.String, null, null, true, false, false, false),
                    new ColumnSchema(nameof(RoofKind), ColumnSchema.ColumnType.String, null, null, true, false, false, false),
                    new ColumnSchema(nameof(RoofMaterial), ColumnSchema.ColumnType.String, null, null, true, false, false, false),
                    new ColumnSchema(nameof(Levels), ColumnSchema.ColumnType.Int32, null, null, true, false, false, false),
                    new ColumnSchema(nameof(Amenity), ColumnSchema.ColumnType.String, null, null, true, false, false, false),
                    new ColumnSchema(nameof(Religion), ColumnSchema.ColumnType.String, null, null, true, false, false, false),
                    new ColumnSchema(nameof(Office), ColumnSchema.ColumnType.String, null, null, true, false, false, false),
                    new ColumnSchema(nameof(Abandoned), ColumnSchema.ColumnType.String, null, null, true, false, false, false),
                    new ColumnSchema(nameof(DetailedDescription), ColumnSchema.ColumnType.String, null, null, true, false, false, false),
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
            return GetSchema(schema, $"{nameof(BuildingEntity)}Collection", srid);
        }
    }
}
