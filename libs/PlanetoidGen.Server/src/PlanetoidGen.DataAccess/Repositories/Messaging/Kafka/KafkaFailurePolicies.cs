using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;

namespace PlanetoidGen.DataAccess.Repositories.Messaging.Kafka
{
    internal static class KafkaFailurePolicies
    {
        public static AsyncRetryPolicy<PersistenceStatus> ProducerWaitAndRetry(int retryCount, int waitMilliseconds, ILogger logger)
        {
            return Policy
                .Handle<KafkaException>()
                .OrResult<PersistenceStatus>(r => r != PersistenceStatus.Persisted)
                .WaitAndRetryAsync(retryCount, i => TimeSpan.FromMilliseconds(waitMilliseconds), onRetry: (DelegateResult<PersistenceStatus> result, TimeSpan time) =>
                {
                    logger.LogError(
                        "Kafka publish action retry attempt. Error: '{ex}', status: '{status}'.",
                        result.Exception?.Message ?? "No exception.",
                        result.Result.ToString());
                });
        }

        public static AsyncRetryPolicy ConsumerWaitAndRetry(int waitMilliseconds, ILogger logger)
        {
            return Policy
                .Handle<KafkaException>()
                .WaitAndRetryForeverAsync(i => TimeSpan.FromMilliseconds(waitMilliseconds), (ex, _) =>
                {
                    logger.LogError("Kafka consume action retry attempt. Error: '{ex}'.", ex.Message);
                });
        }

        public static RetryPolicy ConsumerCommitWaitAndRetry(int retryCount, int waitMilliseconds, ILogger logger)
        {
            return Policy
                .Handle<KafkaException>()
                .WaitAndRetry(retryCount, i => TimeSpan.FromMilliseconds(waitMilliseconds), onRetry: (ex, _) =>
                {
                    logger.LogError("Kafka commit action retry attempt. Error: '{ex}'", ex.Message);
                });
        }
    }
}
