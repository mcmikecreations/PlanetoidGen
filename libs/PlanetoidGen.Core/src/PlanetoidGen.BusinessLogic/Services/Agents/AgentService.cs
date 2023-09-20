using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Repositories.Info;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.BusinessLogic.Services.Agents
{
    public class AgentService : IAgentService
    {
        private readonly IAgentLoaderService _agentLoaderService;
        private readonly IAgentInfoRepository _agentRepository;

        public AgentService(IAgentLoaderService agentLoaderService, IAgentInfoRepository agentRepository)
        {
            _agentLoaderService = agentLoaderService ?? throw new ArgumentNullException(nameof(agentLoaderService));
            _agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
        }

        public async ValueTask<Result<bool>> ClearAgents(int planetoidId, CancellationToken token)
        {
            return await _agentRepository.ClearAgents(planetoidId, token);
        }

        public async ValueTask<Result<IReadOnlyList<AgentInfoModel>>> GetAgents(int planetoidId, CancellationToken token)
        {
            return await _agentRepository.GetAgents(planetoidId, token);
        }

        /// <summary>
        /// Set agents for given planetoids. The agents are automatically cleared inside the repository.
        /// </summary>
        /// <param name="agents"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public ValueTask<Result<int>> SetAgents(IReadOnlyList<AgentInfoModel> agents, CancellationToken token)
        {
            return _agentRepository.InsertAgents(agents, token);
        }

        public async ValueTask<Result<ValidationResult>> ValidateAgentSettings(AgentInfoModel agentInfo, CancellationToken token)
        {
            var getAgentSettingsValidatorResult = _agentLoaderService.GetAgentTypeInfo(agentInfo.Title);

            if (!getAgentSettingsValidatorResult.Success)
            {
                return Result<ValidationResult>.CreateFailure(getAgentSettingsValidatorResult);
            }

            return !getAgentSettingsValidatorResult.Success
                ? Result<ValidationResult>.CreateFailure(getAgentSettingsValidatorResult)
                : await getAgentSettingsValidatorResult.Data.SettingsValidator(agentInfo.Settings);
        }
    }
}
