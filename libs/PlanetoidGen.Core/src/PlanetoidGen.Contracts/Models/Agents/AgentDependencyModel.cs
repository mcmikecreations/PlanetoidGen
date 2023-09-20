using PlanetoidGen.Domain.Enums;
using PlanetoidGen.Domain.Models.Info;

namespace PlanetoidGen.Contracts.Models.Agents
{
    public class AgentDependencyModel
    {
        public RelativeTileDirectionType Direction { get; }

        public DataTypeInfoModel DataType { get; }

        public AgentDependencyModel(RelativeTileDirectionType direction, DataTypeInfoModel dataType)
        {
            Direction = direction;
            DataType = dataType;
        }
    }
}
