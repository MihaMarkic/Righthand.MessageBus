using System;

namespace Righthand.MessageBus
{
    /// <inheritdoc/>
    public class KeyedSyncSubscription<TKey, TMessage> : KeyedSubscription<TKey, TMessage>, IKeyedSubscription
    {
        /// <summary>
        /// Handler for incoming message.
        /// </summary>
        public Action<TKey, TMessage> Handler { get; }
        /// <inheritdoc/>
        object? IKeyedSubscription.Key => Key;
        /// <inheritdoc/>
        object ISubscription.Handler => Handler;
        internal KeyedSyncSubscription(TKey key, Action<TKey, TMessage> handler) : base(key)
        {
            Handler = handler;
        }
    }
    /// <inheritdoc/>
    public class SyncSubscription<TMessage> : Subscription<TMessage>, ISubscription
    {
        /// <summary>
        /// Handler for incoming message.
        /// </summary>
        public Action<TMessage> Handler { get; }
        /// <inheritdoc/>
        object ISubscription.Handler => Handler;
        internal SyncSubscription(Action<TMessage> handler)
        {
            Handler = handler;
        }
    }
}
