using PlanetoidGen.Contracts.Models.Generic;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Models.Agents
{
    public interface IAgentSettings<TData>
    {
        ValueTask<string> Serialize();

        ValueTask<Result<string>> Serialize(TData value);

        ValueTask<Result<TData>> Deserialize(string value);

        ValueTask<Result<ValidationResult>> Validate(string settings);
    }
}
