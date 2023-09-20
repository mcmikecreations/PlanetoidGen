﻿using PlanetoidGen.Agents.Standard.Constants.StringMessages;
using PlanetoidGen.BusinessLogic.Agents.Models.Agents;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Services.Agents;
using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Standard.Agents.Test
{
    public class DummyAgent : ITypedAgent<AgentEmptySettings>
    {
        public string Title => $"{nameof(PlanetoidGen)}.{nameof(DummyAgent)}";

        public string Description => string.Empty;

        public bool IsVisibleToClient => true;

        public async ValueTask<Result> Execute(GenerationJobMessage job, CancellationToken cancellationToken)
        {
            await Task.Delay(3000);

            return Result.CreateSuccess();
        }

        public AgentEmptySettings GetTypedDefaultSettings()
        {
            return AgentEmptySettings.Default;
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
            return new ValueTask<IEnumerable<AgentDependencyModel>>(Array.Empty<AgentDependencyModel>());
        }

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs(int z)
        {
            return GetOutputs();
        }

        public ValueTask<IEnumerable<DataTypeInfoModel>> GetOutputs()
        {
            return new ValueTask<IEnumerable<DataTypeInfoModel>>(new DataTypeInfoModel[]
            {
                new DataTypeInfoModel(DataTypes.Dummy, isRaster: true),
            });
        }

        public ValueTask<Result> Initialize(string settings, IServiceProvider serviceProvider)
        {
            return new ValueTask<Result>(Result.CreateSuccess());
        }
    }
}
