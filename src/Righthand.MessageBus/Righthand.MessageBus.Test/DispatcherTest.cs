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
        protected Dispatcher target = default!;

        [SetUp]
        public void SetUp()
        {
            target = new Dispatcher();
        }
        [TestFixture]
        public class GetMessageAsync: DispatcherTest
        {
            [Test]
            public void WhenStartingAwait_SubscriptionIsAdded()
            {
                var task = target.GetMessageAsync<object>(CancellationToken.None);

                Assert.That(target.GetSyncSubscriptionsCount<object>, Is.EqualTo(1));
            }
            [Test]
            public async Task AfterMessagePublished_SubscriptionsIsEmpty()
            {
                var task = target.GetMessageAsync<object>(CancellationToken.None);
                target.Dispatch(new object());
                bool success = await task.TaskWithTimeoutAsync();

                Assert.That(success, Is.True, "Await timed out");
                Assert.That(target.GetSyncSubscriptionsCount<object?, object>, Is.Zero);
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
                { }

                Assert.That(target.GetSyncSubscriptionsCount<object?, object>, Is.Zero);
            }
            [Test]
            public async Task AfterMessageIsCancelled_OperationCanceledExceptionIsThrown()
            {
                bool wasThrown = false;
                var cts = new CancellationTokenSource();
                var task = target.GetMessageAsync<object>(cts.Token);
                cts.Cancel();
                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {
                    wasThrown = true;
                }

                Assert.That(wasThrown, Is.True);
            }
        }

        [TestFixture]
        public class Subscribe: DispatcherTest
        {
            [Test]
            public void WhenNameIsNull_SubscriptionHasNullName()
            {
                var actual = target.Subscribe<string?, Root>(null, (k, m) => { });

                Assert.That(actual.Name, Is.Null);
            }
            [Test]
            public void WhenNameIsNotNull_SubscriptionHasName()
            {
                var actual = target.Subscribe<string?, Root>(null, (k, m) => { }, name: "SubscriptionName");

                Assert.That(actual.Name, Is.EqualTo("SubscriptionName"));
            }
        }
        [TestFixture]
        public class DispatchAsync : DispatcherTest
        {
            [Test]
            public async Task WhenKeyLessSubscription_ButDispatchUsesKey_MessageIsReceived()
            {
                bool wasReceived = false;
                var actual = target.Subscribe<Root>((m, ct) =>
                {
                    wasReceived = true;
                    return Task.CompletedTask;
                });

                await target.DispatchAsync("key", new Root());

                Assert.That(wasReceived, Is.True);
            }
        }
        [TestFixture]
        public class Dispatch : DispatcherTest
        {
            [Test]
            public void WhenKeyLessSubscription_ButDispatchUsesKey_MessageIsReceived()
            {
                bool wasReceived = false;
                var actual = target.Subscribe<Root>((m) => { wasReceived = true; });

                target.Dispatch("key", new Root());

                Assert.That(wasReceived, Is.True);
            }
            [Test]
            public void WhenNoSubscriptions_NoError()
            {
                target.Dispatch("key", new object());
            }
            [TestCase(null, null, ExpectedResult = true)]
            [TestCase(null, "dKey", ExpectedResult = false)]
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

                Assert.That(actual, Is.EqualTo(new[] { new Invocation(typeof(Root), 0) }));
            }
            [TestCase]
            public void WhenMessageIsRootTypeAndKeyIsAlternate_CorrectSubscriptionsAreInvoked()
            {
                var actual = DoMixedDispatch(GetAlternateKey<Root>(), new Root());

                Assert.That(actual, Is.EqualTo(new[] { new Invocation(typeof(Root), 1) }));
            }
            [TestCase]
            public void WhenMessageIsRootTypeAndKeyUnknown_SubscriptionsWithNullKeyIsNotInvoked()
            {
                var actual = DoMixedDispatch("SomeKey", new Root());

                Assert.That(actual, Is.Empty);
            }

            [TestCase]
            public void WhenMessageIsDerivedTypeAndKeyIsNull_CorrectSubscriptionsAreInvoked()
            {
                var actual = DoMixedDispatch(null, new Derived());

                Assert.That(actual, Is.EqualTo(new[] { new Invocation(typeof(Derived), null) }));
            }
            [TestCase]
            public void WhenMessageIsDerivedTypeAndKeyIsBasic_CorrectSubscriptionsAreInvoked()
            {
                var actual = DoMixedDispatch(GetKey<Derived>(), new Derived());

                Assert.That(actual, Is.EqualTo(new[] { new Invocation(typeof(Derived), 0) }));
            }
            [TestCase]
            public void WhenMessageIsDerivedTypeAndKeyIsAlternate_CorrectSubscriptionsAreInvoked()
            {
                var actual = DoMixedDispatch(GetAlternateKey<Derived>(), new Derived());

                Assert.That(actual, Is.EqualTo(new[] {  new Invocation(typeof(Derived), 1) }));
            }
            [TestCase]
            public void WhenMessageIsDerivedTypeAndKeyUnknown_SubscriptionsWithNullKeyIsNotInvoked()
            {
                var actual = DoMixedDispatch("SomeKey", new Derived());

                Assert.That(actual, Is.Empty);
            }

            bool DoDispatch<TSubscription, TDispatch>(string subscriptionKey, string? dispatchKey)
                where TDispatch: new()
            {
                bool subscriptionCalled = false;
                var subscription = target.Subscribe<string, TSubscription>(subscriptionKey, (k, r) => { subscriptionCalled = true; });

                
                
                target.Dispatch(dispatchKey, new TDispatch());

                return subscriptionCalled;
            }

            Invocation[] DoMixedDispatch<T>(string? key, T message)
            {
                var called = new List<Invocation>();
                target.Subscribe<string?, Root>(null, (k, r) => { called.Add(new Invocation(typeof(Root), null)); });
                target.Subscribe<string?, Root>(GetKey<Root>(), (k, r) => { called.Add(new Invocation(typeof(Root), 0)); });
                target.Subscribe<string?, Root>(GetAlternateKey<Root>(), (k, r) => { called.Add(new Invocation(typeof(Root), 1)); });
                target.Subscribe<string?, Derived>(null, (k, r) => { called.Add(new Invocation(typeof(Derived), null)); });
                target.Subscribe<string?, Derived>(GetKey<Derived>(), (k, r) => { called.Add(new Invocation(typeof(Derived), 0)); });
                target.Subscribe<string?, Derived>(GetAlternateKey<Derived>(), (k, r) => { called.Add(new Invocation(typeof(Derived), 1)); });
                target.Subscribe<string?, AnotherDerived>(null, (k, r) => { called.Add(new Invocation(typeof(AnotherDerived), null)); });
                target.Subscribe<string?, AnotherDerived>(GetKey<AnotherDerived>(), (k, r) => { called.Add(new Invocation(typeof(AnotherDerived), 0)); });
                target.Subscribe<string?, AnotherDerived>(GetAlternateKey<AnotherDerived>(), (k, r) => { called.Add(new Invocation(typeof(AnotherDerived), 1)); });
                target.Subscribe<string?, Another>(null, (k, r) => { called.Add(new Invocation(typeof(Another), null)); });
                target.Subscribe<string?, Another>(GetKey<Another>(), (k, r) => { called.Add(new Invocation(typeof(Another), 0)); });
                target.Subscribe<string?, Another>(GetAlternateKey<Another>(), (k, r) => { called.Add(new Invocation(typeof(Another), 1)); });

                target.Dispatch(key, message);

                return called.ToArray();
            }
            string GetKey<T>() => typeof(T).Name;
            string GetAlternateKey<T>() => typeof(T).Name + "2";
        }

        [TestFixture]
        public class Dispose: DispatcherTest
        {
            [Test]
            public void WhenNoSubscriptions_DisposeDoesNotProduceException()
            {
                target.Dispose();
            }
            [Test]
            public void AfterDispose_IsDisposedIsTrue()
            {
                target.Dispose();

                Assert.That(target.IsDisposed, Is.True);
            }
            [Test]
            public void BeforeDispose_IsDisposedIsFalse()
            {
                Assert.That(target.IsDisposed, Is.False);
            }
            [Test]
            public void WhenSubscriptionIsPresent_DisposeDoesNotProduceException()
            {
                target.Subscribe<string, object>("key", (k, o) => { });
                target.Dispose();
            }
        }

        [DebuggerDisplay("{Type.Name,nq} with key {Key}")]
        public record struct Invocation(Type Type, int? Key);

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
