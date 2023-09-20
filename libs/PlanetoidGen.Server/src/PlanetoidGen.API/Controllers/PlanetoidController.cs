using Grpc.Core;
using PlanetoidGen.Contracts.Services.Generation;
using PlanetoidGen.Domain.Models.Info;

namespace PlanetoidGen.API.Controllers
{
    public class PlanetoidController : Planetoid.PlanetoidBase
    {
        private readonly IPlanetoidService _planetoidService;
        private readonly ILogger<PlanetoidController> _logger;

        public PlanetoidController(
            IPlanetoidService planetoidService,
            ILogger<PlanetoidController> logger)
        {
            _planetoidService = planetoidService;
            _logger = logger;
        }

        public override async Task<QueryIdModel> AddPlanetoid(PlanetoidModel request, ServerCallContext context)
        {
            var result = await _planetoidService.AddPlanetoid(
                new PlanetoidInfoModel(default, request.Title, request.Seed, request.Radius), context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Add Planetoid error: {error}", result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            return new QueryIdModel
            {
                Id = result.Data
            };
        }

        public override async Task<PlanetoidModel> GetPlanetoid(QueryIdModel request, ServerCallContext context)
        {
            var result = await _planetoidService.GetPlanetoid(request.Id, context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Get Planetoid error: {error}", result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            return ToPlanetoidModel(result.Data);
        }

        public override async Task<SuccessModel> RemovePlanetoid(QueryIdModel request, ServerCallContext context)
        {
            var result = await _planetoidService.RemovePlanetoid(request.Id, context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Remove Planetoid error: {error}", result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            return new SuccessModel
            {
                Success = result.Data
            };
        }

        public override async Task<ItemsCountModel> ClearPlanetoids(EmptyModel request, ServerCallContext context)
        {
            var result = await _planetoidService.ClearPlanetoids(context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Clear Planetoids error: {error}", result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            return new ItemsCountModel
            {
                Count = result.Data
            };
        }

        public override async Task<PlanetoidArrayModel> GetAllPlanetoids(EmptyModel request, ServerCallContext context)
        {
            var result = await _planetoidService.GetAllPlanetoids(context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Get All Planetoids error: {error}", result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            var response = new PlanetoidArrayModel();

            foreach (var planetoid in result.Data)
            {
                response.Planetoids.Add(ToPlanetoidModel(planetoid));
            }

            return response;
        }

        private static PlanetoidModel ToPlanetoidModel(PlanetoidInfoModel model)
        {
            return new PlanetoidModel
            {
                PlanetoidId = model.Id,
                Title = model.Title,
                Seed = model.Seed,
                Radius = model.Radius,
            };
        }
    }
}
