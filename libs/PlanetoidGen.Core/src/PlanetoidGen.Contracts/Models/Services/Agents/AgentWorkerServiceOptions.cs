using System.ComponentModel.DataAnnotations;

namespace PlanetoidGen.Contracts.Models.Services.Agents
{
    public class AgentWorkerServiceOptions
    {
        public static string DefaultConfigurationSectionName = nameof(AgentWorkerServiceOptions);

        [Range(1000, int.MaxValue)]
        public int? AgentExecutionSlidingTimeoutMilliseconds { get; set; }

        [Range(1, 64)]
        public int? AgentWorkersCount { get; set; }

        [Required]
        [Range(1, 10)]
        public int AgentExecutionRetryCount { get; set; }

        [Required]
        [Range(100, int.MaxValue)]
        public int AgentExecutionRetryWaitMilliseconds { get; set; }
    }
}
