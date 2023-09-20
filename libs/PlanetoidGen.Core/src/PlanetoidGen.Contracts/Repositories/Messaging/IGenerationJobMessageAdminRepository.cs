using PlanetoidGen.Contracts.Models.Generic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Messaging
{
    public interface IGenerationJobMessageAdminRepository : IGenerationJobMessageRepositoryBase
    {
        /// <summary>
        /// Creates agent topics if they do not exists.
        /// </summary>
        /// <param name="agentsCount">Desired agent topics count.</param>
        /// <returns>A collection of created topics.</returns>
        ValueTask<Result<IEnumerable<string>>> EnsureExists(int agentsCount);

        /// <summary>
        /// Creates topics.
        /// </summary>
        /// <param name="topics">A collection of topic names.</param>
        /// <returns>A collection of created topics.</returns>
        ValueTask<Result<IEnumerable<string>>> CreateTopics(IEnumerable<string> topics);

        /// <summary>
        /// Gets all topics.
        /// </summary>
        /// <returns></returns>
        ValueTask<Result<IEnumerable<string>>> GetAllTopics();

        /// <summary>
        /// Clears all topics.
        /// </summary>
        /// <returns>A collection of deleted topics.</returns>
        ValueTask<Result<IEnumerable<string>>> DeleteAllTopics();

        /// <summary>
        /// Deletes a collection of specified topics.
        /// </summary>
        /// <param name="topics"></param>
        /// <returns>A collection of deleted topics.</returns>
        ValueTask<Result<IEnumerable<string>>> DeleteTopics(IEnumerable<string> topics);
    }
}
