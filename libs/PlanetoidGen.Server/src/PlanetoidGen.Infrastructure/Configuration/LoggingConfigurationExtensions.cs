using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql.Logging;
using PlanetoidGen.Infrastructure.Logging;
using Serilog;
using System;

namespace PlanetoidGen.Infrastructure.Configuration
{
    public static class LoggingConfigurationExtensions
    {
        public static IServiceCollection ConfigureLogging(this IServiceCollection collection, IHostBuilder host)
        {
            collection
                .AddTransient<NpgLogger>();

            // Add logging
            host.UseSerilog((ctx, lc) => lc
                .ReadFrom.Configuration(ctx.Configuration));

            return collection;
        }

        public static IApplicationBuilder UseConfiguredLogging(this IApplicationBuilder app, IServiceProvider services)
        {
            app.UseSerilogRequestLogging();

            // Setting up Npgsql provider
            NpgsqlLogManager.Provider = new NpgLoggingProvider(services);

            return app;
        }
    }
}
