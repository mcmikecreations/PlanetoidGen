using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanetoidGen.Contracts.Enums.Messaging;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Models.Repositories.Messaging.Kafka;
using PlanetoidGen.Contracts.Repositories.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Messaging.Kafka
{
    public class KafkaGenerationJobMessageConsumerRepository : KafkaGenerationJobMessageRepositoryBase, IGenerationJobMessageConsumerRepository
    {
        private readonly KafkaOptions _consumerOptions;
        private readonly IGenerationJobMessageProducerRepository _producerRepository;
        private readonly ILogger<KafkaGenerationJobMessageConsumerRepository> _logger;

        public KafkaGenerationJobMessageConsumerRepository(
            IGenerationJobMessageProducerRepository producerRepository,
            IOptions<KafkaOptions> consumerOptions,
            ILogger<KafkaGenerationJobMessageConsumerRepository> logger)
            : base(consumerOptions.Value.AgentTopicNamePrefix)
        {
            if (consumerOptions is null)
            {
                throw new ArgumentNullException(nameof(consumerOptions));
            }

            _consumerOptions = consumerOptions.Value;
            _producerRepository = producerRepository ?? throw new ArgumentNullException(nameof(producerRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        ///<inheritdoc/>
        public async ValueTask<Result> ConsumeAsync<TMessage>(
            string consumerId,
            int agentsCount,
            Func<TMessage, ValueTask<Result<GenerationJobMessageProcessingStatus>>> messageProcessor,
            CancellationToken token)
            where TMessage : class, IGenerationJobMessage
        {
            try
            {
                return await KafkaFailurePolicies
                    .ConsumerWaitAndRetry(_consumerOptions.RetryWaitMilliseconds, _logger)
                    .ExecuteAsync(async () => await ConsumeInternalAsync(consumerId, agentsCount, messageProcessor, token));
            }
            catch (Exception ex)
            {
                return Result.CreateFailure(ex);
            }
        }

        private async ValueTask<Result> ConsumeInternalAsync<TMessage>(
            string consumerId,
            int agentsCount,
            Func<TMessage, ValueTask<Result<GenerationJobMessageProcessingStatus>>> messageProcessor,
            CancellationToken token)
            where TMessage : class, IGenerationJobMessage
        {
            using (var consumer = new ConsumerBuilder<Null, TMessage?>(CreateConsumerConfig(consumerId))
                .SetValueDeserializer(new KafkaSerializer<TMessage>())
                .Build())
            {
                var agentTopics = GetAgentTopics(agentsCount);

                while (!token.IsCancellationRequested)
                {
                    foreach (var topic in agentTopics)
                    {
                        var shouldProcessFromBeginning = false;
                        consumer.Subscribe(topic);

                        do
                        {
                            var consumeResult = consumer.Consume(_consumerOptions.ConsumeTimeoutMilliseconds);

                            if (consumeResult?.Message?.Value == null)
                            {
                                _logger.LogDebug("Kafka read for topic {topic} resulted with no data. Consumer ID: {id}.", topic, consumerId);
                                break;
                            }

                            var message = consumeResult.Message.Value;
                            var processingResult = await messageProcessor(message);

                            if (!processingResult.Success)
                            {
                                _logger.LogDebug(
                                    "Failed to process message with id {messageId}, attempt {attempt}, and topic {topic} due to agent execution error. Consumer ID: {consumerId}.",
                                    message.Id,
                                    message.DeliveryAttempt,
                                    topic,
                                    consumerId);

                                var produceResult = await ReproduceMessageAsync(message, _consumerOptions.RetryWaitMilliseconds / 2, consumerId, token);

                                if (!produceResult.Success)
                                {
                                    return produceResult;
                                }
                            }
                            else if (processingResult.Data == GenerationJobMessageProcessingStatus.Completion
                                  || processingResult.Data == GenerationJobMessageProcessingStatus.Skip)
                            {
                                _logger.LogTrace(
                                    "Consumer with id {id} will commit the change for topic {topic} and message id {messageId}.",
                                    topic,
                                    consumerId,
                                    message.Id);
                            }
                            else if (processingResult.Data == GenerationJobMessageProcessingStatus.WaitingForPreviousAgent)
                            {
                                var produceResult = await ReproduceMessageAsync(message, _consumerOptions.ConsumeTimeoutMilliseconds, consumerId, token);

                                if (!produceResult.Success)
                                {
                                    return produceResult;
                                }

                                shouldProcessFromBeginning = true;
                                break;
                            }

                            if (message.PlanetoidAgentsCount > agentTopics.Count)
                            {
                                shouldProcessFromBeginning = true;
                                agentTopics = GetAgentTopics(message.PlanetoidAgentsCount);
                                break;
                            }
                        } while (!token.IsCancellationRequested);

                        if (token.IsCancellationRequested || shouldProcessFromBeginning)
                        {
                            break;
                        }
                    }
                }

                consumer.Close();
            }

            return Result.CreateSuccess();
        }

        private async ValueTask<Result> ReproduceMessageAsync<TMessage>(
            TMessage message,
            int timeoutMilliseconds,
            string consumerId,
            CancellationToken token)
            where TMessage : class, IGenerationJobMessage
        {
            _logger.LogDebug(
                "Message with id {messageId} will be republished by consumer with id {consumerId}.",
                message.Id,
                consumerId);

            Thread.Sleep(timeoutMilliseconds);

            message.DeliveryAttempt += 1;

            return await _producerRepository.ProduceAsync(message, token);
        }

        private Result CommitMessage<TMessage>(
            IConsumer<Null, TMessage?> consumer,
            ConsumeResult<Null, TMessage?>? consumeResult,
            TMessage message)
            where TMessage : class, IGenerationJobMessage
        {
            var result = KafkaFailurePolicies.ConsumerCommitWaitAndRetry(
                    _consumerOptions.RetryCount,
                    _consumerOptions.RetryWaitMilliseconds,
                    _logger)
                .ExecuteAndCapture(() =>
                {
                    consumer.StoreOffset(consumeResult);
                    consumer.Commit(consumeResult);
                });

            return result.FinalException != null
                ? Result.CreateFailure($"Kafka failed to commit message with id {message.Id}.", result.FinalException)
                : Result.CreateSuccess();
        }

        private ConsumerConfig CreateConsumerConfig(string consumerId)
        {
            return new ConsumerConfig
            {
                ClientId = _consumerOptions.ClientId,
                BootstrapServers = string.Join(",", _consumerOptions.BootstrapServers),
                SecurityProtocol = Enum.Parse<SecurityProtocol>(_consumerOptions.SecurityProtocol),

                //IsolationLevel = IsolationLevel.ReadUncommitted,

                //// Improving throughput and lowering latency
                //FetchWaitMaxMs = 500,
                //FetchMinBytes = 1,
                //FetchMaxBytes = 52428800,
                //MaxPartitionFetchBytes = 1048576,
                //AutoCommitIntervalMs = 1000,

                //// Recovering from failure
                //SessionTimeoutMs = 10000,
                //HeartbeatIntervalMs = 3000,

                // Managing offset policy
                EnableAutoCommit = true,
                EnableAutoOffsetStore = true,
                AutoOffsetReset = AutoOffsetReset.Earliest,

                // Minimizing the impact of rebalancing consumer group
                GroupId = _consumerOptions.ConsumerGroupId,
                GroupInstanceId = consumerId,
                //MaxPollIntervalMs = 300000,
            };
        }
    }
}
