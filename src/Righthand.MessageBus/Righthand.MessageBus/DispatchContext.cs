using System.Threading.Tasks;

namespace Righthand.MessageBus
{
    /// <summary>
    /// Provides values for different dispatch threading modes when invoking subscriptions.
    /// </summary>
    /// <remarks>
    /// Synchronous subscriptions are always invoked within same thread.
    /// </remarks>
    public enum DispatchThreading
    {
        /// <summary>
        /// Invokes all subscriptions on invoking <see cref="TaskScheduler"/>.
        /// </summary>
        SameThread,
        /// <summary>
        /// Invokes subscriptions on any thread.
        /// </summary>
        AnyThread,
    }
    /// <summary>
    /// <see cref="Dispatcher"/> Dispatch context.
    /// </summary>
    /// <param name="Threading">
    /// Threading model to use when invoking subscriptions. Default is <see cref="DispatchThreading.AnyThread"/>.
    /// </param>
    public readonly record struct DispatchContext(
        DispatchThreading Threading = DispatchThreading.AnyThread
    )
    {
        /// <summary>
        /// Default context.
        /// </summary>
        public static readonly DispatchContext Default = new();
        /// <summary>
        /// A value determining configuration await.
        /// </summary>
        public bool ConfigureAwait => Threading == DispatchThreading.SameThread;
    }
}
