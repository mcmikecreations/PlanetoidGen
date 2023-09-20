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
    public class AgentWithParameterlessConstructor : ITypedAgent<TestAgentSettings>
    {
        public string Title => $"{nameof(AgentLoaderServiceTests)}.{nameof(AgentWithParameterlessConstructor)}";
        public string Description => string.Empty;
        public bool IsVisibleToClient => true;
        public ValueTask<Result> Execute(GenerationJobMessage job, CancellationToken cancellationToken) => throw new NotImplementedException();
        public TestAgentSettings GetTypedDefaultSettings()
        {
            return new TestAgentSettings();
        }
        public ValueTask<string> GetDefaultSettings() => throw new NotImplementedException();
        public ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies(int z) => throw new NotImplementedException();
        public ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies() => throw new NotImplementedException();
        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs(int z) => throw new NotImplementedException();
        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs() => throw new NotImplementedException();
        public ValueTask<Result> Initialize(string settings, IServiceProvider serviceProvider) => throw new NotImplementedException();
    }
}
