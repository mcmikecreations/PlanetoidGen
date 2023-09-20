using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Services.Agents;
using System;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Models.Agents
{
    public class AgentTypeInfo
    {
        public AgentTypeInfo(
            Type type,
            Type settingsType,
            Func<IAgent> defaultCreator,
            Func<string, ValueTask<Result<ValidationResult>>> settingsValidator)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            SettingsType = settingsType ?? throw new ArgumentNullException(nameof(settingsType));
            DefaultCreator = defaultCreator ?? throw new ArgumentNullException(nameof(defaultCreator));
            SettingsValidator = settingsValidator ?? throw new ArgumentNullException(nameof(settingsValidator));
        }

        public Type Type { get; }

        public Type SettingsType { get; }

        public Func<IAgent> DefaultCreator { get; }

        public Func<string, ValueTask<Result<ValidationResult>>> SettingsValidator { get; }
    }
}
