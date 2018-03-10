using NUnit.Framework;
using System;

namespace Righthand.MessageBus.Test
{
    public class SubscriptionTest
    {
        [TestFixture]
        public class Constructor: SubscriptionTest
        {
            [Test]
            public void WhenHandlerIsNull_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => new Subscription("key", handler: null, messageType: typeof(object)));
            }
            [Test]
            public void WhenMessageTypeIsNull_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => new Subscription("key", handler: new object(), messageType: null));
            }
            [Test]
            public void WhenMessageTypeAndHandlerNotNull_DoesNotThrow()
            {
                var actual = new Subscription("key", handler: new object(), messageType: typeof(object));
            }
        }
    }
}
