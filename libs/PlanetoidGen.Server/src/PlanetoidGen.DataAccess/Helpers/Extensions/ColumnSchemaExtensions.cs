using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using System;
using System.Linq;
using System.Text;

namespace PlanetoidGen.DataAccess.Helpers.Extensions
{
    public static class ColumnSchemaExtensions
    {
        public static Type ToType(this ColumnSchema column)
        {
            return (column.DataType, column.CanBeNull) switch
            {
                (ColumnSchema.ColumnType.String, _) => typeof(string),
                (ColumnSchema.ColumnType.Int16, false) => typeof(short),
                (ColumnSchema.ColumnType.Int16, true) => typeof(short?),
                (ColumnSchema.ColumnType.Int32, false) => typeof(int),
                (ColumnSchema.ColumnType.Int32, true) => typeof(int?),
                (ColumnSchema.ColumnType.Int64, false) => typeof(long),
                (ColumnSchema.ColumnType.Int64, true) => typeof(long?),
                (ColumnSchema.ColumnType.Float32, false) => typeof(float),
                (ColumnSchema.ColumnType.Float32, true) => typeof(float?),
                (ColumnSchema.ColumnType.Float64, false) => typeof(double),
                (ColumnSchema.ColumnType.Float64, true) => typeof(double?),
                (ColumnSchema.ColumnType.Geometry, false) => ToGeometryType(column),
                _ => throw new NotImplementedException(column.DataType.ToString()),
            };
        }

        public static string GetTypeName(this ColumnSchema column) => column.DataType switch
        {
            ColumnSchema.ColumnType.Int16 => "smallint",
            ColumnSchema.ColumnType.Int32 => "integer",
            ColumnSchema.ColumnType.Int64 => "bigint",
            ColumnSchema.ColumnType.Float32 => "real",
            ColumnSchema.ColumnType.Float64 => "double precision",
            ColumnSchema.ColumnType.String => "text",
            ColumnSchema.ColumnType.Geometry => "geometry",
            _ => throw new NotImplementedException(),
        };

        public static string CreateTableRowSql(this ColumnSchema column)
        {
            var sb = new StringBuilder();

            sb.Append($"{column.Title} {column.GetTypeName()}");

            switch (column.DataType)
            {
                case ColumnSchema.ColumnType.Geometry:
                    if (!column.Properties.ContainsKey(ColumnSchema.PropertyKeys.GeometryType))
                    {
                        throw new ArgumentException($"Geometry column needs to contain {nameof(ColumnSchema.PropertyKeys.GeometryType)} property.", column.ToString());
                    }

                    sb
                        .Append("(")
                        .AppendJoin(',', ColumnSchema.PropertyKeys.GeometryPropertyKeys
                            .Where(x => column.Properties.ContainsKey(x))
                            .Select(x => column.Properties[x]))
                        .Append(")");

                    break;
                default:
                    break;
            }

            if (!column.CanBeNull) sb.Append(" NOT NULL");

            return sb.ToString();
        }

        #region Private Members

        private static Type ToGeometryType(ColumnSchema column)
        {
            if (column.Properties.ContainsKey(ColumnSchema.PropertyKeys.GeometryType))
            {
                return typeof(object);
                /*return column.Properties[ColumnSchema.PropertyKeys.GeometryType] switch
                {
                    nameof(Point) => typeof(Point),
                    nameof(MultiPolygon) => typeof(MultiPolygon),
                    nameof(MultiLineString) => typeof(MultiLineString),
                    _ => throw new NotImplementedException(column.Properties[ColumnSchema.PropertyKeys.GeometryType]),
                };*/
            }
            else
            {
                throw new NotImplementedException(column.ToString());
            }
        }

        #endregion
    }
}
