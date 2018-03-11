using Android.App;
using Android.Content;
using Android.OS;

namespace Righthand.MessageBus.Android.Sample
{
    public class ItemSelectorDialogFragment : DialogFragment, IDialogInterfaceOnClickListener
    {
        string[] items;
        public ItemSelectorDialogFragment()
        { }
        public void OnClick(IDialogInterface dialog, int which)
        {
            var message = new ItemSelectedMessage(items[which]);
            Globals.Dispatcher.Dispatch(null, message);
            Dismiss();
        }
        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity);
            items = new[] { "One", "Two", "Three" };
            builder.SetTitle("Pick one")
                   .SetCancelable(false)
                   .SetItems(items, this);
            var dialog = builder.Create();
            Cancelable = false;
            return dialog;
        }
    }
}

