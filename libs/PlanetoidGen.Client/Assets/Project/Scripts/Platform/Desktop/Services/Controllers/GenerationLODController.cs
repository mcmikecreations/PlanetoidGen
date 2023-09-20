using PlanetoidGen.API;
using PlanetoidGen.Client.Contracts.Services.Controllers;
using PlanetoidGen.Client.Platform.Desktop.Services.Context.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Platform.Desktop.Services.Controllers
{
    public class GenerationLODController : ControllerBase, IGenerationLODController
    {
        private readonly GenerationLOD.GenerationLODClient _client;

        public GenerationLODController(IConnectionContext context)
        {
            _client = new GenerationLOD.GenerationLODClient(context.Channel);
        }

        public async Task<Domain.Models.Generation.GenerationLODModel> GetLOD(int planetoidId, int lod, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.GetLODAsync(new GetGenerationLODModel
                {
                    PlanetoidId = planetoidId,
                    LOD = lod
                }, cancellationToken: token);

                return new Domain.Models.Generation.GenerationLODModel(result.PlanetoidId, (short)result.LOD, (short)result.Z);
            });
        }

        public async Task<IEnumerable<Domain.Models.Generation.GenerationLODModel>> GetLODs(int planetoidId, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.GetLODsAsync(new QueryIdModel
                {
                    Id = planetoidId
                }, cancellationToken: token);

                return result.GenerationLODs.Select(l => new Domain.Models.Generation.GenerationLODModel(l.PlanetoidId, (short)l.LOD, (short)l.Z));
            });
        }

        public async Task<int> InsertLODs(IEnumerable<Domain.Models.Generation.GenerationLODModel> lods, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var model = new InsertGenerationLODsModel();
                model.GenerationLODs.AddRange(lods.Select(l => new GenerationLODModel
                {
                    PlanetoidId = l.PlanetoidId,
                    Z = l.Z,
                    LOD = l.LOD
                }));

                var result = await _client.InsertLODsAsync(model, cancellationToken: token);

                return result.Count;
            });
        }

        public async Task<int> ClearLODs(int planetoidId, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.ClearLODsAsync(new QueryIdModel
                {
                    Id = planetoidId
                }, cancellationToken: token);

                return result.Count;
            });
        }
    }
}
