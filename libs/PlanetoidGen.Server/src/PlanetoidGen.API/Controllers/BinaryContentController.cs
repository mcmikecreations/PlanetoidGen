using Google.Protobuf;
using Grpc.Core;
using PlanetoidGen.Contracts.Models.Documents;
using PlanetoidGen.Contracts.Services.Documents;
using PlanetoidGen.Domain.Models.Documents;

namespace PlanetoidGen.API.Controllers
{
    public class BinaryContentController : BinaryContent.BinaryContentBase
    {
        private readonly IFileContentService _fileContentService;
        private readonly ILogger<BinaryContentController> _logger;

        public BinaryContentController(IFileContentService fileContentService, ILogger<BinaryContentController> logger)
        {
            _fileContentService = fileContentService;
            _logger = logger;
        }

        public override async Task GetFileContent(IAsyncStreamReader<StringIdModel> requestStream, IServerStreamWriter<FileContentModel> responseStream, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            _logger.LogInformation($"Connection id: {httpContext.Connection.Id}");

            try
            {
                while (await requestStream.MoveNext(context.CancellationToken))
                {
                    if (!string.IsNullOrEmpty(requestStream.Current.Id))
                    {
                        var result = await _fileContentService.GetFileContent(requestStream.Current.Id, context.CancellationToken);

                        if (!result.Success)
                        {
                            _logger.LogError(result.ErrorMessage!.ToString());
                            throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
                        }
                        else if (result.Data == null)
                        {
                            throw new RpcException(new Status(StatusCode.NotFound, $"Record with id {requestStream.Current.Id} was not found."));
                        }

                        var response = BuildResponseModel(result.Data);

                        await responseStream.WriteAsync(response);
                    }
                }
            }
            catch (IOException)
            {
                _logger.LogInformation($"Connection was aborted.");
            }
        }

        public override async Task GetFileContentByTile(IAsyncStreamReader<GetFileContentByTile> requestStream, IServerStreamWriter<FileContentArrayModel> responseStream, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            _logger.LogInformation($"Connection id: {httpContext.Connection.Id}");

            try
            {
                while (await requestStream.MoveNext(context.CancellationToken))
                {
                    if (requestStream.Current != null)
                    {
                        var result = await _fileContentService.GetFileContentByTile(new GenericTileInfo
                        {
                            PlanetoidId = requestStream.Current.PlanetoidId,
                            Z = (short)requestStream.Current.Z,
                            X = requestStream.Current.X,
                            Y = requestStream.Current.Y
                        }, requestStream.Current.IsRequiredOnly, requestStream.Current.IsDynamicOnly, context.CancellationToken);

                        if (!result.Success)
                        {
                            _logger.LogError(result.ErrorMessage!.ToString());
                            throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
                        }
                        else if (result.Data == null)
                        {
                            throw new RpcException(new Status(StatusCode.NotFound, $"Record with id  was not found."));
                        }

                        await responseStream.WriteAsync(BuildResponseModel(result.Data));
                    }
                }
            }
            catch (IOException)
            {
                _logger.LogInformation($"Connection was aborted.");
            }
        }

        public override async Task<FileContentIdsModel> GetFileContentIdsByTile(GetFileContentByTile request, ServerCallContext context)
        {
            var result = await _fileContentService.GetFileContentIdsByTile(new GenericTileInfo
            {
                PlanetoidId = request.PlanetoidId,
                Z = (short)request.Z,
                X = request.X,
                Y = request.Y
            }, request.IsRequiredOnly, request.IsDynamicOnly, context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError(result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }
            else if (result.Data == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Record with file name  and local path  was not found."));
            }

            var response = new FileContentIdsModel();

            response.Ids.AddRange(result.Data);

            return response;
        }

        public override async Task<FileContentModel> GetFileContentByPath(GetFileContentByPathModel request, ServerCallContext context)
        {
            var result = await _fileContentService.GetFileContentByPath(request.FileName, request.LocalPath, context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError(result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }
            else if (result.Data == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Record with file name {request.FileName} and local path {request.LocalPath} was not found."));
            }

            return BuildResponseModel(result.Data);
        }

        public override async Task<SuccessModel> SaveFileContent(FileContentModel request, ServerCallContext context)
        {
            var file = new FileModel();
            file.Content = new Domain.Models.Documents.FileContentModel
            {
                Id = request.Id,
                FileName = request.FileName,
                LocalPath = request.LocalPath,
                Content = request.FileContent.ToArray(),
                Attributes = request.Attributes.ToDictionary(a => a.Key, a => a.Value)
            };

            if (request.TileBasedInfo != null)
            {
                file.TileBasedFileInfo = new TileBasedFileInfoModel(
                     request.Id,
                     request.TileBasedInfo.PlanetoidId,
                     (short)request.TileBasedInfo.Z,
                     request.TileBasedInfo.X,
                     request.TileBasedInfo.Y,
                     request.TileBasedInfo.Position.ToArray(),
                     request.TileBasedInfo.Rotation.ToArray(),
                     request.TileBasedInfo.Scale.ToArray());
            }

            if (request.DependentFiles != null)
            {
                file.DependentFiles = request.DependentFiles.Select(d => new Domain.Models.Documents.FileDependencyModel(
                    request.Id,
                    d.ReferencedFileId,
                    d.IsRequired,
                    d.IsDynamic));
            }

            var result = await _fileContentService.SaveFileContentWithDependencies(file, context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError(result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            return new SuccessModel { Success = result.Success };
        }

        public override async Task<SuccessModel> DeleteFileContent(StringIdModel request, ServerCallContext context)
        {
            var result = await _fileContentService.DeleteFileContent(request.Id, context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError(result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            return new SuccessModel { Success = result.Success };
        }

        public override async Task<SuccessModel> DeleteAllFileContentByTile(GenericTileModel request, ServerCallContext context)
        {
            var result = await _fileContentService.DeleteFileContentByTile(new GenericTileInfo
            {
                PlanetoidId = request.PlanetoidId,
                Z = (short)request.Z,
                X = request.X,
                Y = request.Y
            }, context.CancellationToken);

            if (!result.Success)
            {
                _logger.LogError(result.ErrorMessage!.ToString());
                throw new RpcException(new Status(StatusCode.Internal, result.ErrorMessage!.ToString()));
            }

            return new SuccessModel { Success = result.Success };
        }

        private FileContentModel BuildResponseModel(FileModel result)
        {
            var response = new FileContentModel()
            {
                Id = result.FileId
            };

            if (result.Content != null)
            {
                response.FileContent = ByteString.CopyFrom(result.Content!.Content);
                response.FileName = result.Content.FileName;
                response.LocalPath = result.Content.LocalPath;

                if (result.Content.Attributes != null)
                {
                    foreach (var attribute in result.Content.Attributes)
                    {
                        response.Attributes.Add(attribute.Key, attribute.Value);
                    }
                }
            }

            if (result.TileBasedFileInfo != null)
            {
                response.TileBasedInfo = new TileBasedInfoModel
                {
                    PlanetoidId = result.TileBasedFileInfo.PlanetoidId,
                    Z = result.TileBasedFileInfo.Z,
                    X = result.TileBasedFileInfo.X,
                    Y = result.TileBasedFileInfo.Y
                };

                response.TileBasedInfo.Position.AddRange(result.TileBasedFileInfo.Position);
                response.TileBasedInfo.Rotation.AddRange(result.TileBasedFileInfo.Rotation);
                response.TileBasedInfo.Scale.AddRange(result.TileBasedFileInfo.Scale);
            }

            if (result.DependentFiles != null)
            {
                response.DependentFiles.AddRange(result.DependentFiles.Select(d => new FileDependencyModel
                {
                    ReferencedFileId = d.ReferencedFileId,
                    IsRequired = d.IsRequired,
                    IsDynamic = d.IsDynamic
                }));
            }

            return response;
        }

        private FileContentArrayModel BuildResponseModel(IEnumerable<FileModel> result)
        {
            var response = new FileContentArrayModel();

            response.Files.AddRange(result.Select(f => BuildResponseModel(f)));

            return response;
        }
    }
}
