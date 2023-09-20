using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Domain.Models.Documents;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Documents
{
    public interface IDocumentBaseRepository<T> where T : DocumentBase
    {
        ValueTask<Result<bool>> Create(T item);
        ValueTask<Result<bool>> Exists(string id);
        ValueTask<Result<List<T>>> GetAll();
        ValueTask<Result<T>> GetById(string id);
        ValueTask<Result<bool>> Remove(string id);
        ValueTask<Result<int>> RemoveAll(IEnumerable<string> ids);
        ValueTask<Result<bool>> Update(string id, T item);
    }
}
