using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Documents;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Documents
{
    public interface IFileDependencyRepository : INamedRepository<FileDependencyModel>
    {
        ValueTask<Result<string>> InsertFileDependency(
            FileDependencyModel model,
            CancellationToken token,
            IDbConnection? connection = null);

        ValueTask<Result<bool>> RemoveFileDependencies(
            string fileId,
            CancellationToken token,
            IDbConnection? connection = null);

        ValueTask<Result<IReadOnlyList<FileDependencyModel>>> SelectFileDependencies(
            string fileId,
            bool isRequiredOnly,
            bool isDynamicOnly,
            CancellationToken token,
            IDbConnection? connection = null);

        ValueTask<Result<int>> SelectFileDependenciesCountByReferenceId(
            string referenceId,
            CancellationToken token,
            IDbConnection? connection = null);
    }
}
