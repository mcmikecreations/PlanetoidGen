using Insight.Database;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Dynamic;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Constants.StringMessages;
using PlanetoidGen.DataAccess.Helpers.Extensions;
using PlanetoidGen.DataAccess.Repositories.Generic;
using PlanetoidGen.Domain.Models.Meta;
using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Meta
{
    public class MetaDynamicRepository : RepositoryAccessWrapper<MetaDynamicModel>, IMetaDynamicRepository
    {
        private static readonly Func<IDataReader, MetaDynamicModel> _reader = (r) => new MetaDynamicModel(
                (int)r[nameof(MetaDynamicModel.Id)],
                (int)r[nameof(MetaDynamicModel.PlanetoidId)],
                (string)r[nameof(MetaDynamicModel.Schema)],
                (string)r[nameof(MetaDynamicModel.Title)],
                (string)r[nameof(MetaDynamicModel.Columns)]
                );

        public MetaDynamicRepository(DbConnectionStringBuilder connection, IMetaProcedureRepository meta)
            : base(connection, meta)
        {
        }

        public override string Name => TableStringMessages.MetaDynamic;

        public override Func<IDataReader, MetaDynamicModel>? Reader => _reader;

        public async ValueTask<Result<int>> ClearDynamicContent(MetaDynamicModel model, CancellationToken token)
        {
            var sb = new StringBuilder();
            sb
                .Append("DELETE FROM ")
                .Append(model.Schema)
                .Append('.')
                .Append(model.Title)
                .Append(';');

            return await RunSingleQuery<int>(sb.ToString(), token);
        }

        public async ValueTask<Result<int>> ClearDynamicMeta(CancellationToken token)
        {
            return await RunSingleFunction<int>(
                StoredProcedureStringMessages.MetaDynamicClear,
                Parameters.Empty,
                token);
        }

        public async ValueTask<Result<MetaDynamicModel>> GetDynamicById(int id, CancellationToken token)
        {
            return await RunSingleFunction<MetaDynamicModel>(
                StoredProcedureStringMessages.MetaDynamicSelectById,
                new { ddynamicId = id },
                token);
        }

        public async ValueTask<Result<MetaDynamicModel>> GetDynamicByName(int planetoidId, string schema, string title, CancellationToken token)
        {
            return await RunSingleFunction<MetaDynamicModel>(
                StoredProcedureStringMessages.MetaDynamicSelectByName,
                new { dplanetoidid = planetoidId, ddynamicSchema = schema, ddynamicTitle = title, },
                token);
        }

        public MetaDynamicModel GetMetaDynamicModel(TableSchema tableSchema, int planetoidId)
        {
            return new MetaDynamicModel(
                default,
                planetoidId,
                tableSchema.Schema,
                tableSchema.Title,
                string.Join(' ', tableSchema.Columns.Select(x => $"{x.GetTypeName()} {x.Title}"))
                );
        }

        public async ValueTask<Result<int>> InsertDynamic(MetaDynamicModel model, CancellationToken token)
        {
            return await RunSingleFunction<int>(
                StoredProcedureStringMessages.MetaDynamicInsert,
                new { dplanetoidid = model.PlanetoidId, dschema = model.Schema, dtitle = model.Title, dcolumns = model.Columns },
                token);
        }

        public async ValueTask<Result<bool>> RemoveDynamicById(int id, CancellationToken token)
        {
            return await RunSingleFunction<bool>(
                StoredProcedureStringMessages.MetaDynamicDelete,
                new { dynamicId = id },
                token);
        }
    }
}
