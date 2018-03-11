using System;
using System.Threading;
using System.Threading.Tasks;

namespace Righthand.MessageBus
{
    public interface IDispatcher : IDisposable
    {
        /// <summary>
        /// Dispatches the <paramref name="message"/> with a given <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="message"/>.</typeparam>
        /// <param name="key">Associated key. Can be null.</param>
        /// <param name="message">An instance of message being published.</param>
        void Dispatch<T>(string key, T message);
        /// <summary>
        /// Subscribes to a message type <typeparamref name="T"/> with a given <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="message"/>.</typeparam>
        /// <param name="key">Key of message to wait for.</param>
        /// <param name="handler">An instance of handler that is invoked when message is published.</param>
        /// <param name="name">Optional name of the subscription.</param>
        /// <returns>An instance of <see cref="Subscription"/>.</returns>
        Subscription Subscribe<T>(string key, Action<string, T> handler, string name = null);
        /// <summary>
        /// Waits asynchronously for a message of type <typeparamref name="T"/> and <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">Type of message to wait for.</typeparam>
        /// <param name="key">Key of message to wait for.</param>
        /// <param name="ct">The cancellation token that will be checked prior to completing the returned task.</param>
        /// <returns>An instance of message of type <typeparamref name="T"/> when message is received.</returns>
        Task<T> GetMessageAsync<T>(string key, CancellationToken ct);
    }
}
