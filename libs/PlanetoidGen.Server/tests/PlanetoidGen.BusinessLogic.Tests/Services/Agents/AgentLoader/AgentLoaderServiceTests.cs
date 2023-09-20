using Microsoft.Extensions.Options;
using PlanetoidGen.BusinessLogic.Services.Agents;
using PlanetoidGen.BusinessLogic.Tests.Services.Agents.AgentLoader.Models;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Services.Agents;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace PlanetoidGen.BusinessLogic.Tests.Services.Agents.AgentLoader
{
    public class AgentLoaderServiceTests
    {
        [Fact]
        public void GivenAssebmyNames_GetsAllAgents()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var loader = new AgentLoaderService(Options.Create(
                new AgentLoaderServiceOptions
                {
                    AssembliesToLoad = new string[] { assembly.FullName ?? string.Empty }
                }));
            var expectedAgent = new AgentWithParameterlessConstructor();

            var allAgents = loader.GetAllAgents().Data;
            var actualTestAgent = allAgents.Where(x => x.Title.StartsWith(nameof(AgentLoaderServiceTests)));

            Assert.NotEmpty(actualTestAgent);
            Assert.Collection(actualTestAgent, item => Assert.IsType<AgentWithParameterlessConstructor>(item));
        }

        [Fact]
        public void GivenAssebmyNames_GivenAgentTitle_GetsAgent()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var loader = new AgentLoaderService(Options.Create(
                new AgentLoaderServiceOptions
                {
                    AssembliesToLoad = new string[] { assembly.FullName ?? string.Empty }
                }));
            var expectedAgent = new AgentWithParameterlessConstructor();

            var actualAgent = loader.GetAgent(expectedAgent.Title).Data;

            Assert.NotNull(actualAgent);
            Assert.IsType<AgentWithParameterlessConstructor>(actualAgent);
        }

        [Fact]
        public void GivenAssebmyNames_GivenAgentTitle_GetsNewAgentInstance()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var loader = new AgentLoaderService(Options.Create(
                new AgentLoaderServiceOptions
                {
                    AssembliesToLoad = new string[] { assembly.FullName ?? string.Empty }
                }));
            var expectedAgent = new AgentWithParameterlessConstructor();

            var actualAgent1 = loader.GetAgent(expectedAgent.Title).Data;
            var actualAgent2 = loader.GetAgent(expectedAgent.Title).Data;

            Assert.NotSame(actualAgent1, actualAgent2);
        }

        [Fact]
        public void GivenAssebmyNames_GivenAgentTitle_GetsAgentTypeInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var loader = new AgentLoaderService(Options.Create(
                new AgentLoaderServiceOptions
                {
                    AssembliesToLoad = new string[] { assembly.FullName ?? string.Empty }
                }));
            var expectedAgent = new AgentWithParameterlessConstructor();
            var expectedAgentType = typeof(AgentWithParameterlessConstructor);
            var expectedSettingsType = typeof(TestAgentSettings);

            var getTypeInfoResult = loader.GetAgentTypeInfo(expectedAgent.Title);
            var typeInfo = getTypeInfoResult.Data;

            Assert.True(getTypeInfoResult.Success);
            Assert.Equal(expectedAgentType, typeInfo.Type);
            Assert.Equal(expectedSettingsType, typeInfo.SettingsType);
            Assert.IsType<AgentWithParameterlessConstructor>(typeInfo.DefaultCreator());
        }

        [Fact]
        public async Task GivenAssebmyNames_GivenAgentTitle_CanUseSettingsValidator()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var loader = new AgentLoaderService(Options.Create(
                new AgentLoaderServiceOptions
                {
                    AssembliesToLoad = new string[] { assembly.FullName ?? string.Empty }
                }));
            var settings = new TestAgentSettings { RequiredString = null };
            var expectedValidationResult = new ValidationResult(new List<string>
            {
                $"The {nameof(TestAgentSettings.RequiredString)} field is required."
            });

            var getTypeInfoResult = loader.GetAgentTypeInfo(new AgentWithParameterlessConstructor().Title);
            var typeInfo = getTypeInfoResult.Data;

            Assert.True(getTypeInfoResult.Success);

            var validateSettingsResult = await typeInfo.SettingsValidator(await settings.Serialize());

            Assert.True(validateSettingsResult.Success);
            Assert.Equivalent(expectedValidationResult, validateSettingsResult.Data);
        }
    }
}
