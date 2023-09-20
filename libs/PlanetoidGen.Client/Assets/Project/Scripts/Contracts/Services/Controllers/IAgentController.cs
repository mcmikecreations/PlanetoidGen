using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Contracts.Services.Controllers
{
    public interface IAgentController
    {
        Task<bool> ClearAgents(int planetoidId, CancellationToken token = default);
        Task<IEnumerable<AgentInfoModel>> GetAgents(int planetoidId, CancellationToken token = default);
        Task<IEnumerable<PlanetoidGen.Contracts.Models.Agents.AgentImplementationModel>> GetAllAgentImplementations(CancellationToken token = default);
        Task<int> SetAgents(int planetoidId, IEnumerable<AgentInfoModel> agents, CancellationToken token = default);
    }
}
