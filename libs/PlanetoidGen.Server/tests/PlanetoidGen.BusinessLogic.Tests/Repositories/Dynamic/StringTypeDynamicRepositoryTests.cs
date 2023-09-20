using Insight.Database;
using Microsoft.Extensions.DependencyInjection;
using PlanetoidGen.Contracts.Factories.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Meta;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PlanetoidGen.BusinessLogic.Tests.Repositories.Dynamic
{
    public class StringTypeDynamicRepositoryTests : BaseDynamicRepositoryTests
    {
        private class DynamicStringObject : IComparable<DynamicStringObject>
        {
            public int Id { get; }
            public string Z { get; }
            public DynamicStringObject(int id, string z)
            {
                Id = id;
                Z = z;
            }
            public override string ToString() => $"DynamicStringObject(Id={Id}, Z={Z})";

            public int CompareTo(DynamicStringObject? other)
            {
                if (other == null) return 1;
                if (other.Id != Id) return Id - other.Id;
                if (other.Z != Z) return Z.CompareTo(other.Z);
                return 0;
            }
        }

        protected override TableSchema GetSchema()
        {
            return new TableSchema("test", nameof(DynamicStringObject), new List<ColumnSchema>()
            {
                new ColumnSchema(nameof(DynamicStringObject.Id), ColumnSchema.ColumnType.Int32,
                    properties: null,
                    canBeNull: false,
                    usedInCreate: true,
                    usedInRead: true,
                    usedInUpdate: true,
                    usedInDelete: true),
                new ColumnSchema(nameof(DynamicStringObject.Z), ColumnSchema.ColumnType.String,
                    properties: null,
                    canBeNull: false,
                    usedInCreate: true,
                    usedInRead: false,
                    usedInUpdate: false,
                    usedInDelete: false),
            }, new List<IndexSchema>()
            {
                new IndexSchema(
                    IndexSchema.IndexKind.PrimaryKey,
                    new List<string>(){ nameof(DynamicStringObject.Id) },
                    null),
            });
        }

        [Fact]
        public async Task GivenEmptyDatabase_VerifiesMainFlow()
        {
            var token = CancellationToken.None;

            var schema = GetSchema();

            var factory = ServiceProvider.GetService<IDynamicRepositoryFactory<DynamicStringObject>>()!;

            var dResult = factory.CreateRepository(schema);
            Assert.True(dResult.Success, dResult.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(dResult.Data);

            var objectActual = new DynamicStringObject(1, "2");
            var objectRead = new DynamicStringObject(1, "0");
            var objectUpdate = new DynamicStringObject(1, "3");

            var insert = await dResult.Data!.Create(objectActual, token);
            Assert.True(insert.Success, insert.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(insert.Data);
            Assert.Equal(objectActual, insert.Data!);

            var read = await dResult.Data!.Read(objectRead, token);
            Assert.True(read.Success, read.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(read.Data);
            Assert.Equal(objectActual, read.Data!);

            var update = await dResult.Data!.Update(objectUpdate, token);
            Assert.True(update.Success, update.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(update.Data);
            Assert.Equal(objectUpdate, update.Data!);

            var delete = await dResult.Data!.Delete(objectRead, token);
            Assert.True(delete.Success, delete.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(delete.Data);
            Assert.Equal(objectUpdate, delete.Data!);
        }

        [Fact]
        public async Task GivenEmptyDatabase_VerifiesRecreationFlow()
        {
            var token = CancellationToken.None;
            var schemaName = "testdynstring";
            var extensionName = "postgis";
            var tableName = "test";

            var connectionBuilder = ServiceProvider.GetService<DbConnectionStringBuilder>()!;
            var meta = ServiceProvider.GetService<IMetaProcedureRepository>()!;

            using (var conn = await connectionBuilder.OpenAsync(token))
            {
                await conn.QuerySqlAsync($"CREATE SCHEMA IF NOT EXISTS {schemaName};", cancellationToken: token);

                var schemaResult = await meta.SchemaExists(schemaName, token);
                Assert.NotNull(schemaResult);
                Assert.True(schemaResult.Success, schemaResult.ErrorMessage?.ToString());

                await conn.QuerySqlAsync($"CREATE SCHEMA IF NOT EXISTS public;", cancellationToken: token);
                await conn.QuerySqlAsync($"CREATE EXTENSION IF NOT EXISTS {extensionName} SCHEMA public CASCADE;", cancellationToken: token);

                var extensionResult = await meta.ExtensionExists(extensionName, token);
                Assert.NotNull(extensionResult);
                Assert.True(extensionResult.Success, extensionResult.ErrorMessage?.ToString());

                await conn.QuerySqlAsync($"CREATE TABLE IF NOT EXISTS {schemaName}.{tableName}(a integer);", cancellationToken: token);

                var tableResult = await meta.TableExists(schemaName, tableName, token);
                Assert.NotNull(tableResult);
                Assert.True(tableResult.Success, tableResult.ErrorMessage?.ToString());

                await conn.QuerySqlAsync($"DROP SCHEMA IF EXISTS {schemaName} CASCADE;", cancellationToken: token);

                schemaResult = await meta.SchemaExists(schemaName, token);
                Assert.NotNull(schemaResult);
                Assert.False(schemaResult.Success);
                Assert.NotNull(schemaResult.ErrorMessage);

                tableResult = await meta.TableExists(schemaName, tableName, token);
                Assert.NotNull(tableResult);
                Assert.False(tableResult.Success);
                Assert.NotNull(tableResult.ErrorMessage);
            }
        }
    }
}
