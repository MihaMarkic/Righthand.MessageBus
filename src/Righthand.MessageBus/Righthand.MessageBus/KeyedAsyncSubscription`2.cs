using System;
using System.Threading;
using System.Threading.Tasks;

namespace Righthand.MessageBus
{
    /// <inheritdoc/>
    public class KeyedAsyncSubscription<TKey, TMessage> : KeyedSubscription<TKey, TMessage>, IKeyedSubscription
    {
        /// <summary>
        /// Handler for incoming message.
        /// </summary>
        public Func<TKey, TMessage, CancellationToken, Task> Handler { get; }
        object ISubscription.Handler => Handler;
        object? IKeyedSubscription.Key => Key;
        internal KeyedAsyncSubscription(TKey key, Func<TKey, TMessage, CancellationToken, Task> handler) : base(key)
        {
            Handler = handler;
        }
    }
    /// <inheritdoc/>
    public class AsyncSubscription<TMessage> : Subscription<TMessage>, ISubscription
    {
        /// <summary>
        /// Handler for incoming message.
        /// </summary>
        public Func<TMessage, CancellationToken, Task> Handler { get; }
        /// <inheritdoc/>
        object ISubscription.Handler => Handler;
        internal AsyncSubscription(Func<TMessage, CancellationToken, Task> handler)
        {
            Handler = handler;
        }
    }
}
