using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Info
{
    public interface IAgentInfoRepository : INamedRepository<AgentInfoModel>
    {
        /// <summary>
        /// Insert agent list into the repository.
        /// </summary>
        /// <param name="names">Ordered set of unique agent names.</param>
        /// <returns>Number of agents in the repository.</returns>
        ValueTask<Result<int>> InsertAgents(IEnumerable<AgentInfoModel> agents, CancellationToken token);

        /// <summary>
        /// Get all agents from the repository.
        /// </summary>
        /// <returns>Set of available agents.</returns>
        ValueTask<Result<IReadOnlyList<AgentInfoModel>>> GetAgents(int planetoidId, CancellationToken token);

        /// <summary>
        /// Get agent by index from the repository.
        /// </summary>
        /// <returns>Agent.</returns>
        ValueTask<Result<AgentInfoModel>> GetAgentByIndex(int planetoidId, int agentIndex, CancellationToken token);

        /// <summary>
        /// Clears all agents from the repository. Also clears all jobs that have an agent specified.
        /// </summary>
        /// <returns>True if the agents have been cleared, false otherwise.</returns>
        ValueTask<Result<bool>> ClearAgents(int planetoidId, CancellationToken token);
    }
}
