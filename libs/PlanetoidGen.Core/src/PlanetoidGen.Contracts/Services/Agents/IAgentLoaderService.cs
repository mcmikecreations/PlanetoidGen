using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Generic;
using System.Collections.Generic;

namespace PlanetoidGen.Contracts.Services.Agents
{
    public interface IAgentLoaderService
    {
        Result<IEnumerable<IAgent>> GetAllAgents();

        Result<IAgent> GetAgent(string title);

        Result<AgentTypeInfo> GetAgentTypeInfo(string title);

        Result<AgentTypeInfo> GetAgentTypeInfo(IAgent agent);
    }
}
