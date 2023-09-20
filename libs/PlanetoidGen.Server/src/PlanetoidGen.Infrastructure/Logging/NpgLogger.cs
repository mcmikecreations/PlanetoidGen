using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql.Logging;
using System;

namespace PlanetoidGen.Infrastructure.Logging
{
    public class NpgLogger : NpgsqlLogger
    {
        private readonly ILogger<NpgLogger> _logger;
        private readonly NpgsqlLogLevel _minimumLevel;

        public NpgLogger(ILogger<NpgLogger> logger, IConfiguration configuration)
        {
            _logger = logger;
            _minimumLevel = Enum.Parse<NpgsqlLogLevel>(configuration["NpgLogger:MinimumLevel"]);
        }

        public override bool IsEnabled(NpgsqlLogLevel level)
        {
            return _logger.IsEnabled(ToMyLogLevel(level));
        }

        public override void Log(NpgsqlLogLevel level, int connectorId, string msg, Exception? exception = null)
        {
            if (level >= _minimumLevel)
            {
                _logger.Log(ToMyLogLevel(level), exception, $"{connectorId} : {msg}");
            }
        }

        private LogLevel ToMyLogLevel(NpgsqlLogLevel logLevel)
        {
            return logLevel switch
            {
                NpgsqlLogLevel.Debug => LogLevel.Debug,
                NpgsqlLogLevel.Error => LogLevel.Error,
                NpgsqlLogLevel.Fatal => LogLevel.Critical,
                NpgsqlLogLevel.Info => LogLevel.Information,
                NpgsqlLogLevel.Trace => LogLevel.Trace,
                NpgsqlLogLevel.Warn => LogLevel.Warning,
                _ => LogLevel.None,
            };
        }
    }
}
