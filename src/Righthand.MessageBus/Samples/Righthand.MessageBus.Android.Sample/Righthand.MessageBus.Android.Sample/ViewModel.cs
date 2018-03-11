using System.ComponentModel;

namespace Righthand.MessageBus.Android.Sample
{
    /// <summary>
    /// Simplistic ViewModel implementation
    /// </summary>
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        string selectedItem;
        /// <summary>
        /// Currently selected item.
        /// </summary>
        public string SelectedItem
        {
            get => selectedItem;
            set
            {
                if (!string.Equals(selectedItem, value, System.StringComparison.Ordinal))
                {
                    selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
                }
            }

        }
    }
}