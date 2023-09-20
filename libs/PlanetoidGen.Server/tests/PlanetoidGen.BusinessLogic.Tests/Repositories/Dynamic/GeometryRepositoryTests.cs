using Insight.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using PlanetoidGen.Contracts.Factories.Repositories.Dynamic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Factories.Repositories.Dynamic;
using PlanetoidGen.DataAccess.Repositories.Dynamic;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PlanetoidGen.BusinessLogic.Tests.Repositories.Dynamic
{
    public class GeometryRepositoryTests : BaseDynamicRepositoryTests
    {
        private class DynamicGeometryObject : IComparable<DynamicGeometryObject>
        {
            public int GID { get; }
            public string Title { get; }
            public Geometry Geom { get; } // Needs to be Geometry for Insight.Database auto-casting
            public Point GeomPoint => (Point)Geom; // For the actual data
            public DynamicGeometryObject(int gid, string title, Geometry geom)
            {
                GID = gid;
                Title = title;
                Geom = (Point)geom; // Needs a cast here to ensure that we receive a Point
            }
            public override string ToString() => $"DynamicGeometryObject(GID={GID},Title={Title},Geom={Geom.ToText()})";

            public int CompareTo(DynamicGeometryObject? other)
            {
                if (other == null) return 1;
                return GID.CompareTo(other.GID);
            }
        }

        private class DynamicPointObject : IComparable<DynamicPointObject>
        {
            public int GID { get; }
            public string Title { get; }
            public Point Geom { get; }
            public DynamicPointObject(int gid, string title, Point geom)
            {
                GID = gid;
                Title = title;
                Geom = geom;
            }
            public override string ToString() => $"DynamicPointObject(GID={GID},Title={Title},Geom={Geom.ToText()})";

            public int CompareTo(DynamicPointObject? other)
            {
                if (other == null) return 1;
                return GID.CompareTo(other.GID);
            }
        }

        protected override TableSchema GetSchema()
        {
            return new TableSchema("test", "DynamicGeometryObject", new List<ColumnSchema>()
            {
                new ColumnSchema(nameof(DynamicGeometryObject.GID), ColumnSchema.ColumnType.Int32,
                    properties: null,
                    canBeNull: false,
                    usedInCreate: true,
                    usedInRead: true,
                    usedInUpdate: true,
                    usedInDelete: true),
                new ColumnSchema(nameof(DynamicGeometryObject.Title), ColumnSchema.ColumnType.String,
                    properties: null,
                    canBeNull: false,
                    usedInCreate: true,
                    usedInRead: false,
                    usedInUpdate: false,
                    usedInDelete: false),
                new ColumnSchema(nameof(DynamicGeometryObject.Geom), ColumnSchema.ColumnType.Geometry,
                    properties: new Dictionary<string, string>()
                    {
                        { ColumnSchema.PropertyKeys.GeometryType, nameof(Point) },
                        { ColumnSchema.PropertyKeys.SpatialRefSys, "26918" },
                    },
                    canBeNull: false,
                    usedInCreate: true,
                    usedInRead: false,
                    usedInUpdate: false,
                    usedInDelete: false),
            }, new List<IndexSchema>()
            {
                new IndexSchema(
                    IndexSchema.IndexKind.PrimaryKey,
                    new List<string>() { nameof(DynamicGeometryObject.GID) },
                    null),
                new IndexSchema(
                    IndexSchema.IndexKind.Gist,
                    new List<string>() { nameof(DynamicGeometryObject.Geom) },
                    null),
            });
        }

        protected override IServiceCollection ConfigureDynamicRepoFactory(IServiceCollection c)
        {
            return c
                .AddSingleton<IDynamicRepositoryFactory<DynamicGeometryObject>>((provider) =>
                {
                    return new DelegateDynamicRepositoryFactory<DynamicGeometryObject>(
                        provider.GetService<DbConnectionStringBuilder>()!,
                        provider.GetService<IMetaProcedureRepository>()!,
                        provider.GetService<IConfiguration>()!,
                        (schema) => new UniversalTableProcedureGenerator(schema),
                        (schema) => new AnonymousTypeRowSerializer<DynamicGeometryObject>(schema));
                })
                .AddSingleton<IDynamicRepositoryFactory<DynamicPointObject>>((provider) =>
                {
                    return new DelegateDynamicRepositoryFactory<DynamicPointObject>(
                        provider.GetService<DbConnectionStringBuilder>()!,
                        provider.GetService<IMetaProcedureRepository>()!,
                        provider.GetService<IConfiguration>()!,
                        (schema) => new UniversalTableProcedureGenerator(schema),
                        (schema) => new AnonymousTypeRowSerializer<DynamicPointObject>(schema, (r) =>
                        {
                            return new DynamicPointObject(
                                (int)r[nameof(DynamicPointObject.GID)],
                                (string)r[nameof(DynamicPointObject.Title)],
                                (Point)r[nameof(DynamicPointObject.Geom)]
                                );
                        }));
                });
        }

        [Fact]
        public async Task GivenEmptyDatabase_VerifiesMainFlowGeometry()
        {
            var token = CancellationToken.None;

            var schema = GetSchema();

            var factory = ServiceProvider.GetService<IDynamicRepositoryFactory<DynamicGeometryObject>>()!;

            var dResult = factory.CreateRepository(schema);
            Assert.True(dResult.Success, dResult.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(dResult.Data);

            var objectActual = new DynamicGeometryObject(1, "Hello", new Point(1.0, 1.0));
            var objectRead = new DynamicGeometryObject(1, "World", new Point(2.0, 2.0));
            var objectUpdate = new DynamicGeometryObject(1, "World", new Point(3.0, 3.0));

            var insert = await dResult.Data!.Create(objectActual, token);
            Assert.True(insert.Success, insert.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(insert.Data);
            Assert.Equal(objectActual, insert.Data!);
            Assert.IsType<Point>(insert.Data!.Geom);
            Assert.Equal(new Point(1.0, 1.0), insert.Data!.GeomPoint);

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
        public async Task GivenEmptyDatabase_VerifiesCreateMultipleGeometry()
        {
            var token = CancellationToken.None;

            var schema = GetSchema();

            var factory = ServiceProvider.GetService<IDynamicRepositoryFactory<DynamicGeometryObject>>()!;

            var dResult = factory.CreateRepository(schema);
            Assert.True(dResult.Success, dResult.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(dResult.Data);

            var objectActual1 = new DynamicGeometryObject(1, "Hello", new Point(1.0, 1.0));
            var objectActual2 = new DynamicGeometryObject(2, "Goodbye", new Point(4.0, 4.0));
            var objectRead = new DynamicGeometryObject(1, "World", new Point(2.0, 2.0));
            var objectUpdate = new DynamicGeometryObject(1, "World", new Point(3.0, 3.0));

            var insert = await dResult.Data!.CreateMultiple(new DynamicGeometryObject[] { objectActual1, objectActual2 }, false, token);
            Assert.True(insert.Success, insert.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(insert.Data);
            Assert.Equal(2, insert.Data!.Count());
            Assert.Equal(objectActual1, insert.Data!.First());
            Assert.Equal(objectActual2, insert.Data!.Last());
            Assert.All(insert.Data!, x => Assert.IsType<Point>(x.Geom));
            Assert.Equal(new Point(1.0, 1.0), insert.Data!.First().GeomPoint);
            Assert.Equal(new Point(4.0, 4.0), insert.Data!.Last().GeomPoint);

            var read = await dResult.Data!.Read(objectRead, token);
            Assert.True(read.Success, read.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(read.Data);
            Assert.Equal(objectActual1, read.Data!);

            var update = await dResult.Data!.Update(objectUpdate, token);
            Assert.True(update.Success, update.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(update.Data);
            Assert.Equal(objectUpdate, update.Data!);

            var delete1 = await dResult.Data!.Delete(objectRead, token);
            Assert.True(delete1.Success, delete1.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(delete1.Data);
            Assert.Equal(objectUpdate, delete1.Data!);

            var delete2 = await dResult.Data!.Delete(objectActual2, token);
            Assert.True(delete2.Success, delete2.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(delete2.Data);
            Assert.Equal(objectActual2, delete2.Data!);
        }

        [Fact]
        public async Task GivenEmptyDatabase_VerifiesMainFlowPoint()
        {
            var token = CancellationToken.None;

            var schema = GetSchema();

            var factory = ServiceProvider.GetService<IDynamicRepositoryFactory<DynamicPointObject>>()!;

            var dResult = factory.CreateRepository(schema);
            Assert.True(dResult.Success, dResult.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(dResult.Data);

            var objectActual = new DynamicPointObject(1, "Hello", new Point(1.0, 1.0));
            var objectRead = new DynamicPointObject(1, "World", new Point(2.0, 2.0));
            var objectUpdate = new DynamicPointObject(1, "World", new Point(3.0, 3.0));

            var insert = await dResult.Data!.Create(objectActual, token);
            Assert.True(insert.Success, insert.ErrorMessage?.ToString() ?? "");
            Assert.NotNull(insert.Data);
            Assert.Equal(objectActual, insert.Data!);
            Assert.Equal(new Point(1.0, 1.0), insert.Data!.Geom);

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
        public async Task GivenEmptyDatabase_ExecuteGeometrySql()
        {
            var token = CancellationToken.None;

            var builder = ServiceProvider.GetService<DbConnectionStringBuilder>()!;

            var meta = ServiceProvider.GetService<IMetaProcedureRepository>()!;

            // To enable postgis
            var metaExists = await meta.EnsureExistsAsync(token);
            Assert.True(metaExists.Success, metaExists.ErrorMessage?.ToString() ?? "");

            using (var c = await builder.OpenAsync(token))
            {
                var sql = "SELECT st_makepoint(1,1)";

                var command = c.CreateCommandSql(sql);
                command.Prepare();

                var reader = await command.GetReaderAsync(System.Data.CommandBehavior.SingleRow, token);
                Assert.True(reader.Read());

                Assert.Equal(reader.GetValue(0), new Point(new Coordinate(1d, 1d)));
            }
        }

        private class DynamicSqlObject
        {
            public int GID { get; }
            public Geometry Geom { get; }
            public DynamicSqlObject(int gid, Geometry geom)
            {
                GID = gid;
                Geom = geom;
            }
        }

        [Fact]
        public async Task GivenEmptyDatabase_ExecuteDynamicSqlObject()
        {
            var token = CancellationToken.None;

            var builder = ServiceProvider.GetService<DbConnectionStringBuilder>()!;

            var meta = ServiceProvider.GetService<IMetaProcedureRepository>()!;

            // To enable postgis
            var metaExists = await meta.EnsureExistsAsync(token);
            Assert.True(metaExists.Success, metaExists.ErrorMessage?.ToString() ?? "");

            using (var c = await builder.OpenAsync(token))
            {
                const string schema = "test";

                string[] sqlCreate = new string[]
                {
                    $"DROP FUNCTION IF EXISTS {schema}.dynamicsqlobjectcreate(integer, geometry);",
                    $"DROP INDEX IF EXISTS {schema}.dynamicsqlobject_geom_gist;",
                    $"DROP TABLE IF EXISTS {schema}.dynamicsqlobject CASCADE;",
                    $"CREATE SCHEMA IF NOT EXISTS {schema};",
                    "CREATE EXTENSION IF NOT EXISTS postgis;",
                    $@"
CREATE TABLE IF NOT EXISTS {schema}.dynamicsqlobject
(
    gid integer NOT NULL,
    geom geometry(Point,26918) NOT NULL,
    CONSTRAINT dynamicsqlobject_pk PRIMARY KEY (gid)
);",
                    $@"
CREATE INDEX IF NOT EXISTS dynamicsqlobject_geom_gist
    ON {schema}.dynamicsqlobject USING gist
    (geom);",
                    $@"
CREATE OR REPLACE FUNCTION {schema}.dynamicsqlobjectcreate(
	dgid integer,
	dgeom geometry)
    RETURNS {schema}.dynamicsqlobject
    LANGUAGE 'sql'
AS $BODY$
    INSERT INTO {schema}.dynamicsqlobject(gid, geom)
    VALUES (dgid, dgeom)
    RETURNING *;
$BODY$;",
                };

                foreach (var sql in sqlCreate)
                {
                    var command = c.CreateCommandSql(sql);
                    command.Prepare();
                    var setupResult = await command.QueryAsync(cancellationToken: token);
                    Assert.NotNull(setupResult);
                }

                await Assert.ThrowsAnyAsync<Exception>(async () =>
                {
                    var result = await c.SingleAsync<DynamicSqlObject>(
                        $"{schema}.dynamicsqlobjectcreate",
                        new { dgid = 1, dgeom = new Point(1.0, 1.0) },
                        cancellationToken: token);

                    Assert.NotNull(result);
                    Assert.Equal(1, result.GID);
                    Assert.Equal(new Point(1.0, 1.0), Assert.IsType<Point>(result.Geom));
                });

                {
                    var result = await c.SingleAsync<DynamicSqlObject>(
                        $"{schema}.dynamicsqlobjectcreate",
                        new { dgid = 2, dgeom = (object)new Point(2.0, 2.0) },
                        cancellationToken: token);

                    Assert.NotNull(result);
                    Assert.Equal(2, result.GID);
                    Assert.Equal(new Point(2.0, 2.0), Assert.IsType<Point>(result.Geom));
                }

                {
                    const string sqlDelete = $@"
DROP FUNCTION IF EXISTS {schema}.dynamicsqlobjectcreate(integer, geometry);
DROP TABLE IF EXISTS {schema}.dynamicsqlobject CASCADE;";

                    var command = c.CreateCommandSql(sqlDelete);
                    command.Prepare();
                    var terminateResult = await command.QueryAsync(cancellationToken: token);
                    Assert.NotNull(terminateResult);
                }
            }
        }
    }
}
