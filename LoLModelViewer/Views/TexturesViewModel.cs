using LoLModelViewer.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace LoLModelViewer.Views
{
    public class TexturesViewModel : INotifyPropertyChanged
    {
        private Dictionary<string, BitmapSource> _loadedTextures = new Dictionary<string, BitmapSource>();
        public Dictionary<string, BitmapSource> LoadedTextures
        {
            get { return _loadedTextures; }
            set
            {
                _loadedTextures = value;
                OnPropertyChanged(nameof(LoadedTextures));
                OnPropertyChanged(nameof(AvailableTextureNames));
            }
        }

        public ObservableCollection<string> AvailableTextureNames
        {
            get { return new ObservableCollection<string>(_loadedTextures.Keys); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}