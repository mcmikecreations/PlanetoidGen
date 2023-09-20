using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using PlanetoidGen.Contracts.Constants.StringMessages;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Documents;
using PlanetoidGen.Contracts.Repositories.Documents;
using PlanetoidGen.Domain.Models.Documents;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Documents
{
    public class FileContentRepository : DocumentBaseRepository<FileContentModel>, IFileContentRepository
    {
        private readonly GridFSBucket _bucket;
        private readonly int _maxDocumentSizeInBytes;

        public FileContentRepository(IOptions<DocumentDbOptions> dbOptions) : base(dbOptions)
        {
            _maxDocumentSizeInBytes = dbOptions.Value.MaxDocumentSizeInBytes!.Value;

            var options = new GridFSBucketOptions
            {
                BucketName = dbOptions.Value.BucketName,
                DisableMD5 = true
            };

            _bucket = new GridFSBucket(Database, options);
        }

        public override async ValueTask<Result<FileContentModel>> GetById(string id)
        {
            var item = await base.GetById(id);
            if (item.Success && !string.IsNullOrEmpty(item.Data?.LargeContentId))
            {
                return await DownloadLargeContent(item.Data);
            }

            return item;
        }

        public override async ValueTask<Result<List<FileContentModel>>> GetAll()
        {
            var items = await base.GetAll();
            if (items.Success && items.Data != null)
            {
                foreach (var item in items.Data)
                {
                    if (!string.IsNullOrEmpty(item?.LargeContentId))
                    {
                        var downloadResult = await DownloadLargeContent(item);

                        if (!downloadResult.Success)
                        {
                            return Result<List<FileContentModel>>.CreateFailure(downloadResult);
                        }
                    }
                }
            }

            return items;
        }

        public async ValueTask<Result<FileContentModel>> GetByPath(string fileName, string localPath)
        {
            var item = await Execute(async () =>
            {
                var result = await Collection.Find(x => x.FileName == fileName && x.LocalPath == localPath).FirstOrDefaultAsync();
                return Result<FileContentModel>.CreateSuccess(result);
            });

            if (item.Success && !string.IsNullOrEmpty(item.Data?.LargeContentId))
            {
                return await DownloadLargeContent(item.Data);
            }

            return item;
        }

        public override async ValueTask<Result<bool>> Create(FileContentModel item)
        {
            if (!string.IsNullOrEmpty(item.LargeContentId))
            {
                return Result<bool>.CreateFailure(FileStringMessages.LargeContentIdShouldBeEmpty);
            }

            if (item.Content.Length > _maxDocumentSizeInBytes)
            {
                var uploadResult = await UploadLargeContent(item);

                if (!uploadResult.Success)
                {
                    return Result<bool>.CreateFailure(uploadResult);
                }
            }

            return await base.Create(item);
        }

        public override async ValueTask<Result<bool>> Update(string id, FileContentModel item)
        {
            var itemBeforeUpdate = await base.GetById(id);
            if (itemBeforeUpdate.Success && !string.IsNullOrEmpty(itemBeforeUpdate.Data?.LargeContentId))
            {
                var deleteResult = await RemoveLargeContent(itemBeforeUpdate.Data.LargeContentId);

                if (!deleteResult.Success)
                {
                    return Result<bool>.CreateFailure(deleteResult);
                }

                item.LargeContentId = null;
            }

            if (item.Content.Length > _maxDocumentSizeInBytes)
            {
                var uploadResult = await UploadLargeContent(item);

                if (!uploadResult.Success)
                {
                    return Result<bool>.CreateFailure(uploadResult);
                }
            }

            return await base.Update(id, item);
        }

        public override async ValueTask<Result<bool>> Remove(string id)
        {
            var item = await base.GetById(id);
            if (item.Success && item.Data != null && !string.IsNullOrEmpty(item.Data.LargeContentId))
            {
                var deleteResult = await RemoveLargeContent(item.Data.LargeContentId);

                if (!deleteResult.Success)
                {
                    return Result<bool>.CreateFailure(deleteResult);
                }
            }

            return await base.Remove(id);
        }

        public override async ValueTask<Result<int>> RemoveAll(IEnumerable<string> ids)
        {
            foreach (var id in ids)
            {
                var item = await base.GetById(id);
                if (item.Success && item.Data != null && !string.IsNullOrEmpty(item.Data.LargeContentId))
                {
                    var deleteResult = await RemoveLargeContent(item.Data.LargeContentId);

                    if (!deleteResult.Success)
                    {
                        return Result<int>.CreateFailure(deleteResult);
                    }
                }
            }

            return await base.RemoveAll(ids);
        }

        private async Task<Result<FileContentModel>> DownloadLargeContent(FileContentModel item)
        {
            return await Execute(async () =>
            {
                item.Content = await _bucket.DownloadAsBytesAsync(new ObjectId(item.LargeContentId));
                return Result<FileContentModel>.CreateSuccess(item);
            });
        }

        private async Task<Result> UploadLargeContent(FileContentModel item)
        {
            return await Execute(async () =>
            {
                var contentId = await _bucket.UploadFromBytesAsync(item.Id, item.Content);
                item.LargeContentId = contentId.ToString();
                item.Content = null;

                return Result.CreateSuccess();
            });
        }

        private async Task<Result> RemoveLargeContent(string id)
        {
            return await Execute(async () =>
            {
                await _bucket.DeleteAsync(new ObjectId(id));
                return Result.CreateSuccess();
            });
        }
    }
}
