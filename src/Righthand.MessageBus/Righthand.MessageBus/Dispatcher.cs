using Nito.AsyncEx;
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
        readonly AsyncReaderWriterLock sync = new AsyncReaderWriterLock();
        /// <summary>
        /// 
        /// </summary>
        /// <threadsafety>Thread safe.</threadsafety>
        public bool IsDisposed => Interlocked.CompareExchange(ref isDisposedCounter, 0, 0) > 0;
        /// <inheritdoc/>
        public async Task DispatchAsync<TKey, TMessage>(TKey key, TMessage message, DispatchContext? context = null, 
            CancellationToken ct = default)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            using (sync.ReaderLock())
            {
                try
                {
                    var activeContext = context ?? DispatchContext.Default;
                    await DispatchCoreAsync(key, message, activeContext, ct).ConfigureAwait(activeContext.ConfigureAwait);
                    // dispatches to non async handlers
                    DispatchCore(key, message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unhandled exception {ex.Message} during invoking subscription of type {typeof(Func<TKey, TMessage>)}");
                }
            }
        }
        internal async Task DispatchCoreAsync<TKey, TMessage>(TKey key, TMessage message, DispatchContext context,
            CancellationToken ct = default)
        {
            if (subscriptions.TryGetValue(typeof(Func<TKey, TMessage, CancellationToken, Task>), out var typeKeyedSubscriptions))
            {
                await typeKeyedSubscriptions.DispatchAsync(key, message, ct).ConfigureAwait(context.ConfigureAwait);
            }
            // dispatches also to keyless subscriptions with the same message type
            if (subscriptions.TryGetValue(typeof(Func<TMessage, CancellationToken, Task>), out var typeSubscriptions))
            {
                await typeSubscriptions.DispatchAsync(message, ct).ConfigureAwait(context.ConfigureAwait);
            }
        }
        /// <inheritdoc/>
        public async Task DispatchAsync<TMessage>(TMessage message, DispatchContext? context = null, CancellationToken ct = default)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            using (sync.ReaderLock())
            {
                try
                {
                    var activeContext = context ?? DispatchContext.Default;
                    await DispatchCoreAsync(message, activeContext, ct).ConfigureAwait(activeContext.ConfigureAwait);
                    // dispatches to non async handlers
                    DispatchCore(message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unhandled exception {ex.Message} during invoking subscription of type {typeof(Func<TMessage>)}");
                }
            }
        }
        /// <inheritdoc/>
        public async Task DispatchCoreAsync<TMessage>(TMessage message, DispatchContext context, CancellationToken ct = default)
        {
            if (subscriptions.TryGetValue(typeof(Func<TMessage, CancellationToken, Task>), out var typeSubscriptions))
            {
                await typeSubscriptions.DispatchAsync(message, ct).ConfigureAwait(context.ConfigureAwait);
            }
        }
        /// <inheritdoc/>
        public void Dispatch<TKey, TMessage>(TKey key, TMessage message, DispatchContext? context = null)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            using (sync.ReaderLock())
            {
                try
                {
                    DispatchCore(key, message);
                    // fire & forget for async handlers
                    _ = DispatchCoreAsync(key, message, context ?? DispatchContext.Default);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unhandled exception {ex.Message} during invoking subscription of type {typeof(TMessage)}");
                }
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
        public void Dispatch<TMessage>(TMessage message, DispatchContext? context = null)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Dispatcher));
            }
            using (sync.ReaderLock())
            {
                try
                {
                    DispatchCore(message);
                    var activeContext = context ?? DispatchContext.Default;
                    // fire & forget for async handlers
                    _ = DispatchCoreAsync(message, false, activeContext).ConfigureAwait(activeContext.ConfigureAwait);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unhandled exception {ex.Message} during invoking subscription of type {typeof(Action<TMessage>)}");
                }
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
            using (sync.WriterLock())
            {
                if (!subscriptions.TryGetValue(typeKey, out var typeSubscriptions))
                {
                    //sync.EnterWriteLock();
                    //try
                    //{
                    // needs to check again whether another thread didn't add subscriptions in between
                    if (!subscriptions.TryGetValue(typeKey, out typeSubscriptions))
                    {
                        typeSubscriptions = new Subscriptions();
                        subscriptions.Add(typeKey, typeSubscriptions);
                    }
                    //}
                    //finally
                    //{
                    //    sync.ExitWriteLock();
                    //}
                }
                typeSubscriptions.Add(subscription);
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
            using (sync.ReaderLock())
            {
                if (subscriptions.TryGetValue(typeKey, out var typeSubscriptions))
                {
                    return typeSubscriptions.SubscriptionsCount;
                }
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
                using (sync.WriterLock())
                {
                    var typeSubscriptions = subscriptions.Values.ToImmutableArray();
                    foreach (var s in typeSubscriptions)
                    {
                        s.Dispose();
                    }
                    subscriptions.Clear();
                }
            }
        }
    }
}
