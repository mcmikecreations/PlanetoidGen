using PlanetoidGen.API;
using PlanetoidGen.Client.Contracts.Services.Controllers;
using PlanetoidGen.Client.Platform.Desktop.Services.Context.Abstractions;
using PlanetoidGen.Contracts.Models.Reflection;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Platform.Desktop.Services.Controllers
{
    public class AgentController : ControllerBase, IAgentController
    {
        private readonly Agent.AgentClient _client;

        public AgentController(IConnectionContext context)
        {
            _client = new Agent.AgentClient(context.Channel);
        }

        public async Task<IEnumerable<AgentInfoModel>> GetAgents(int planetoidId, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.GetAgentsAsync(new QueryIdModel
                {
                    Id = planetoidId
                }, cancellationToken: token);

                return result.Agents.Select(a => new AgentInfoModel(a.PlanetoidId, a.IndexId, a.Title, a.Settings, a.ShouldRerunIfLast));
            });
        }

        public async Task<IEnumerable<PlanetoidGen.Contracts.Models.Agents.AgentImplementationModel>> GetAllAgentImplementations(CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var agentImplemenntations = await _client.GetAllAgentImplementationsAsync(new EmptyModel(), cancellationToken: token);

                return agentImplemenntations.Agents?.Select(a =>
                {
                    return new PlanetoidGen.Contracts.Models.Agents.AgentImplementationModel
                    {
                        Title = a.Title,
                        IsVisibleToClient = a.IsVisibleToClient,
                        DefaultSettings = a.DefaultSettings,
                        Description = a.Description,
                        Dependencies = a.Dependencies?.Select(d =>
                            new PlanetoidGen.Contracts.Models.Agents.AgentDependencyModel(
                                (Domain.Enums.RelativeTileDirectionType)d.Direction,
                                new DataTypeInfoModel(d.DataType.Title, d.DataType.IsRaster))),
                        Outputs = a.Outputs?.Select(o => new DataTypeInfoModel(o.Title, o.IsRaster)),
                        SettingsAttributes = a.SettingsAtributes?.Select(s => new ValidatableTypeMetaData
                        {
                            Description = s.Description,
                            Name = s.Name,
                            IsNullable = s.IsNullable,
                            TypeName = s.TypeName,
                            ValidationAttributes = s.ValidationAttributes?.Select(v => new ValidationAttributeInfo
                            {
                                Name = v.Name,
                                PropertiesInfos = v.Properties?.Select(p => new ValidationAttributePropertyInfo
                                {
                                    Name = p.Name,
                                    Value = p.Value
                                }).ToArray()
                            }).ToList()
                        })
                    };
                });
            });
        }

        public async Task<int> SetAgents(int planetoidId, IEnumerable<AgentInfoModel> agents, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var model = new SetAgentsModel
                {
                    PlanetoidId = planetoidId
                };

                model.Agents.AddRange(agents.Select(a => new SetAgentModel
                {
                    Title = a.Title,
                    Settings = a.Settings,
                    ShouldRerunIfLast = a.ShouldRerunIfLast,
                }));

                var result = await _client.SetAgentsAsync(model, cancellationToken: token);

                return result.Count;
            });
        }

        public async Task<bool> ClearAgents(int planetoidId, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.ClearAgentsAsync(new QueryIdModel
                { 
                    Id = planetoidId
                }, cancellationToken: token);

                return result.Success;
            });
        }
    }
}
