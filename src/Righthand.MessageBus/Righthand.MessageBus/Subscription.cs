using System;

namespace Righthand.MessageBus
{
    /// <inheritdoc/>
    public class KeyedSubscription<TKey, TMessage> : Subscription<TMessage>
    {
        /// <summary>
        /// Assigned key.
        /// </summary>
        public TKey? Key { get; }
        /// <summary>
        /// Creates an instance of <see cref="KeyedSubscription&lt;TKey, TMessage&gt;"/>.
        /// </summary>
        /// <param name="key">Key to assign.</param>
        internal KeyedSubscription(TKey? key)
        {
            Key = key;
        }
        /// <summary>
        /// Returns a <see cref="String"/> containing information about subscription.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            base.ToString();
            string name = Name ?? GetHashCode().ToString();
            string key = Key != null ? $"with key '{Key}'" : "no key";
            return $"'{name}' on type {typeof(TMessage).Name} {key}";
        }
    }
    /// <summary>
    /// Represents a subscription for a given key/message type pair.
    /// </summary>
    /// <remarks>Dispose it to unsubscribe.</remarks>
    public class Subscription<TMessage> : IDisposable
    {
        bool isDisposed;
        /// <summary>
        /// Notifies that instance has been disposed.
        /// </summary>
        public event EventHandler? Disposed;
        /// <summary>
        /// Subscription name.
        /// </summary>
        public string? Name { get; init; }
        void OnDisposed(EventArgs e) => Disposed?.Invoke(this, e);
        /// <summary>
        /// Disposes the instance.
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                OnDisposed(EventArgs.Empty);
            }
        }
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            base.ToString();
            string name = Name ?? GetHashCode().ToString();
            return $"'{name}' on type {typeof(TMessage).Name}";
        }
    }
}
