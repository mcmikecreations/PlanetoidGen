using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Services.Meta;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Meta
{
    public interface IMetaProcedureRepository : IStaticRepository
    {
        MetaProcedureOptions Options { get; }

        /// <summary>
        /// Check if a schema exists in the database. Does not use a transaction inside.
        /// </summary>
        /// <param name="name">The exact name of the schema to check.</param>
        /// <returns>True if exists, result error otherwise.</returns>
        ValueTask<Result> SchemaExists(string name, CancellationToken token, IDbConnection? connection = null);

        /// <summary>
        /// Check if an extension is installed in the database. Does not use a transaction inside.
        /// </summary>
        /// <param name="name">The exact name of the extension to check.</param>
        /// <returns>True if installed, result error otherwise.</returns>
        ValueTask<Result> ExtensionExists(string name, CancellationToken token, IDbConnection? connection = null);

        /// <summary>
        /// Check if a table exists in a schema in the database. Does not use a transaction inside.
        /// </summary>
        /// <param name="schemaName">Name of the schema to look for the table in.</param>
        /// <param name="tableName">Name of the table to check.</param>
        /// <returns>True if exists, result error otherwise.</returns>
        ValueTask<Result> TableExists(string schemaName, string tableName, CancellationToken token, IDbConnection? connection = null);

        /// <summary>
        /// Check if a function exists in a schema in the database.
        /// Does not use a transaction inside.
        /// Ignores parameters.
        /// </summary>
        /// <param name="schemaName">Name of the schema to look for the function in.</param>
        /// <param name="functionName">Name of the function to check.</param>
        /// <returns>True if exists, result error otherwise.</returns>
        ValueTask<Result> FunctionNameExists(string schemaName, string functionName, CancellationToken token, IDbConnection? connection = null);

        /// <summary>
        /// Check if a function exists in a schema in the database.
        /// Does not use a transaction inside.
        /// Ignores parameters.
        /// </summary>
        /// <param name="functionName">Name of the function to check, starting with schema.</param>
        /// <returns>True if exists, result error otherwise.</returns>
        ValueTask<Result> FunctionNameExists(string functionName, CancellationToken token, IDbConnection? connection = null);
    }
}
