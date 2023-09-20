using System;

namespace PlanetoidGen.Contracts.Models.Services.Agents
{
    public class AgentLoaderServiceOptions
    {
        public AgentLoaderServiceOptions()
        {
            AssembliesToLoad = Array.Empty<string>();
        }

        public string[] AssembliesToLoad { get; set; }

        public static string DefaultConfigurationSectionName = nameof(AgentLoaderServiceOptions);
    }
}
