using Microsoft.Extensions.DependencyInjection;
using PlanetoidGen.Contracts.Factories.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PlanetoidGen.BusinessLogic.Tests.Repositories.Dynamic
{
    public class IntegralTypeDynamicRepositoryTests : BaseDynamicRepositoryTests
    {
        private class DynamicObject : IComparable<DynamicObject>
        {
            public int PlanetoidId { get; }
            public short Z { get; }
            public int P { get; }
            public DynamicObject(int planetoidId, short z, int p)
            {
                PlanetoidId = planetoidId;
                Z = z;
                P = p;
            }
            public override string ToString() => $"DynamicObject(PlanetoidId={PlanetoidId}, Z={Z}, P={P})";

            public int CompareTo(DynamicObject? other)
            {
                if (other == null) return 1;
                if (other.PlanetoidId != PlanetoidId) return PlanetoidId - other.PlanetoidId;
                if (other.Z != Z) return Z - other.Z;
                if (other.P != P) return P - other.P;
                return 0;
            }
        }

        protected override TableSchema GetSchema()
        {
            return new TableSchema("test", nameof(DynamicObject), new List<ColumnSchema>()
            {
                new ColumnSchema(nameof(DynamicObject.PlanetoidId), ColumnSchema.ColumnType.Int32,
                    properties: null,
                    canBeNull: false,
                    usedInCreate: true,
                    usedInRead: true,
                    usedInUpdate: true,
                    usedInDelete: true),
                new ColumnSchema(nameof(DynamicObject.Z), ColumnSchema.ColumnType.Int16,
                    properties: null,
                    canBeNull: false,
                    usedInCreate: true,
                    usedInRead: true,
                    usedInUpdate: true,
                    usedInDelete: true),
                new ColumnSchema(nameof(DynamicObject.P), ColumnSchema.ColumnType.Int32,
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
                    new List<string>(){ nameof(DynamicObject.PlanetoidId), nameof(DynamicObject.Z) },
                    null),
                new IndexSchema(
                    IndexSchema.IndexKind.Unique,
                    new List<string>(){ nameof(DynamicObject.P) },
                    null),
                new IndexSchema(
                    IndexSchema.IndexKind.Duplicated,
                    new List<string>(){ nameof(DynamicObject.Z) },
                    new List<string>(){ nameof(DynamicObject.P) }),
            });
        }

        [Fact]
        public async Task GivenEmptyDatabase_VerifiesMainFlow()
        {
            var token = CancellationToken.None;

            var schema = GetSchema();

            var factory = ServiceProvider.GetService<IDynamicRepositoryFactory<DynamicObject>>()!;

            var dResult = factory.CreateRepository(schema);
            Assert.True(dResult.Success, dResult.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(dResult.Data);

            var objectActual = new DynamicObject(1, 0, 2);
            var objectRead = new DynamicObject(1, 0, 0);
            var objectUpdate = new DynamicObject(1, 0, 3);

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
        public async Task GivenEmptyDatabase_VerifiesFailedReadFlow()
        {
            var token = CancellationToken.None;

            var schema = GetSchema();

            var factory = ServiceProvider.GetService<IDynamicRepositoryFactory<DynamicObject>>()!;

            var dResult = factory.CreateRepository(schema);
            Assert.True(dResult.Success, dResult.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(dResult.Data);

            var objectRead = new DynamicObject(1, 0, 0);

            var read = await dResult.Data!.Read(objectRead, token);
            Assert.False(read.Success);
            Assert.Null(read.Data);
        }

        [Fact]
        public async Task GivenEmptyDatabase_VerifiesFailedUpdateFlow()
        {
            var token = CancellationToken.None;

            var schema = GetSchema();

            var factory = ServiceProvider.GetService<IDynamicRepositoryFactory<DynamicObject>>()!;

            var dResult = factory.CreateRepository(schema);
            Assert.True(dResult.Success, dResult.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(dResult.Data);

            var objectUpdate = new DynamicObject(1, 0, 3);

            var update = await dResult.Data!.Update(objectUpdate, token);
            Assert.False(update.Success);
            Assert.Null(update.Data);
        }

        [Fact]
        public async Task GivenEmptyDatabase_VerifiesFailedDeleteFlow()
        {
            var token = CancellationToken.None;

            var schema = GetSchema();

            var factory = ServiceProvider.GetService<IDynamicRepositoryFactory<DynamicObject>>()!;

            var dResult = factory.CreateRepository(schema);
            Assert.True(dResult.Success, dResult.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(dResult.Data);

            var objectDelete = new DynamicObject(1, 0, 0);

            var delete = await dResult.Data!.Delete(objectDelete, token);
            Assert.False(delete.Success);
            Assert.Null(delete.Data);
        }
    }
}
