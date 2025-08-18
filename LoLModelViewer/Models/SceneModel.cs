using LoLModelViewer.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Media3D;

namespace LoLModelViewer.Models
{
    public class SceneModel
    {
        public string Name { get; set; }
        public ModelVisual3D RootVisual { get; set; }
        public TranslateTransform3D Transform { get; set; }
        public ObservableCollection<ModelPart> Parts { get; set; }

        public SceneModel()
        {
            Name = "New Model";
            RootVisual = new ModelVisual3D();
            Transform = new TranslateTransform3D();
            RootVisual.Transform = this.Transform;
            Parts = new ObservableCollection<ModelPart>();
        }
    }
}
