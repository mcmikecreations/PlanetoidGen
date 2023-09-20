using PlanetoidGen.API;
using PlanetoidGen.Client.Contracts.Services.Controllers;
using PlanetoidGen.Client.Platform.Desktop.Services.Context.Abstractions;
using PlanetoidGen.Domain.Models.Info;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Platform.Desktop.Services.Controllers
{
    public class PlanetoidController : ControllerBase, IPlanetoidController
    {
        private readonly Planetoid.PlanetoidClient _client;

        public PlanetoidController(IConnectionContext context)
        {
            _client = new Planetoid.PlanetoidClient(context.Channel);
        }

        public async Task<int> AddPlanetoid(PlanetoidInfoModel planetoid, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.AddPlanetoidAsync(new PlanetoidModel
                {
                    Title = planetoid.Title,
                    Radius = planetoid.Radius,
                    Seed = planetoid.Seed
                }, cancellationToken: token);

                return result.Id;
            });
        }

        public async Task<PlanetoidInfoModel> GetPlanetoid(int id, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.GetPlanetoidAsync(new QueryIdModel
                {
                    Id = id
                }, cancellationToken: token);

                return new PlanetoidInfoModel(result.PlanetoidId, result.Title, result.Seed, result.Radius);
            });
        }

        public async Task<IEnumerable<PlanetoidInfoModel>> GetAllPlanetoids(CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.GetAllPlanetoidsAsync(new EmptyModel(), cancellationToken: token);

                return result.Planetoids.Select(p => new PlanetoidInfoModel(p.PlanetoidId, p.Title, p.Seed, p.Radius));
            });
        }

        public async Task<bool> RemovePlanetoid(int id, CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.RemovePlanetoidAsync(new QueryIdModel
                {
                    Id = id
                }, cancellationToken: token);

                return result.Success;
            });
        }

        public async Task<int> ClearPlanetoids(CancellationToken token = default)
        {
            return await HandleRequest(async () =>
            {
                var result = await _client.ClearPlanetoidsAsync(new EmptyModel(), cancellationToken: token);

                return result.Count;
            });
        }
    }
}
