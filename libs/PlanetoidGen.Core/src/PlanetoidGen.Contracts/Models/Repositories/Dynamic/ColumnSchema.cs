using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PlanetoidGen.Contracts.Models.Repositories.Dynamic
{
    /// <summary>
    /// A description of a column in a dynamic table.
    /// </summary>
    public class ColumnSchema
    {
        public enum ColumnType
        {
            Int16,
            Int32,
            Int64,
            String,
            Float32,
            Float64,
            Geometry,
        }

        /// <summary>
        /// Column name.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Column data type.
        /// </summary>
        public ColumnType DataType { get; }

        /// <summary>
        /// Can the value of the column in a row be <see langword="null"/>.
        /// </summary>
        public bool CanBeNull { get; }

        /// <summary>
        /// Is the column used in a create operation.
        /// </summary>
        public bool UsedInCreate { get; }

        /// <summary>
        /// Is the column used in the WHERE expression in a read operation.
        /// </summary>
        public bool UsedInRead { get; }

        /// <summary>
        /// Is the column used in the WHERE expression in an update operation.
        /// The columns with <see cref="UsedInUpdate"/> set to <see langword="false"/>
        /// will be used as the columns being updated.
        /// </summary>
        public bool UsedInUpdate { get; }

        /// <summary>
        /// Is the column used in the WHERE expression in a delete operation.
        /// </summary>
        public bool UsedInDelete { get; }

        /// <summary>
        /// Additional properties for SQL code generation, like ids, increments, etc.
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties { get; }

        public ColumnSchema(
            string title, ColumnType dataType, IDictionary<string, string>? properties,
            bool? canBeNull,
            bool usedInCreate = false, bool usedInRead = false,
            bool usedInUpdate = false, bool usedInDelete = false)
        {
            Title = title;
            DataType = dataType;
            Properties = new ReadOnlyDictionary<string, string>(properties == null ? new Dictionary<string, string>() : properties!);
            CanBeNull = canBeNull ?? true;
            UsedInCreate = usedInCreate;
            UsedInRead = usedInRead;
            UsedInUpdate = usedInUpdate;
            UsedInDelete = usedInDelete;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Column {Title} {DataType}");
            if (Properties.Any()) sb.AppendJoin('&', Properties.Select(x => $"{x.Key}={x.Value}"));
            if (CanBeNull) sb.Append("?");
            sb.Append(" ");
            if (UsedInCreate) sb.Append("C");
            if (UsedInRead) sb.Append("R");
            if (UsedInUpdate) sb.Append("U");
            if (UsedInDelete) sb.Append("D");
            return sb.ToString();
        }

        public static class PropertyKeys
        {
            /// <summary>
            /// Type of geometry primitive, e.g. Point, Polygon, MultiLine.
            /// </summary>
            public const string GeometryType = nameof(GeometryType);

            /// <summary>
            /// SRID of the Spatial Reference System of the column.
            /// </summary>
            public const string SpatialRefSys = nameof(SpatialRefSys);

            public static string[] GeometryPropertyKeys = new string[]
            {
                GeometryType,
                SpatialRefSys,
            };
        }
    }
}
