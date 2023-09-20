using Grpc.Core;
using PlanetoidGen.API.Helpers.Abstractions;
using PlanetoidGen.Contracts.Models.Coordinates;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using PlanetoidGen.Contracts.Services.Generation;

namespace PlanetoidGen.API.Controllers
{
    public class TileGenerationController : TileGeneration.TileGenerationBase
    {
        private readonly IGenerationService _generationService;
        private readonly IGenerationLODsService _generationLODsService;
        private readonly IStreamContext<GenerationJobMessage> _streamContext;
        private readonly ILogger<TileGenerationController> _logger;

        public TileGenerationController(
            IGenerationService generationService,
            IGenerationLODsService generationLODsService,
            IStreamContext<GenerationJobMessage> streamContext,
            ILogger<TileGenerationController> logger)
        {
            _generationService = generationService ?? throw new ArgumentNullException(nameof(generationService));
            _generationLODsService = generationLODsService ?? throw new ArgumentNullException(nameof(generationLODsService));
            _streamContext = streamContext ?? throw new ArgumentNullException(nameof(streamContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task QueueTilesGeneration(
            IAsyncStreamReader<TileGenerationArrayModel> requestStream,
            IServerStreamWriter<TileArrayModel> responseStream,
            ServerCallContext context)
        {
            var connectionId = context.GetHttpContext().Connection.Id;

            try
            {
                var messagesResult = Task.Run(async () =>
                {
                    while (!context.CancellationToken.IsCancellationRequested)
                    {
                        var messages = _streamContext.StreamMessages!.GetValueOrDefault(connectionId);

                        while (messages != null && !messages.IsEmpty)
                        {
                            if (messages.TryTake(out var message))
                            {
                                var response = new TileArrayModel();

                                response.TileInfos.Add(
                                    new TileModel
                                    {
                                        Id = message.Id,
                                        PlanetoidId = message.PlanetoidId,
                                        Z = message.Z,
                                        X = message.X,
                                        Y = message.Y,
                                        LastAgent = message.AgentIndex,
                                    }
                                );

                                await responseStream.WriteAsync(response);
                            }
                        }

                        Thread.Sleep(16);
                    }
                });

                while (!context.CancellationToken.IsCancellationRequested)
                {
                    if (await requestStream.MoveNext(context.CancellationToken))
                    {
                        await QueueTilesGeneration(requestStream, connectionId, context.CancellationToken);
                    }
                }
            }
            catch (IOException)
            {
                _logger.LogInformation($"Connection was aborted.");
            }
            finally
            {
                _streamContext.StreamMessages!.TryRemove(connectionId, out _);
            }
        }

        private async Task QueueTilesGeneration(
            IAsyncStreamReader<TileGenerationArrayModel> requestStream,
            string connectionId,
            CancellationToken cancellationToken)
        {
            if (!requestStream.Current.TileGenerationInfos.Any())
            {
                return;
            }

            var lodsResult = await _generationLODsService.GetLODs(requestStream.Current.PlanetoidId, cancellationToken);

            if (!lodsResult.Success)
            {
                _logger.LogError("GetLODs error: {error}", lodsResult.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, lodsResult.ErrorMessage!.ToString()));
            }

            var zoomByLOD = lodsResult.Data!.ToDictionary(x => x.LOD, y => y.Z);

            var coordinateModels = requestStream.Current.TileGenerationInfos
                .Select(i =>
                {
                    var lod = (short)i.LOD;

                    if (!zoomByLOD.TryGetValue(lod, out var zoom))
                    {
                        _logger.LogError("No zoom value exists for LOD={lod}", lod);
                        throw new RpcException(new Status(StatusCode.Internal, $"No zoom value exists for LOD={lod}"));
                    }

                    return new SphericalCoordinateModel(
                        requestStream.Current.PlanetoidId,
                        i.Longtitude,
                        i.Latitude,
                        zoom);
                })
                .ToList();

            var result = await _generationService.QueueTilesGeneration(coordinateModels, connectionId, cancellationToken);
        }
    }
}
