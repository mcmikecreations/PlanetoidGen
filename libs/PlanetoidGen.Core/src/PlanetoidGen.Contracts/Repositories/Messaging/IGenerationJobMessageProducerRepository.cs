using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Repositories.Messaging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Contracts.Repositories.Messaging
{
    public interface IGenerationJobMessageProducerRepository
    {
        /// <summary>
        /// Produces single message of type <typeparamref name="TMessage"/>.
        /// </summary>
        /// <typeparam name="TMessage">Type param for message.</typeparam>
        /// <param name="message">Message to be produced.</param>
        /// <returns></returns>
        ValueTask<Result> ProduceAsync<TMessage>(
            TMessage message,
            CancellationToken token) where TMessage : class, IGenerationJobMessage;

        /// <summary>
        /// Produces a collection of messages of type <typeparamref name="TMessage"/>.
        /// </summary>
        /// <typeparam name="TMessage">Type param for message.</typeparam>
        /// <param name="messages">Messages to be produced.</param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        ValueTask<Result> ProduceAsync<TMessage>(
            IEnumerable<TMessage> messages,
            CancellationToken token) where TMessage : class, IGenerationJobMessage;
    }
}
