using Microsoft.Extensions.DependencyInjection;
using Npgsql.Logging;
using System;

namespace PlanetoidGen.Infrastructure.Logging
{
    public class NpgLoggingProvider : INpgsqlLoggingProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public NpgLoggingProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public NpgsqlLogger CreateLogger(string name) => _serviceProvider.GetService<NpgLogger>()!;
    }
}
