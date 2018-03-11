using System;

namespace Righthand.MessageBus
{
    /// <summary>
    /// Represents a subscription for a given key/message type pair.
    /// </summary>
    /// <remarks>Dispose it to unsubscribe.</remarks>
    public sealed class Subscription: IDisposable
    {
        bool isDisposed;
        /// <summary>
        /// Notifies that instance has been disposed.
        /// </summary>
        public event EventHandler Disposed;
        /// <summary>
        /// Assigned key.
        /// </summary>
        public object Key { get; }
        /// <summary>
        /// Handler for incoming message.
        /// </summary>
        public object Handler { get; }
        /// <summary>
        /// Type of message to handle.
        /// </summary>
        public  Type MessageType { get; }
        /// <summary>
        /// Subscription name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Creates an instance of <see cref="Subscription"/>.
        /// </summary>
        /// <param name="key">Key to assign.</param>
        /// <param name="handler">Handler for incoming message.</param>
        /// <param name="messageType">Message type.</param>
        internal Subscription(object key, object handler, Type messageType)
        {
            Key = key;
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        }
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
            string key = Key != null ? $"with key '{Key}'": "no key";
            return $"'{name}' on type {MessageType.Name} {key}";
        }
    }
}
