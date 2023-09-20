using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetoidGen.Contracts.Models.Repositories.Dynamic
{
    public class IndexSchema
    {
        public enum IndexKind
        {
            Duplicated,
            Unique,
            PrimaryKey,
            Gist,
        }

        public IndexKind IndexType { get; }

        /// <summary>
        /// Names of columns on which the index is built.
        /// </summary>
        public IReadOnlyList<string> IndexColumnNames { get; }

        /// <summary>
        /// Names of columns which are included in the index.
        /// </summary>
        public IReadOnlyList<string> IncludeColumnNames { get; }

        public IndexSchema(IndexKind indexType, IList<string> indexColumnNames, IList<string>? includeColumnNames)
        {
            if (indexColumnNames == null || indexColumnNames.Count == 0)
            {
                throw new ArgumentNullException(nameof(indexColumnNames));
            }

            IndexType = indexType;
            IndexColumnNames = indexColumnNames?.ToList() ?? new List<string>();
            IncludeColumnNames = includeColumnNames?.ToList() ?? new List<string>();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append($"Index {IndexType}");
            sb.Append(" on {");
            sb.AppendJoin(", ", IndexColumnNames);
            sb.Append("}");
            sb.Append(" including {");
            sb.AppendJoin(", ", IncludeColumnNames);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
