# Righthand.MessageBus

[![NuGet](https://img.shields.io/nuget/v/Righthand.MessageBus.svg)](https://www.nuget.org/packages/Righthand.MessageBus)

An open source library that provides support for a very simple message bus with subscribe/publish model.

Dispatcher class is thread safe, others are not.
Subscriber instance returned from Dispatcher.Subscribe<T>(...) has to be disposed to unsubscribe from Dispatcher.

Subscribers gets messages based on published key and message type:
- when subscriber doesn't use key, all keys are valid
- when subscriber specifies a key, only same keys are valid

and

- subscriber recieves only message types of given requested type or its subclasses

Let's say we have this class hierarchy:

```csharp
class Root {}
class Derived: Root {}
```

If subscriber subscribes to Root type, it will receive messages when published message are either Root or Derived type.

If subscriber subscribes to Derived type, it will receive only Derived type messages.

## History

1.1.0

- Makes key a generic type, was string previously

1.0.0

* First version

## How to use
To dispatch a message.
```csharp
void Dispatch<T>(string key, T message);
```
To subscribe to a message.
```csharp
Subscription Subscribe<T>(string key, Action<string, T> handler, string name = null);
```
To asynchronously wait for a message.
```csharp
Task<T> GetMessageAsync<T>(string key, CancellationToken ct);
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

    static void AnyKeyMessageReceived(object key, string content)
    {
        Console.WriteLine($"[no key required] Got message '{content}' with key '{key}'");
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

### Righthand.MessageBus.Android.Sample

A sample that demonstrates how to handle dialog fragments. It gets interesting when dialog is show,
activity goes into background and is destroyed to be later recreated when getting back into foreground.
Most similar code will misbehave it such situations.

The sample would work correctly because it is publishing message to a global dispatcher when item is selected.
Excerpt that shows dialog and sets ViewModel.SelectedItem accordingly.

```csharp
async Task SelectItemAsync()
{
    var dialog = new ItemSelectorDialogFragment();
    dialog.Show(FragmentManager, "xy");
    // result is published through dispatcher
    var message = await Globals.Dispatcher.GetMessageAsync<ItemSelectedMessage>(CancellationToken.None);
    // where global ViewModel instance receives it
    viewModel.SelectedItem = message.Item;
}
```