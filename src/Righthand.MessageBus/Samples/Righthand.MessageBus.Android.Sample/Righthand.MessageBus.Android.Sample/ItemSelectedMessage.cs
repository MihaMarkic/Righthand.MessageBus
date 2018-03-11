namespace Righthand.MessageBus.Android.Sample
{
    public class ItemSelectedMessage
    {
        public string Item { get; }
        public ItemSelectedMessage(string item)
        {
            Item = item;
        }
    }
}

