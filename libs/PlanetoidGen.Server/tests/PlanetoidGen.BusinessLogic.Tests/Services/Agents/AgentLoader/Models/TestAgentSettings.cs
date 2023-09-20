using PlanetoidGen.BusinessLogic.Agents.Models.Agents;
using System.ComponentModel.DataAnnotations;

namespace PlanetoidGen.BusinessLogic.Tests.Services.Agents.AgentLoader.Models
{
    public class TestAgentSettings : BaseAgentSettings<TestAgentSettings>
    {
        [Required]
        public string? RequiredString { get; set; }
    }
}
