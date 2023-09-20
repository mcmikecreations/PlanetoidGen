using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetoidGen.Contracts.Models.Repositories.Dynamic
{
    /// <summary>
    /// A description of a dynamic table. 
    /// </summary>
    public class TableSchema
    {
        /// <summary>
        /// Namespace of the table.
        /// </summary>
        public string Schema { get; }

        /// <summary>
        /// Table name.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// The column descriptions of the table. They are guaranteed to be in the correct order.
        /// By design, tables with vector data always use global coordinates,
        /// while raster data is per-tile.
        /// </summary>
        public IReadOnlyList<ColumnSchema> Columns { get; }

        public IReadOnlyList<IndexSchema> Indices { get; }

        public TableSchema(string schema, string title, IList<ColumnSchema>? columns, IList<IndexSchema>? indices)
        {
            Schema = schema;
            Title = title;
            Columns = columns?.ToList() ?? new List<ColumnSchema>();
            Indices = indices?.ToList() ?? new List<IndexSchema>();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Table {Schema}.{Title}(");
            sb.AppendJoin(",\n", Columns);
            sb.AppendLine(")\nWith Indices (");
            sb.AppendJoin(",\n", Indices);
            sb.AppendLine(")");
            return sb.ToString();
        }
    }
}
