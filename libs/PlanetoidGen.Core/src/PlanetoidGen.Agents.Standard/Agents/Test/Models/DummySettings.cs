using PlanetoidGen.BusinessLogic.Agents.Models.Agents;
using System.ComponentModel.DataAnnotations;

namespace PlanetoidGen.Agents.Standard.Agents.Test.Models
{
    public class DummySettings : BaseAgentSettings<DummySettings>
    {
        [Required]
        public string? Test { get; set; }
    }
}
