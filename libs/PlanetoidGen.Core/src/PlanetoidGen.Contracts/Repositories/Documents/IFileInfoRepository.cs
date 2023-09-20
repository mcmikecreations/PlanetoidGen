using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Documents;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Documents
{
    public interface IFileInfoRepository : INamedRepository<FileInfoModel>
    {
        ValueTask<Result<string>> InsertFileInfo(
            FileInfoModel model,
            CancellationToken token,
            IDbConnection? connection = null);

        ValueTask<Result<bool>> RemoveFileInfo(
            string fileId,
            CancellationToken token,
            IDbConnection? connection = null);

        ValueTask<Result<FileInfoModel>> SelectFileInfo(
            string fileId,
            CancellationToken token,
            IDbConnection? connection = null);

        ValueTask<Result<bool>> FileInfoExists(
            string fileId,
            CancellationToken token,
            IDbConnection? connection = null);
    }
}
