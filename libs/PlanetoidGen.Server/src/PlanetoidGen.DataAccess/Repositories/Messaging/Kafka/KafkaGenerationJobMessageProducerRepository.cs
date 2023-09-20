using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Models.Repositories.Messaging.Kafka;
using PlanetoidGen.Contracts.Repositories.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Messaging.Kafka
{
    public class KafkaGenerationJobMessageProducerRepository : KafkaGenerationJobMessageRepositoryBase, IGenerationJobMessageProducerRepository
    {
        private readonly KafkaOptions _kafkaProducerOptions;
        private readonly ProducerConfig _producerConfig;
        private readonly ILogger<KafkaGenerationJobMessageProducerRepository> _logger;

        public KafkaGenerationJobMessageProducerRepository(
            IOptions<KafkaOptions> producerOptions,
            ILogger<KafkaGenerationJobMessageProducerRepository> logger)
            : base(producerOptions.Value.AgentTopicNamePrefix)
        {
            _kafkaProducerOptions = producerOptions.Value!;
            _producerConfig = new ProducerConfig
            {
                BootstrapServers = string.Join(",", _kafkaProducerOptions.BootstrapServers),
                SecurityProtocol = Enum.Parse<SecurityProtocol>(_kafkaProducerOptions.SecurityProtocol),
                ClientId = _kafkaProducerOptions.ClientId,
            };
            _logger = logger;
        }

        /// <inheritdoc/>
        public async ValueTask<Result> ProduceAsync<TMessage>(TMessage message, CancellationToken token)
            where TMessage : class, IGenerationJobMessage
        {
            try
            {
                await KafkaFailurePolicies
                    .ProducerWaitAndRetry(
                        _kafkaProducerOptions.RetryCount,
                        _kafkaProducerOptions.RetryWaitMilliseconds,
                        _logger)
                    .ExecuteAsync(async () => await ProduceSingleAsync(message, token));
            }
            catch (Exception ex)
            {
                return Result.CreateFailure(ex);
            }

            return Result.CreateSuccess();
        }

        /// <inheritdoc/>
        public async ValueTask<Result> ProduceAsync<TMessage>(IEnumerable<TMessage> messages, CancellationToken token)
            where TMessage : class, IGenerationJobMessage
        {
            if (messages is null)
            {
                return Result.CreateFailure(new ArgumentNullException(nameof(messages)));
            }

            try
            {
                using (var producer = new ProducerBuilder<Null, TMessage>(_producerConfig)
                    .SetValueSerializer(new KafkaSerializer<TMessage>())
                    .Build())
                {
                    var tasks = messages.Select(m =>
                    {
                        return KafkaFailurePolicies
                            .ProducerWaitAndRetry(
                                _kafkaProducerOptions.RetryCount,
                                _kafkaProducerOptions.RetryWaitMilliseconds,
                                _logger)
                            .ExecuteAsync(() => ProduceSingleAsync(producer, m, token));
                    });

                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex)
            {
                return Result.CreateFailure(ex);
            }

            return Result.CreateSuccess();
        }

        private async Task<PersistenceStatus> ProduceSingleAsync<TMessage>(TMessage message, CancellationToken token)
            where TMessage : class, IGenerationJobMessage
        {
            using (var producer = new ProducerBuilder<Null, TMessage>(_producerConfig)
                .SetValueSerializer(new KafkaSerializer<TMessage>())
                .Build())
            {
                return await ProduceSingleAsync(producer, message, token);
            }
        }

        private async Task<PersistenceStatus> ProduceSingleAsync<TMessage>(IProducer<Null, TMessage> producer, TMessage message, CancellationToken token)
            where TMessage : class, IGenerationJobMessage
        {
            var result = await producer.ProduceAsync(
                GetAgentTopic(message.AgentIndex),
                new Message<Null, TMessage>
                {
                    Value = message,
                },
                token);

            return result.Status;
        }
    }
}
