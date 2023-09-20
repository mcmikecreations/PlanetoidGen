using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace PlanetoidGen.Agents.Tests.Integration
{
    internal class TestLogger<T> : ILogger<T>
    {
        private readonly ILogger<T> _logger;

        public TestLogger()
        {
            _logger = new SerilogLoggerFactory(TestLoggerGlobal.Logger).CreateLogger<T>();
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return _logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    internal static class TestLoggerGlobal
    {
        public static Serilog.ILogger Logger { get; } = new LoggerConfiguration()
                        .WriteTo.Console()
                        .WriteTo.File("test-log-.txt", rollingInterval: RollingInterval.Day)
                        .CreateLogger();
    }
}
