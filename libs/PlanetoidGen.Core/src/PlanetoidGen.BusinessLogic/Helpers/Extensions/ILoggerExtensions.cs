using Microsoft.Extensions.Logging;

namespace PlanetoidGen.BusinessLogic.Helpers.Extensions
{
    public static class ILoggerExtensions
    {
        public static void LogFailure<TLogger, TResultData>(
            this ILogger<TLogger> logger,
            string method,
            Contracts.Models.Generic.Result<TResultData> result)
        {
            logger.LogError("'{method}' failed: {errorMessage}", method, result.ErrorMessage!.ToString().TrimEnd());
        }

        public static void LogFailure<TLogger>(
            this ILogger<TLogger> logger,
            string method,
            Contracts.Models.Result result)
        {
            logger.LogError("'{method}' failed: {errorMessage}", method, result.ErrorMessage!.ToString().TrimEnd());
        }
    }
}
