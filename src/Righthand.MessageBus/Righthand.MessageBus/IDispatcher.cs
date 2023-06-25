using System;
using System.Threading;
using System.Threading.Tasks;

namespace Righthand.MessageBus
{
    /// <summary>
    /// Dispatcher interface.
    /// </summary>
    public interface IDispatcher : IDisposable
    {
        /// <summary>
        /// Dispatches the <paramref name="message"/> with a given <paramref name="key"/> asynchronously.
        /// </summary>
        /// <typeparam name="TKey">Type of key.</typeparam>
        /// <typeparam name="TMessage">Type of <paramref name="message"/>.</typeparam>
        /// <param name="key">Associated key. Can be null.</param>
        /// <param name="message">An instance of message being published.</param>
        /// <param name="ct">CancellationToken</param>
        Task DispatchAsync<TKey, TMessage>(TKey key, TMessage message, CancellationToken ct = default);
        /// <summary>
        /// Dispatches the <paramref name="message"/> asynchronously.
        /// </summary>
        /// <typeparam name="TMessage">Type of <paramref name="message"/>.</typeparam>
        /// <param name="message">An instance of message being published.</param>
        /// <param name="ct">CancellationToken</param>
        Task DispatchAsync<TMessage>(TMessage message, CancellationToken ct = default);
        /// <summary>
        /// Dispatches the <paramref name="message"/> with a given <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of key.</typeparam>
        /// <typeparam name="TMessage">Type of <paramref name="message"/>.</typeparam>
        /// <param name="key">Associated key. Can be null.</param>
        /// <param name="message">An instance of message being published.</param>
        void Dispatch<TKey, TMessage>(TKey key, TMessage message);
        /// <summary>
        /// Dispatches the <paramref name="message"/> without a key.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="message"></param>
        void Dispatch<TMessage>(TMessage message);
        /// <summary>
        /// Subscribes to a message type <typeparamref name="TMessage"/> with a given <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of key.</typeparam>
        /// <typeparam name="TMessage">Type of message.</typeparam>
        /// <param name="key">Key of message to wait for.</param>
        /// <param name="handler">An instance of handler that is invoked when message is published.</param>
        /// <param name="name">Optional name of the subscription.</param>
        /// <returns>An instance of <see cref="ISubscription"/>.</returns>
        ISubscription Subscribe<TKey, TMessage>(TKey key, Action<TKey, TMessage> handler, string? name = null);
        /// <summary>
        /// Subscribes to a message type <typeparamref name="TMessage"/> with a given <paramref name="key"/> with 
        /// asynchronous handling.
        /// </summary>
        /// <typeparam name="TKey">Type of key.</typeparam>
        /// <typeparam name="TMessage">Type of message.</typeparam>
        /// <param name="key">Key of message to wait for.</param>
        /// <param name="handler">An instance of handler that is invoked asynchronously when message is published.</param>
        /// <param name="name">Optional name of the subscription.</param>
        /// <returns>An instance of <see cref="ISubscription"/>.</returns>
        ISubscription Subscribe<TKey, TMessage>(TKey key, Func<TKey, TMessage, CancellationToken, Task> handler,
            string? name = null);
        /// <summary>
        /// Subscribes to a message type <typeparamref name="TMessage"/> with any key.
        /// </summary>
        /// <typeparam name="TMessage">Type of message.</typeparam>
        /// <param name="handler">An instance of handler that is invoked when message is published.</param>
        /// <param name="name">Optional name of the subscription.</param>
        /// <returns>An instance of <see cref="ISubscription"/>.</returns>
        ISubscription Subscribe<TMessage>(Action<TMessage> handler, string? name = null);
        /// <summary>
        /// Subscribes to a message type <typeparamref name="TMessage"/> with any key and  with 
        /// asynchronous handling.
        /// </summary>
        /// <typeparam name="TMessage">Type of message.</typeparam>
        /// <param name="handler">An instance of handler that is invoked when message is published.</param>
        /// <param name="name">Optional name of the subscription.</param>
        /// <returns>An instance of <see cref="ISubscription"/>.</returns>
        ISubscription Subscribe<TMessage>(Func<TMessage, CancellationToken, Task> handler, string? name = null);
        /// <summary>
        /// Waits asynchronously for a message of type <typeparamref name="TMessage"/> and <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of key.</typeparam>
        /// <typeparam name="TMessage">Type of message to wait for.</typeparam>
        /// <param name="key">Key of message to wait for.</param>
        /// <param name="ct">The cancellation token that will be checked prior to completing the returned task.</param>
        /// <returns>An instance of message of type <typeparamref name="TMessage"/> when message is received.</returns>
        Task<TMessage> GetMessageAsync<TKey, TMessage>(TKey key, CancellationToken ct);
        /// <summary>
        /// Waits asynchronously for a message of type <typeparamref name="TMessage"/> with any key.
        /// </summary>
        /// <typeparam name="TMessage">Type of message to wait for.</typeparam>
        /// <param name="ct">The cancellation token that will be checked prior to completing the returned task.</param>
        /// <returns>An instance of message of type <typeparamref name="TMessage"/> when message is received.</returns>
        Task<TMessage> GetMessageAsync<TMessage>(CancellationToken ct);
    }
}
