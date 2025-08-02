using System.ComponentModel;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;

namespace LoLModelViewer.Models
{
    public class ModelPart : INotifyPropertyChanged
    {
        public string? Name { get; set; }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
                }
            }
        }

        public ModelVisual3D? Visual { get; set; }

        public Dictionary<string, BitmapSource> AllTextures { get; set; } = new Dictionary<string, BitmapSource>();

        public ObservableCollection<string> AvailableTextureNames { get; set; } = new ObservableCollection<string>();

        private string? _selectedTextureName;
        public string? SelectedTextureName
        {
            get { return _selectedTextureName; }
            set
            {
                if (_selectedTextureName != value)
                {
                    _selectedTextureName = value;
                    UpdateMaterial();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTextureName)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void UpdateMaterial()
        {
            if (Visual?.Content is GeometryModel3D geometryModel && 
                !string.IsNullOrEmpty(SelectedTextureName) && 
                AllTextures.TryGetValue(SelectedTextureName, out BitmapSource? texture))
            {
                geometryModel.Material = new DiffuseMaterial(new ImageBrush(texture));
            }
        }
    }
}
