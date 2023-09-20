using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Repositories.Generation;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Constants.StringMessages;
using PlanetoidGen.DataAccess.Repositories.Generic;
using PlanetoidGen.Domain.Models.Generation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Generation
{
    public class GenerationLODsRepository : RepositoryAccessWrapper<GenerationLODModel>, IGenerationLODsRepository
    {
        private static readonly Func<IDataReader, GenerationLODModel> _reader = (r) => new GenerationLODModel(
                (int)r[nameof(GenerationLODModel.PlanetoidId)],
                (short)r[nameof(GenerationLODModel.LOD)],
                (short)r[nameof(GenerationLODModel.Z)]
                );

        public GenerationLODsRepository(DbConnectionStringBuilder connection, IMetaProcedureRepository meta)
            : base(connection, meta)
        {
        }

        public override string Name => TableStringMessages.GenerationLODs;

        public override Func<IDataReader, GenerationLODModel>? Reader => _reader;

        public async ValueTask<Result<int>> ClearLODs(int planetoidId, CancellationToken token)
        {
            return await RunSingleFunction<int>(
                StoredProcedureStringMessages.GenerationLODClear,
                new { dplanetoidId = planetoidId },
                token);
        }

        public async ValueTask<Result<GenerationLODModel>> GetLOD(int planetoidId, short lod, CancellationToken token)
        {
            var result = await GetLODs(planetoidId, token);
            return result.Success
                ? Result<GenerationLODModel>.CreateSuccess(result.Data!.First(x => x.LOD == lod))
                : Result<GenerationLODModel>.CreateFailure(result);
        }

        public async ValueTask<Result<IEnumerable<GenerationLODModel>>> GetLODs(int planetoidId, CancellationToken token)
        {
            return Result<IEnumerable<GenerationLODModel>>.Convert(
                await RunMultipleFunction<GenerationLODModel>(
                StoredProcedureStringMessages.GenerationLODSelect,
                new { dplanetoidId = planetoidId },
                token));
        }

        public async ValueTask<Result<int>> InsertLODs(IEnumerable<GenerationLODModel> models, CancellationToken token)
        {
            var planetoidId = models.First().PlanetoidId;
            // TODO: table-valued argument is currently unsupported in Insights.Database.
            var clearResult = await ClearLODs(planetoidId, token);
            if (!clearResult.Success) return Result<int>.CreateFailure(clearResult);

            var count = models.Count();
            var i = 0;
            var results = new List<Result<int>>(count);
            foreach (var model in models)
            {
                results.Add(await RunSingleFunction<int>(
                        StoredProcedureStringMessages.GenerationLODInsert,
                        new { dplanetoidId = model.PlanetoidId, dlod = model.LOD, dz = model.Z },
                        token));
                if (!results[i].Success) return results[i];
                ++i;
            }

            return results.Last();
        }
    }
}
