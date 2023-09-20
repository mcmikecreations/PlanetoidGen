using PlanetoidGen.Contracts.Models;
using Polly;
using Polly.Retry;

namespace PlanetoidGen.API.AgentWorker.Workers
{
    public static class AgentExecutionFailurePolicies
    {
        public static AsyncRetryPolicy<Result> AgentExecuteWaitAndRetry(int retryCount, int waitMilliseconds, ILogger logger)
        {
            return Policy
                .Handle<Exception>()
                .OrResult<Result>(r => r == null || !r.Success)
                .WaitAndRetryAsync(
                    retryCount,
                    i => TimeSpan.FromMilliseconds(waitMilliseconds),
                    onRetry: (DelegateResult<Result> result, TimeSpan time) =>
                    {
                        logger.LogError(
                            "Agent execution retry attempt. Error: '{ex}'.",
                            result.Exception?.Message ?? result.Result?.ErrorMessage?.ToString());
                    });
        }

        public static AsyncRetryPolicy<Result> MessageConsumeWaitAndRetryForever(int waitMilliseconds, ILogger logger)
        {
            return Policy
                .HandleResult<Result>(r => r == null || !r.Success)
                .WaitAndRetryForeverAsync(
                    i => TimeSpan.FromMilliseconds(waitMilliseconds),
                    onRetry: (DelegateResult<Result> result, TimeSpan time) =>
                    {
                        logger.LogError(
                            "Agent worker consume message action retry attempt. Error: '{ex}'.",
                            result.Result?.ErrorMessage!.ToString() ?? $"Consume action returned empty {nameof(Result)}.");
                    });
        }
    }
}
