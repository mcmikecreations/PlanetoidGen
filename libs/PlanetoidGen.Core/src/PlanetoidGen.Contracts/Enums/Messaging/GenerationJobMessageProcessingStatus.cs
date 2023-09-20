namespace PlanetoidGen.Contracts.Enums.Messaging
{
    public enum GenerationJobMessageProcessingStatus
    {
        /// <summary>
        /// Current agent execution is dependent on the previous agent execution.
        /// </summary>
        WaitingForPreviousAgent,

        /// <summary>
        /// Current agent execution completed successfully.
        /// </summary>
        Completion,

        /// <summary>
        ///  Current agent execution skipped.
        /// </summary>
        Skip,
    }
}
