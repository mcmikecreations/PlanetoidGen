namespace PlanetoidGen.Contracts.Models.Repositories.Messaging
{
    public class GenerationJobMessage : IGenerationJobMessage
    {
        /// <inheritdoc/>
        public string? Id { get; set; }

        /// <inheritdoc/>
        public int PlanetoidAgentsCount { get; set; }

        /// <inheritdoc/>
        public int AgentIndex { get; set; }

        /// <inheritdoc/>
        public int DeliveryAttempt { get; set; }

        /// <summary>
        /// Planetoid identifier.
        /// </summary>
        public int PlanetoidId { get; set; }

        /// <summary>
        /// Planar coordinate Z index.
        /// </summary>
        public short Z { get; set; }

        /// <summary>
        /// Planar coordinate X index.
        /// </summary>
        public long X { get; set; }

        /// <summary>
        /// Planar coordinate Y index.
        /// </summary>
        public long Y { get; set; }

        /// <summary>
        /// Client connection ID
        /// </summary>
        public string? ConnectionId { get; set; }

        public override string ToString()
        {
            return $"Id={Id}, P={PlanetoidId}, A={AgentIndex}, DA={DeliveryAttempt}, AC={PlanetoidAgentsCount}, Z={Z}, X={X}, Y={Y}, C={ConnectionId}";
        }
    }
}
