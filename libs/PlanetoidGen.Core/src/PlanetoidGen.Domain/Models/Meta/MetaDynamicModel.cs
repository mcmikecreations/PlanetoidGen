using System.Collections.Generic;
using System.Linq;

namespace PlanetoidGen.Domain.Models.Meta
{
    public class MetaDynamicModel
    {
        public int Id { get; }

        /// <summary>
        /// Planetoid that this table relates to.
        /// Used for cleaning purposes later.
        /// </summary>
        public int PlanetoidId { get; }

        /// <summary>
        /// Schema containing the table.
        /// </summary>
        public string Schema { get; }

        /// <summary>
        /// Dynamic table name.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Columns in the form of "≪type1≫ ≪name1≫ ≪type2≫ ≪name2≫".
        /// </summary>
        public string Columns { get; }

        /// <summary>
        /// List of column types, useful for fully-qualified names in e.g. DROP.
        /// </summary>
        public IReadOnlyList<string> ColumnTypes => Columns
            .Split(' ')
            .Where((v, i) => i % 2 == 0)
            .ToList();

        /// <summary>
        /// List of column names, useful for column specification in e.g. INSERT.
        /// </summary>
        public IReadOnlyList<string> ColumnNames => Columns
            .Split(' ')
            .Where((v, i) => i % 2 == 1)
            .ToList();

        public MetaDynamicModel(int id, int planetoidId, string schema, string title, string columns)
        {
            Id = id;
            PlanetoidId = planetoidId;
            Schema = schema;
            Title = title;
            Columns = columns;
        }

        public override string ToString()
        {
            return $"DynamicMetaModel(Id={Id}, PlanetoidId={PlanetoidId}, Declaration={Schema}.{Title}({Columns}))";
        }
    }
}
