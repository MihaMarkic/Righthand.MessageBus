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
        readonly ReaderWriterLockSlim sync = new ReaderWriterLockSlim();
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
            sync.EnterWriteLock();
            try
            {
                CheckDisposed();
                subscriptions.Add(subscription);
                subscription.Disposed += Subscription_Disposed;
            }
            finally
            {
                sync.ExitWriteLock();
            }
            return this;
        }

        void Subscription_Disposed(object? sender, EventArgs e)
        {
            if (sender is ISubscription subscription)
            {
                sync.EnterWriteLock();
                try
                {
                    CheckDisposed();
                    RemoveSubscription(subscription);
                }
                finally
                {
                    sync.ExitWriteLock();
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
            sync.EnterReadLock();
            try
            {
                foreach (var s in subscriptions)
                {
                    yield return s;
                }
            }
            finally
            {
                sync.ExitReadLock();
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
                sync.EnterReadLock();
                try
                {
                    return subscriptions.Count;
                }
                finally
                {
                    sync.ExitReadLock();
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
            sync.EnterWriteLock();
            try
            {
                isDisposed = true;
                RemoveAllSubscriptions();
            }
            finally
            {
                sync.ExitWriteLock();
            }
            sync.Dispose();
        }
    }
}
