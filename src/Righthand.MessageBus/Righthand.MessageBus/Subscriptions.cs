using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Righthand.MessageBus
{
    /// <summary>
    /// 
    /// </summary>
    /// <threadsafety>Somewhat thread safe</threadsafety>
    internal sealed class Subscriptions: IDisposable
    {
        readonly AsyncReaderWriterLock sync = new AsyncReaderWriterLock();
        internal readonly HashSet<ISubscription> subscriptions = new HashSet<ISubscription>();
        /// <remarks>There is a chance that object has been disposed, but sync object has not been yet.</remarks>
        /// <threadsafety>It should be called only within a lock.</threadsafety>
        bool isDisposed;

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <threadsafety>It should be called only within a lock.</threadsafety>
        void CheckDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(Subscriptions));
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subscription"></param>
        /// <threadsafety>Thread safe</threadsafety>
        internal Subscriptions Add(ISubscription subscription)
        {
            using (sync.WriterLock())
            {
                CheckDisposed();
                subscriptions.Add(subscription);
                subscription.Disposed += Subscription_Disposed;
            }
            return this;
        }

        void Subscription_Disposed(object? sender, EventArgs e)
        {
            if (sender is ISubscription subscription)
            {
                using (sync.WriterLock())
                {
                    CheckDisposed();
                    RemoveSubscription(subscription);
                }
            }
            else
            {
                throw new Exception("Disposed subscription should be of type ISubscription");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <threadsafety>Caller has to lock access before invocation.</threadsafety>
        void RemoveAllSubscriptions()
        {
            foreach (var subscription in subscriptions.ToImmutableArray())
            {
                RemoveSubscription(subscription);
            }
        }

        /// <summary>Removes subscription from list.</summary>
        /// <threadsafety>It should be called only within a lock.</threadsafety>
        internal void RemoveSubscription(ISubscription subscription)
        {
            subscriptions.Remove(subscription);
            subscription.Disposed -= Subscription_Disposed;
        }

        internal IEnumerable<ISubscription> GetSubscriptions()
        {
            using (sync.ReaderLock())
            {
                foreach (var s in subscriptions)
                {
                    yield return s;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <threadsafety>Thread safe.</threadsafety>
        internal int SubscriptionsCount
        {
            get
            {
                using (sync.ReaderLock())
                {
                    return subscriptions.Count;
                }
            }
        }

        internal void DispatchSync<TKey, TMessage>(TKey key, TMessage message)
        {
            foreach (var subscription in subscriptions)
            {
                var typedSubscription = (KeyedSyncSubscription<TKey, TMessage>)subscription;
                if (Equals(key, typedSubscription.Key))
                {
                    typedSubscription.Handler(key, message);
                }
            }
        }
        internal void DispatchSync<TMessage>(TMessage message)
        {
            foreach (var subscription in subscriptions)
            {
                var typedSubscription = (SyncSubscription<TMessage>)subscription;
                typedSubscription.Handler(message);
            }
        }

        internal async Task DispatchAsync<TKey, TMessage>(TKey key, TMessage message, CancellationToken ct)
        {
            var tasks = new List<Task>();
            foreach (var subscription in subscriptions)
            {
                var typedSubscription = (KeyedAsyncSubscription<TKey, TMessage>)subscription;
                ct.ThrowIfCancellationRequested();
                if (Equals(key, typedSubscription.Key))
                {
                    tasks.Add(typedSubscription.Handler(key, message, ct));
                }
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        internal async Task DispatchAsync<TMessage>(TMessage message, CancellationToken ct)
        {
            var tasks = new List<Task>();
            foreach (var subscription in subscriptions)
            {
                var typedSubscription = (AsyncSubscription<TMessage>)subscription;
                ct.ThrowIfCancellationRequested();
                tasks.Add(typedSubscription.Handler(message, ct));
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public void Dispose()
        {
            using (sync.WriterLock())
            {
                isDisposed = true;
                RemoveAllSubscriptions();
            }
            //sync.Dispose();
        }
    }
}
