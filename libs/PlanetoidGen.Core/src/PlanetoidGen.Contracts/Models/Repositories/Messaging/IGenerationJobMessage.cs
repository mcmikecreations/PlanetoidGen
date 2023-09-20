namespace PlanetoidGen.Contracts.Models.Repositories.Messaging
{
    public interface IGenerationJobMessage
    {
        /// <summary>
        /// Unique identifier of message.
        /// </summary>
        string? Id { get; set; }

        /// <summary>
        /// 0-based agent index.
        /// </summary>
        int AgentIndex { get; set; }

        /// <summary>
        /// Message publisher should set correct value of planetoid count agents
        /// so that consumer will be able to process all required message
        /// topics.
        /// </summary>
        int PlanetoidAgentsCount { get; set; }

        /// <summary>
        /// Can be used to track how many times message was attempted to process.
        /// </summary>
        int DeliveryAttempt { get; set; }
    }
}
