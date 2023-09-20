using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Messaging.Kafka;
using PlanetoidGen.Contracts.Repositories.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Messaging.Kafka
{
    public class KafkaGenerationJobMessageAdminRepository : KafkaGenerationJobMessageRepositoryBase, IGenerationJobMessageAdminRepository
    {
        private readonly AdminClientConfig _adminClientConfig;
        private readonly KafkaOptions _kafkaOptions;

        public KafkaGenerationJobMessageAdminRepository(IOptions<KafkaOptions> producerOptions)
            : base(producerOptions.Value.AgentTopicNamePrefix)
        {
            _kafkaOptions = producerOptions.Value!;

            _adminClientConfig = new AdminClientConfig
            {
                BootstrapServers = string.Join(",", _kafkaOptions.BootstrapServers),
                SecurityProtocol = Enum.Parse<SecurityProtocol>(_kafkaOptions.SecurityProtocol),
            };
        }

        public async ValueTask<Result<IEnumerable<string>>> EnsureExists(int agentsCount)
        {
            try
            {
                using (var adminClient = new AdminClientBuilder(_adminClientConfig).Build())
                {
                    var existingAgentTopics = adminClient.GetMetadata(TimeSpan.FromSeconds(5000))
                        .Topics
                        .Select(x => x.Topic)
                        .Where(x => x != null && x.StartsWith(AgentTopicNamePrefix))
                        .ToList();

                    var topicsToBeCreated = GetAgentTopics(agentsCount)
                        .Except(existingAgentTopics)
                        .ToList();

                    if (topicsToBeCreated.Any())
                    {
                        await CreateTopicsAsync(adminClient, topicsToBeCreated);
                    }

                    return Result<IEnumerable<string>>.CreateSuccess(topicsToBeCreated);
                }
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<string>>.CreateFailure(ex);
            }
        }

        public async ValueTask<Result<IEnumerable<string>>> CreateTopics(IEnumerable<string> topics)
        {
            try
            {
                using (var adminClient = new AdminClientBuilder(_adminClientConfig).Build())
                {
                    await CreateTopicsAsync(adminClient, topics);
                }

                return await GetAllTopics();
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<string>>.CreateFailure(ex);
            }
        }

        public ValueTask<Result<IEnumerable<string>>> GetAllTopics()
        {
            try
            {
                using (var adminClient = new AdminClientBuilder(_adminClientConfig).Build())
                {
                    var meta = adminClient.GetMetadata(TimeSpan.FromSeconds(20));

                    return new ValueTask<Result<IEnumerable<string>>>(Result<IEnumerable<string>>.CreateSuccess(
                        meta.Topics.Select(x => x.Topic).ToList()));
                }
            }
            catch (Exception ex)
            {
                return new ValueTask<Result<IEnumerable<string>>>(Result<IEnumerable<string>>.CreateFailure(ex));
            }
        }

        public async ValueTask<Result<IEnumerable<string>>> DeleteAllTopics()
        {
            try
            {
                using (var adminClient = new AdminClientBuilder(_adminClientConfig).Build())
                {
                    var meta = adminClient.GetMetadata(TimeSpan.FromSeconds(20));
                    var topics = meta.Topics.Select(x => x.Topic).ToList();

                    if (topics.Any())
                    {
                        await adminClient.DeleteTopicsAsync(topics);
                    }

                    return Result<IEnumerable<string>>.CreateSuccess(topics);
                }
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<string>>.CreateFailure(ex);
            }
        }

        public async ValueTask<Result<IEnumerable<string>>> DeleteTopics(IEnumerable<string> topics)
        {
            try
            {
                using (var adminClient = new AdminClientBuilder(_adminClientConfig).Build())
                {
                    if (topics.Any())
                    {
                        await adminClient.DeleteTopicsAsync(topics);
                    }

                    return Result<IEnumerable<string>>.CreateSuccess(topics);
                }
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<string>>.CreateFailure(ex);
            }
        }

        private async Task CreateTopicsAsync(IAdminClient adminClient, IEnumerable<string> topics)
        {
            await adminClient.CreateTopicsAsync(topics.Select(t => new TopicSpecification
            {
                Name = t,
                NumPartitions = _kafkaOptions.NumPartitions!.Value,
            }));
        }
    }
}
