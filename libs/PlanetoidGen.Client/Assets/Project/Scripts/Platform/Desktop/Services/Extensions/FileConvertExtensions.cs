using Google.Protobuf;
using PlanetoidGen.API;
using PlanetoidGen.Contracts.Models.Documents;
using System.Linq;

namespace PlanetoidGen.Client.Platform.Desktop.Services.Extensions
{
    public static class FileConvertExtensions
    {
        public static FileModel ToResponseModel(this FileContentModel model)
        {
            var file = new FileModel
            {
                FileId = model.Id,
                Content = new Domain.Models.Documents.FileContentModel
                {
                    Id = model.Id,
                    Content = model.FileContent.ToArray(),
                    FileName = model.FileName,
                    LocalPath = model.LocalPath,
                    Attributes = model.Attributes.ToDictionary(i => i.Key, i => i.Value)
                }
            };

            if (model.TileBasedInfo != null)
            {
                file.TileBasedFileInfo = new Domain.Models.Documents.TileBasedFileInfoModel(
                    model.Id,
                    model.TileBasedInfo.PlanetoidId,
                    (short)model.TileBasedInfo.Z,
                    model.TileBasedInfo.X,
                    model.TileBasedInfo.Y,
                    model.TileBasedInfo.Position.ToArray(),
                    model.TileBasedInfo.Rotation.ToArray(),
                    model.TileBasedInfo.Scale.ToArray());
            }

            if (model.DependentFiles != null)
            {
                file.DependentFiles = model.DependentFiles.Select(d => new Domain.Models.Documents.FileDependencyModel(
                    model.Id,
                    d.ReferencedFileId,
                    d.IsDynamic,
                    d.IsRequired));
            }

            return file;
        }

        public static FileContentModel ToRequestModel(this FileModel model)
        {
            var file = new FileContentModel()
            {
                Id = model.FileId
            };

            if (model.Content != null)
            {
                file.FileContent = ByteString.CopyFrom(model.Content!.Content);
                file.FileName = model.Content.FileName;
                file.LocalPath = model.Content.LocalPath;

                if (model.Content.Attributes != null)
                {
                    foreach (var attribute in model.Content.Attributes)
                    {
                        file.Attributes.Add(attribute.Key, attribute.Value);
                    }
                }
            }

            if (model.TileBasedFileInfo != null)
            {
                file.TileBasedInfo = new TileBasedInfoModel
                {
                    PlanetoidId = model.TileBasedFileInfo.PlanetoidId,
                    Z = model.TileBasedFileInfo.Z,
                    X = model.TileBasedFileInfo.X,
                    Y = model.TileBasedFileInfo.Y
                };

                file.TileBasedInfo.Position.AddRange(model.TileBasedFileInfo.Position);
                file.TileBasedInfo.Rotation.AddRange(model.TileBasedFileInfo.Rotation);
                file.TileBasedInfo.Scale.AddRange(model.TileBasedFileInfo.Scale);
            }

            if (model.DependentFiles != null)
            {
                file.DependentFiles.AddRange(model.DependentFiles.Select(d => new FileDependencyModel
                {
                    ReferencedFileId = d.ReferencedFileId,
                    IsRequired = d.IsRequired,
                    IsDynamic = d.IsDynamic
                }));
            }

            return file;
        }
    }
}
