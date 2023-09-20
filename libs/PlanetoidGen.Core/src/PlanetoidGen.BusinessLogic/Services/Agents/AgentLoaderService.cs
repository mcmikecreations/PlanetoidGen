using Microsoft.Extensions.Options;
using PlanetoidGen.BusinessLogic.Agents.Models.Agents;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Services.Agents;
using PlanetoidGen.Contracts.Services.Agents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace PlanetoidGen.BusinessLogic.Services.Agents
{
    public class AgentLoaderService : IAgentLoaderService
    {
        private readonly IEnumerable<string> _assemblyNames;
        private readonly IDictionary<string, AgentTypeInfo> _creators;

        /// <summary>
        /// Creates an instance of <see cref="AgentLoaderService"/>.
        /// During initialization of this instance types from specified assemblies
        /// are loaded for further processing. Such types should implement
        /// <see cref="ITypedAgent{TSettings}"/> interface.
        /// </summary>
        /// <param name="options"></param>
        public AgentLoaderService(IOptions<AgentLoaderServiceOptions> options)
        {
            _assemblyNames = options.Value.AssembliesToLoad;
            _creators = LoadAgents();
        }

        /// <inheritdoc/>
        public Result<IEnumerable<IAgent>> GetAllAgents()
        {
            return Result<IEnumerable<IAgent>>.CreateSuccess(_creators.Select(x => x.Value.DefaultCreator()));
        }

        /// <inheritdoc/>
        public Result<IAgent> GetAgent(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return Result<IAgent>.CreateFailure($"'{nameof(title)}' cannot be null or whitespace.");
            }

            return _creators.TryGetValue(title, out var agentType)
                ? Result<IAgent>.CreateSuccess(agentType.DefaultCreator())
                : Result<IAgent>.CreateFailure($"Agent with title '{title}' not found.");
        }

        public Result<AgentTypeInfo> GetAgentTypeInfo(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException($"'{nameof(title)}' cannot be null or whitespace.", nameof(title));
            }

            return _creators.TryGetValue(title, out var agentType)
                ? Result<AgentTypeInfo>.CreateSuccess(agentType)
                : Result<AgentTypeInfo>.CreateFailure($"Agent with title '{title}' not found.");
        }

        public Result<AgentTypeInfo> GetAgentTypeInfo(IAgent agent)
        {
            return GetAgentTypeInfo(agent.Title);
        }

        private IDictionary<string, AgentTypeInfo> LoadAgents()
        {
            return _assemblyNames
                .SelectMany(a =>
                {
                    return Assembly.Load(new AssemblyName(a))
                        .GetTypes()
                        .Where(x => !x.IsInterface && !x.IsAbstract && x.IsVisible
                                 && x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypedAgent<>)));
                })
                .Select(x => new
                {
                    type = x,
                    ctor = x.GetConstructor(Type.EmptyTypes)
                })
                .Where(x => x.ctor != null)
                .Select(x =>
                {
                    var (settingsType, validatorExpression) = CreateAgentSettingsValidator(x.type, x.ctor);

                    return new AgentTypeInfo(
                        x.type,
                        settingsType,
                        CreateDefaultCreator(x.ctor),
                        validatorExpression);
                })
                .ToDictionary(x => x.DefaultCreator().Title, x => x);
        }

        private Func<IAgent> CreateDefaultCreator(ConstructorInfo constructor)
        {
            if (constructor == null)
            {
                throw new InvalidOperationException("Default contructor is not avaliable");
            }

            var creatorExpression = Expression.Lambda<Func<IAgent>>(Expression.New(constructor));

            return creatorExpression.Compile();
        }

        private (Type settingsType, Func<string, ValueTask<Result<ValidationResult>>> validatorExpression) CreateAgentSettingsValidator(
            Type agentImplementationType,
            ConstructorInfo constructorInfo)
        {
            var settingsObject = agentImplementationType
                .GetMethod(nameof(ITypedAgent<AgentEmptySettings>.GetTypedDefaultSettings))
                .Invoke(constructorInfo.Invoke(null), null);

            var settingsValidateMethod = settingsObject
                .GetType()
                .GetMethod(nameof(AgentEmptySettings.Validate), new Type[] { typeof(string) });

            if (settingsValidateMethod == null
                || !settingsValidateMethod.ReturnType.Equals(typeof(ValueTask<Result<ValidationResult>>)))
            {
                throw new InvalidOperationException("Failed to retrieve method info for IAgentSettings<>.Validate");
            }

            var validateMethodParams = settingsValidateMethod
                .GetParameters()
                .Select(x => Expression.Parameter(x.ParameterType, x.Name))
                .ToArray();

            var validatorExpression = Expression.Lambda<Func<string, ValueTask<Result<ValidationResult>>>>(
                    Expression.Call(Expression.Constant(settingsObject), settingsValidateMethod, validateMethodParams),
                    validateMethodParams)
                .Compile();

            return (settingsObject.GetType(), validatorExpression);
        }
    }
}
