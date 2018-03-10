using System;

namespace Righthand.MessageBus
{
    public sealed class Subscription: IDisposable
    {
        bool isDisposed;
        public event EventHandler Disposed;
        public string Key { get; }
        public object Handler { get; }
        public  Type MessageType { get; }
        public string Name { get; set; }
        public Subscription(string key, object handler, Type messageType)
        {
            Key = key;
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        }
        void OnDisposed(EventArgs e) => Disposed?.Invoke(this, e);
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                OnDisposed(EventArgs.Empty);
            }
        }
        public override string ToString()
        {
            string name = Name ?? GetHashCode().ToString();
            string key = Key != null ? $"with key '{Key}'": "no key";
            return $"'{name}' on type {MessageType.Name} {key}";
        }
    }
}
