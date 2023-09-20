using PlanetoidGen.Contracts.Enums.Messaging;
using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Messaging
{
    public interface IGenerationJobMessageConsumerRepository : IGenerationJobMessageRepositoryBase
    {
        /// <summary>
        /// Starts consuming messages from configured topic until cancellation is requested
        /// or <see cref="IGenerationJobMessage.PlanetoidAgentsCount"/> is different from
        /// <paramref name="agentsCount"/>.
        /// </summary>
        /// <typeparam name="TMessage">Type param for message.</typeparam>
        /// <param name="consumerId"></param>
        /// <param name="agentsCount">Agents count.</param>
        /// <param name="messageProcessor">A callback to process consumed messages.</param>
        /// <returns></returns>
        ValueTask<Result> ConsumeAsync<TMessage>(
            string consumerId,
            int agentsCount,
            Func<TMessage, ValueTask<Result<GenerationJobMessageProcessingStatus>>> messageProcessor,
            CancellationToken token)
            where TMessage : class, IGenerationJobMessage;
    }
}
