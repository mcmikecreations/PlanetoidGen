using System;
using System.ComponentModel.DataAnnotations;

namespace PlanetoidGen.Contracts.Models.Repositories.Messaging.Kafka
{
    public class KafkaOptions
    {
        public static string DefaultConfigurationSectionName = nameof(KafkaOptions);

        public KafkaOptions()
        {
            BootstrapServers = Array.Empty<string>();
        }

        [Required]
        public string? ClientId { get; set; }

        [Required, MinLength(1)]
        public string[] BootstrapServers { get; set; }

        /// <summary>
        /// Possible values: Plaintext, Ssl, SaslPlaintext, SaslSsl
        /// </summary>
        [Required]
        public string? SecurityProtocol { get; set; }

        [Required]
        [Range(typeof(int), "0", "20000")]
        public int RetryWaitMilliseconds { get; set; }

        [Required]
        [Range(typeof(int), "1", "10")]
        public int RetryCount { get; set; }

        [Required]
        public string? AgentTopicNamePrefix { get; set; }

        [Required]
        [Range(typeof(int), "1", "100")]
        public int? NumPartitions { get; set; }

        [Required]
        public string? ConsumerGroupId { get; set; }

        [Required]
        public int ConsumeTimeoutMilliseconds { get; set; }
    }
}
