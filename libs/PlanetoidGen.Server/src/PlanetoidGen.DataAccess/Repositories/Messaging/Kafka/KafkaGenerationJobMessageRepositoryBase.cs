using PlanetoidGen.Contracts.Repositories.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanetoidGen.DataAccess.Repositories.Messaging.Kafka
{
    public abstract class KafkaGenerationJobMessageRepositoryBase : IGenerationJobMessageRepositoryBase
    {
        /// <summary>
        /// Creates an instance of <see cref="KafkaGenerationJobMessageRepositoryBase"/>.
        /// </summary>
        /// <param name="agentTopicNamePrefix">Prefix that will be used for topic namings.</param>
        /// <exception cref="ArgumentException"></exception>
        protected KafkaGenerationJobMessageRepositoryBase(string? agentTopicNamePrefix)
        {
            if (string.IsNullOrWhiteSpace(agentTopicNamePrefix))
            {
                throw new ArgumentException(
                    $"'{nameof(agentTopicNamePrefix)}' cannot be null or whitespace.",
                    nameof(agentTopicNamePrefix));
            }

            AgentTopicNamePrefix = agentTopicNamePrefix.Trim();
        }

        public string AgentTopicNamePrefix { get; }

        /// <inheritdoc/>
        public IList<string> GetAgentTopics(int agentsCount)
        {
            return Enumerable.Range(0, agentsCount).Select(i => GetAgentTopic(i)).ToList();
        }

        /// <inheritdoc/>
        public string GetAgentTopic(int agentIndex)
        {
            return $"{AgentTopicNamePrefix}{agentIndex}";
        }
    }
}
