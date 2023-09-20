using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Services.Agents
{
    public interface IAgentService
    {
        ValueTask<Result<IReadOnlyList<AgentInfoModel>>> GetAgents(int planetoidId, CancellationToken token);

        ValueTask<Result<int>> SetAgents(IReadOnlyList<AgentInfoModel> agents, CancellationToken token);

        ValueTask<Result<bool>> ClearAgents(int planetoidId, CancellationToken token);

        ValueTask<Result<ValidationResult>> ValidateAgentSettings(AgentInfoModel agentInfo, CancellationToken token);
    }
}
