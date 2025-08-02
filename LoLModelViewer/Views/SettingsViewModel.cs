using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace LoLModelViewer.Views
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private double _ambientBrightness = 1.0;
        public double AmbientBrightness
        {
            get { return _ambientBrightness; }
            set
            {
                if (_ambientBrightness != value)
                {
                    _ambientBrightness = value;
                    OnPropertyChanged(nameof(AmbientBrightness));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}