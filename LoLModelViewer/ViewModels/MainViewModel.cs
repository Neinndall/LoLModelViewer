using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using LoLModelViewer.Models;
using LoLModelViewer.Services;
using System.Windows.Media.Media3D;

namespace LoLModelViewer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IDialogService _dialogService;
        private readonly IModelLoadingService _modelLoadingService;

        private double _ambientBrightness = 0.5;
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

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<SceneModel> SceneModels { get; set; } = new ObservableCollection<SceneModel>();
        private SceneModel? _selectedModel;
        public SceneModel? SelectedModel
        {
            get { return _selectedModel; }
            set
            {
                if (_selectedModel != value)
                {
                    _selectedModel = value;
                    OnPropertyChanged(nameof(SelectedModel));
                    UpdateModelParts();
                }
            }
        }

        public ObservableCollection<ModelPart> ModelParts { get; set; } = new ObservableCollection<ModelPart>();
        public ObservableCollection<ModelVisual3D> ModelVisuals { get; set; } = new ObservableCollection<ModelVisual3D>();

        public ICommand LoadModelCommand { get; }
        public ICommand RemoveModelCommand { get; }

        public MainViewModel(IDialogService dialogService, IModelLoadingService modelLoadingService, ITextureLoadingService textureLoadingService)
        {
            _dialogService = dialogService;
            _modelLoadingService = modelLoadingService;

            LoadModelCommand = new LoLModelViewer.ViewModels.RelayCommand(LoadModel);
            RemoveModelCommand = new RelayCommand<SceneModel>(RemoveModel);

            EnvironmentService.Load(ModelVisuals, textureLoadingService, LogService.LogError);
        }

        private void LoadModel()
        {
            string? filePath = _dialogService.ShowOpenFileDialog("League of Legends Models (*.skn)|*.skn|All files (*.*)|*.*" );

            if (string.IsNullOrEmpty(filePath)) return;

            SceneModel? sceneModel = _modelLoadingService.LoadModel(filePath, LogService.LogError);

            if (sceneModel != null)
            {
                foreach (var part in sceneModel.Parts)
                {
                    part.PropertyChanged += ModelPart_PropertyChanged;
                }

                SceneModels.Add(sceneModel);
                SelectedModel = sceneModel;
                ModelVisuals.Add(sceneModel.RootVisual);
                _dialogService.ShowMessageBox($"Successfully loaded model with {sceneModel.Parts.Count} submeshes.", "Success");
            }
            else
            {
                _dialogService.ShowMessageBox("Failed to load model. Details logged to app.log", "Error");
            }
        }

        private void RemoveModel(SceneModel model)
        {
            if (model == null) return;

            foreach (var part in model.Parts)
            {
                part.PropertyChanged -= ModelPart_PropertyChanged;
            }

            SceneModels.Remove(model);
            ModelVisuals.Remove(model.RootVisual);

            if (SelectedModel == model)
            {
                SelectedModel = null;
            }
        }

        private void UpdateModelParts()
        {
            ModelParts.Clear();
            if (SelectedModel != null)
            {
                foreach (var part in SelectedModel.Parts)
                {
                    ModelParts.Add(part);
                }
            }
        }

        private void ModelPart_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is ModelPart part && part.Visual != null && e.PropertyName == nameof(ModelPart.IsVisible))
            {
                part.Visual.Content.SetValue(System.Windows.UIElement.VisibilityProperty, part.IsVisible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed);
            }
        }
    }
}
