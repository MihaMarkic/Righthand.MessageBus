using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
                var subscription = target.Subscribe<string, object>("key", (k, m) => { });

                var actual = target.GetAllSubscriptions();

                Assert.That(actual, Is.EqualTo(new [] { subscription }));
            }
            [Test]
            public void WhenSubscriptionIsRemoved_NoEntries()
            {
                var subscription = target.Subscribe<string, object>("key", (k, m) => { });
                subscription.Dispose();

                var actual = target.GetAllSubscriptions();

                Assert.That(actual, Is.Empty);
            }
        }
        [TestFixture]
        public class GetMessageAsync: DispatcherTest
        {
            [Test]
            public void WhenStartingAwait_SubscriptionIsAdded()
            {
                var task = target.GetMessageAsync<object>(CancellationToken.None);

                Assert.That(target.GetAllSubscriptions().Length, Is.EqualTo(1));
            }
            [Test]
            public async Task AfterMessagePublished_SubscriptionsIsEmpty()
            {
                var task = target.GetMessageAsync<object>(CancellationToken.None);
                target.Dispatch(new object());
                await task;

                Assert.That(target.GetAllSubscriptions().Length, Is.EqualTo(0));
            }
            [Test]
            public async Task AfterMessageIsCancelled_SubscriptionsIsEmpty()
            {
                var cts = new CancellationTokenSource();
                var task = target.GetMessageAsync<object>(cts.Token);
                cts.Cancel();
                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {}

                Assert.That(target.GetAllSubscriptions().Length, Is.EqualTo(0));
            }

            [Test]
            public async Task AfterMessageIsCancelled_TaskCancelledExceptionIsThrown()
            {
                var cts = new CancellationTokenSource();
                var task = target.GetMessageAsync<object>(cts.Token);
                cts.Cancel();
                Assert.ThrowsAsync<TaskCanceledException>(() => task);
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
            public bool WhenRootTypeInvokedOnRootSubscriptionWithStringKey_ReturnsCorrectValue(string subscriptionKey, string dispatchKey)
            {
                var actual = Dispatcher.GetActionIfMatches<string, Root>(target.Subscribe<string, Root>(subscriptionKey, (k, m) => { }), dispatchKey);

                return actual != null;
            }
            [TestCase(null, null, ExpectedResult = true)]
            [TestCase("one", null, ExpectedResult = false)]
            [TestCase(null, "two", ExpectedResult = true)]
            [TestCase("one", "two", ExpectedResult = false)]
            [TestCase("one", "one", ExpectedResult = true)]
            public bool WhenDerivedTypeInvokedOnRootSubscriptionWithStringKey_ReturnsCorrectValue(string subscriptionKey, string dispatchKey)
            {
                var actual = Dispatcher.GetActionIfMatches<string, Derived>(target.Subscribe<string, Root>(subscriptionKey, (k, m) => { }), dispatchKey);

                return actual != null;
            }
            [TestCase(null, null)]
            [TestCase("one", null)]
            [TestCase(null, "two")]
            [TestCase("one", "two")]
            [TestCase("one", "one")]
            public void WhenRootTypeInvokedOnDerivedSubscriptionWithStringKey_ReturnsNull(string subscriptionKey, string dispatchKey)
            {
                var actual = Dispatcher.GetActionIfMatches<string, Root>(target.Subscribe<string, Derived>(subscriptionKey, (k, m) => { }), dispatchKey);

                Assert.That(actual, Is.Null);
            }
            [TestCase(null, null)]
            [TestCase("one", null)]
            [TestCase(null, "two")]
            [TestCase("one", "two")]
            [TestCase("one", "one")]
            public void WhenDerivedTypeInvokedOnAnotherDerivedSubscriptionWithStringKey_ReturnsNull(string subscriptionKey, string dispatchKey)
            {
                var actual = Dispatcher.GetActionIfMatches<string, Derived>(target.Subscribe<string, AnotherDerived>(subscriptionKey, (k, m) => { }), dispatchKey);

                Assert.That(actual, Is.Null);
            }
            [TestCase(null, null)]
            [TestCase("one", null)]
            [TestCase(null, "two")]
            [TestCase("one", "two")]
            [TestCase("one", "one")]
            public void WhenAnotherDerivedTypeInvokedOnDerivedSubscriptionWithStringKey_ReturnsNull(string subscriptionKey, string dispatchKey)
            {
                var actual = Dispatcher.GetActionIfMatches<string, AnotherDerived>(target.Subscribe<string, Derived>(subscriptionKey, (k, m) => { }), dispatchKey);

                Assert.That(actual, Is.Null);
            }
            [TestCase(null, null, ExpectedResult = true)]
            [TestCase(TestEnumKey.One, null, ExpectedResult = false)]
            [TestCase(null, TestEnumKey.Two, ExpectedResult = true)]
            [TestCase(TestEnumKey.One, TestEnumKey.Two, ExpectedResult = false)]
            [TestCase(TestEnumKey.One, TestEnumKey.One, ExpectedResult = true)]
            public bool WhenRootTypeInvokedOnRootSubscriptionWitNullableEnumKey_ReturnsCorrectValue(TestEnumKey? subscriptionKey, TestEnumKey? dispatchKey)
            {
                var actual = Dispatcher.GetActionIfMatches<TestEnumKey?, Root>(target.Subscribe<TestEnumKey?, Root>(subscriptionKey, (k, m) => { }), dispatchKey);

                return actual != null;
            }
            [TestCase(TestEnumKey.One, TestEnumKey.Two, ExpectedResult = false)]
            [TestCase(TestEnumKey.One, TestEnumKey.One, ExpectedResult = true)]
            public bool WhenRootTypeInvokedOnRootSubscriptionWitEnumKey_ReturnsCorrectValue(TestEnumKey subscriptionKey, TestEnumKey dispatchKey)
            {
                var actual = Dispatcher.GetActionIfMatches<TestEnumKey, Root>(target.Subscribe<TestEnumKey, Root>(subscriptionKey, (k, m) => { }), dispatchKey);

                return actual != null;
            }
        }

        [TestFixture]
        public class Subscribe: DispatcherTest
        {
            [Test]
            public void WhenNameIsNull_SubscriptionHasNullName()
            {
                var actual = target.Subscribe<string, Root>(null, (k, m) => { });

                Assert.That(actual.Name, Is.Null);
            }
            [Test]
            public void WhenNameIsNotNull_SubscriptionHasName()
            {
                var actual = target.Subscribe<string, Root>(null, (k, m) => { }, name: "SubscriptionName");

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
                var subscription = target.Subscribe<string, TSubscription>(subscriptionKey, (k, r) => { subscriptionCalled = true; });

                target.Dispatch(dispatchKey, new TDispatch());

                return subscriptionCalled;
            }

            Invocation[] DoMixedDispatch<T>(string key, T message)
            {
                var called = new List<Invocation>();
                target.Subscribe<string, Root>(null, (k, r) => { called.Add(new Invocation(typeof(Root), null)); });
                target.Subscribe<string, Root>(GetKey<Root>(), (k, r) => { called.Add(new Invocation(typeof(Root), 0)); });
                target.Subscribe<string, Root>(GetAlternateKey<Root>(), (k, r) => { called.Add(new Invocation(typeof(Root), 1)); });
                target.Subscribe<string, Derived>(null, (k, r) => { called.Add(new Invocation(typeof(Derived), null)); });
                target.Subscribe<string, Derived>(GetKey<Derived>(), (k, r) => { called.Add(new Invocation(typeof(Derived), 0)); });
                target.Subscribe<string, Derived>(GetAlternateKey<Derived>(), (k, r) => { called.Add(new Invocation(typeof(Derived), 1)); });
                target.Subscribe<string, AnotherDerived>(null, (k, r) => { called.Add(new Invocation(typeof(AnotherDerived), null)); });
                target.Subscribe<string, AnotherDerived>(GetKey<AnotherDerived>(), (k, r) => { called.Add(new Invocation(typeof(AnotherDerived), 0)); });
                target.Subscribe<string, AnotherDerived>(GetAlternateKey<AnotherDerived>(), (k, r) => { called.Add(new Invocation(typeof(AnotherDerived), 1)); });
                target.Subscribe<string, Another>(null, (k, r) => { called.Add(new Invocation(typeof(Another), null)); });
                target.Subscribe<string, Another>(GetKey<Another>(), (k, r) => { called.Add(new Invocation(typeof(Another), 0)); });
                target.Subscribe<string, Another>(GetAlternateKey<Another>(), (k, r) => { called.Add(new Invocation(typeof(Another), 1)); });

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

        public enum TestEnumKey
        {
            One,
            Two,
            Three
        }
    }
}
