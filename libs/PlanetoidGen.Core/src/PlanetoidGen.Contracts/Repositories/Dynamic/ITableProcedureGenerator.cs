using PlanetoidGen.Contracts.Models.Repositories.Dynamic;

namespace PlanetoidGen.Contracts.Repositories.Dynamic
{
    /// <summary>
    /// Stored procedure factory.
    /// A single implementation should exist per column schema.
    /// Since these factories can be invoked during startup, the performance impact
    /// of using them should be acceptable.
    /// </summary>
    public interface ITableProcedureGenerator
    {
        /// <summary>
        /// Table description.
        /// </summary>
        TableSchema Schema { get; }

        /// <summary>
        /// Source text of the SQL statement to create a table.
        /// Creates the table only if it was missing before.
        /// </summary>
        string TableCreateStatement();

        /// <summary>
        /// Source text of the SQL statement to delete a table.
        /// </summary>
        string TableDeleteStatement();

        /// <summary>
        /// Source text of the SQL procedure to create a row.
        /// Replaces the old procedure if such existed.
        /// </summary>
        string RowCreateProcedure();

        string RowCreateMultipleProcedure();

        string RowReadProcedure();

        string RowReadMultipleByBoundingBoxProcedure();

        string RowUpdateProcedure();

        string RowUpdateMultipleProcedure();

        string RowDeleteProcedure();

        /// <summary>
        /// Source text of the SQL procedure to
        /// delete models by UsedInDelete and possible constraint matches.
        /// Used in CreateMultiple.
        /// </summary>
        string RowDeleteMultipleProcedure();

        /// <summary>
        /// Name of the procedure to invoke to create a row.
        /// </summary>
        string RowCreateName();

        string RowCreateMultipleName();

        string RowReadName();

        string RowReadMultipleByBoundingBoxName();

        string RowUpdateName();

        string RowUpdateMultipleName();

        string RowDeleteName();

        string RowDeleteMultipleName();
    }
}
