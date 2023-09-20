using PlanetoidGen.Agents.Procedural.Agents.River.Models;
using PlanetoidGen.BusinessLogic.Common.Constants;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Domain.Enums;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Procedural.Agents.River
{
    public class RiverAgent : ITypedAgent<RiverAgentSettings>
    {
        public string Title => $"{nameof(PlanetoidGen)}.{nameof(Procedural)}.{nameof(RiverAgent)}";

        public string Description => string.Empty;

        public bool IsVisibleToClient => true;

        public ValueTask<Result> Execute(GenerationJobMessage job, CancellationToken token)
        {
            return new ValueTask<Result>(Result.CreateSuccess());
        }

        public RiverAgentSettings GetTypedDefaultSettings()
        {
            return new RiverAgentSettings();
        }

        public ValueTask<string> GetDefaultSettings()
        {
            return GetTypedDefaultSettings().Serialize();
        }

        public ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies(int z)
        {
            return GetDependencies();
        }

        public ValueTask<IEnumerable<AgentDependencyModel>> GetDependencies()
        {
            return new ValueTask<IEnumerable<AgentDependencyModel>>(new AgentDependencyModel[]
            {
                new AgentDependencyModel(
                    RelativeTileDirectionType.Current,
                    new DataTypeInfoModel(DataTypes.HeightMapRgba32Encoded, isRaster: true))
            });
        }

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs(int z)
        {
            return GetOutputs();
        }

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs()
        {
            return new ValueTask<IEnumerable<DataTypeInfoModel>>(new DataTypeInfoModel[]
            {
                new DataTypeInfoModel(DataTypes.HeightMapRgba32Encoded, isRaster: true),
                // new DataTypeInfoModel(DataTypes.RiverVector, isRaster: true),
            });
        }

        public ValueTask<Result> Initialize(string settings, IServiceProvider serviceProvider)
        {
            return new ValueTask<Result>(Result.CreateSuccess());
        }
    }
}
