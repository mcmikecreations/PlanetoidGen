using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PlanetoidGen.Contracts.Models.Services.GeoInfo;
using PlanetoidGen.Contracts.Models.Services.Meta;
using PlanetoidGen.Contracts.Repositories.Generation;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Helpers.Extensions;
using PlanetoidGen.DataAccess.Repositories.Generation;
using PlanetoidGen.DataAccess.Repositories.Meta;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PlanetoidGen.BusinessLogic.Tests.Repositories.Generation
{
    public class SpatialReferenceSystemRepositoryTests
    {
        protected IServiceProvider ServiceProvider { get; }

        public SpatialReferenceSystemRepositoryTests()
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

        protected IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Testing.json")
                .Build();
        }

        protected virtual IServiceCollection ConfigureServices(IConfiguration configuration) => new ServiceCollection()
            .AddSingleton(configuration)
            .ConfigureConnection(configuration)
            .AddSingleton(Options.Create(new MetaProcedureOptions()
            {
                RecreateExtensions = false,
                RecreateProcedures = true,
                RecreateSchemas = false,
                RecreateTables = false,
                RecreateDynamicTables = false
            }))
            .AddSingleton(Options.Create(new GeoInfoServiceOptions()
            {
                AvailableMinSrid = 200_000,
                AvailableMaxSrid = 900_000,
            }))
            .AddSingleton<IMetaProcedureRepository, MetaProcedureRepository>()
            .AddSingleton<ISpatialReferenceSystemRepository, SpatialReferenceSystemRepository>();

        protected IServiceProvider CreateServiceProvider(IConfiguration configuration) =>
            ConfigureServices(configuration)
            .BuildServiceProvider();

        [Fact]
        public async Task GivenEmptyDatabase_VerifiesGet()
        {
            // Using Anguilla SRID=2000, AUTH=EPSG for the test.
            var repo = ServiceProvider.GetService<ISpatialReferenceSystemRepository>()!;
            var token = CancellationToken.None;

            var getResult = await repo.GetSRS(2000, token);
            Assert.NotNull(getResult);
            Assert.True(getResult.Success, getResult.ErrorMessage?.ToString());

            var sridProj = getResult.Data;
            Assert.NotNull(sridProj);

            getResult = await repo.GetSRS(2000, "EPSG", token);
            Assert.NotNull(getResult);
            Assert.True(getResult.Success, getResult.ErrorMessage?.ToString());

            var authProj = getResult.Data;
            Assert.NotNull(authProj);

            Assert.Equal(sridProj.Srid, authProj.Srid);
            Assert.Equal(sridProj.AuthorityName, authProj.AuthorityName);
            Assert.Equal(sridProj.AuthoritySrid, authProj.AuthoritySrid);
            Assert.Equal(sridProj.WktString, authProj.WktString);
            Assert.Equal(sridProj.Proj4String, authProj.Proj4String);
        }

        [Fact]
        public async Task GivenEmptyDatabase_VerifiesInsertDelete()
        {
            // Using Anguilla SRID=2000, AUTH=EPSG for the test.
            var repo = ServiceProvider.GetService<ISpatialReferenceSystemRepository>()!;
            var token = CancellationToken.None;

            var getResult = await repo.GetSRS(2000, token);
            Assert.NotNull(getResult);
            Assert.True(getResult.Success, getResult.ErrorMessage?.ToString());

            var sridProj = getResult.Data;
            Assert.NotNull(sridProj);

            var insertResult = await repo.InsertOrUpdateSRS(sridProj.WktString, sridProj.Proj4String, sridProj.AuthoritySrid, nameof(PlanetoidGen), token);
            Assert.NotNull(insertResult);
            Assert.True(insertResult.Success, insertResult.ErrorMessage?.ToString());

            var insertProjSrid = insertResult.Data;

            getResult = await repo.GetSRS(insertProjSrid, token);
            Assert.NotNull(getResult);
            Assert.True(getResult.Success, getResult.ErrorMessage?.ToString());

            var addedProj = getResult.Data;
            Assert.NotNull(addedProj);

            Assert.NotEqual(sridProj.Srid, addedProj.Srid);
            Assert.NotEqual(sridProj.AuthorityName, addedProj.AuthorityName);
            Assert.Equal(sridProj.AuthoritySrid, addedProj.AuthoritySrid);
            Assert.Equal(sridProj.WktString, addedProj.WktString);
            Assert.Equal(sridProj.Proj4String, addedProj.Proj4String);

            var deleteResult = await repo.DeleteSRS(insertProjSrid, token);
            Assert.NotNull(deleteResult);
            Assert.True(deleteResult.Success, deleteResult.ErrorMessage?.ToString());
        }

        [Fact]
        public async Task GivenEmptyDatabase_VerifiesInsertCountClear()
        {
            // Using Anguilla SRID=2000, AUTH=EPSG for the test.
            var repo = ServiceProvider.GetService<ISpatialReferenceSystemRepository>()!;
            var token = CancellationToken.None;

            var getResult = await repo.GetSRS(2000, token);
            Assert.NotNull(getResult);
            Assert.True(getResult.Success, getResult.ErrorMessage?.ToString());

            var sridProj = getResult.Data;
            Assert.NotNull(sridProj);

            var insertResult = await repo.InsertOrUpdateSRS(sridProj.WktString, sridProj.Proj4String, sridProj.AuthoritySrid, nameof(PlanetoidGen), token);
            Assert.NotNull(insertResult);
            Assert.True(insertResult.Success, insertResult.ErrorMessage?.ToString());

            var insertProjSrid = insertResult.Data;

            getResult = await repo.GetSRS(insertProjSrid, token);
            Assert.NotNull(getResult);
            Assert.True(getResult.Success, getResult.ErrorMessage?.ToString());

            var addedProj = getResult.Data;
            Assert.NotNull(addedProj);

            Assert.NotEqual(sridProj.Srid, addedProj.Srid);
            Assert.NotEqual(sridProj.AuthorityName, addedProj.AuthorityName);
            Assert.Equal(sridProj.AuthoritySrid, addedProj.AuthoritySrid);
            Assert.Equal(sridProj.WktString, addedProj.WktString);
            Assert.Equal(sridProj.Proj4String, addedProj.Proj4String);

            var countResult = await repo.CountCustomSRS(token);
            Assert.NotNull(countResult);
            Assert.True(countResult.Success, countResult.ErrorMessage?.ToString());
            Assert.Equal(1, countResult.Data);

            var clearResult = await repo.ClearCustomSRS(token);
            Assert.NotNull(clearResult);
            Assert.True(clearResult.Success, clearResult.ErrorMessage?.ToString());
            Assert.Equal(1, clearResult.Data);
        }
    }
}
