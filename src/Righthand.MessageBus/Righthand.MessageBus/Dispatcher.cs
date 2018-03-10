using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Righthand.MessageBus
{
    public class Dispatcher : IDispatcher
    {
        readonly object sync = new object();
        readonly List<Subscription> subscriptions = new List<Subscription>();
        public void Dispatch<T>(string key, T message)
        {
            foreach (var subscription in GetSubscriptions())
            {
                Action<string, T> action;
                if ((action = GetActionIfMatches<T>(subscription, key)) != null)
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
        internal static Action<string, T> GetActionIfMatches<T>(Subscription subscription, string key)
        {
            if (subscription.Key == null || string.Equals(subscription.Key, key))
            {
                if (subscription.Handler is Action<string, T> action)
                {
                    return action;
                }
            }
            return null;
        }
        internal Subscription[] GetSubscriptions()
        {
            lock (sync)
            {
                return subscriptions.ToArray();
            }
        }

        public Subscription Subscribe<T>(string key, Action<string, T> handler, string name = null)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            var subscription = new Subscription(key, handler, typeof(T)) { Name = name };
            subscription.Disposed += Subscription_Disposed;
            lock (sync)
            {
                subscriptions.Add(subscription);
            }
            return subscription;
        }

        void Subscription_Disposed(object sender, EventArgs e)
        {
            Subscription subscription = (Subscription)sender;
            Debug.WriteLine($"Disposing subscription {subscription.Name}");
            subscription.Disposed -= Subscription_Disposed;
            subscriptions.Remove(subscription);
        }
    }
}
