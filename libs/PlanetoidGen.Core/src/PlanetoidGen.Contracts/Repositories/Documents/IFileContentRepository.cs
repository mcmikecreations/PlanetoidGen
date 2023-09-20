using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Documents;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Documents
{
    public interface IFileContentRepository : IDocumentBaseRepository<FileContentModel>
    {
        ValueTask<Result<FileContentModel>> GetByPath(string fileName, string localPath);
    }
}
