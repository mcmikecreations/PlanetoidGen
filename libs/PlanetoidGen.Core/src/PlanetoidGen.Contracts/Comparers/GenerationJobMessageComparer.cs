using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using System.Collections.Generic;

namespace PlanetoidGen.Contracts.Comparers
{
    public class GenerationJobMessageComparer : IEqualityComparer<GenerationJobMessage>
    {
        public bool Equals(GenerationJobMessage x, GenerationJobMessage y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }
            else if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
            {
                return false;
            }
            else if (x.PlanetoidId == y.PlanetoidId
                && x.PlanetoidAgentsCount == y.PlanetoidAgentsCount
                && x.AgentIndex == y.AgentIndex
                && x.Z == y.Z
                && x.X == y.X
                && x.Y == y.Y)
            {
                return true;
            }

            return false;
        }

        public int GetHashCode(GenerationJobMessage job)
        {
            // Force Equals() method to be called
            return job.PlanetoidId.GetHashCode();
        }
    }
}
