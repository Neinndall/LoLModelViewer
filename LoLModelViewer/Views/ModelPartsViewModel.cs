using LoLModelViewer.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LoLModelViewer.Views
{
    public class ModelPartsViewModel : INotifyPropertyChanged
    {
        private List<ModelPart> _modelParts = new List<ModelPart>();
        public List<ModelPart> ModelParts
        {
            get { return _modelParts; }
            set
            {
                _modelParts = value;
                OnPropertyChanged(nameof(ModelParts));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}