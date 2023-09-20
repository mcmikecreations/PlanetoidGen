using System.Collections.Generic;

namespace PlanetoidGen.Contracts.Repositories.Messaging
{
    public interface IGenerationJobMessageRepositoryBase
    {
        /// <summary>
        /// Agent topic name prefix.
        /// </summary>
        string AgentTopicNamePrefix { get; }

        /// <summary>
        /// Formats agent topic name based on its index (0-based).
        /// </summary>
        /// <param name="agentsCount"></param>
        /// <returns></returns>
        IList<string> GetAgentTopics(int agentsCount);

        /// <summary>
        /// Formats agents topic names based on its index.
        /// </summary>
        /// <param name="agentIndex">Agent index.</param>
        /// <returns></returns>
        string GetAgentTopic(int agentIndex);
    }
}
