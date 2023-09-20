using Insight.Database;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Dynamic;
using PlanetoidGen.DataAccess.Helpers.Extensions;
using System.Linq;
using System.Text;

namespace PlanetoidGen.DataAccess.Repositories.Dynamic
{
    public class UniversalTableProcedureGenerator : ITableProcedureGenerator
    {
        private readonly TableSchema _schema;

        public UniversalTableProcedureGenerator(TableSchema schema)
        {
            _schema = schema;
        }

        public TableSchema Schema => _schema;

        public string RowCreateName()
        {
            return $"{_schema.Schema}.{_schema.Title}Create";
        }

        public string RowCreateProcedure()
        {
            var columns = _schema.Columns.Where(x => x.UsedInCreate);

            var sb = new StringBuilder();

            sb.Append($"CREATE OR REPLACE FUNCTION {RowCreateName()}(\n");

            sb.AppendJoin(",\n", columns.Select(x => $"d{x.Title} {x.GetTypeName()}"));

            sb.Append($@")
    RETURNS {_schema.Schema}.{_schema.Title}
    LANGUAGE 'sql'
AS $BODY$
    INSERT INTO {_schema.Schema}.{_schema.Title}(");

            sb.AppendJoin(", ", columns.Select(x => x.Title));

            sb.Append(")\nVALUES (");

            sb.AppendJoin(", ", columns.Select(x => $"d{x.Title}"));

            sb.Append(")\nRETURNING *;\n$BODY$;");

            return sb.ToString();
        }

        public string RowDeleteName()
        {
            return $"{_schema.Schema}.{_schema.Title}Delete";
        }

        public string RowDeleteProcedure()
        {
            var columns = _schema.Columns.Where(x => x.UsedInDelete);
            var sb = new StringBuilder();

            sb.Append($"CREATE OR REPLACE FUNCTION {RowDeleteName()}(\n");

            sb.AppendJoin(",\n", columns.Select(x => $"d{x.Title} {x.GetTypeName()}"));

            sb.Append($@")
    RETURNS SETOF {_schema.Schema}.{_schema.Title}
    LANGUAGE 'sql'
AS $BODY$
    DELETE FROM {_schema.Schema}.{_schema.Title} WHERE
");

            sb.AppendJoin(" AND ", columns.Select(x => $"{x.Title} = d{x.Title}"));

            sb.Append("\nRETURNING *;\n$BODY$;");

            return sb.ToString();
        }

        public string RowReadName()
        {
            return $"{_schema.Schema}.{_schema.Title}Read";
        }

        public string RowReadProcedure()
        {
            var columns = _schema.Columns.Where(x => x.UsedInRead);
            var sb = new StringBuilder();

            sb.Append($"CREATE OR REPLACE FUNCTION {RowReadName()}(\n");

            sb.AppendJoin(",\n", columns.Select(x => $"d{x.Title} {x.GetTypeName()}"));

            sb.Append($@")
    RETURNS SETOF {_schema.Schema}.{_schema.Title}
    LANGUAGE 'sql'
AS $BODY$
    SELECT * FROM {_schema.Schema}.{_schema.Title} WHERE
");

            sb.AppendJoin(" AND ", columns.Select(x => $"{x.Title} = d{x.Title}"));

            sb.Append(";\n$BODY$;");

            return sb.ToString();
        }

        public string RowUpdateName()
        {
            return $"{_schema.Schema}.{_schema.Title}Update";
        }

        public string RowUpdateProcedure()
        {
            var searchColumns = _schema.Columns.Where(x => x.UsedInUpdate).ToList();
            var updateColumns = _schema.Columns.Except(searchColumns);
            var sb = new StringBuilder();

            sb.Append($"CREATE OR REPLACE FUNCTION {RowUpdateName()}(\n");

            sb.AppendJoin(",\n", _schema.Columns.Select(x => $"d{x.Title} {x.GetTypeName()}"));

            sb.Append($@")
    RETURNS SETOF {_schema.Schema}.{_schema.Title}
    LANGUAGE 'sql'
AS $BODY$
    UPDATE {_schema.Schema}.{_schema.Title} SET
");

            sb.AppendJoin(", ", updateColumns.Select(x => $"{x.Title} = d{x.Title}"));

            sb.Append("\nWHERE\n");

            sb.AppendJoin(" AND ", searchColumns.Select(x => $"{x.Title} = d{x.Title}"));

            sb.Append("\nRETURNING *;\n$BODY$;");

            return sb.ToString();
        }

        public string TableCreateStatement()
        {
            var columns = _schema.Columns;
            var sb = new StringBuilder();

            sb.AppendLine($"CREATE SCHEMA IF NOT EXISTS {_schema.Schema};");

            sb.AppendLine($"CREATE TABLE IF NOT EXISTS {_schema.Schema}.{_schema.Title}(");

            sb.AppendJoin(",\n", columns.Select(x => x.CreateTableRowSql()));

            AppendIndicesTable(sb);

            sb.Append("\n);");

            AppendIndicesExternal(sb);

            return sb.ToString();
        }

        private void AppendIndicesTable(StringBuilder sb)
        {
            var keyIndex = _schema.Indices.FirstOrDefault(x => x.IndexType == IndexSchema.IndexKind.PrimaryKey);
            var uniqueIndices = _schema.Indices.Where(x => x.IndexType == IndexSchema.IndexKind.Unique).ToList();

            if (keyIndex != null)
            {
                sb.AppendLine(",")
                    .Append($"CONSTRAINT {_schema.Schema}_{_schema.Title}_pk PRIMARY KEY (")
                    .AppendJoin(", ", keyIndex.IndexColumnNames)
                    .Append(")");

                if (keyIndex.IncludeColumnNames.Any())
                {
                    sb.Append(" INCLUDE (")
                        .AppendJoin(", ", keyIndex.IncludeColumnNames)
                        .Append(")");
                }
            }

            foreach (var index in uniqueIndices)
            {
                sb.AppendLine(",")
                    .Append($"CONSTRAINT {_schema.Schema}_{_schema.Title}_")
                    .AppendJoin("_", index.IndexColumnNames)
                    .Append("_u UNIQUE (")
                    .AppendJoin(", ", index.IndexColumnNames)
                    .Append(")");

                if (index.IncludeColumnNames.Any())
                {
                    sb.Append(" INCLUDE (")
                        .AppendJoin(", ", index.IncludeColumnNames)
                        .Append(")");
                }
            }
        }
        private void AppendIndicesExternal(StringBuilder sb)
        {
            var indices = _schema.Indices
                .Where(x =>
                    x.IndexType != IndexSchema.IndexKind.Unique &&
                    x.IndexType != IndexSchema.IndexKind.PrimaryKey)
                .ToList();

            foreach (var index in indices)
            {
                sb
                    .Append($"\nCREATE INDEX IF NOT EXISTS {_schema.Schema}_{_schema.Title}_")
                    .AppendJoin("_", index.IndexColumnNames)
                    .Append($"_{index.IndexType} ON {_schema.Schema}.{_schema.Title}");

                switch (index.IndexType)
                {
                    case IndexSchema.IndexKind.Duplicated:
                        break;
                    case IndexSchema.IndexKind.Gist:
                        sb.Append(" USING gist");
                        break;
                }

                sb.Append(" (")
                    .AppendJoin(", ", index.IndexColumnNames)
                    .Append(")");

                if (index.IncludeColumnNames.Any())
                {
                    sb.Append(" INCLUDE (")
                        .AppendJoin(", ", index.IncludeColumnNames)
                        .Append(")");
                }

                sb.Append(";");
            }
        }

        public string TableDeleteStatement()
        {
            var columnsCreate = _schema.Columns.Where(x => x.UsedInCreate);
            var columnsRead = _schema.Columns.Where(x => x.UsedInRead);
            var columnsUpdate = _schema.Columns;
            var columnsDelete = _schema.Columns.Where(x => x.UsedInDelete);

            var sb = new StringBuilder();

            sb
                .AppendLine($"DROP TABLE IF EXISTS {_schema.Schema}.{_schema.Title} CASCADE;")
                .Append("DROP FUNCTION IF EXISTS ")
                    .Append(RowCreateName())
                    .Append("(")
                    .AppendJoin(',', columnsCreate.Select(x => x.GetTypeName()))
                    .AppendLine(");")
                .Append("DROP PROCEDURE IF EXISTS ")
                    .Append(RowCreateMultipleName())
                    .Append("(")
                    .AppendJoin(',', columnsCreate.Select(x => x.GetTypeName()))
                    .AppendLine(");")
                .Append("DROP FUNCTION IF EXISTS ")
                    .Append(RowReadName())
                    .Append("(")
                    .AppendJoin(',', columnsRead.Select(x => x.GetTypeName()))
                    .AppendLine(");")
                .Append("DROP FUNCTION IF EXISTS ")
                    .Append(RowReadMultipleByBoundingBoxName())
                    .AppendLine("(geometry);")
                .Append("DROP FUNCTION IF EXISTS ")
                    .Append(RowUpdateName())
                    .Append("(")
                    .AppendJoin(',', columnsUpdate.Select(x => x.GetTypeName()))
                    .AppendLine(");")
                .Append("DROP PROCEDURE IF EXISTS ")
                    .Append(RowUpdateMultipleName())
                    .Append("(")
                    .AppendJoin(',', columnsUpdate.Select(x => x.GetTypeName()))
                    .AppendLine(");")
                .Append("DROP FUNCTION IF EXISTS ")
                    .Append(RowDeleteName())
                    .Append("(")
                    .AppendJoin(',', columnsDelete.Select(x => x.GetTypeName()))
                    .AppendLine(");")
                .Append("DROP PROCEDURE IF EXISTS ")
                    .Append(RowDeleteMultipleName())
                    .Append("(")
                    .AppendJoin(',', columnsCreate.Select(x => x.GetTypeName()))
                    .AppendLine(");");

            return sb.ToString();
        }

        public string RowCreateMultipleProcedure()
        {
            var columns = _schema.Columns.Where(x => x.UsedInCreate);

            var sb = new StringBuilder();

            sb.Append($"CREATE OR REPLACE PROCEDURE {RowCreateMultipleName()}(\n");

            sb.AppendJoin(",\n", columns.Select(x => $"INOUT {x.Title} {x.GetTypeName()}"));

            sb.Append($@")
    LANGUAGE 'sql'
AS $BODY$
    INSERT INTO {_schema.Schema}.{_schema.Title}(");

            sb.AppendJoin(", ", columns.Select(x => x.Title));

            sb.Append(")\nVALUES (");

            sb.AppendJoin(", ", columns.Select(x => $"{x.Title}"));

            sb.AppendLine(")");

            sb.AppendLine($"ON CONFLICT ON CONSTRAINT {_schema.Schema}_{_schema.Title}_pk DO UPDATE SET");

            sb.AppendJoin(", ", columns.Select(x => $"{x.Title} = EXCLUDED.{x.Title}"));

            sb.Append("\nRETURNING *;\n$BODY$;");

            return sb.ToString();
        }

        public string RowCreateMultipleName()
        {
            return $"{_schema.Schema}.{_schema.Title}CreateMultipleUnsafe";
        }

        public string RowReadMultipleByBoundingBoxProcedure()
        {
            var sb = new StringBuilder();
            sb.Append($"CREATE OR REPLACE FUNCTION {RowReadMultipleByBoundingBoxName()}(\n");
            sb.Append($@"dbbox geometry)
    RETURNS SETOF {_schema.Schema}.{_schema.Title}
    LANGUAGE 'sql'
AS $BODY$
    SELECT * FROM 
            {_schema.Schema}.{_schema.Title} WHERE
    ");

            sb.AppendJoin(" AND ", _schema.Columns
                .Where(x => x.DataType == ColumnSchema.ColumnType.Geometry)
                .Select(x => x.CanBeNull
                        ? $"({x.Title} IS NOT NULL) AND {x.Title} && dbbox"
                        : $"{x.Title} && dbbox"));

            sb.Append(";\n$BODY$;");

            return sb.ToString();
        }

        public string RowReadMultipleByBoundingBoxName()
        {
            return $"{_schema.Schema}.{_schema.Title}ReadMultipleByBoundingBox";
        }

        public string RowUpdateMultipleProcedure()
        {
            var searchColumns = _schema.Columns.Where(x => x.UsedInUpdate).ToList();
            var updateColumns = _schema.Columns.Except(searchColumns);
            var sb = new StringBuilder();

            sb.Append($"CREATE OR REPLACE PROCEDURE {RowUpdateMultipleName()}(\n");

            sb.AppendJoin(",\n", _schema.Columns.Select(x => $"IN d{x.Title} {x.GetTypeName()}"));

            sb.Append($@")
    LANGUAGE 'sql'
AS $BODY$
    UPDATE {_schema.Schema}.{_schema.Title} SET
");

            sb.AppendJoin(", ", updateColumns.Select(x => $"{x.Title} = d{x.Title}"));

            sb.Append("\nWHERE\n");

            sb.AppendJoin(" AND ", searchColumns.Select(x => $"{x.Title} = d{x.Title}"));

            sb.Append(";\n$BODY$;");

            return sb.ToString();
        }

        public string RowUpdateMultipleName()
        {
            return $"{_schema.Schema}.{_schema.Title}UpdateMultipleUnsafe";
        }

        public string RowDeleteMultipleProcedure()
        {
            var paramColumns = _schema.Columns.Where(x => x.UsedInCreate);
            var deleteColumns = _schema.Columns.Where(x => x.UsedInDelete);

            var keyColumns = _schema.Indices
                .Where(x => x.IndexType == IndexSchema.IndexKind.PrimaryKey || x.IndexType == IndexSchema.IndexKind.Unique)
                .Select(x => x.IndexColumnNames);
            var keyColumnQuery = keyColumns.Select(k => $"({string.Join(" AND ", k.Select(x => $"{x} = d{x}"))})");

            var sb = new StringBuilder();

            sb.Append($"CREATE OR REPLACE PROCEDURE {RowDeleteMultipleName()}(\n");

            sb.AppendJoin(",\n", paramColumns.Select(x => $"IN d{x.Title} {x.GetTypeName()}"));

            sb.Append($@")
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    IF EXISTS (SELECT * FROM {_schema.Schema}.{_schema.Title} WHERE
");

            sb.AppendJoin(" OR\n", keyColumnQuery);

            sb.Append(@"
    OR ("
            );

            sb.AppendJoin(" AND ", deleteColumns.Select(x => $"{x.Title} = d{x.Title}"));

            sb.Append(@$"
)) THEN
        DELETE FROM {_schema.Schema}.{_schema.Title} WHERE
");

            sb.AppendJoin(" OR\n", keyColumnQuery);

            sb.Append(@"
    OR ("
            );

            sb.AppendJoin(" AND ", deleteColumns.Select(x => $"{x.Title} = d{x.Title}"));

            sb.Append($@");
    END IF;
END;
$BODY$;");

            return sb.ToString();
        }

        public string RowDeleteMultipleName()
        {
            return $"{_schema.Schema}.{_schema.Title}DeleteMultipleUnsafe";
        }
    }
}
