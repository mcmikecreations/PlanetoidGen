using Grpc.Core;
using PlanetoidGen.BusinessLogic.Helpers;
using PlanetoidGen.Contracts.Models.Reflection;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Domain.Models.Info;

namespace PlanetoidGen.API.Controllers
{
    public class AgentController : Agent.AgentBase
    {
        private readonly IAgentService _agentService;
        private readonly IAgentLoaderService _agentLoaderService;
        private readonly ILogger<AgentController> _logger;

        public AgentController(
            IAgentService agentService,
            IAgentLoaderService agentLoaderService,
            ILogger<AgentController> logger)
        {
            _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
            _agentLoaderService = agentLoaderService ?? throw new ArgumentNullException(nameof(agentLoaderService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<ItemsCountModel> SetAgents(SetAgentsModel request, ServerCallContext context)
        {
            var agents = request.Agents
                .Select(a => new AgentInfoModel(request.PlanetoidId, default, a.Title, a.Settings, a.ShouldRerunIfLast))
                .ToList();

            await ValidateAgentSettings(context, agents);

            var result = await _agentService.SetAgents(agents, context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Set Agents error: {error}", result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            return new ItemsCountModel
            {
                Count = result.Data,
            };
        }

        public override async Task<AgentArrayModel> GetAgents(QueryIdModel request, ServerCallContext context)
        {
            _logger.LogInformation("Getting agents for planetoid {id}.", request.Id);

            var result = await _agentService.GetAgents(request.Id, context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Get Agents error: {error}", result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            var response = new AgentArrayModel();

            foreach (var agent in result.Data)
            {
                response.Agents.Add(new AgentModel
                {
                    PlanetoidId = agent.PlanetoidId,
                    IndexId = agent.IndexId,
                    Title = agent.Title,
                    Settings = agent.Settings,
                    ShouldRerunIfLast = agent.ShouldRerunIfLast
                });
            }

            return response;
        }

        public override async Task<SuccessModel> ClearAgents(QueryIdModel request, ServerCallContext context)
        {
            _logger.LogInformation("Getting agents for planetoid {id}.", request.Id);

            var result = await _agentService.ClearAgents(request.Id, context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Clear Agents error: {error}", result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            return new SuccessModel
            {
                Success = result.Data
            };
        }

        public override async Task<AgentImplementationArrayModel> GetAllAgentImplementations(EmptyModel request, ServerCallContext context)
        {
            var result = _agentLoaderService.GetAllAgents();

            if (!result.Success)
            {
                _logger.LogError("Get All Agents error: {error}", result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            var response = new AgentImplementationArrayModel();

            foreach (var agent in result.Data)
            {
                response.Agents.Add(await GetAgentImplementationModel(agent));
            }

            return response;
        }

        private async Task ValidateAgentSettings(ServerCallContext context, List<AgentInfoModel> agents)
        {
            var agentsWithInvalidSettings = new List<string>();

            foreach (var agent in agents)
            {
                var validationResult = await _agentService.ValidateAgentSettings(agent, context.CancellationToken);

                if (!validationResult.Success)
                {
                    _logger.LogError("Set Agents error: {error}", validationResult.ErrorMessage!.ToString());
                    throw new RpcException(new Status(StatusCode.Internal, validationResult.ErrorMessage!.ToString()));
                }

                if (!validationResult.Data.IsValid)
                {
                    agentsWithInvalidSettings.Add(agent.Title);
                }
            }

            if (agentsWithInvalidSettings.Any())
            {
                throw new RpcException(new Status(
                    StatusCode.InvalidArgument,
                    $"Agents with invalid settings: [{string.Join(", ", agentsWithInvalidSettings)}]"));
            }
        }

        private async Task<AgentImplementationModel> GetAgentImplementationModel(IAgent agent)
        {
            var agentImplementationInfo = new AgentImplementationModel
            {
                Title = agent.Title,
                Description = agent.Description,
                IsVisibleToClient = agent.IsVisibleToClient,
                DefaultSettings = await agent.GetDefaultSettings(),
            };

            agentImplementationInfo.Outputs.AddRange((await agent.GetOutputs()).Select(o => new DataTypeModel
            {
                Title = o.Title,
                IsRaster = o.IsRaster,
            }));

            agentImplementationInfo.Dependencies.AddRange((await agent.GetDependencies()).Select(d => new AgentDependencyModel
            {
                DataType = new DataTypeModel
                {
                    Title = d.DataType.Title,
                    IsRaster = d.DataType.IsRaster,
                },
                Direction = (RelativeTileDirectionType)d.Direction,
            }));

            var agentInfoResult = _agentLoaderService.GetAgentTypeInfo(agent);

            if (!agentInfoResult.Success)
            {
                _logger.LogError("Get All Agents error: {error}", agentInfoResult.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, agentInfoResult.ErrorMessage!.ToString()));
            }

            agentImplementationInfo.SettingsAtributes.AddRange(agentInfoResult.Data.SettingsType
                .GetValidatableTypeAttributesMetaData()
                .Where(attr => attr?.Name != null && attr?.TypeName != null)
                .Select(attr =>
                {
                    var settingsAttribute = new SettingsAttributeModel
                    {
                        Name = attr.Name,
                        TypeName = attr.TypeName,
                        Description = attr.Description ?? string.Empty,
                        IsNullable = attr.IsNullable,
                    };

                    settingsAttribute.ValidationAttributes.AddRange((attr.ValidationAttributes ?? Array.Empty<ValidationAttributeInfo>())
                        .Where(va => va?.Name != null)
                        .Select(va =>
                        {
                            var validationAttribute = new ValidationAttributeModel
                            {
                                Name = va.Name,
                            };

                            validationAttribute.Properties.AddRange((va.PropertiesInfos ?? Array.Empty<ValidationAttributePropertyInfo>())
                                .Where(pi => pi?.Name != null && pi?.Value != null)
                                .Select(pi => new ValidationAttributePropertyModel
                                {
                                    Name = pi.Name,
                                    Value = pi.Value ?? string.Empty,
                                }));

                            return validationAttribute;
                        }));

                    return settingsAttribute;
                }));

            return agentImplementationInfo;
        }
    }
}
