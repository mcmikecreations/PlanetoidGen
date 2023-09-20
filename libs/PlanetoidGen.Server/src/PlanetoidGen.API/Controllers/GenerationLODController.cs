using Grpc.Core;
using PlanetoidGen.Contracts.Services.Generation;

namespace PlanetoidGen.API.Controllers
{
    public class GenerationLODController : GenerationLOD.GenerationLODBase
    {
        private readonly IGenerationLODsService _generationLODsService;
        private readonly ILogger<PlanetoidController> _logger;

        public GenerationLODController(
            IGenerationLODsService generationLODsService,
            ILogger<PlanetoidController> logger)
        {
            _generationLODsService = generationLODsService;
            _logger = logger;
        }

        public override async Task<GenerationLODModel> GetLOD(GetGenerationLODModel request, ServerCallContext context)
        {
            var result = await _generationLODsService.GetLOD(request.PlanetoidId, (short)request.LOD, context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError(result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            return new GenerationLODModel
            {
                PlanetoidId = result.Data.PlanetoidId,
                LOD = result.Data.LOD,
                Z = result.Data.Z
            };
        }

        public override async Task<GenerationLODArrayModel> GetLODs(QueryIdModel request, ServerCallContext context)
        {
            var result = await _generationLODsService.GetLODs(request.Id, context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError(result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            var response = new GenerationLODArrayModel();

            foreach (var lod in result.Data)
            {
                response.GenerationLODs.Add(new GenerationLODModel
                {
                    PlanetoidId = lod.PlanetoidId,
                    LOD = lod.LOD,
                    Z = lod.Z
                });
            }

            return response;
        }

        public override async Task<ItemsCountModel> InsertLODs(InsertGenerationLODsModel request, ServerCallContext context)
        {
            var result = await _generationLODsService.InsertLODs(
                request.GenerationLODs.Select(l => new Domain.Models.Generation.GenerationLODModel(l.PlanetoidId, (short)l.LOD, (short)l.Z)),
                context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError(result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            return new ItemsCountModel
            {
                Count = result.Data
            };
        }

        public override async Task<ItemsCountModel> ClearLODs(QueryIdModel request, ServerCallContext context)
        {
            var result = await _generationLODsService.ClearLODs(request.Id, context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError(result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            return new ItemsCountModel
            {
                Count = result.Data
            };
        }
    }
}
