using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PlanetoidGen.Contracts.Factories.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models.Services.Meta;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Factories.Repositories.Dynamic;
using PlanetoidGen.DataAccess.Helpers.Extensions;
using PlanetoidGen.DataAccess.Repositories.Meta;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.BusinessLogic.Tests.Repositories.Dynamic
{
    public abstract class BaseDynamicRepositoryTests
    {
        protected IServiceProvider ServiceProvider { get; }

        public BaseDynamicRepositoryTests()
        {
            var configuration = CreateConfiguration();

            ServiceProvider = CreateServiceProvider(configuration);

            Task.Run(async () =>
            {
                var meta = ServiceProvider.GetService<IMetaProcedureRepository>()!;
                var metaExists = await meta!.EnsureExistsAsync(CancellationToken.None);

                Console.WriteLine(metaExists.ToString());
            }).Wait();
        }

        protected abstract TableSchema GetSchema();

        protected IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Testing.json")
                .AddInMemoryCollection(new Dictionary<string, string?>()
                {
                    {
                        "MetaProcedureOptions",
                        JsonSerializer.Serialize(new MetaProcedureOptions()
                        {
                            RecreateExtensions = false,
                            RecreateProcedures = true,
                            RecreateSchemas = false,
                            RecreateTables = true,
                            RecreateDynamicTables = true
                        })
                    },
                })
                .Build();
        }

        protected virtual IServiceCollection ConfigureDynamicRepoFactory(IServiceCollection c) => c
            .AddSingleton(typeof(IDynamicRepositoryFactory<>), typeof(UniversalDynamicRepositoryFactory<>));

        protected virtual IServiceCollection ConfigureServices(IConfiguration configuration) => new ServiceCollection()
            .AddSingleton(configuration)
            .ConfigureConnection(configuration)
            .AddSingleton(Options.Create(new MetaProcedureOptions()
            {
                RecreateExtensions = false,
                RecreateProcedures = true,
                RecreateSchemas = false,
                RecreateTables = true,
                RecreateDynamicTables = true
            }))
            .AddSingleton<IMetaProcedureRepository, MetaProcedureRepository>();

        protected IServiceProvider CreateServiceProvider(IConfiguration configuration) =>
            ConfigureDynamicRepoFactory(ConfigureServices(configuration))
            .BuildServiceProvider();
    }
}
