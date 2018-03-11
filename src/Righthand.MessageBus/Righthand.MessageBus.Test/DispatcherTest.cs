using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Righthand.MessageBus.Test
{
    public class DispatcherTest
    {
        protected Dispatcher target;

        [SetUp]
        public void SetUp()
        {
            target = new Dispatcher();
        }
        [TestFixture]
        public class GetSubscriptions: DispatcherTest
        {
            [Test]
            public void WhenInitialized_NoEntries()
            {
                var actual = target.GetAllSubscriptions();

                Assert.That(actual, Is.Empty);
            }
            [Test]
            public void WhenSubscriptionAdded_ItIsOnlyOneReturned()
            {
                var subscription = target.Subscribe<object>("key", (k, m) => { });

                var actual = target.GetAllSubscriptions();

                Assert.That(actual, Is.EqualTo(new [] { subscription }));
            }
            [Test]
            public void WhenSubscriptionIsRemoved_NoEntries()
            {
                var subscription = target.Subscribe<object>("key", (k, m) => { });
                subscription.Dispose();

                var actual = target.GetAllSubscriptions();

                Assert.That(actual, Is.Empty);
            }
        }

        [TestFixture]
        public class GetActionIfMatches: DispatcherTest
        {
            [TestCase(null, null, ExpectedResult = true)]
            [TestCase("one", null, ExpectedResult = false)]
            [TestCase(null, "two", ExpectedResult = true)]
            [TestCase("one", "two", ExpectedResult = false)]
            [TestCase("one", "one", ExpectedResult = true)]
            public bool WhenRootTypeInvokedOnRootSubscription_ReturnsCorrectValue(string subscriptionKey, string dispatchKey)
            {
                var actual = Dispatcher.GetActionIfMatches<Root>(target.Subscribe<Root>(subscriptionKey, (k, m) => { }), dispatchKey);

                return actual != null;
            }
            [TestCase(null, null, ExpectedResult = true)]
            [TestCase("one", null, ExpectedResult = false)]
            [TestCase(null, "two", ExpectedResult = true)]
            [TestCase("one", "two", ExpectedResult = false)]
            [TestCase("one", "one", ExpectedResult = true)]
            public bool WhenDerivedTypeInvokedOnRootSubscription_ReturnsCorrectValue(string subscriptionKey, string dispatchKey)
            {
                var actual = Dispatcher.GetActionIfMatches<Derived>(target.Subscribe<Root>(subscriptionKey, (k, m) => { }), dispatchKey);

                return actual != null;
            }
            [TestCase(null, null)]
            [TestCase("one", null)]
            [TestCase(null, "two")]
            [TestCase("one", "two")]
            [TestCase("one", "one")]
            public void WhenRootTypeInvokedOnDerivedSubscription_ReturnsNull(string subscriptionKey, string dispatchKey)
            {
                var actual = Dispatcher.GetActionIfMatches<Root>(target.Subscribe<Derived>(subscriptionKey, (k, m) => { }), dispatchKey);

                Assert.That(actual, Is.Null);
            }
            [TestCase(null, null)]
            [TestCase("one", null)]
            [TestCase(null, "two")]
            [TestCase("one", "two")]
            [TestCase("one", "one")]
            public void WhenDerivedTypeInvokedOnAnotherDerivedSubscription_ReturnsNull(string subscriptionKey, string dispatchKey)
            {
                var actual = Dispatcher.GetActionIfMatches<Derived>(target.Subscribe<AnotherDerived>(subscriptionKey, (k, m) => { }), dispatchKey);

                Assert.That(actual, Is.Null);
            }
            [TestCase(null, null)]
            [TestCase("one", null)]
            [TestCase(null, "two")]
            [TestCase("one", "two")]
            [TestCase("one", "one")]
            public void WhenAnotherDerivedTypeInvokedOnDerivedSubscription_ReturnsNull(string subscriptionKey, string dispatchKey)
            {
                var actual = Dispatcher.GetActionIfMatches<AnotherDerived>(target.Subscribe<Derived>(subscriptionKey, (k, m) => { }), dispatchKey);

                Assert.That(actual, Is.Null);
            }
        }

        [TestFixture]
        public class Subscribe: DispatcherTest
        {
            [Test]
            public void WhenNameIsNull_SubscriptionHasNullName()
            {
                var actual = target.Subscribe<Root>(null, (k, m) => { });

                Assert.That(actual.Name, Is.Null);
            }
            [Test]
            public void WhenNameIsNotNull_SubscriptionHasName()
            {
                var actual = target.Subscribe<Root>(null, (k, m) => { }, name: "SubscriptionName");

                Assert.That(actual.Name, Is.EqualTo("SubscriptionName"));
            }
        }

        [TestFixture]
        public class Dispatch: DispatcherTest
        {
            [Test]
            public void WhenNoSubscriptions_NoError()
            {
                target.Dispatch("key", new object());
            }
            [TestCase(null, null, ExpectedResult = true)]
            [TestCase(null, "dKey", ExpectedResult = true)]
            [TestCase("dKey", "dKey", ExpectedResult = true)]
            [TestCase("sKey", "dKey", ExpectedResult = false)]
            [TestCase("sKey", null, ExpectedResult = false)]
            public bool WhenSubscriptionKeyAndDispatchKey_MatchingTypeSubscriptionIsCalled(string subscriptionKey, string dispatchKey)
            {
                return DoDispatch<Root, Root>(subscriptionKey, dispatchKey);
            }

            [TestCase]
            public void WhenMessageIsRootTypeAndKeyIsNull_CorrectSubscriptionsAreInvoked()
            {
                var actual = DoMixedDispatch(null, new Root());

                Assert.That(actual, Is.EqualTo(new[] { new Invocation(typeof(Root), null) }));
            }
            [TestCase]
            public void WhenMessageIsRootTypeAndKeyIsBasic_CorrectSubscriptionsAreInvoked()
            {
                var actual = DoMixedDispatch(GetKey<Root>(), new Root());

                Assert.That(actual, Is.EqualTo(new[] { new Invocation(typeof(Root), null), new Invocation(typeof(Root), 0) }));
            }
            [TestCase]
            public void WhenMessageIsRootTypeAndKeyIsAlternate_CorrectSubscriptionsAreInvoked()
            {
                var actual = DoMixedDispatch(GetAlternateKey<Root>(), new Root());

                Assert.That(actual, Is.EqualTo(new[] { new Invocation(typeof(Root), null), new Invocation(typeof(Root), 1) }));
            }
            [TestCase]
            public void WhenMessageIsRootTypeAndKeyUnknown_SubscriptionsWithNullKeyIsInvoked()
            {
                var actual = DoMixedDispatch("SomeKey", new Root());

                Assert.That(actual, Is.EqualTo(new[] { new Invocation(typeof(Root), null) }));
            }

            [TestCase]
            public void WhenMessageIsDerivedTypeAndKeyIsNull_CorrectSubscriptionsAreInvoked()
            {
                var actual = DoMixedDispatch(null, new Derived());

                Assert.That(actual, Is.EqualTo(new[] { new Invocation(typeof(Root), null), new Invocation(typeof(Derived), null) }));
            }
            [TestCase]
            public void WhenMessageIsDerivedTypeAndKeyIsBasic_CorrectSubscriptionsAreInvoked()
            {
                var actual = DoMixedDispatch(GetKey<Derived>(), new Derived());

                Assert.That(actual, Is.EqualTo(new[] { new Invocation(typeof(Root), null), new Invocation(typeof(Derived), null), new Invocation(typeof(Derived), 0) }));
            }
            [TestCase]
            public void WhenMessageIsDerivedTypeAndKeyIsAlternate_CorrectSubscriptionsAreInvoked()
            {
                var actual = DoMixedDispatch(GetAlternateKey<Derived>(), new Derived());

                Assert.That(actual, Is.EqualTo(new[] { new Invocation(typeof(Root), null), new Invocation(typeof(Derived), null), new Invocation(typeof(Derived), 1) }));
            }
            [TestCase]
            public void WhenMessageIsDerivedTypeAndKeyUnknown_SubscriptionsWithNullKeyIsInvoked()
            {
                var actual = DoMixedDispatch("SomeKey", new Derived());

                Assert.That(actual, Is.EqualTo(new[] { new Invocation(typeof(Root), null), new Invocation(typeof(Derived), null) }));
            }

            bool DoDispatch<TSubscription, TDispatch>(string subscriptionKey, string dispatchKey)
                where TDispatch: new()
            {
                bool subscriptionCalled = false;
                var subscription = target.Subscribe<TSubscription>(subscriptionKey, (k, r) => { subscriptionCalled = true; });

                target.Dispatch(dispatchKey, new TDispatch());

                return subscriptionCalled;
            }

            Invocation[] DoMixedDispatch<T>(string key, T message)
            {
                var called = new List<Invocation>();
                target.Subscribe<Root>(null, (k, r) => { called.Add(new Invocation(typeof(Root), null)); });
                target.Subscribe<Root>(GetKey<Root>(), (k, r) => { called.Add(new Invocation(typeof(Root), 0)); });
                target.Subscribe<Root>(GetAlternateKey<Root>(), (k, r) => { called.Add(new Invocation(typeof(Root), 1)); });
                target.Subscribe<Derived>(null, (k, r) => { called.Add(new Invocation(typeof(Derived), null)); });
                target.Subscribe<Derived>(GetKey<Derived>(), (k, r) => { called.Add(new Invocation(typeof(Derived), 0)); });
                target.Subscribe<Derived>(GetAlternateKey<Derived>(), (k, r) => { called.Add(new Invocation(typeof(Derived), 1)); });
                target.Subscribe<AnotherDerived>(null, (k, r) => { called.Add(new Invocation(typeof(AnotherDerived), null)); });
                target.Subscribe<AnotherDerived>(GetKey<AnotherDerived>(), (k, r) => { called.Add(new Invocation(typeof(AnotherDerived), 0)); });
                target.Subscribe<AnotherDerived>(GetAlternateKey<AnotherDerived>(), (k, r) => { called.Add(new Invocation(typeof(AnotherDerived), 1)); });
                target.Subscribe<Another>(null, (k, r) => { called.Add(new Invocation(typeof(Another), null)); });
                target.Subscribe<Another>(GetKey<Another>(), (k, r) => { called.Add(new Invocation(typeof(Another), 0)); });
                target.Subscribe<Another>(GetAlternateKey<Another>(), (k, r) => { called.Add(new Invocation(typeof(Another), 1)); });

                target.Dispatch(key, message);

                return called.ToArray();
            }
            string GetKey<T>() => typeof(T).Name;
            string GetAlternateKey<T>() => typeof(T).Name + "2";
        }
        [DebuggerDisplay("{Type.Name,nq} with key {Key}")]
        public struct Invocation
        {
            public  Type Type { get; }
            public  int? Key { get; }
            public Invocation(Type type, int? key)
            {
                Type = type;
                Key = key;
            }
            public override bool Equals(object obj)
            {
                var other = (Invocation)obj;
                return Type == other.Type && Key == other.Key;
            }
            public override int GetHashCode()
            {
                return Type.GetHashCode() ^ (Key?.GetHashCode() ?? 0);
            }
        }

        public class Root { }
        public class Derived : Root { }
        public class AnotherDerived : Root { }
        public class Another { }
    }
}
