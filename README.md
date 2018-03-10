# Righthand.Immutable

[![NuGet](https://img.shields.io/nuget/v/Righthand.MessageBus.svg)](https://www.nuget.org/packages/Righthand.MessageBus)

An open source tool that provides support for a very simple message bus with subscribe/publish model.

Subscriber instance returned from Dispatcher.Subscribe<T> has to be disposed to unsubscribe from Dispatcher.

## Sample code

```csharp
class Program
{
    static void Main()
    {
        IDispatcher dispatcher = new Dispatcher();
        using (dispatcher.Subscribe<string>(null, AnyKeyMessageReceived)) // will receive any message with string type regardless of the key
        using (dispatcher.Subscribe<string>("some_key", KeyMessageReceived)) // will receive any message with string type where the key is the same
        {
            dispatcher.Dispatch(null, "A message without key");
            dispatcher.Dispatch("some_key", "A message with key");
        }
        dispatcher.Dispatch(null, "After subscribers disposed, a message without key"); // won't receive this message since subscribers have been disposed
        Console.WriteLine("Press ENTER to exit");
        Console.ReadLine();
    }

    static void AnyKeyMessageReceived(string key, string content)
    {
        Console.WriteLine($"[no key required] Got message '{content}' with key '{key}'");
    }
    static void KeyMessageReceived(string key, string content)
    {
        Console.WriteLine($"[key required] Got message '{content}' with key '{key}'");
    }
}
```