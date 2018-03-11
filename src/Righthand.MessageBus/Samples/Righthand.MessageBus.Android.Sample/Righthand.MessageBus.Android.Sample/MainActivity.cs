using Android.App;
using Android.OS;
using Android.Widget;
using System.Threading;
using System.Threading.Tasks;

namespace Righthand.MessageBus.Android.Sample
{
    [Activity(Label = "Righthand.MessageBus.Sample", MainLauncher = true)]
    public class MainActivity : Activity
    {
        Button button;
        ViewModel viewModel;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            button = FindViewById<Button>(Resource.Id.button);
            button.Click += (s, e) =>
            {
                var ignore = SelectItemAsync();
            };
            viewModel = Globals.ViewModel;
        }
        protected override void OnResume()
        {
            base.OnResume();
            UpdateButtonText();
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
        protected override void OnPause()
        {
            base.OnPause();
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
        void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(viewModel.SelectedItem):
                    UpdateButtonText();
                    break;
            }
        }
        void UpdateButtonText()
        {
            button.Text = $"Item selected is {viewModel.SelectedItem}";
        }
        async Task SelectItemAsync()
        {
            var dialog = new ItemSelectorDialogFragment();
            dialog.Show(FragmentManager, "xy");
            // result is published through dispatcher
            var message = await Globals.Dispatcher.GetMessageAsync<ItemSelectedMessage>(CancellationToken.None);
            // where global ViewModel instance receives it
            viewModel.SelectedItem = message.Item;
        }
    }
}

