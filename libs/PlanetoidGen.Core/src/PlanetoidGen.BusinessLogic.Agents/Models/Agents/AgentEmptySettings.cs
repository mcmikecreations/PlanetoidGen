namespace PlanetoidGen.BusinessLogic.Agents.Models.Agents
{
    /// <summary>
    /// A class that inherits <see cref="BaseAgentSettings{TData}"/>
    /// and can be used for agents that do not require any input.
    /// </summary>
    public sealed class AgentEmptySettings : BaseAgentSettings<AgentEmptySettings>
    {
        /// <summary>
        /// Default instance of <see cref="AgentEmptySettings"/>.
        /// </summary>
        public static readonly AgentEmptySettings Default = new AgentEmptySettings();
    }
}
