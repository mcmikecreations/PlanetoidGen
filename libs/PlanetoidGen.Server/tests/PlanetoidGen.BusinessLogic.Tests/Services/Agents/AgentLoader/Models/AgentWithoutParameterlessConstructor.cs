using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.BusinessLogic.Tests.Services.Agents.AgentLoader.Models
{
    public class AgentWithoutParameterlessConstructor : ITypedAgent<TestAgentSettings>
    {
        private readonly int _dummy;

        public AgentWithoutParameterlessConstructor(int dummy)
        {
            _dummy = dummy;
        }

        public string Title => $"{nameof(AgentLoaderServiceTests)}.{nameof(AgentWithoutParameterlessConstructor)}";

        public string Description => string.Empty;

        public bool IsVisibleToClient => true;

        public ValueTask<Result> Execute(GenerationJobMessage job, CancellationToken cancellationToken)
        {
            return new ValueTask<Result>(Result.CreateSuccess());
        }

        public TestAgentSettings GetTypedDefaultSettings()
        {
            return new TestAgentSettings();
        }

        public ValueTask<string> GetDefaultSettings()
        {
            return GetTypedDefaultSettings().Serialize();
        }

        public ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies(int z) => throw new NotImplementedException();

        public ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies() => throw new NotImplementedException();

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs(int z) => throw new NotImplementedException();

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs() => throw new NotImplementedException();

        public ValueTask<Result> Initialize(string settings, IServiceProvider serviceProvider)
        {
            return new ValueTask<Result>(Result.CreateSuccess());
        }
    }
}
