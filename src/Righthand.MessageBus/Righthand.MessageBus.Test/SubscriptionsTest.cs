using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Righthand.MessageBus.Test
{
    internal class SubscriptionsTest
    {
        protected Subscriptions target = default!;

        [SetUp]
        public void SetUp()
        {
            target = new Subscriptions();
        }
        [TestFixture]
        public class GetSubscriptions : SubscriptionsTest
        {
            [Test]
            public void WhenInitialized_NoEntries()
            {
                var actual = target.GetSubscriptions();

                Assert.That(actual, Is.Empty);
            }
            [Test]
            public void WhenSubscriptionAdded_ItIsOnlyOneReturned()
            {
                var subscription = new KeyedSyncSubscription<string, object>("key", (k, m) => { });
                target.Add(subscription);

                var actual = target.GetSubscriptions();

                Assert.That(actual, Is.EqualTo(new[] { subscription }));
            }
            [Test]
            public void WhenSubscriptionIsRemoved_NoEntries()
            {
                var subscription = new KeyedSyncSubscription<string, object>("key", (k, m) => { });
                target.Add(subscription);
                target.RemoveSubscription(subscription);

                var actual = target.GetSubscriptions();

                Assert.That(actual, Is.Empty);
            }
        }
        [TestFixture]
        public class DispatchAsync : SubscriptionsTest
        {
            [Test]
            public async Task WhenSubscriptionIsPresent_WaitsForItsCompletition()
            {
                bool wasCalled = false;
                var subscription = new KeyedAsyncSubscription<string, object>("key", 
                    async (k, m, ct) =>
                    {
                        await Task.Yield();
                        wasCalled = true;
                    });
                target.Add(subscription);

                await target.DispatchAsync("key", new object(), CancellationToken.None);

                Assert.That(wasCalled, Is.True);
            }
        }
    }
}
