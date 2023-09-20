using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Repositories.Documents;
using PlanetoidGen.Contracts.Repositories.Meta;
using PlanetoidGen.DataAccess.Constants.StringMessages;
using PlanetoidGen.DataAccess.Repositories.Generic;
using PlanetoidGen.Domain.Models.Documents;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.DataAccess.Repositories.Generation
{
    public class FileDependencyRepository : RepositoryAccessWrapper<FileDependencyModel>, IFileDependencyRepository
    {
        private static readonly Func<IDataReader, FileDependencyModel> _reader = (r) => new FileDependencyModel(
            (string)r[nameof(FileDependencyModel.FileId)],
            (string)r[nameof(FileDependencyModel.ReferencedFileId)],
            (bool)r[nameof(FileDependencyModel.IsRequired)],
            (bool)r[nameof(FileDependencyModel.IsDynamic)]
            );

        public FileDependencyRepository(DbConnectionStringBuilder connection, IMetaProcedureRepository meta) : base(connection, meta)
        {
        }

        public override string Name => TableStringMessages.FileDependency;

        public override Func<IDataReader, FileDependencyModel>? Reader => _reader;

        public async ValueTask<Result<string>> InsertFileDependency(
            FileDependencyModel model,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunSingleFunction<string>(
                StoredProcedureStringMessages.FileDependencyInsert,
                new { dfileId = model.FileId, dreferencedFileId = model.ReferencedFileId, disRequired = model.IsRequired, disDynamic = model.IsDynamic },
                token,
                connection: connection);
        }

        public async ValueTask<Result<IReadOnlyList<FileDependencyModel>>> SelectFileDependencies(
            string fileId,
            bool isRequiredOnly,
            bool isDynamicOnly,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunMultipleFunction<FileDependencyModel>(
                StoredProcedureStringMessages.FileDependencySelect,
                new { dfileId = fileId, disRequiredOnly = isRequiredOnly, disDynamicOnly = isDynamicOnly },
                token,
                connection: connection);
        }

        public async ValueTask<Result<int>> SelectFileDependenciesCountByReferenceId(
            string referenceId,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunSingleFunction<int>(
                StoredProcedureStringMessages.FileDependencyCountByReferenceId,
                new { dreferenceId = referenceId },
                token,
                connection: connection);
        }

        public async ValueTask<Result<bool>> RemoveFileDependencies(
            string fileId,
            CancellationToken token,
            IDbConnection? connection = null)
        {
            return await RunSingleFunction<bool>(
                StoredProcedureStringMessages.FileDependencyDelete,
                new { dfileId = fileId },
                token,
                connection: connection);
        }
    }
}
