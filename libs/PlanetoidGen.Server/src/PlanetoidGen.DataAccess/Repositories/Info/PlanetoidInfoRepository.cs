using Insight.Database;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Repositories.Info;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Constants.StringMessages;
using PlanetoidGen.DataAccess.Repositories.Generic;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Info
{
    public class PlanetoidInfoRepository : RepositoryAccessWrapper<PlanetoidInfoModel>, IPlanetoidInfoRepository
    {
        private static readonly Func<IDataReader, PlanetoidInfoModel> _reader = (r) => new PlanetoidInfoModel(
                (int)r[nameof(PlanetoidInfoModel.Id)],
                (string)r[nameof(PlanetoidInfoModel.Title)],
                (long)r[nameof(PlanetoidInfoModel.Seed)],
                (double)r[nameof(PlanetoidInfoModel.Radius)]
                );

        public PlanetoidInfoRepository(DbConnectionStringBuilder connection, IMetaProcedureRepository meta)
            : base(connection, meta)
        {
        }

        public override string Name => TableStringMessages.PlanetoidInfo;

        public override Func<IDataReader, PlanetoidInfoModel>? Reader => _reader;

        public async ValueTask<Result<int>> ClearPlanetoids(CancellationToken token)
        {
            return await RunSingleFunction<int>(
                StoredProcedureStringMessages.PlanetoidInfoClear,
                Parameters.Empty,
                token);
        }

        public async ValueTask<Result<PlanetoidInfoModel>> GetPlanetoidById(int id, CancellationToken token)
        {
            return await RunSingleFunction<PlanetoidInfoModel>(
                StoredProcedureStringMessages.PlanetoidInfoSelect,
                new { dplanetoidid = id },
                token);
        }

        public async ValueTask<Result<int>> InsertPlanetoid(PlanetoidInfoModel model, CancellationToken token)
        {
            return await RunSingleFunction<int>(
                StoredProcedureStringMessages.PlanetoidInfoInsert,
                new { dname = model.Title, dseed = model.Seed, dradius = model.Radius },
                token);
        }

        public async ValueTask<Result<bool>> RemovePlanetoidById(int id, CancellationToken token)
        {
            return await RunSingleFunction<bool>(
                StoredProcedureStringMessages.PlanetoidInfoDelete,
                new { planetoidid = id },
                token);
        }

        public async ValueTask<Result<IReadOnlyList<PlanetoidInfoModel>>> GetAllPlanetoids(CancellationToken token)
        {
            return await RunMultipleFunction<PlanetoidInfoModel>(
                StoredProcedureStringMessages.PlanetoidInfoSelectAll,
                arguments: null,
                token);
        }
    }
}
