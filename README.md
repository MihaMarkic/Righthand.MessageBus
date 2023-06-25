# Righthand.MessageBus

[![NuGet](https://img.shields.io/nuget/v/Righthand.MessageBus.svg)](https://www.nuget.org/packages/Righthand.MessageBus)

## About

An open source library that provides support for a very simple message bus with subscribe/publish model.

Dispatcher and public classes are thread safe, others are not necessarily.
Subscriber instance returned from Dispatcher.Subscribe<T>(...) has to be disposed to unsubscribe from Dispatcher.

Subscribers gets messages based on published key and message type:
- when subscriber doesn't use a key, all keys are valid
- when subscriber specifies a key, only specified keys are valid

and

- subscriber receives only message types of given requested type but not of its subclasses (the later is a breaking change in version 2.x)

Subscription can be handled through synchronous (`Action<TMessage> or Action<TKey, TMessage>`{:.csharp}) or asynchronous methods (`Func<TMessage, CancellationToken, Task> or Func<TKey, TMessage, CancellationToken, Task>`{:.csharp}).

Let's say we have this message class hierarchy:

```csharp
class Root {}
class Derived: Root {}
```

If subscriber subscribes to Root type, it will receive messages when published message is `Root` but not `Derived` type.

If subscriber subscribes to `Derived` type, it will receive only `Derived` type messages.

Handling message type hierarchies (i.e. when subscribed to `Root` subscription would handle both `Derived` and `Root` messages) introduces performance penalty and isn't worth an effort at this time.

Dispatch handles subscriptions sequentially and doesn't provide any error handling. Thus if one of handlers throws an unhandled exception, subscriptions in queue won't get the message. Make sure handlers don't throw if this in an undesired effect.

There are two types of dispatch:
- synchronous `Dispatch<TKey, TMessage>` and `Dispatch<TMessage>`
- asynchronous `DispatchAsync<TKey, TMessage>` and `DispatchAsync<TMessage>`

The important difference between the two is that the later can wait for all subscriptions to finish (await the `Task` result), while the former starts asynchronous subscriptions but doesn't wait for them to finish.

Supports .NET 6.0 and later.

## How to compile

Simple `dotnet build -c Release` in `src\Righthand.MessageBus` directory.

## Where to get

[nuget](https://www.nuget.org/packages/Righthand.MessageBus/)

## History

2.0.0

- Rewritten
- Improved performances
- Omits support for catching super types (breaking change)
- Drops support for .NET standard and framework
- Supports async subscriptions (handler method has to be `Func<TKey, TMessage, CancellationToken, Task>` or `Func<TMessage, CancellationToken, Task>` when key is ignored, async dispatch through `DispatchAsync` method and awaiting for async message through `GetAsyncMessageAsync` method.
- Sync counterparts are message handler of either `Action<TKey, TMessage>` or `Action<TMessage>` type, `Dispatch` and `GetSyncMessageAsync`
- Removes Android sample for the time being.

1.2.0

- Adds net6 support

1.1.0

- Makes key a generic type, was string previously

1.0.0

* First version

## How to use
To dispatch a message.
```csharp
void Dispatch<TKey, TMessage>(TKey key, TMessage message);
void Dispatch<TMessage>(TMessage message);
// or with an option to wait for all subscriptions to finish
Task DispatchAsync<TKey, TMessage>(TKey key, TMessage message, CancellationToken ct);
Task DispatchAsync<TMessage>(TMessage message, CancellationToken ct);
```
To subscribe to a message.
```csharp
Subscription Subscribe<TKey, TMessage>(TKey key, Action<string, TMessage> handler, string name = null);
Subscription Subscribe<TMessage>(Action<TKey, TMessage> handler, string name = null);
Subscription Subscribe<TKey, TMessage>(TKey key, Action<string, TMessage> handler, string name = null);
Subscription Subscribe<TMessage>(Action<TKey, TMessage> handler, string name = null);
```
To asynchronously wait for a message without an explicit subscriptions (one is created in code behind implicitly and disposed after method finishes).
```csharp
Task<TMessage> GetMessageAsync<TKey, TMessage>(TKey key, CancellationToken ct);
Task<TMessage> GetMessageAsync<TMessage>(CancellationToken ct);
```

## Sample code

```csharp
class Program
{
    static void Main()
    {
        using (IDispatcher dispatcher = new Dispatcher())
        {
            using (dispatcher.Subscribe<string>(AnyKeyMessageReceived)) // will receive any message with string type regardless of the key
            using (dispatcher.Subscribe<string, string>("some_key", KeyMessageReceived)) // will receive any message with string type where the key is the same
            {
                dispatcher.Dispatch("A message without key");
                dispatcher.Dispatch("some_key", "A message with key");
            }
            dispatcher.Dispatch("After subscribers disposed, a message without key"); // won't receive this message since subscribers have been disposed
        }
        Console.WriteLine("Press ENTER to exit");
        Console.ReadLine();
    }

    static void AnyKeyMessageReceived(string content)
    {
        Console.WriteLine($"[no key required] Got message '{content}'");
    }
    static void KeyMessageReceived(string key, string content)
    {
        Console.WriteLine($"[key required] Got message '{content}' with key '{key}'");
    }
}
```

## Samples

### Righthand.MessageBus.Sample

A minimal sample demonstrating dispatching and subscriptions. The sample code above is taken from this sample.