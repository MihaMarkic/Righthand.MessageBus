using Righthand.MessageBus;

namespace Righthand.MessageBus.Android.Sample
{
    public static class Globals
    {
        public static readonly IDispatcher Dispatcher = new Dispatcher();
        public static readonly ViewModel ViewModel = new ViewModel();
    }
}