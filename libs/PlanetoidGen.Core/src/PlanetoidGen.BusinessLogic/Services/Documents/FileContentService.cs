using Microsoft.Extensions.Logging;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Models.Documents;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Repositories;
using PlanetoidGen.Contracts.Repositories.Documents;
using PlanetoidGen.Contracts.Services.Documents;
using PlanetoidGen.Domain.Models.Documents;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.BusinessLogic.Services.Documents
{
    public class FileContentService : IFileContentService
    {
        private readonly IFileContentRepository _fileContentRepository;
        private readonly IFileInfoRepository _fileInfoRepository;
        private readonly IFileDependencyRepository _fileDependencyRepository;
        private readonly ITileBasedFileInfoRepository _tileBasedFileInfoRepository;
        private readonly ILogger<FileContentService> _logger;
        private readonly IRepositoryAccessWrapper _repositoryAccessWrapper;

        public FileContentService(
            IFileContentRepository fileContentRepository,
            IFileInfoRepository fileInfoRepository,
            IFileDependencyRepository fileDependencyRepository,
            ITileBasedFileInfoRepository tileBasedFileInfoRepository,
            IRepositoryAccessWrapper repositoryAccessWrapper,
            ILogger<FileContentService> logger)
        {
            _fileContentRepository = fileContentRepository;
            _fileInfoRepository = fileInfoRepository;
            _fileDependencyRepository = fileDependencyRepository;
            _tileBasedFileInfoRepository = tileBasedFileInfoRepository;
            _logger = logger;
            _repositoryAccessWrapper = repositoryAccessWrapper;
        }

        public async ValueTask<Result<FileModel>> GetFileContent(string fileId, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                return CreateFailure<FileModel>(FileStringMessages.FileIdIsEmpty);
            }

            var fileInfoResult = await _fileInfoRepository.SelectFileInfo(fileId, token);
            if (!fileInfoResult.Success)
            {
                return CreateFailure<FileModel>(FileStringMessages.FileContentNotExist, fileId);
            }

            var file = new FileModel(fileId);

            // Check if file is tile based and set proper info
            // Select by id will return Failure if record does not exist
            var tileBasedFileInfoResult = await _tileBasedFileInfoRepository.SelectTileBasedFileInfo(fileId, token);
            if (tileBasedFileInfoResult.Success)
            {
                file.TileBasedFileInfo = tileBasedFileInfoResult.Data;
            }

            var fileDependencyResult = await _fileDependencyRepository.SelectFileDependencies(fileId, false, false, token);
            if (!fileDependencyResult.Success)
            {
                return CreateFailure<FileModel>(FileStringMessages.SelectError, fileDependencyResult.ErrorMessage!.ToString());
            }

            file.DependentFiles = fileDependencyResult.Data.AsEnumerable();

            var fileContentResult = await _fileContentRepository.GetById(fileId);

            if (!fileContentResult.Success)
            {
                return CreateFailure<FileModel>(FileStringMessages.SelectError, fileContentResult.ErrorMessage!.ToString());
            }

            file.Content = fileContentResult.Data;

            return Result<FileModel>.CreateSuccess(file);
        }

        public async ValueTask<Result<IEnumerable<FileModel>>> GetFileContentByTile(GenericTileInfo tileInfo, bool isRequiredOnly, bool isDynamicOnly, CancellationToken token)
        {
            if (tileInfo == null)
            {
                return CreateFailure<IEnumerable<FileModel>>(FileStringMessages.TileInfoIsEmpty);
            }

            var files = new List<FileModel>();
            var tileBasedFileInfosResult = await _tileBasedFileInfoRepository.SelectTileBasedFileInfosByTile(tileInfo.PlanetoidId, tileInfo.Z, tileInfo.X, tileInfo.Y, token);
            if (!tileBasedFileInfosResult.Success)
            {
                return CreateFailure<IEnumerable<FileModel>>(FileStringMessages.SelectError, tileBasedFileInfosResult.ErrorMessage!.ToString());
            }

            foreach (var tileBasedFileInfo in tileBasedFileInfosResult.Data)
            {
                var fileDependenciesResult = await _fileDependencyRepository.SelectFileDependencies(tileBasedFileInfo.FileId, isRequiredOnly, isDynamicOnly, token);
                if (!fileDependenciesResult.Success)
                {
                    return CreateFailure<IEnumerable<FileModel>>(FileStringMessages.SelectError, fileDependenciesResult.ErrorMessage!.ToString());
                }

                foreach (var dependency in fileDependenciesResult.Data)
                {
                    if (!files.Any(f => f.FileId == dependency.ReferencedFileId))
                    {
                        var dependencyResult = await GetFileContent(dependency.ReferencedFileId, token);
                        if (!dependencyResult.Success)
                        {
                            return CreateFailure<IEnumerable<FileModel>>(FileStringMessages.SelectError, dependencyResult.ErrorMessage!.ToString());
                        }
                        else if (dependencyResult.Data.Content == null && dependency.IsRequired)
                        {
                            return CreateFailure<IEnumerable<FileModel>>(FileStringMessages.FileContentNotExist, dependency.ReferencedFileId);
                        }
                        else
                        {
                            files.Add(dependencyResult.Data);
                        }
                    }
                }

                var tileBasedFileContentResult = await _fileContentRepository.GetById(tileBasedFileInfo.FileId);
                if (!tileBasedFileContentResult.Success)
                {
                    return CreateFailure<IEnumerable<FileModel>>(FileStringMessages.SelectError, tileBasedFileContentResult.ErrorMessage!.ToString());
                }
                else if (tileBasedFileContentResult.Data == null)
                {
                    return CreateFailure<IEnumerable<FileModel>>(FileStringMessages.FileContentNotExist, tileBasedFileInfo.FileId);
                }

                files.Add(new FileModel
                {
                    FileId = tileBasedFileInfo.FileId,
                    Content = tileBasedFileContentResult.Data,
                    DependentFiles = fileDependenciesResult.Data,
                    TileBasedFileInfo = tileBasedFileInfo
                });
            }

            return Result<IEnumerable<FileModel>>.CreateSuccess(files);
        }

        public async ValueTask<Result<IEnumerable<string>>> GetFileContentIdsByTile(GenericTileInfo tileInfo, bool isRequiredOnly, bool isDynamicOnly, CancellationToken token)
        {
            if (tileInfo == null)
            {
                return CreateFailure<IEnumerable<string>>(FileStringMessages.TileInfoIsEmpty);
            }

            var fileIds = new List<string>();
            var tileBasedFileInfosResult = await _tileBasedFileInfoRepository.SelectTileBasedFileInfosByTile(tileInfo.PlanetoidId, tileInfo.Z, tileInfo.X, tileInfo.Y, token);
            if (!tileBasedFileInfosResult.Success)
            {
                return CreateFailure<IEnumerable<string>>(FileStringMessages.SelectError, tileBasedFileInfosResult.ErrorMessage!.ToString());
            }

            fileIds.AddRange(tileBasedFileInfosResult.Data.Select(f => f.FileId));

            foreach (var tileBasedFileInfo in tileBasedFileInfosResult.Data)
            {
                var fileDependenciesResult = await _fileDependencyRepository.SelectFileDependencies(tileBasedFileInfo.FileId, isRequiredOnly, isDynamicOnly, token);
                if (!fileDependenciesResult.Success)
                {
                    return CreateFailure<IEnumerable<string>>(FileStringMessages.SelectError, fileDependenciesResult.ErrorMessage!.ToString());
                }

                fileIds.AddRange(fileDependenciesResult.Data.Select(d => d.ReferencedFileId));
            }

            return Result<IEnumerable<string>>.CreateSuccess(fileIds.Distinct());
        }

        public async ValueTask<Result<FileModel>> GetFileContentByPath(string fileName, string localPath, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return CreateFailure<FileModel>(FileStringMessages.FileNameIsEmpty);
            }

            var file = new FileModel();
            var fileContentResult = await _fileContentRepository.GetByPath(fileName, localPath);

            if (!fileContentResult.Success)
            {
                return CreateFailure<FileModel>(FileStringMessages.SelectError, fileContentResult.ErrorMessage!.ToString());
            }
            else if (fileContentResult.Data == null)
            {
                return CreateFailure<FileModel>(FileStringMessages.FileContentNotExist, fileName);
            }

            file.FileId = fileContentResult.Data.Id;
            file.Content = fileContentResult.Data;

            // Check if file is tile based and set proper info
            // Select by id will return Failure if record does not exist
            var tileBasedFileInfoResult = await _tileBasedFileInfoRepository.SelectTileBasedFileInfo(file.Content.Id, token);
            if (tileBasedFileInfoResult.Success)
            {
                file.TileBasedFileInfo = tileBasedFileInfoResult.Data;
            }

            var fileDependencyResult = await _fileDependencyRepository.SelectFileDependencies(file.Content.Id, false, false, token);
            if (!fileDependencyResult.Success)
            {
                return CreateFailure<FileModel>(FileStringMessages.SelectError, fileDependencyResult.ErrorMessage!.ToString());
            }

            file.DependentFiles = fileDependencyResult.Data.AsEnumerable();

            return Result<FileModel>.CreateSuccess(file);
        }

        public async ValueTask<Result<bool>> SaveFileContentWithDependencies(FileModel file, CancellationToken token)
        {
            if (file == null || file.Content == null)
            {
                return CreateFailure<bool>(FileStringMessages.FileModelIsEmpty);
            }
            else if (string.IsNullOrWhiteSpace(file.Content.Id))
            {
                return CreateFailure<bool>(FileStringMessages.FileIdIsEmpty, nameof(file.Content.Id));
            }

            var fileId = file.Content!.Id;

            var transactionResult = await _repositoryAccessWrapper.OpenTransaction(token);

            using (var connection = transactionResult.Data)
            {
                // If file content already exists, it will be overrided
                var fileInfoResult = await _fileInfoRepository.SelectFileInfo(fileId, token, connection);
                if (fileInfoResult.Success)
                {
                    // File info exists in database
                    var removeTileBasedInfoResult = await _tileBasedFileInfoRepository.RemoveTileBasedFileInfo(fileId, token, connection);
                    if (!removeTileBasedInfoResult.Success)
                    {
                        await _repositoryAccessWrapper.RollbackTransaction(connection, token);
                        return CreateFailure<bool>(FileStringMessages.RemoveError, removeTileBasedInfoResult.ErrorMessage!.ToString());
                    }

                    var selectDependenciesResult = await _fileDependencyRepository.SelectFileDependencies(fileId, false, false, token, connection);
                    if (!selectDependenciesResult.Success)
                    {
                        await _repositoryAccessWrapper.RollbackTransaction(connection, token);
                        return CreateFailure<bool>(FileStringMessages.SelectError, selectDependenciesResult.ErrorMessage!.ToString());
                    }

                    var removeDependenciesResult = await _fileDependencyRepository.RemoveFileDependencies(fileId, token, connection);
                    if (!removeDependenciesResult.Success)
                    {
                        await _repositoryAccessWrapper.RollbackTransaction(connection, token);
                        return CreateFailure<bool>(FileStringMessages.RemoveError, removeDependenciesResult.ErrorMessage!.ToString());
                    }

                    foreach (var dependency in selectDependenciesResult.Data)
                    {
                        if (file.DependentFiles != null && !file.DependentFiles.Any(d => d.ReferencedFileId == dependency.ReferencedFileId))
                        {
                            var removeDependencyResult = await DeleteFileContentInternal(dependency.ReferencedFileId, connection, token);
                            if (!removeDependencyResult.Success && !removeDependencyResult.ErrorMessage!.ToString().Contains(FileStringMessages.FileIsReferenced))
                            {
                                await _repositoryAccessWrapper.RollbackTransaction(connection, token);
                                return CreateFailure<bool>(FileStringMessages.RemoveError, removeDependencyResult.ErrorMessage!.ToString());
                            }
                        }
                    }

                    var removeFileInfoResult = await _fileInfoRepository.RemoveFileInfo(fileId, token, connection);
                    if (!removeFileInfoResult.Success)
                    {
                        await _repositoryAccessWrapper.RollbackTransaction(connection, token);
                        return CreateFailure<bool>(FileStringMessages.RemoveError, removeFileInfoResult.ErrorMessage!.ToString());
                    }
                }

                var insertFileInfoResult = await _fileInfoRepository.InsertFileInfo(new FileInfoModel(fileId, default), token, connection);
                if (!insertFileInfoResult.Success)
                {
                    await _repositoryAccessWrapper.RollbackTransaction(connection, token);
                    return CreateFailure<bool>(FileStringMessages.InsertError, insertFileInfoResult.ErrorMessage!.ToString());
                }

                if (file.TileBasedFileInfo != null)
                {
                    var insertTileBasedInfoResult = await _tileBasedFileInfoRepository.InsertTileBasedFileInfo(file.TileBasedFileInfo, token, connection);
                    if (!insertTileBasedInfoResult.Success)
                    {
                        await _repositoryAccessWrapper.RollbackTransaction(connection, token);
                        return CreateFailure<bool>(FileStringMessages.InsertError, insertTileBasedInfoResult.ErrorMessage!.ToString());
                    }
                }

                if (file.DependentFiles != null && file.DependentFiles.Any())
                {
                    foreach (var dependency in file.DependentFiles)
                    {
                        var insertDependencyResult = await _fileDependencyRepository.InsertFileDependency(dependency, token, connection);
                        if (!insertDependencyResult.Success)
                        {
                            await _repositoryAccessWrapper.RollbackTransaction(connection, token);
                            return CreateFailure<bool>(FileStringMessages.InsertError, insertDependencyResult.ErrorMessage!.ToString());
                        }
                    }
                }

                var exists = await _fileContentRepository.Exists(fileId);
                if (!exists.Success)
                {
                    await _repositoryAccessWrapper.RollbackTransaction(connection, token);
                    _logger.LogError(FileStringMessages.ExistsError, exists.ErrorMessage!.ToString());
                    return Result<bool>.CreateFailure(exists);
                }

                var updateContentResult = exists.Data
                    ? await _fileContentRepository.Update(fileId, file.Content)
                    : await _fileContentRepository.Create(file.Content);

                if (!updateContentResult.Success)
                {
                    await _repositoryAccessWrapper.RollbackTransaction(connection, token);
                    return CreateFailure<bool>(FileStringMessages.InsertError, updateContentResult.ErrorMessage!.ToString());
                }

                var commitResult = await _repositoryAccessWrapper.CommitTransaction(connection, token);
                return commitResult.Success ? updateContentResult : Result<bool>.CreateFailure(commitResult);
            }
        }

        public async ValueTask<Result<bool>> DeleteFileContent(string fileId, CancellationToken token)
        {
            var transactionResult = await _repositoryAccessWrapper.OpenTransaction(token);

            using (var connection = transactionResult.Data)
            {
                var result = await DeleteFileContentInternal(fileId, connection, token);

                if (result.Success)
                {
                    await _repositoryAccessWrapper.CommitTransaction(connection, token);
                }
                else
                {
                    await _repositoryAccessWrapper.RollbackTransaction(connection, token);
                }

                return result;
            }
        }

        private async ValueTask<Result<bool>> DeleteFileContentInternal(string fileId, IDbConnection connection, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                return CreateFailure<bool>(FileStringMessages.FileIdIsEmpty);
            }

            // Check if file is referenced
            var countResult = await _fileDependencyRepository.SelectFileDependenciesCountByReferenceId(fileId, token, connection);
            if (!countResult.Success)
            {
                return CreateFailure<bool>(FileStringMessages.CountError, countResult.ErrorMessage!.ToString());
            }
            else if (countResult.Data > 0)
            {
                return CreateFailure<bool>(FileStringMessages.FileIsReferencedError, fileId);
            }

            var fileInfoResult = await _fileInfoRepository.SelectFileInfo(fileId, token, connection);
            if (fileInfoResult.Success)
            {
                // File info exists in database
                var removeTileBasedInfoResult = await _tileBasedFileInfoRepository.RemoveTileBasedFileInfo(fileId, token, connection);
                if (!removeTileBasedInfoResult.Success)
                {
                    return CreateFailure<bool>(FileStringMessages.RemoveError, removeTileBasedInfoResult.ErrorMessage!.ToString());
                }

                var deleteInfoResult = await DeleteFileDependencies(fileId, connection, token);
                if (!deleteInfoResult.Success)
                {
                    return CreateFailure<bool>(FileStringMessages.RemoveError, deleteInfoResult.ErrorMessage!.ToString());
                }

                var removeFileInfoResult = await _fileInfoRepository.RemoveFileInfo(fileId, token, connection);
                if (!removeFileInfoResult.Success)
                {
                    return CreateFailure<bool>(FileStringMessages.RemoveError, removeFileInfoResult.ErrorMessage!.ToString());
                }
            }

            var removeContentResult = await _fileContentRepository.Remove(fileId);
            if (!removeContentResult.Success)
            {
                return CreateFailure<bool>(FileStringMessages.RemoveError, removeContentResult.ErrorMessage!.ToString());
            }

            return Result<bool>.CreateSuccess(true);
        }

        public async ValueTask<Result<bool>> DeleteFileContentByTile(GenericTileInfo tileInfo, CancellationToken token)
        {
            var transactionResult = await _repositoryAccessWrapper.OpenTransaction(token);

            using (var connection = transactionResult.Data)
            {
                var result = await DeleteFileContentByTileInternal(tileInfo, connection, token);

                if (result.Success)
                {
                    await _repositoryAccessWrapper.CommitTransaction(connection, token);
                }
                else
                {
                    await _repositoryAccessWrapper.RollbackTransaction(connection, token);
                }

                return result;
            }
        }

        private async ValueTask<Result<bool>> DeleteFileContentByTileInternal(GenericTileInfo tileInfo, IDbConnection connection, CancellationToken token)
        {
            if (tileInfo == null)
            {
                return CreateFailure<bool>(FileStringMessages.TileInfoIsEmpty);
            }

            var fileInfosResult = await _tileBasedFileInfoRepository.SelectTileBasedFileInfosByTile(tileInfo.PlanetoidId, tileInfo.Z, tileInfo.X, tileInfo.Y, token, connection);
            if (!fileInfosResult.Success)
            {
                CreateFailure<bool>(FileStringMessages.SelectError, fileInfosResult.ErrorMessage!.ToString());
            }

            foreach (var fileInfo in fileInfosResult.Data)
            {
                var deleteResult = await DeleteFileContentInternal(fileInfo.FileId, connection, token);
                if (!deleteResult.Success && !deleteResult.ErrorMessage!.ToString().Contains(FileStringMessages.FileIsReferenced))
                {
                    return CreateFailure<bool>(FileStringMessages.RemoveError, deleteResult.ErrorMessage!.ToString());
                }
            }

            return Result<bool>.CreateSuccess(true);
        }

        public async ValueTask<Result<bool>> FileIdExists(string fileId, CancellationToken token)
        {
            return string.IsNullOrWhiteSpace(fileId)
                ? CreateFailure<bool>(FileStringMessages.FileIdIsEmpty)
                : await _fileInfoRepository.FileInfoExists(fileId, token);
        }

        private async ValueTask<Result<bool>> DeleteFileDependencies(string fileId, IDbConnection connection, CancellationToken token)
        {
            var selectDependenciesResult = await _fileDependencyRepository.SelectFileDependencies(fileId, false, false, token, connection);
            if (!selectDependenciesResult.Success)
            {
                return CreateFailure<bool>(FileStringMessages.SelectError, selectDependenciesResult.ErrorMessage!.ToString());
            }

            var removeDependenciesResult = await _fileDependencyRepository.RemoveFileDependencies(fileId, token, connection);
            if (!removeDependenciesResult.Success)
            {
                return CreateFailure<bool>(FileStringMessages.RemoveError, removeDependenciesResult.ErrorMessage!.ToString());
            }

            foreach (var dependency in selectDependenciesResult.Data)
            {
                var removeDependencyResult = await DeleteFileContentInternal(dependency.ReferencedFileId, connection, token);
                if (!removeDependencyResult.Success && !removeDependencyResult.ErrorMessage!.ToString().Contains(FileStringMessages.FileIsReferenced))
                {
                    return CreateFailure<bool>(FileStringMessages.RemoveError, removeDependencyResult.ErrorMessage!.ToString());
                }
            }

            return Result<bool>.CreateSuccess(true);
        }

        private Result<T> CreateFailure<T>(string message, params string[] args)
        {
            _logger.LogError(message, args);
            return Result<T>.CreateFailure(message, args);
        }
    }
}
