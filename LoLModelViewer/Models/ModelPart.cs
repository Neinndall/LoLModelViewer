using System.ComponentModel;
using System.Windows.Media.Media3D;

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

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
