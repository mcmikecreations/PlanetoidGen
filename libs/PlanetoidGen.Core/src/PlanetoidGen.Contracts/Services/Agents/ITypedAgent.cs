using PlanetoidGen.Contracts.Models.Agents;

namespace PlanetoidGen.Contracts.Services.Agents
{
    public interface ITypedAgent<TSettings> : IAgent
        where TSettings : IAgentSettings<TSettings>
    {
        TSettings GetTypedDefaultSettings();
    }
}
