using System;

namespace Righthand.MessageBus
{
    public interface IDispatcher
    {
        void Dispatch<T>(string key, T message);
        Subscription Subscribe<T>(string key, Action<string, T> handler, string name = null);
    }
}
