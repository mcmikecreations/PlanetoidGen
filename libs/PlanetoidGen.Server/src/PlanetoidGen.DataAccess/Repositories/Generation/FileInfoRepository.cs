using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Repositories.Documents;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Constants.StringMessages;
using PlanetoidGen.DataAccess.Repositories.Generic;
using PlanetoidGen.Domain.Models.Documents;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Generation
{
    public class FileInfoRepository : RepositoryAccessWrapper<FileInfoModel>, IFileInfoRepository
    {
        private static readonly Func<IDataReader, FileInfoModel> _reader = (r) => new FileInfoModel(
            (string)r[nameof(FileInfoModel.FileId)],
            (DateTime)r[nameof(FileInfoModel.ModifiedOn)]
            );

        public FileInfoRepository(DbConnectionStringBuilder connection, IMetaProcedureRepository meta) : base(connection, meta)
        {
        }

        public override string Name => TableStringMessages.FileInfo;

        public override Func<IDataReader, FileInfoModel>? Reader => _reader;

        public async ValueTask<Result<string>> InsertFileInfo(
            FileInfoModel model,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunSingleFunction<string>(
                StoredProcedureStringMessages.FileInfoInsert,
                new { dfileId = model.FileId },
                token,
                connection: connection);
        }

        public async ValueTask<Result<FileInfoModel>> SelectFileInfo(
            string fileId,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunSingleFunction<FileInfoModel>(
                StoredProcedureStringMessages.FileInfoSelect,
                new { dfileId = fileId },
                token,
                connection: connection);
        }

        public async ValueTask<Result<bool>> RemoveFileInfo(
            string fileId,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunSingleFunction<bool>(
                StoredProcedureStringMessages.FileInfoDelete,
                new { dfileId = fileId },
                token,
                connection: connection);
        }

        public async ValueTask<Result<bool>> FileInfoExists(
            string fileId,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunSingleFunction<bool>(
                StoredProcedureStringMessages.FileInfoExists,
                new { dfileId = fileId },
                token,
                connection: connection);
        }
    }
}
