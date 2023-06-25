using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        int isDisposedCounter = 0;
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Access should be protected with <see cref="sync"/>.</remarks>
        readonly Dictionary<Type, Subscriptions> subscriptions = new Dictionary<Type, Subscriptions>();
        readonly ReaderWriterLockSlim sync = new ReaderWriterLockSlim();
        /// <summary>
        /// 
        /// </summary>
        /// <threadsafety>Thread safe.</threadsafety>
        public bool IsDisposed => Interlocked.CompareExchange(ref isDisposedCounter, 0, 0) > 0;
        /// <inheritdoc/>
        public async Task DispatchAsync<TKey, TMessage>(TKey key, TMessage message, CancellationToken ct = default)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            sync.EnterReadLock();
            try
            {
                await DispatchCoreAsync(key, message, ct).ConfigureAwait(false);
                // dispatches to non async handlers
                DispatchCore(key, message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unhandled exception {ex.Message} during invoking subscription of type {typeof(Func<TKey, TMessage>)}");
            }
            finally
            {
                sync.ExitReadLock();
            }
        }
        internal async Task DispatchCoreAsync<TKey, TMessage>(TKey key, TMessage message, CancellationToken ct = default)
        {
            if (subscriptions.TryGetValue(typeof(Func<TKey, TMessage, CancellationToken, Task>), out var typeKeyedSubscriptions))
            {
                await typeKeyedSubscriptions.DispatchAsync(key, message, ct).ConfigureAwait(false);
            }
            // dispatches also to keyless subscriptions with the same message type
            if (subscriptions.TryGetValue(typeof(Func<TMessage, CancellationToken, Task>), out var typeSubscriptions))
            {
                await typeSubscriptions.DispatchAsync(message, ct).ConfigureAwait(false);
            }
        }
        /// <inheritdoc/>
        public async Task DispatchAsync<TMessage>(TMessage message, CancellationToken ct = default)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            sync.EnterReadLock();
            try
            {
                await DispatchCoreAsync(message, ct).ConfigureAwait(false);
                // dispatches to non async handlers
                DispatchCore(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unhandled exception {ex.Message} during invoking subscription of type {typeof(Func<TMessage>)}");
            }
            finally 
            { 
                sync.ExitReadLock(); 
            }
        }
        /// <inheritdoc/>
        public async Task DispatchCoreAsync<TMessage>(TMessage message, CancellationToken ct = default)
        {
            if (subscriptions.TryGetValue(typeof(Func<TMessage, CancellationToken, Task>), out var typeSubscriptions))
            {
                await typeSubscriptions.DispatchAsync(message, ct).ConfigureAwait(false);
            }
        }
        /// <inheritdoc/>
        public void Dispatch<TKey, TMessage>(TKey key, TMessage message)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            sync.EnterReadLock();
            try
            {
                DispatchCore(key, message);
                // fire & forget for async handlers
                _ = DispatchCoreAsync(key, message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unhandled exception {ex.Message} during invoking subscription of type {typeof(TMessage)}");
            }
            finally
            {
                sync.ExitReadLock();
            }
        }
        internal void DispatchCore<TKey, TMessage>(TKey key, TMessage message)
        {
            if (subscriptions.TryGetValue(typeof(Action<TKey, TMessage>), out var typeKeyedSubscriptions))
            {
                typeKeyedSubscriptions.DispatchSync(key, message);
            }
            // dispatches also to keyless subscriptions with the same message type
            if (subscriptions.TryGetValue(typeof(Action<TMessage>), out var typeSubscriptions))
            {
                typeSubscriptions.DispatchSync(message);
            }
        }
        /// <inheritdoc/>
        public void Dispatch<TMessage>(TMessage message)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            sync.EnterReadLock();
            try
            {
                DispatchCore(message);
                // fire & forget for async handlers
                _ = DispatchCoreAsync(message, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unhandled exception {ex.Message} during invoking subscription of type {typeof(Action<TMessage>)}");
            }
            finally
            {
                sync.ExitReadLock();
            }
        }
        internal void DispatchCore<TMessage>(TMessage message)
        {

            if (subscriptions.TryGetValue(typeof(Action<TMessage>), out var typeSubscriptions))
            {
                typeSubscriptions.DispatchSync(message);
            }

        }
        internal void Subscribe(Type typeKey, ISubscription subscription)
        {
            sync.EnterUpgradeableReadLock();
            try
            {
                if (!subscriptions.TryGetValue(typeKey, out var typeSubscriptions))
                {
                    sync.EnterWriteLock();
                    try
                    {
                        // needs to check again whether another thread didn't add subscriptions in between
                        if (!subscriptions.TryGetValue(typeKey, out typeSubscriptions))
                        {
                            typeSubscriptions = new Subscriptions();
                            subscriptions.Add(typeKey, typeSubscriptions);
                        }
                    }
                    finally
                    {
                        sync.ExitWriteLock();
                    }
                }
                typeSubscriptions.Add(subscription);
            }
            finally
            {
                sync.ExitUpgradeableReadLock();
            }
        }
        /// <inheritdoc/>
        public ISubscription Subscribe<TKey, TMessage>(TKey key, Func<TKey, TMessage, CancellationToken, Task> handler,
            string? name = null)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            var subscription = new KeyedAsyncSubscription<TKey, TMessage>(key, handler) { Name = name };
            var typeKey = typeof(Func<TKey, TMessage, CancellationToken, Task>);
            Subscribe(typeKey, subscription);

            return subscription;
        }
        /// <inheritdoc/>
        public ISubscription Subscribe<TMessage>(Func<TMessage, CancellationToken, Task> handler,
            string? name = null)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            var subscription = new AsyncSubscription<TMessage>(handler) { Name = name };
            var typeKey = typeof(Func<TMessage, CancellationToken, Task>);
            Subscribe(typeKey, subscription);

            return subscription;
        }
        /// <inheritdoc/>
        public ISubscription Subscribe<TKey, TMessage>(TKey key, Action<TKey, TMessage> handler, string? name = null)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            var subscription = new KeyedSyncSubscription<TKey, TMessage>(key, handler) { Name = name };
            var typeKey = typeof(Action<TKey, TMessage>);
            Subscribe(typeKey, subscription);
            
            return subscription;
        }
        /// <inheritdoc/>
        public ISubscription Subscribe<TMessage>(Action<TMessage> handler, string? name = null)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            var subscription = new SyncSubscription<TMessage>(handler) { Name = name };
            var typeKey = typeof(Action<TMessage>);
            Subscribe(typeKey, subscription);

            return subscription;
        }
        /// <inheritdoc/>
        public Task<TMessage> GetMessageAsync<TKey, TMessage>(TKey key, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<TMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            ISubscription? subscription = null;
            Action<TKey, TMessage> handler = (k, m) =>
            {
                subscription?.Dispose();
                tcs.TrySetResult(m);
            };
            ct.Register(() =>
            {
                subscription?.Dispose();
                tcs.TrySetCanceled();
            });
            subscription = Subscribe(key, handler);
            return tcs.Task;
        }
        /// <inheritdoc/>
        public Task<TMessage> GetMessageAsync<TMessage>(CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<TMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            ISubscription? subscription = null;
            Action<TMessage> handler = m =>
            {
                subscription?.Dispose();
                tcs.TrySetResult(m);
            };
            ct.Register(() =>
            {
                subscription?.Dispose();
                tcs.TrySetCanceled();
            });
            subscription = Subscribe(handler);
            return tcs.Task;
        }
        internal int GetSyncSubscriptionsCount(Type typeKey)
        {
            sync.EnterReadLock();
            try
            {
                if (subscriptions.TryGetValue(typeKey, out var typeSubscriptions))
                {
                    return typeSubscriptions.SubscriptionsCount;
                }
            }
            finally
            {
                sync.ExitReadLock();
            }
            return 0;
        }
        internal int GetSyncSubscriptionsCount<TKey, TMessage>()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            return GetSyncSubscriptionsCount(typeof(Action<TKey, TMessage>));
        }
        internal int GetSyncSubscriptionsCount<TMessage>()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            return GetSyncSubscriptionsCount(typeof(Action<TMessage>));
        }
        internal int GetAsyncSubscriptionsCount<TKey, TMessage>()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            return GetSyncSubscriptionsCount(typeof(Func<TKey, TMessage, CancellationToken, Task>));
        }
        internal int GetAsyncSubscriptionsCount<TMessage>()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            return GetSyncSubscriptionsCount(typeof(Func<TMessage, CancellationToken, Task>));
        }
        /// <summary>
        /// Disposes the dispatcher.
        /// </summary>
        public void Dispose()
        {
            if (!IsDisposed)
            {
                // sets disposed flag
                Interlocked.Increment(ref isDisposedCounter);
                sync.EnterWriteLock();
                try
                {
                    var typeSubscriptions = subscriptions.Values.ToImmutableArray();
                    foreach (var s in typeSubscriptions)
                    {
                        s.Dispose();
                    }
                    subscriptions.Clear();
                }
                finally
                {
                    sync.ExitWriteLock();
                }
            }
        }
    }
}
