using System;

namespace Righthand.MessageBus
{
    /// <summary>
    /// Subscription interface, represents an active subscription. Implemented by synchronous subscriptions. 
    /// </summary>
    /// <remarks>It is required to dispose of it once it is not used anymore.</remarks>
    public interface ISubscription : IDisposable
    {
        /// <summary>
        /// Occurs when <see cref="ISubscription"/> is disposed.
        /// </summary>
        event EventHandler? Disposed;
        /// <summary>
        /// Name of the subscription. Optional.
        /// </summary>
        string? Name { get; }
        /// <summary>
        /// Associated handler.
        /// </summary>
        object Handler { get; }
    }
    /// <summary>
    /// Subscription interface for key based subscriptions.
    /// </summary>
    public interface IKeyedSubscription : ISubscription
    {
        /// <summary>
        /// Associated key.
        /// </summary>
        object? Key { get; }
    }
}
