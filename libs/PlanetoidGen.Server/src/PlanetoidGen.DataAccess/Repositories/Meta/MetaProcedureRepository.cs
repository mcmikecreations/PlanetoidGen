using Insight.Database;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Npgsql;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Services.Meta;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Constants.StringMessages;
using PlanetoidGen.Domain.Models.Documents;
using PlanetoidGen.Domain.Models.Generation;
using PlanetoidGen.Domain.Models.Info;
using PlanetoidGen.Domain.Models.Meta;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using SqlF = System.Func<
    Insight.Database.DbConnectionWrapper,
    System.Threading.CancellationToken,
    System.Threading.Tasks.Task<PlanetoidGen.Contracts.Models.Result>
    >;

namespace PlanetoidGen.DataAccess.Repositories.Meta
{
    public class MetaProcedureRepository : RepositoryAccessWrapper, IMetaProcedureRepository
    {
        private readonly AsyncLock _mutex = new AsyncLock();

        private bool _initialized;

        public MetaProcedureOptions Options { get; }

        public MetaProcedureRepository(DbConnectionStringBuilder connection, IOptions<MetaProcedureOptions> options)
            : base(connection, null)
        {
            _static = this;
            Options = options.Value;
            _initialized = false;
        }

        private async ValueTask<Result> ExistsInternal(string sql, object parameters, IDbConnection connection, CancellationToken token)
        {
            var result = await RunSingleQueryRaw<string>(sql, connection, token, parameters: parameters);
            return result.Success ? Result.CreateSuccess() : Result.CreateFailure(result);
        }

        public async ValueTask<Result> SchemaExists(string name, CancellationToken token, IDbConnection? connection = null)
        {
            var sql = $"SELECT schema_name FROM information_schema.schemata WHERE schema_name = LOWER(@SchemaName);";

            if (connection != null)
            {
                return await ExistsInternal(sql, new { SchemaName = name, }, connection, token);
            }
            else
            {
                using (var c = await _connection.OpenAsync())
                {
                    return await ExistsInternal(sql, new { SchemaName = name, }, c, token);
                }
            }
        }

        public async ValueTask<Result> ExtensionExists(string name, CancellationToken token, IDbConnection? connection = null)
        {
            var sql = $"SELECT extname FROM pg_extension WHERE extname = LOWER(@ExtensionName);";

            if (connection != null)
            {
                return await ExistsInternal(sql, new { ExtensionName = name, }, connection, token);
            }
            else
            {
                using (var c = await _connection.OpenAsync())
                {
                    return await ExistsInternal(sql, new { ExtensionName = name, }, c, token);
                }
            }
        }

        public async ValueTask<Result> TableExists(string schemaName, string tableName, CancellationToken token, IDbConnection? connection = null)
        {
            var sql = $"SELECT tablename FROM pg_tables WHERE schemaname = LOWER(@SchemaName) AND tablename = LOWER(@TableName);";

            if (connection != null)
            {
                return await ExistsInternal(sql, new { SchemaName = schemaName, TableName = tableName, }, connection, token);
            }
            else
            {
                using (var c = await _connection.OpenAsync())
                {
                    return await ExistsInternal(sql, new { SchemaName = schemaName, TableName = tableName, }, c, token);
                }
            }
        }

        public ValueTask<Result> FunctionNameExists(string schemaName, string functionName, CancellationToken token, IDbConnection? connection = null)
        {
            return FunctionNameExists($"{schemaName}.{functionName}", token, connection);
        }

        public async ValueTask<Result> FunctionNameExists(string functionName, CancellationToken token, IDbConnection? connection = null)
        {
            var sql = $"SELECT LOWER(@FunctionName)::regproc::text;";

            try
            {
                if (connection != null)
                {
                    return await ExistsInternal(sql, new { FunctionName = functionName, }, connection, token);
                }
                else
                {
                    using (var c = await _connection.OpenAsync())
                    {
                        return await ExistsInternal(sql, new { FunctionName = functionName, }, c, token);
                    }
                }
            }
            catch (PostgresException e)
            {
                return Result.CreateFailure(e);
            }
        }

        public ValueTask<bool> ExistsAsync(CancellationToken token)
        {
            return new ValueTask<bool>(_initialized);
        }

        public async ValueTask<Result> EnsureExistsAsync(CancellationToken token)
        {
            if (_initialized)
            {
                return Result.CreateSuccess();
            }

            using (await _mutex.LockAsync(token))
            {
                if (_initialized)
                {
                    return Result.CreateSuccess();
                }

                using (var c = await _connection.OpenWithTransactionAsync(token))
                {
                    var result = await CreateObjects(c, token);

                    if (!result.Success)
                    {
                        return result;
                    }

                    c.Commit();
                    _initialized = true;
                }

                return Result.CreateSuccess();
            }
        }

        #region Initialization
        private async Task<Result> CreateObjects(DbConnectionWrapper c, CancellationToken token)
        {
            Result result;

            var schemaExistsResult = await SchemaExists("public", token);
            var tableExistsResult = await TableExists("public", TableStringMessages.PlanetoidInfo, token);
            var extensionExists = (await ExtensionExists("postgis", token)).Success
                               && (await ExtensionExists("uuid-ossp", token)).Success;

            bool recreateSchemas = Options.RecreateSchemas || !schemaExistsResult.Success;
            bool recreateExtensions = Options.RecreateExtensions || !extensionExists || recreateSchemas;
            bool recreateTables = Options.RecreateTables || !tableExistsResult.Success || recreateExtensions;
            bool recreateProcedures = Options.RecreateProcedures || recreateTables;

            IEnumerable<SqlF> tasks = new List<SqlF>();

            if (recreateSchemas)
            {
                tasks.Concat(new SqlF[]
                {
                    DropSchemas,
                    CreateSchemas,
                });
            }

            if (recreateExtensions)
            {
                tasks = new SqlF[] { DropExtensions }
                    .Concat(tasks)
                    .Concat(new SqlF[] { CreateExtensions });
            }

            if (recreateTables)
            {
                tasks = new SqlF[] { DropAllTables }
                    .Concat(tasks)
                    .Concat(new SqlF[]
                    {
                        CreateTablePlanetoidInfo,
                        CreateTableGenerationLODs,
                        CreateTableAgentInfo,
                        CreateTableTileInfo,
                        CreateTableFileInfo,
                        CreateTableTileBasedFileInfo,
                        CreateTableFileDependency,
                        CreateTableMetaDynamic,
                    });
            }

            if (recreateProcedures)
            {
                tasks = new SqlF[] { DropAllTableProcedures }
                    .Concat(tasks)
                    .Concat(new SqlF[]
                    {
                        CreateProcedureTileInfoSelect,
                        CreateProcedureTileInfoInsert,
                        CreateProcedureTileInfoLastModfiedInfoUpdate,

                        CreateProcedureGenerationLODsInsert,
                        CreateProcedureGenerationLODsSelect,
                        CreateProcedureGenerationLODsClear,

                        CreateProcedureSpatialReferenceSystemCountCustom,
                        CreateProcedureSpatialReferenceSystemClearCustom,
                        CreateProcedureSpatialReferenceSystemDelete,
                        CreateProcedureSpatialReferenceSystemInsertOrUpdate,
                        CreateProcedureSpatialReferenceSystemGet,

                        CreateProcedureAgentInfoInsert,
                        CreateProcedureAgentInfoSelect,
                        CreateProcedureAgentInfoSelectByIndex,
                        CreateProcedureAgentInfoClear,

                        CreateProcedurePlanetoidInfoInsert,
                        CreateProcedurePlanetoidInfoSelect,
                        CreateProcedurePlanetoidInfoSelectAll,
                        CreateProcedurePlanetoidInfoDelete,
                        CreateProcedurePlanetoidInfoClear,

                        CreateProcedureMetaDynamicInsert,
                        CreateProcedureMetaDynamicSelect,
                        CreateProcedureMetaDynamicDelete,
                        CreateProcedureMetaDynamicClear,

                        CreateProcedureFileInfoInsert,
                        CreateProcedureFileInfoSelect,
                        CreateProcedureFileInfoDelete,
                        CreateProcedureFileInfoExists,

                        CreateProcedureTileBasedFileInfoInsert,
                        CreateProcedureTileBasedFileInfoSelectById,
                        CreateProcedureTileBasedFileInfoSelectAllByTile,
                        CreateProcedureTileBasedFileInfoDelete,
                        CreateProcedureTileBasedFileInfoDeleteAllByTile,

                        CreateProcedureFileDependencyInsert,
                        CreateProcedureFileDependencySelect,
                        CreateProcedureFileDependencyCountByReferenceId,
                        CreateProcedureFileDependencyDelete
                    });
            }

            foreach (var task in tasks)
            {
                result = await task(c, token);
                if (!result.Success)
                {
                    return result;
                }
            }

            return Result.CreateSuccess();
        }

        #region Tables
        private async Task<Result> DropAllTables(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
DROP TABLE IF EXISTS public.{TableStringMessages.MetaDynamic} CASCADE;
DROP TABLE IF EXISTS public.{TableStringMessages.TileInfo} CASCADE;
DROP TABLE IF EXISTS public.{TableStringMessages.AgentInfo} CASCADE;
DROP TABLE IF EXISTS public.{TableStringMessages.GenerationLODs} CASCADE;
DROP TABLE IF EXISTS public.{TableStringMessages.PlanetoidInfo} CASCADE;
DROP TABLE IF EXISTS public.{TableStringMessages.FileDependency} CASCADE;
DROP TABLE IF EXISTS public.{TableStringMessages.TileBasedFileInfo} CASCADE;
DROP TABLE IF EXISTS public.{TableStringMessages.FileInfo} CASCADE;
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> DropSchemas(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
DROP SCHEMA IF EXISTS public CASCADE;";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateSchemas(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE SCHEMA IF NOT EXISTS public;";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> DropExtensions(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
DROP EXTENSION IF EXISTS ""uuid-ossp"" CASCADE;";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateExtensions(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"" SCHEMA public CASCADE;
CREATE EXTENSION IF NOT EXISTS postgis SCHEMA public CASCADE;";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateTableGenerationLODs(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE TABLE IF NOT EXISTS public.{TableStringMessages.GenerationLODs}
(
    {nameof(GenerationLODModel.PlanetoidId)} integer NOT NULL,
    {nameof(GenerationLODModel.LOD)} smallint NOT NULL,
    {nameof(GenerationLODModel.Z)} smallint NOT NULL,
    CONSTRAINT {TableStringMessages.GenerationLODs}_pk PRIMARY KEY({nameof(GenerationLODModel.PlanetoidId)},{nameof(GenerationLODModel.LOD)}),
    CONSTRAINT {TableStringMessages.GenerationLODs}_{nameof(GenerationLODModel.PlanetoidId)}_fk FOREIGN KEY({nameof(GenerationLODModel.PlanetoidId)})
        REFERENCES public.{TableStringMessages.PlanetoidInfo}({nameof(PlanetoidInfoModel.Id)}) MATCH FULL
);

ALTER TABLE IF EXISTS public.{TableStringMessages.GenerationLODs}
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateTableAgentInfo(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE TABLE IF NOT EXISTS public.{TableStringMessages.AgentInfo}
(
    {nameof(AgentInfoModel.PlanetoidId)} integer NOT NULL,
    {nameof(AgentInfoModel.IndexId)} integer NOT NULL,
    {nameof(AgentInfoModel.Title)} text NOT NULL,
    {nameof(AgentInfoModel.Settings)} text NOT NULL,
    {nameof(AgentInfoModel.ShouldRerunIfLast)} boolean NOT NULL,
    CONSTRAINT {TableStringMessages.AgentInfo}_pk PRIMARY KEY({nameof(AgentInfoModel.PlanetoidId)},{nameof(AgentInfoModel.IndexId)}),
    CONSTRAINT {TableStringMessages.AgentInfo}_{nameof(AgentInfoModel.PlanetoidId)}_fk FOREIGN KEY({nameof(AgentInfoModel.PlanetoidId)})
        REFERENCES public.{TableStringMessages.PlanetoidInfo}({nameof(PlanetoidInfoModel.Id)}) MATCH FULL
);

ALTER TABLE IF EXISTS public.{TableStringMessages.AgentInfo}
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateTableTileInfo(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE TABLE IF NOT EXISTS public.{TableStringMessages.TileInfo}
(
    {nameof(TileInfoModel.PlanetoidId)} integer NOT NULL,
    {nameof(TileInfoModel.Z)} smallint NOT NULL,
    {nameof(TileInfoModel.X)} bigint NOT NULL,
    {nameof(TileInfoModel.Y)} bigint NOT NULL,
    {nameof(TileInfoModel.LastAgent)} integer,
    {nameof(TileInfoModel.Id)} text NOT NULL,
    {nameof(TileInfoModel.CreatedDate)} timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    {nameof(TileInfoModel.ModifiedDate)} timestamp with time zone,
    CONSTRAINT {TableStringMessages.TileInfo}_pk PRIMARY KEY({nameof(TileInfoModel.PlanetoidId)}, {nameof(TileInfoModel.Z)}, {nameof(TileInfoModel.X)}, {nameof(TileInfoModel.Y)}),
    CONSTRAINT {TableStringMessages.TileInfo}_{nameof(TileInfoModel.PlanetoidId)}_fk FOREIGN KEY({nameof(TileInfoModel.PlanetoidId)})
        REFERENCES public.{TableStringMessages.PlanetoidInfo}({nameof(PlanetoidInfoModel.Id)}) MATCH FULL
);

ALTER TABLE IF EXISTS public.{TableStringMessages.TileInfo}
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateTablePlanetoidInfo(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE TABLE IF NOT EXISTS public.{TableStringMessages.PlanetoidInfo}
(
    {nameof(PlanetoidInfoModel.Id)} integer NOT NULL,
    {nameof(PlanetoidInfoModel.Title)} text NOT NULL,
    {nameof(PlanetoidInfoModel.Seed)} bigint NOT NULL,
    {nameof(PlanetoidInfoModel.Radius)} double precision NOT NULL,
    CONSTRAINT {TableStringMessages.PlanetoidInfo}_pk PRIMARY KEY({nameof(PlanetoidInfoModel.Id)})
);

ALTER TABLE IF EXISTS public.{TableStringMessages.PlanetoidInfo}
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateTableMetaDynamic(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE TABLE IF NOT EXISTS public.{TableStringMessages.MetaDynamic}
(
    {nameof(MetaDynamicModel.Id)} integer NOT NULL,
    {nameof(MetaDynamicModel.PlanetoidId)} integer NOT NULL,
    {nameof(MetaDynamicModel.Schema)} text NOT NULL,
    {nameof(MetaDynamicModel.Title)} text NOT NULL,
    {nameof(MetaDynamicModel.Columns)} text NOT NULL,
    CONSTRAINT {TableStringMessages.MetaDynamic}_pk PRIMARY KEY({nameof(MetaDynamicModel.Id)})
);

ALTER TABLE IF EXISTS public.{TableStringMessages.MetaDynamic}
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateTableFileInfo(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE TABLE IF NOT EXISTS public.{TableStringMessages.FileInfo}
(
    {nameof(FileInfoModel.FileId)} text NOT NULL,
    {nameof(FileInfoModel.ModifiedOn)} timestamp NOT NULL,
    CONSTRAINT {TableStringMessages.FileInfo}_pk PRIMARY KEY({nameof(FileInfoModel.FileId)})
);

ALTER TABLE IF EXISTS public.{TableStringMessages.FileInfo}
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateTableTileBasedFileInfo(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE TABLE IF NOT EXISTS public.{TableStringMessages.TileBasedFileInfo}
(
    {nameof(TileBasedFileInfoModel.FileId)} text NOT NULL,
    {nameof(TileBasedFileInfoModel.PlanetoidId)} integer NOT NULL,
    {nameof(TileBasedFileInfoModel.Z)} smallint NOT NULL,
    {nameof(TileBasedFileInfoModel.X)} bigint NOT NULL,
    {nameof(TileBasedFileInfoModel.Y)} bigint NOT NULL,
    {nameof(TileBasedFileInfoModel.Position)} double precision[] NOT NULL,
    {nameof(TileBasedFileInfoModel.Rotation)} double precision[] NOT NULL,
    {nameof(TileBasedFileInfoModel.Scale)} double precision[] NOT NULL,
    CONSTRAINT {TableStringMessages.TileBasedFileInfo}_{nameof(TileBasedFileInfoModel.FileId)}_fk FOREIGN KEY({nameof(TileBasedFileInfoModel.FileId)})
        REFERENCES public.{TableStringMessages.FileInfo}({nameof(FileInfoModel.FileId)}) MATCH FULL,
    CONSTRAINT {TableStringMessages.TileBasedFileInfo}_{nameof(TableStringMessages.TileInfo)}_fk FOREIGN KEY({nameof(TileBasedFileInfoModel.PlanetoidId)}, {nameof(TileBasedFileInfoModel.Z)}, {nameof(TileBasedFileInfoModel.X)}, {nameof(TileBasedFileInfoModel.Y)})
        REFERENCES public.{TableStringMessages.TileInfo}({nameof(TileInfoModel.PlanetoidId)}, {nameof(TileInfoModel.Z)}, {nameof(TileInfoModel.X)}, {nameof(TileInfoModel.Y)}) MATCH FULL
);

ALTER TABLE IF EXISTS public.{TableStringMessages.TileBasedFileInfo}
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateTableFileDependency(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE TABLE IF NOT EXISTS public.{TableStringMessages.FileDependency}
(
    {nameof(FileDependencyModel.FileId)} text NOT NULL,
    {nameof(FileDependencyModel.ReferencedFileId)} text NOT NULL,
    {nameof(FileDependencyModel.IsRequired)} boolean NOT NULL,
    {nameof(FileDependencyModel.IsDynamic)} boolean NOT NULL,
    CONSTRAINT {TableStringMessages.FileDependency}_{nameof(FileDependencyModel.FileId)}_fk FOREIGN KEY({nameof(FileDependencyModel.FileId)})
        REFERENCES public.{TableStringMessages.FileInfo}({nameof(FileInfoModel.FileId)}) MATCH FULL,
    CONSTRAINT {TableStringMessages.FileDependency}_{nameof(FileDependencyModel.ReferencedFileId)}_fk FOREIGN KEY({nameof(FileDependencyModel.ReferencedFileId)})
        REFERENCES public.{TableStringMessages.FileInfo}({nameof(FileInfoModel.FileId)}) MATCH FULL
);

ALTER TABLE IF EXISTS public.{TableStringMessages.FileInfo}
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }
        #endregion

        #region Stored Table Procedures
        private async Task<Result> DropAllTableProcedures(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.MetaDynamicInsert}(integer,text,text,text);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.MetaDynamicSelectById}(integer);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.MetaDynamicSelectByName}(integer,text,text);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.MetaDynamicDelete}(integer);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.MetaDynamicClear}();

DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.TileInfoSelect}(integer, smallint, bigint, bigint);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.TileInfoInsert}(integer, smallint, bigint, bigint);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.TileInfoLastModfiedInfoUpdate}(text, integer, timestamp with time zone);

DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.AgentInfoInsert}(integer,text,text,boolean);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.AgentInfoSelect}(integer);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.AgentInfoSelectByIndex}(integer,integer);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.AgentInfoClear}(integer);

DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.SpatialReferenceSystemCountCustom}(integer, integer);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.SpatialReferenceSystemClearCustom}(integer, integer);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.SpatialReferenceSystemDelete}(integer);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.SpatialReferenceSystemSelectByAuthority}(text, integer);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.SpatialReferenceSystemSelectBySrid}(integer);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.SpatialReferenceSystemInsertOrUpdate}(integer, text, integer, text, text);

DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.PlanetoidInfoInsert}(text);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.PlanetoidInfoSelect}(integer);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.PlanetoidInfoSelectAll}();
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.PlanetoidInfoDelete}(integer);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.PlanetoidInfoClear}();

DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.GenerationLODInsert}(integer, smallint, smallint);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.GenerationLODSelect}(integer);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.GenerationLODClear}(integer);

DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.FileInfoInsert}(text);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.FileInfoSelect}(text);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.FileInfoDelete}(text);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.FileInfoExists}(text);

DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.TileBasedFileInfoInsert}(text, integer, smallint, bigint, bigint, text, text, text);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.TileBasedFileInfoSelectById}(text);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.TileBasedFileInfoSelectAllByTile}(text, integer, smallint, bigint, bigint);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.TileBasedFileInfoDelete}(text);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.TileBasedFileInfoDeleteAllByTile}(text, integer, smallint, bigint, bigint);

DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.FileDependencyInsert}(text, text, boolean, boolean);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.FileDependencySelect}(text, boolean, boolean);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.FileDependencyDelete}(text);
DROP FUNCTION IF EXISTS public.{StoredProcedureStringMessages.FileDependencyCountByReferenceId}(text);
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureTileInfoSelect(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.TileInfoSelect}(
    dplanetoidId integer,
    dz smallint,
    dx bigint,
    dy bigint)
    RETURNS SETOF public.{TableStringMessages.TileInfo}
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    IF NOT EXISTS (SELECT * FROM public.{TableStringMessages.TileInfo} WHERE
        {nameof(TileInfoModel.PlanetoidId)} = dplanetoidId AND
        {nameof(TileInfoModel.Z)} = dz AND
        {nameof(TileInfoModel.X)} = dx AND
        {nameof(TileInfoModel.Y)} = dy) THEN
        PERFORM * FROM public.{StoredProcedureStringMessages.TileInfoInsert}(dplanetoidId, dz, dx, dy);
    END IF;
    RETURN QUERY SELECT * FROM public.{TableStringMessages.TileInfo} WHERE
        {nameof(TileInfoModel.PlanetoidId)} = dplanetoidId AND
        {nameof(TileInfoModel.Z)} = dz AND
        {nameof(TileInfoModel.X)} = dx AND
        {nameof(TileInfoModel.Y)} = dy
        ORDER BY {nameof(TileInfoModel.Id)}
        FETCH FIRST 1 ROWS ONLY;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.TileInfoSelect}(integer, smallint, bigint, bigint)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureTileInfoInsert(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.TileInfoInsert}(
    planetoidId integer,
    z smallint,
    x bigint,
    y bigint)
    RETURNS text
    LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    tile_id text;
BEGIN
    tile_id = uuid_generate_v5(uuid_ns_oid(), FORMAT('%s;%s;%s;%s',planetoidId::text,z::text,x::text,y::text));
    INSERT INTO public.{TableStringMessages.TileInfo}({nameof(TileInfoModel.PlanetoidId)},{nameof(TileInfoModel.Z)},{nameof(TileInfoModel.X)},{nameof(TileInfoModel.Y)},{nameof(TileInfoModel.LastAgent)},{nameof(TileInfoModel.Id)})
    VALUES (planetoidId,z,x,y,NULL,tile_id)
    ON CONFLICT ON CONSTRAINT {TableStringMessages.TileInfo}_pk 
    DO NOTHING;
    RETURN tile_id;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.TileInfoInsert}(integer,smallint, bigint, bigint)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureTileInfoLastModfiedInfoUpdate(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.TileInfoLastModfiedInfoUpdate}(
    tileId text,
    lastAgentIndex integer,
    lastModifiedDate timestamp with time zone DEFAULT NULL)
    RETURNS SETOF public.{TableStringMessages.TileInfo}
    LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    tile_id text;
BEGIN
    UPDATE public.{TableStringMessages.TileInfo}
    SET {nameof(TileInfoModel.LastAgent)} = lastAgentIndex, {nameof(TileInfoModel.ModifiedDate)} = lastModifiedDate
    WHERE {nameof(TileInfoModel.Id)} = tileId
    RETURNING {nameof(TileInfoModel.Id)}
    INTO tile_id;

    RETURN QUERY SELECT * FROM public.{TableStringMessages.TileInfo}
        WHERE {nameof(TileInfoModel.Id)} = tile_id
        FETCH FIRST 1 ROWS ONLY;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.TileInfoLastModfiedInfoUpdate}(text, integer, timestamp with time zone)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureAgentInfoInsert(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.AgentInfoInsert}(
        dplanetoidId integer,
        name text,
        settings text,
        shouldRerunIfLast boolean)
    RETURNS integer
    LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    order_id integer;
BEGIN
    order_id = (SELECT COUNT(*) FROM public.{TableStringMessages.AgentInfo} WHERE {nameof(AgentInfoModel.PlanetoidId)} IN (dplanetoidId));
    INSERT INTO public.{TableStringMessages.AgentInfo}({nameof(AgentInfoModel.PlanetoidId)}, {nameof(AgentInfoModel.IndexId)}, {nameof(AgentInfoModel.Title)}, {nameof(AgentInfoModel.Settings)}, {nameof(AgentInfoModel.ShouldRerunIfLast)})
        VALUES (dplanetoidId, order_id, name, settings, shouldRerunIfLast);
    RETURN order_id + 1;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.AgentInfoInsert}(integer,text,text,boolean)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureAgentInfoSelect(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.AgentInfoSelect}(
        dplanetoidId integer)
    RETURNS SETOF public.{TableStringMessages.AgentInfo}
    LANGUAGE 'sql'
AS $BODY$
SELECT {nameof(AgentInfoModel.PlanetoidId)}, {nameof(AgentInfoModel.IndexId)}, {nameof(AgentInfoModel.Title)}, {nameof(AgentInfoModel.Settings)}, {nameof(AgentInfoModel.ShouldRerunIfLast)}
    FROM public.{TableStringMessages.AgentInfo}
    WHERE {nameof(AgentInfoModel.PlanetoidId)} IN (dplanetoidId)
    ORDER BY {nameof(AgentInfoModel.IndexId)};
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.AgentInfoSelect}(integer)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureAgentInfoSelectByIndex(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.AgentInfoSelectByIndex}(
        dplanetoidId integer, dagentIndex integer)
    RETURNS SETOF public.{TableStringMessages.AgentInfo}
    LANGUAGE 'sql'
AS $BODY$
SELECT {nameof(AgentInfoModel.PlanetoidId)}, {nameof(AgentInfoModel.IndexId)}, {nameof(AgentInfoModel.Title)}, {nameof(AgentInfoModel.Settings)}, {nameof(AgentInfoModel.ShouldRerunIfLast)}
FROM public.{TableStringMessages.AgentInfo}
WHERE {nameof(AgentInfoModel.PlanetoidId)} IN (dplanetoidId) AND {nameof(AgentInfoModel.IndexId)} IN (dagentIndex)
FETCH FIRST 1 ROWS ONLY
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.AgentInfoSelectByIndex}(integer, integer)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureAgentInfoClear(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.AgentInfoClear}(
    dplanetoidId integer)
    RETURNS boolean
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    DELETE FROM public.{TableStringMessages.AgentInfo} WHERE
        {nameof(AgentInfoModel.PlanetoidId)} IN (dplanetoidId);
    RETURN TRUE;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.AgentInfoClear}(integer)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureGenerationLODsInsert(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.GenerationLODInsert}(
    dplanetoidId integer, dlod smallint, dz smallint)
    RETURNS integer
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    INSERT INTO public.{TableStringMessages.GenerationLODs}({nameof(GenerationLODModel.PlanetoidId)}, {nameof(GenerationLODModel.LOD)}, {nameof(GenerationLODModel.Z)})
        VALUES (dplanetoidId, dlod, dz);
    RETURN (SELECT COUNT(*) FROM public.{TableStringMessages.GenerationLODs} WHERE {nameof(GenerationLODModel.PlanetoidId)} IN (dplanetoidId));
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.GenerationLODInsert}(integer, smallint, smallint)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureGenerationLODsSelect(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.GenerationLODSelect}(
    dplanetoidId integer)
    RETURNS SETOF public.{TableStringMessages.GenerationLODs}
    LANGUAGE 'sql'
AS $BODY$
SELECT {nameof(GenerationLODModel.PlanetoidId)}, {nameof(GenerationLODModel.LOD)}, {nameof(GenerationLODModel.Z)}
    FROM public.{TableStringMessages.GenerationLODs}
    WHERE {nameof(GenerationLODModel.PlanetoidId)} IN (dplanetoidId)
    ORDER BY {nameof(GenerationLODModel.LOD)};
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.GenerationLODSelect}(integer)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureGenerationLODsClear(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.GenerationLODClear}(
        dplanetoidId integer)
    RETURNS integer
    LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    model_count integer;
BEGIN
model_count = (SELECT COUNT(*) FROM public.{TableStringMessages.GenerationLODs} WHERE {nameof(GenerationLODModel.PlanetoidId)} IN (dplanetoidId));
DELETE FROM public.{TableStringMessages.GenerationLODs} WHERE {nameof(GenerationLODModel.PlanetoidId)} IN (dplanetoidId);
RETURN model_count;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.GenerationLODClear}(integer)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureSpatialReferenceSystemCountCustom(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.SpatialReferenceSystemCountCustom}(dmin integer, dmax integer)
    RETURNS integer
    LANGUAGE 'sql'
AS $BODY$
SELECT COUNT(*) FROM public.{TableStringMessages.SpatialReferenceSystems} WHERE srid >= dmin AND srid < dmax;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.SpatialReferenceSystemCountCustom}(integer, integer)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureSpatialReferenceSystemClearCustom(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.SpatialReferenceSystemClearCustom}(dmin integer, dmax integer)
    RETURNS integer
    LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    model_count integer;
BEGIN
model_count = (SELECT COUNT(*) FROM public.{TableStringMessages.SpatialReferenceSystems} WHERE srid >= dmin AND srid < dmax);
DELETE FROM public.{TableStringMessages.SpatialReferenceSystems} WHERE srid >= dmin AND srid < dmax;
RETURN model_count;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.SpatialReferenceSystemClearCustom}(integer, integer)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureSpatialReferenceSystemDelete(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.SpatialReferenceSystemDelete}(
    dsrid integer)
    RETURNS boolean
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
IF EXISTS (SELECT * FROM public.{TableStringMessages.SpatialReferenceSystems} WHERE srid=dsrid) THEN
    DELETE FROM public.{TableStringMessages.SpatialReferenceSystems} WHERE srid=dsrid;
    RETURN TRUE;
ELSE
    RETURN FALSE;
END IF;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.SpatialReferenceSystemDelete}(integer)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureSpatialReferenceSystemGet(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.SpatialReferenceSystemSelectBySrid}(
    dsrid integer)
    RETURNS SETOF public.{TableStringMessages.SpatialReferenceSystems}
    LANGUAGE 'sql'
AS $BODY$
SELECT srid, auth_name, auth_srid, srtext, proj4text
    FROM public.{TableStringMessages.SpatialReferenceSystems}
    WHERE srid = dsrid;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.SpatialReferenceSystemSelectBySrid}(integer)
    OWNER TO {_connection["User Id"]};
            ";

            var result = await RunQuery(sql, c, token);

            if (!result.Success)
            {
                return result;
            }

            sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.SpatialReferenceSystemSelectByAuthority}(
    dauthName text, dauthSrid integer)
    RETURNS SETOF public.{TableStringMessages.SpatialReferenceSystems}
    LANGUAGE 'sql'
AS $BODY$
SELECT srid, auth_name, auth_srid, srtext, proj4text
    FROM public.{TableStringMessages.SpatialReferenceSystems}
    WHERE auth_name = dauthName AND auth_srid = dauthSrid
    ORDER BY srid
    FETCH FIRST 1 ROWS ONLY;;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.SpatialReferenceSystemSelectByAuthority}(text, integer)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureSpatialReferenceSystemInsertOrUpdate(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.SpatialReferenceSystemInsertOrUpdate}(
    dsrid integer, dauthName text, dauthSrid integer, dwktString text, dproj4String text)
    RETURNS integer
    LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    model_id integer;
    old_model public.{TableStringMessages.SpatialReferenceSystems} = NULL;
BEGIN
    SELECT * INTO old_model FROM public.{TableStringMessages.SpatialReferenceSystems} WHERE
        srid = dsrid;
    IF old_model IS NOT NULL THEN
        UPDATE public.{TableStringMessages.SpatialReferenceSystems}
            SET auth_name = dauthName, auth_srid = dauthSrid, srtext = dwktString, proj4text = dproj4String
            WHERE srid = dsrid;
    ELSE
        INSERT INTO public.{TableStringMessages.SpatialReferenceSystems}(srid, auth_name, auth_srid, srtext, proj4text)
            VALUES (dsrid, dauthName, dauthSrid, dwktString, dproj4String);
    END IF;
    RETURN dsrid;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.SpatialReferenceSystemInsertOrUpdate}(integer, text, integer, text, text)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedurePlanetoidInfoInsert(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.PlanetoidInfoInsert}(
    dname text, dseed bigint, dradius double precision)
    RETURNS integer
    LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    model_id integer;
    old_model public.{TableStringMessages.PlanetoidInfo} = NULL;
BEGIN
    SELECT * INTO old_model FROM public.{TableStringMessages.PlanetoidInfo} WHERE
        {nameof(PlanetoidInfoModel.Title)} = dname AND
        {nameof(PlanetoidInfoModel.Seed)} = dseed AND
        {nameof(PlanetoidInfoModel.Radius)} = dradius;
    IF old_model IS NOT NULL THEN
        RETURN old_model.{nameof(PlanetoidInfoModel.Id)};
    ELSE
        model_id = (SELECT COUNT(*) FROM public.{TableStringMessages.PlanetoidInfo});
        INSERT INTO public.{TableStringMessages.PlanetoidInfo}({nameof(PlanetoidInfoModel.Id)}, {nameof(PlanetoidInfoModel.Title)}, {nameof(PlanetoidInfoModel.Seed)}, {nameof(PlanetoidInfoModel.Radius)})
            VALUES (model_id, dname, dseed, dradius);
        RETURN model_id;
    END IF;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.PlanetoidInfoInsert}(text, bigint, double precision)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedurePlanetoidInfoSelect(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.PlanetoidInfoSelect}(
    dplanetoidId integer)
    RETURNS SETOF public.{TableStringMessages.PlanetoidInfo} 
    LANGUAGE 'sql'
AS $BODY$
SELECT {nameof(PlanetoidInfoModel.Id)}, {nameof(PlanetoidInfoModel.Title)}, {nameof(PlanetoidInfoModel.Seed)}, {nameof(PlanetoidInfoModel.Radius)}
FROM public.{TableStringMessages.PlanetoidInfo}
WHERE {nameof(PlanetoidInfoModel.Id)}=dplanetoidId ORDER BY {nameof(PlanetoidInfoModel.Id)}
FETCH FIRST 1 ROWS ONLY;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.PlanetoidInfoSelect}(integer)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedurePlanetoidInfoSelectAll(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.PlanetoidInfoSelectAll}()
    RETURNS SETOF public.{TableStringMessages.PlanetoidInfo}
    LANGUAGE 'sql'
AS $BODY$
SELECT {nameof(PlanetoidInfoModel.Id)}, {nameof(PlanetoidInfoModel.Title)}, {nameof(PlanetoidInfoModel.Seed)}, {nameof(PlanetoidInfoModel.Radius)}
FROM public.{TableStringMessages.PlanetoidInfo}
ORDER BY {nameof(PlanetoidInfoModel.Id)}
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.PlanetoidInfoSelectAll}()
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedurePlanetoidInfoDelete(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.PlanetoidInfoDelete}(
    planetoidId integer)
    RETURNS boolean
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
IF EXISTS (SELECT * FROM public.{TableStringMessages.PlanetoidInfo} WHERE {nameof(PlanetoidInfoModel.Id)}=planetoidId) THEN
    DELETE FROM public.{TableStringMessages.PlanetoidInfo} WHERE {nameof(PlanetoidInfoModel.Id)}=planetoidId;
    RETURN TRUE;
ELSE
    RETURN FALSE;
END IF;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.PlanetoidInfoDelete}(integer)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedurePlanetoidInfoClear(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.PlanetoidInfoClear}()
    RETURNS integer
    LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    model_count integer;
BEGIN
model_count = (SELECT COUNT(*) FROM public.{TableStringMessages.PlanetoidInfo});
DELETE FROM public.{TableStringMessages.PlanetoidInfo};
RETURN model_count;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.PlanetoidInfoClear}()
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureMetaDynamicInsert(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.MetaDynamicInsert}(
    dplanetoidid integer, dschema text, dtitle text, dcolumns text)
    RETURNS integer
    LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    model_id integer;
    old_model public.{TableStringMessages.MetaDynamic} = NULL;
BEGIN
    SELECT * INTO old_model FROM public.{TableStringMessages.MetaDynamic} WHERE
        {nameof(MetaDynamicModel.PlanetoidId)} = dplanetoidid AND
        {nameof(MetaDynamicModel.Schema)} = dschema AND
        {nameof(MetaDynamicModel.Title)} = dtitle AND
        {nameof(MetaDynamicModel.Columns)} = dcolumns;
    IF old_model IS NOT NULL THEN
        RETURN old_model.{nameof(MetaDynamicModel.Id)};
    ELSE
        model_id = (SELECT COUNT(*) FROM public.{TableStringMessages.MetaDynamic});
        INSERT INTO public.{TableStringMessages.MetaDynamic}({nameof(MetaDynamicModel.Id)}, {nameof(MetaDynamicModel.PlanetoidId)}, {nameof(MetaDynamicModel.Schema)}, {nameof(MetaDynamicModel.Title)}, {nameof(MetaDynamicModel.Columns)})
            VALUES (model_id, dplanetoidid, dschema, dtitle, dcolumns)
            ON CONFLICT ON CONSTRAINT {TableStringMessages.MetaDynamic}_pk 
            DO UPDATE SET
                {nameof(MetaDynamicModel.PlanetoidId)} = dplanetoidid,
                {nameof(MetaDynamicModel.Schema)} = dschema,
                {nameof(MetaDynamicModel.Title)} = dtitle,
                {nameof(MetaDynamicModel.Columns)} = dcolumns;
        RETURN model_id;
    END IF;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.MetaDynamicInsert}(integer, text, text, text)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureMetaDynamicSelect(DbConnectionWrapper c, CancellationToken token)
        {
            var sqlById = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.MetaDynamicSelectById}(
    ddynamicId integer)
    RETURNS SETOF public.{TableStringMessages.MetaDynamic} 
    LANGUAGE 'sql'
AS $BODY$
SELECT {nameof(MetaDynamicModel.Id)}, {nameof(MetaDynamicModel.PlanetoidId)}, {nameof(MetaDynamicModel.Schema)}, {nameof(MetaDynamicModel.Title)}, {nameof(MetaDynamicModel.Columns)}
FROM public.{TableStringMessages.MetaDynamic}
WHERE {nameof(MetaDynamicModel.Id)}=ddynamicId ORDER BY {nameof(MetaDynamicModel.Id)}
FETCH FIRST 1 ROWS ONLY;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.MetaDynamicSelectById}(integer)
    OWNER TO {_connection["User Id"]};
            ";

            var sqlByName = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.MetaDynamicSelectByName}(
    dplanetoidid integer, ddynamicSchema text, ddynamicTitle text)
    RETURNS SETOF public.{TableStringMessages.MetaDynamic} 
    LANGUAGE 'sql'
AS $BODY$
SELECT {nameof(MetaDynamicModel.Id)}, {nameof(MetaDynamicModel.PlanetoidId)}, {nameof(MetaDynamicModel.Schema)}, {nameof(MetaDynamicModel.Title)}, {nameof(MetaDynamicModel.Columns)}
FROM public.{TableStringMessages.MetaDynamic}
WHERE {nameof(MetaDynamicModel.PlanetoidId)}=dplanetoidid AND {nameof(MetaDynamicModel.Schema)}=ddynamicSchema AND {nameof(MetaDynamicModel.Title)}=ddynamicTitle ORDER BY {nameof(MetaDynamicModel.Id)}
FETCH FIRST 1 ROWS ONLY;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.MetaDynamicSelectByName}(integer,text,text)
    OWNER TO {_connection["User Id"]};
            ";

            var result = await RunQuery(sqlById, c, token);

            return !result.Success
                ? result
                : await RunQuery(sqlByName, c, token);
        }

        private async Task<Result> CreateProcedureMetaDynamicDelete(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.MetaDynamicDelete}(
    dynamicId integer)
    RETURNS boolean
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
IF EXISTS (SELECT * FROM public.{TableStringMessages.MetaDynamic} WHERE {nameof(MetaDynamicModel.Id)}=dynamicId) THEN
    DELETE FROM public.{TableStringMessages.MetaDynamic} WHERE {nameof(MetaDynamicModel.Id)}=dynamicId;
    RETURN TRUE;
ELSE
    RETURN FALSE;
END IF;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.MetaDynamicDelete}(integer)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureMetaDynamicClear(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.MetaDynamicClear}()
    RETURNS integer
    LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    model_count integer;
BEGIN
model_count = (SELECT COUNT(*) FROM public.{TableStringMessages.MetaDynamic});
DELETE FROM public.{TableStringMessages.MetaDynamic};
RETURN model_count;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.MetaDynamicClear}()
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureFileInfoInsert(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.FileInfoInsert}(
    dfileId text)
    RETURNS text
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    INSERT INTO public.{TableStringMessages.FileInfo}({nameof(FileInfoModel.FileId)}, {nameof(FileInfoModel.ModifiedOn)})
        VALUES (dfileId, now()::timestamp);
    RETURN dfileId;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.FileInfoInsert}(text)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureFileInfoSelect(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.FileInfoSelect}(
    dfileId text)
    RETURNS SETOF public.{TableStringMessages.FileInfo} 
    LANGUAGE 'sql'
AS $BODY$
SELECT {nameof(FileInfoModel.FileId)}, {nameof(FileInfoModel.ModifiedOn)}
FROM public.{TableStringMessages.FileInfo}
WHERE {nameof(FileInfoModel.FileId)}=dfileId ORDER BY {nameof(FileInfoModel.FileId)}
FETCH FIRST 1 ROWS ONLY;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.FileInfoSelect}(text)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureFileInfoDelete(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.FileInfoDelete}(
    dfileId text)
    RETURNS boolean
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
IF EXISTS (SELECT * FROM public.{TableStringMessages.FileInfo} WHERE {nameof(FileInfoModel.FileId)}=dfileId) THEN
    DELETE FROM public.{TableStringMessages.FileInfo} WHERE {nameof(FileInfoModel.FileId)}=dfileId;
    RETURN TRUE;
ELSE
    RETURN FALSE;
END IF;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.FileInfoDelete}(text)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureFileInfoExists(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.FileInfoExists}(
    dfileId text)
    RETURNS boolean
    LANGUAGE 'sql'
AS $BODY$
    SELECT EXISTS (SELECT {nameof(FileInfoModel.FileId)} FROM public.{TableStringMessages.FileInfo} WHERE {nameof(FileInfoModel.FileId)}=dfileId);
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.FileInfoExists}(text)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureTileBasedFileInfoInsert(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.TileBasedFileInfoInsert}(
    dfileId text,
    dplanetoidId integer,
    dz smallint,
    dx bigint,
    dy bigint,
    dposition text,
    drotation text,
    dscale text
)
    RETURNS text
    LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    positionArr double precision[];
    rotationArr double precision[];
    scaleArr double precision[];
BEGIN
    positionArr = cast(string_to_array(dposition, ';') as double precision[]);
    rotationArr = cast(string_to_array(drotation, ';') as double precision[]);
    scaleArr = cast(string_to_array(dscale, ';') as double precision[]);
    INSERT INTO public.{TableStringMessages.TileBasedFileInfo}({nameof(TileBasedFileInfoModel.FileId)}, {nameof(TileBasedFileInfoModel.PlanetoidId)}, {nameof(TileBasedFileInfoModel.Z)}, {nameof(TileBasedFileInfoModel.X)}, {nameof(TileBasedFileInfoModel.Y)}, {nameof(TileBasedFileInfoModel.Position)}, {nameof(TileBasedFileInfoModel.Rotation)}, {nameof(TileBasedFileInfoModel.Scale)})
        VALUES (dfileId, dplanetoidId, dz, dx, dy, positionArr, rotationArr, scaleArr);
    RETURN dfileId;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.TileBasedFileInfoInsert}(text, integer, smallint, bigint, bigint, text, text, text)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureTileBasedFileInfoSelectById(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.TileBasedFileInfoSelectById}(
    dfileId text)
    RETURNS SETOF public.{TableStringMessages.TileBasedFileInfo} 
    LANGUAGE 'sql'
AS $BODY$
SELECT 
    {nameof(TileBasedFileInfoModel.FileId)},
    {nameof(TileBasedFileInfoModel.PlanetoidId)},
    {nameof(TileBasedFileInfoModel.Z)},
    {nameof(TileBasedFileInfoModel.X)},
    {nameof(TileBasedFileInfoModel.Y)},
    {nameof(TileBasedFileInfoModel.Position)},
    {nameof(TileBasedFileInfoModel.Rotation)},
    {nameof(TileBasedFileInfoModel.Scale)}
FROM public.{TableStringMessages.TileBasedFileInfo}
WHERE {nameof(TileBasedFileInfoModel.FileId)}=dfileId ORDER BY {nameof(TileBasedFileInfoModel.FileId)}
FETCH FIRST 1 ROWS ONLY;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.TileBasedFileInfoSelectById}(text)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureTileBasedFileInfoSelectAllByTile(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.TileBasedFileInfoSelectAllByTile}(
    dplanetoidId integer,
    dz smallint,
    dx bigint,
    dy bigint)
    RETURNS SETOF public.{TableStringMessages.TileBasedFileInfo}
    LANGUAGE 'sql'
AS $BODY$
SELECT 
    {nameof(TileBasedFileInfoModel.FileId)},
    {nameof(TileBasedFileInfoModel.PlanetoidId)},
    {nameof(TileBasedFileInfoModel.Z)},
    {nameof(TileBasedFileInfoModel.X)},
    {nameof(TileBasedFileInfoModel.Y)},
    {nameof(TileBasedFileInfoModel.Position)},
    {nameof(TileBasedFileInfoModel.Rotation)},
    {nameof(TileBasedFileInfoModel.Scale)}
FROM public.{TableStringMessages.TileBasedFileInfo}
WHERE {nameof(TileBasedFileInfoModel.PlanetoidId)}=dplanetoidId
    AND {nameof(TileBasedFileInfoModel.Z)} = dz
    AND {nameof(TileBasedFileInfoModel.X)} = dx
    AND {nameof(TileBasedFileInfoModel.Y)} = dy
ORDER BY {nameof(TileBasedFileInfoModel.FileId)}
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.TileBasedFileInfoSelectAllByTile}(integer, smallint, bigint, bigint)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureTileBasedFileInfoDelete(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.TileBasedFileInfoDelete}(
    dfileId text)
    RETURNS boolean
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
IF EXISTS (SELECT * FROM public.{TableStringMessages.TileBasedFileInfo} WHERE {nameof(TileBasedFileInfoModel.FileId)}=dfileId) THEN
    DELETE FROM public.{TableStringMessages.TileBasedFileInfo} WHERE {nameof(TileBasedFileInfoModel.FileId)}=dfileId;
    RETURN TRUE;
ELSE
    RETURN FALSE;
END IF;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.TileBasedFileInfoDelete}(text)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureTileBasedFileInfoDeleteAllByTile(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.TileBasedFileInfoDeleteAllByTile}(
    dplanetoidId integer,
    dz smallint,
    dx bigint,
    dy bigint)
    RETURNS boolean
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
IF EXISTS (
    SELECT * FROM public.{TableStringMessages.TileBasedFileInfo}
    WHERE {nameof(TileBasedFileInfoModel.PlanetoidId)} = dplanetoidId
        AND {nameof(TileBasedFileInfoModel.Z)} = dz
        AND {nameof(TileBasedFileInfoModel.X)} = dx
        AND {nameof(TileBasedFileInfoModel.Y)} = dy)
THEN
    DELETE FROM public.{TableStringMessages.TileBasedFileInfo}
    WHERE {nameof(TileBasedFileInfoModel.PlanetoidId)} = dplanetoidId
        AND {nameof(TileBasedFileInfoModel.Z)} = dz
        AND {nameof(TileBasedFileInfoModel.X)} = dx
        AND {nameof(TileBasedFileInfoModel.Y)} = dy;
    RETURN TRUE;
ELSE
    RETURN FALSE;
END IF;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.TileBasedFileInfoDeleteAllByTile}(integer, smallint, bigint, bigint)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureFileDependencyInsert(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.FileDependencyInsert}(
    dfileId text,
    dreferencedFileId text,
    disRequired boolean,
    disDynamic boolean)
    RETURNS text
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    INSERT INTO public.{TableStringMessages.FileDependency}({nameof(FileDependencyModel.FileId)}, {nameof(FileDependencyModel.ReferencedFileId)}, {nameof(FileDependencyModel.IsRequired)}, {nameof(FileDependencyModel.IsDynamic)})
        VALUES (dfileId, dreferencedFileId, disRequired, disDynamic);
    RETURN dfileId;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.FileDependencyInsert}(text, text, boolean, boolean)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureFileDependencySelect(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.FileDependencySelect}(
    dfileId text,
    disRequiredOnly boolean,
    disDynamicOnly boolean)
    RETURNS SETOF public.{TableStringMessages.FileDependency} 
    LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    isRequiredValues boolean[];
    isDynamicValues boolean[];
BEGIN
    isRequiredValues = array_append(isRequiredValues, true);
    isDynamicValues = array_append(isDynamicValues, true);
    IF disRequiredOnly IS false THEN isRequiredValues = array_append(isRequiredValues, false); END IF;
    IF disDynamicOnly IS false THEN isDynamicValues = array_append(isDynamicValues, false); END IF;
    RETURN QUERY (
        SELECT *
        FROM public.{TableStringMessages.FileDependency}
        WHERE {nameof(FileDependencyModel.FileId)} = dfileId
            AND {nameof(FileDependencyModel.IsRequired)} = any(isRequiredValues)
            AND {nameof(FileDependencyModel.IsDynamic)} = any(isDynamicValues)
        ORDER BY {nameof(FileDependencyModel.FileId)}
    );
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.FileDependencySelect}(text, boolean, boolean)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureFileDependencyCountByReferenceId(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.FileDependencyCountByReferenceId}(
    dreferenceId text)
    RETURNS integer
    LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    model_count integer;
BEGIN
    model_count = (SELECT COUNT(*) FROM public.{TableStringMessages.FileDependency} WHERE {nameof(FileDependencyModel.ReferencedFileId)} = dreferenceId);
    RETURN model_count;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.FileDependencyCountByReferenceId}(text)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }

        private async Task<Result> CreateProcedureFileDependencyDelete(DbConnectionWrapper c, CancellationToken token)
        {
            var sql = $@"
CREATE OR REPLACE FUNCTION public.{StoredProcedureStringMessages.FileDependencyDelete}(
    dfileId text)
    RETURNS boolean
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
IF EXISTS (SELECT * FROM public.{TableStringMessages.FileDependency} WHERE {nameof(FileDependencyModel.FileId)}=dfileId) THEN
    DELETE FROM public.{TableStringMessages.FileDependency} WHERE {nameof(FileDependencyModel.FileId)}=dfileId;
    RETURN TRUE;
ELSE
    RETURN FALSE;
END IF;
END;
$BODY$;

ALTER FUNCTION public.{StoredProcedureStringMessages.FileDependencyDelete}(text)
    OWNER TO {_connection["User Id"]};
            ";

            return await RunQuery(sql, c, token);
        }
        #endregion

        #endregion
    }
}
