using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Righthand.MessageBus
{
    /// <summary>
    /// Handles pub/sub message bus.
    /// </summary>
    /// <threadsafety>Thread safe.</threadsafety>
    public sealed class Dispatcher : IDispatcher
    {
        readonly object sync = new object();
        volatile bool isDisposed;
        readonly List<Subscription> subscriptions = new List<Subscription>();
        /// <summary>
        /// Dispatches the <paramref name="message"/> with a given <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of key.</typeparam>
        /// <typeparam name="TMessage">Type of <paramref name="message"/>.</typeparam>
        /// <param name="key">Associated key. Can be null.</param>
        /// <param name="message">An instance of message being published.</param>
        public void Dispatch<TKey, TMessage>(TKey key, TMessage message)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            foreach (var subscription in GetAllSubscriptions())
            {
                Action<TKey, TMessage> action;
                if ((action = GetActionIfMatches<TKey, TMessage>(subscription, key)) != null)
                {
                    try
                    {
                        action(key, message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Unhandled exception {ex.Message} during invoking subscription {subscription.ToString()}");
                    }
                }
            }
        }
        /// <summary>
        /// Dispatches the <paramref name="message"/> without a key.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="message"></param>
        public void Dispatch<TMessage>(TMessage message)
        {
            Dispatch<object, TMessage>(null, message);
        }
        internal static Action<TKey, TMessage> GetActionIfMatches<TKey, TMessage>(Subscription subscription, TKey key)
        {
            if (ReferenceEquals(subscription.Key, null) || Equals(subscription.Key, key))
            {
                if (subscription.Handler is Action<TKey, TMessage> action)
                {
                    return action;
                }
            }
            return null;
        }
        internal Subscription[] GetAllSubscriptions()
        {
            lock (sync)
            {
                return subscriptions.ToArray();
            }
        }
        /// <summary>
        /// Subscribes to a message type <typeparamref name="TMessage"/> with a given <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of key.</typeparam>
        /// <typeparam name="TMessage">Type of message.</typeparam>
        /// <param name="key">Key of message to wait for.</param>
        /// <param name="handler">An instance of handler that is invoked when message is published.</param>
        /// <param name="name">Optional name of the subscription.</param>
        /// <returns>An instance of <see cref="Subscription"/>.</returns>
        public Subscription Subscribe<TKey, TMessage>(TKey key, Action<TKey, TMessage> handler, string name = null)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            var subscription = new Subscription(key, handler, typeof(TMessage)) { Name = name };
            subscription.Disposed += Subscription_Disposed;
            lock (sync)
            {
                subscriptions.Add(subscription);
            }
            return subscription;
        }
        /// <summary>
        /// Subscribes to a message type <typeparamref name="TMessage"/> with any key.
        /// </summary>
        /// <typeparam name="TMessage">Type of message.</typeparam>
        /// <param name="handler">An instance of handler that is invoked when message is published.</param>
        /// <param name="name">Optional name of the subscription.</param>
        /// <returns>An instance of <see cref="Subscription"/>.</returns>
        public Subscription Subscribe<TMessage>(Action<object, TMessage> handler, string name = null)
        {
            return Subscribe(null, handler, name);
        }
        void Subscription_Disposed(object sender, EventArgs e)
        {
            Subscription subscription = (Subscription)sender;
            Debug.WriteLine($"Disposing subscription {subscription.Name}");
            subscription.Disposed -= Subscription_Disposed;
            subscriptions.Remove(subscription);
        }
        /// <summary>
        /// Waits asynchronously for a message of type <typeparamref name="TMessage"/> and <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of key.</typeparam>
        /// <typeparam name="TMessage">Type of message to wait for.</typeparam>
        /// <param name="key">Key of message to wait for.</param>
        /// <param name="ct">The cancellation token that will be checked prior to completing the returned task.</param>
        /// <returns>An instance of message of type <typeparamref name="TMessage"/> when message is received.</returns>
        public Task<TMessage> GetMessageAsync<TKey, TMessage>(TKey key, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<TMessage>();
            Subscription subscription = null;
            Action<TKey, TMessage> handler = (k, m) =>
            {
                subscription.Dispose();
                tcs.TrySetResult(m);
            };
            ct.Register(() =>
            {
                subscription.Dispose();
                tcs.TrySetCanceled();
            });
            subscription = Subscribe(key, handler);
            return tcs.Task;
        }
        /// <summary>
        /// Waits asynchronously for a message of type <typeparamref name="TMessage"/> with any key.
        /// </summary>
        /// <typeparam name="TMessage">Type of message to wait for.</typeparam>
        /// <param name="ct">The cancellation token that will be checked prior to completing the returned task.</param>
        /// <returns>An instance of message of type <typeparamref name="TMessage"/> when message is received.</returns>
        public Task<TMessage> GetMessageAsync<TMessage>(CancellationToken ct)
        {
            return GetMessageAsync<object, TMessage>(null, ct);
        }
        /// <summary>
        /// Disposes the dispatcher.
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                lock (sync)
                {
                    for (int i = subscriptions.Count-1; i >= 0; i++)
                    {
                        subscriptions[i].Dispose();
                    }
                }
            }
        }
    }
}
