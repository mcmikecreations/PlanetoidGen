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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Info
{
    public class AgentInfoRepository : RepositoryAccessWrapper<AgentInfoModel>, IAgentInfoRepository
    {
        private static readonly Func<IDataReader, AgentInfoModel> _reader = (r) => new AgentInfoModel(
                (int)r[nameof(AgentInfoModel.PlanetoidId)],
                (int)r[nameof(AgentInfoModel.IndexId)],
                (string)r[nameof(AgentInfoModel.Title)],
                (string)r[nameof(AgentInfoModel.Settings)],
                (bool)r[nameof(AgentInfoModel.ShouldRerunIfLast)]
                );

        public AgentInfoRepository(DbConnectionStringBuilder connection, IMetaProcedureRepository meta)
            : base(connection, meta)
        {
        }

        public override string Name => TableStringMessages.AgentInfo;

        public override Func<IDataReader, AgentInfoModel>? Reader => _reader;

        public async ValueTask<Result<bool>> ClearAgents(int planetoidId, CancellationToken token)
        {
            return await RunSingleFunction<bool>(
                StoredProcedureStringMessages.AgentInfoClear,
                new { dplanetoidId = planetoidId },
                token);
        }

        public async ValueTask<Result<IReadOnlyList<AgentInfoModel>>> GetAgents(int planetoidId, CancellationToken token)
        {
            return await RunMultipleFunction<AgentInfoModel>(
                StoredProcedureStringMessages.AgentInfoSelect,
                new { dplanetoidId = planetoidId },
                token);
        }

        public async ValueTask<Result<AgentInfoModel>> GetAgentByIndex(int planetoidId, int agentIndex, CancellationToken token)
        {
            return await RunSingleFunction<AgentInfoModel>(
                StoredProcedureStringMessages.AgentInfoSelectByIndex,
                new { dplanetoidId = planetoidId, dagentIndex = agentIndex },
                token);
        }

        public async ValueTask<Result<int>> InsertAgents(IEnumerable<AgentInfoModel> agents, CancellationToken token)
        {
            using (var c = await _connection.OpenWithTransactionAsync(token))
            {
                var planetoidId = agents.First().PlanetoidId;
                // TODO: table-valued argument is currently unsupported in Insights.Database.
                var clearResult = await ClearAgents(planetoidId, token);
                if (!clearResult.Success) return Result<int>.CreateFailure(clearResult);

                var agentCount = agents.Count();
                var i = 0;
                var results = new List<Result<int>>(agentCount);

                foreach (var agent in agents)
                {
                    results.Add(await RunSingleFunction<int>(
                            StoredProcedureStringMessages.AgentInfoInsert,
                            new { dplanetoidId = agent.PlanetoidId, name = agent.Title, settings = agent.Settings, shouldRerunIfLast = agent.ShouldRerunIfLast },
                            token));
                    if (!results[i].Success)
                    {
                        c.Rollback();
                        return results[i];
                    }

                    ++i;
                }

                c.Commit();
                return results.Last();
            }
        }
    }
}
