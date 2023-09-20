using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Services.Agents
{
    public interface IAgent
    {
        /// <summary>
        /// Agent title.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Agent description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// A flag that indicates whether client should display this agent for user selection.
        /// </summary>
        public bool IsVisibleToClient { get; }

        /// <summary>
        /// Get list of dependencies to generate for a single tile.
        /// The same tile is added automatically elsewhere, so this
        /// method only returns neighboring tiles.
        /// </summary>
        /// <param name="z">Zoom level to get dependencies for.</param>
        /// <returns>List of dependencies excluding current tile.</returns>
        ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies(int z);

        /// <summary>
        /// Get list of dependencies to generate for a single tile independent of zoom level.
        /// The same tile is added automatically elsewhere, so this
        /// method only returns neighboring tiles.
        /// </summary>
        /// <returns>List of dependencies excluding current tile.</returns>
        ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies();

        /// <summary>
        /// Get all outputs that the agent produces.
        /// </summary>
        /// <param name="z">Zoom level to get results for.</param>
        /// <returns>List of outputs.</returns>
        ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs(int z);

        /// <summary>
        /// Get all outputs that the agent produces independent of zoom level.
        /// </summary>
        /// <returns>List of outputs.</returns>
        ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs();

        /// <summary>
        /// Gets the default agent's settings as a serialized string.
        /// </summary>
        /// <returns>Serialized string</returns>
        ValueTask<string> GetDefaultSettings();

        ValueTask<Result> Initialize(string settings, IServiceProvider serviceProvider);

        ValueTask<Result> Execute(GenerationJobMessage job, CancellationToken token);
    }
}
