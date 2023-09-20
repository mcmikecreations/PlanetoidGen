using PlanetoidGen.Contracts.Models.Reflection;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;

namespace PlanetoidGen.Contracts.Models.Agents
{
    public class AgentImplementationModel
    {
        public string? Title { get; set; }
        public string? DefaultSettings { get; set; }
        public string? Description { get; set; }
        public bool IsVisibleToClient { get; set; }
        public IEnumerable<AgentDependencyModel>? Dependencies { get; set; }
        public IEnumerable<DataTypeInfoModel>? Outputs { get; set; }
        public IEnumerable<ValidatableTypeMetaData>? SettingsAttributes { get; set; }
    }
}
