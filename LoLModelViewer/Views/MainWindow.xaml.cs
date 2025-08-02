using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Reflection;
using System.Diagnostics;
using LeagueToolkit.Core.Memory;
using LeagueToolkit.Core.Mesh;
using LeagueToolkit.Core.Renderer;
using LeagueToolkit.Toolkit;
using LoLModelViewer.Models;
using static LoLModelViewer.Models.SceneElements;
using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using LoLModelViewer.Services;
using LoLModelViewer.Info;

namespace LoLModelViewer.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
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
                    UpdateAmbientLight();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string AppVersion
        {
            get
            {
                return LoLModelViewer.Info.AssemblyInfo.Version;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateAmbientLight()
        {
            byte brightness = (byte)(_ambientBrightness * 255);
            ambientLight.Color = System.Windows.Media.Color.FromRgb(brightness, brightness, brightness);
        }
        private System.Windows.Point _lastMousePosition;
        private System.Windows.Point _lastPanPosition;
        private double _rotationX = 0;
        private double _rotationY = 0;
        private double _initialCameraZPosition;
        private List<ModelPart> _modelParts = new List<ModelPart>();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _initialCameraZPosition = mainCamera.Position.Z; // Store initial Z position
            modelViewport.MouseDown += modelViewport_MouseDown;
            modelViewport.MouseMove += modelViewport_MouseMove;
            modelViewport.MouseWheel += modelViewport_MouseWheel;
            UpdateCameraTransform();
            this.Title = $"LoL Model Viewer {AppVersion}";
        }

        private void modelViewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _lastMousePosition = e.GetPosition(modelViewport);
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                _lastPanPosition = e.GetPosition(modelViewport);
            }
        }

        private void modelViewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point currentMousePosition = e.GetPosition(modelViewport);
                double deltaX = currentMousePosition.X - _lastMousePosition.X;
                double deltaY = currentMousePosition.Y - _lastMousePosition.Y;

                _rotationY += deltaX * 0.2;
                _rotationX -= deltaY * 0.2;

                UpdateCameraTransform();
                _lastMousePosition = currentMousePosition;
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                System.Windows.Point currentPanPosition = e.GetPosition(modelViewport);
                double deltaX = currentPanPosition.X - _lastPanPosition.X;
                double deltaY = currentPanPosition.Y - _lastPanPosition.Y;

                cameraTranslation.OffsetX += deltaX * 0.1;
                cameraTranslation.OffsetY -= deltaY * 0.1;

                UpdateCameraTransform();
                _lastPanPosition = currentPanPosition;
            }
        }

        private void modelViewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Vector3D lookDirection = mainCamera.LookDirection;
            double moveAmount = e.Delta * 0.1; // Puedes ajustar este valor para cambiar la sensibilidad del zoom

            Point3D newPosition = mainCamera.Position + (lookDirection * moveAmount);
            mainCamera.Position = newPosition;
        }

        private void UpdateCameraTransform()
        {
            ((AxisAngleRotation3D)cameraRotationX.Rotation).Angle = _rotationX;
            ((AxisAngleRotation3D)cameraRotationY.Rotation).Angle = _rotationY;
        }

        private void LoadModel_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "League of Legends Models (*.skn)|*.skn|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    SkinnedMesh skinnedMesh = SkinnedMesh.ReadFromSimpleSkin(openFileDialog.FileName);
                    string? modelDirectory = Path.GetDirectoryName(openFileDialog.FileName);

                    if (string.IsNullOrEmpty(modelDirectory))
                    {
                        LogService.LogError("Could not determine the model directory.");
                        MessageBox.Show("Could not determine the model directory.", "Error");
                        return;
                    }

                    var loadedTextures = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
                    string[] textureFiles = Directory.GetFiles(modelDirectory, "*.tex", SearchOption.TopDirectoryOnly);
                    foreach (string texPath in textureFiles)
                    {
                        if (texPath.IndexOf("loadscreen", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            continue;
                        }

                        BitmapSource? loadedTex = LoadTexture(texPath);
                        if (loadedTex != null)
                        {
                            loadedTextures[Path.GetFileNameWithoutExtension(texPath)] = loadedTex;
                        }
                    }

                    DisplayModel(skinnedMesh, loadedTextures);
                    MessageBox.Show($"Successfully loaded model with {skinnedMesh.Ranges.Count} submeshes.", "Success");
                }
                catch (Exception ex)
                {
                    LogService.LogError($"Failed to load model: {ex.Message}\n{ex.StackTrace}");
                    MessageBox.Show("Failed to load model. Details logged to app.log", "Error");
                }
            }
        }

        private void DisplayModel(SkinnedMesh skinnedMesh, Dictionary<string, BitmapSource> loadedTextures)
        {
            modelContainer.Children.Clear();
            _modelParts.Clear();
            partsListBox.ItemsSource = null;

            LogService.LogError("--- Displaying Model ---");
            LogService.LogError($"Available texture keys: {string.Join(", ", loadedTextures.Keys)}");

            var availableTextureNames = new ObservableCollection<string>(loadedTextures.Keys);

            foreach (var rangeObj in skinnedMesh.Ranges)
            {
                string textureName = rangeObj.Material.TrimEnd('\0');
                LogService.LogError($"Submesh material name: '{textureName}'");

                MeshGeometry3D meshGeometry = new MeshGeometry3D();

                var positions = skinnedMesh.VerticesView.GetAccessor(ElementName.Position).AsVector3Array();
                meshGeometry.Positions = new Point3DCollection(positions.Select(p => new Point3D(p.X, p.Y, p.Z)));

                Int32Collection triangleIndices = new Int32Collection();
                for (int i = rangeObj.StartIndex; i < rangeObj.StartIndex + rangeObj.IndexCount; i++)
                {
                    triangleIndices.Add((int)skinnedMesh.Indices[i]);
                }
                meshGeometry.TriangleIndices = triangleIndices;

                var texCoords = skinnedMesh.VerticesView.GetAccessor(ElementName.Texcoord0).AsVector2Array();
                meshGeometry.TextureCoordinates = new PointCollection(texCoords.Select(uv => new System.Windows.Point(uv.X, uv.Y)));

                string? initialMatchingKey = null;

                // 1. Coincidencia exacta del nombre del material con el nombre de la textura (sin extensión)
                initialMatchingKey = loadedTextures.Keys.FirstOrDefault(key => key.Equals(textureName, StringComparison.OrdinalIgnoreCase));

                // 2. Coincidencia del nombre del material con el nombre de la textura + _tx_cm
                if (initialMatchingKey == null)
                {
                    initialMatchingKey = loadedTextures.Keys.FirstOrDefault(key => key.Equals($"{textureName}_tx_cm", StringComparison.OrdinalIgnoreCase));
                }

                // 3. Coincidencia del nombre del material como parte de la textura (ej: _material_tx_cm)
                if (initialMatchingKey == null)
                {
                    initialMatchingKey = loadedTextures.Keys.FirstOrDefault(key => key.IndexOf($"_{textureName}_tx_cm", StringComparison.OrdinalIgnoreCase) >= 0);
                }

                // 4. Coincidencia de la excepción "Banner"
                if (initialMatchingKey == null && textureName.Equals("Banner", StringComparison.OrdinalIgnoreCase))
                {
                    initialMatchingKey = loadedTextures.Keys.FirstOrDefault(key => key.IndexOf("wings", StringComparison.OrdinalIgnoreCase) >= 0);
                }

                // 5. Coincidencia de textura base específica
                if (initialMatchingKey == null)
                {
                    initialMatchingKey = loadedTextures.Keys.FirstOrDefault(key => 
                        key.IndexOf("_base_tx_cm", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        key.IndexOf("sword", StringComparison.OrdinalIgnoreCase) < 0 &&
                        key.IndexOf("wings", StringComparison.OrdinalIgnoreCase) < 0 &&
                        key.IndexOf("banner", StringComparison.OrdinalIgnoreCase) < 0
                    );
                }

                // 6. Coincidencia de cualquier textura base
                if (initialMatchingKey == null)
                {
                    initialMatchingKey = loadedTextures.Keys.FirstOrDefault(key => key.IndexOf("_base_tx_cm", StringComparison.OrdinalIgnoreCase) >= 0);
                }

                var modelPart = new ModelPart
                {
                    Name = string.IsNullOrEmpty(textureName) ? "Default" : textureName,
                    Visual = new ModelVisual3D(),
                    AllTextures = loadedTextures,
                    AvailableTextureNames = availableTextureNames,
                    SelectedTextureName = initialMatchingKey
                };

                if (modelPart.Visual != null)
                {
                    modelPart.Visual.Content = new GeometryModel3D(meshGeometry, new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.Magenta)));
                    modelPart.PropertyChanged += ModelPart_PropertyChanged;
                    _modelParts.Add(modelPart);
                    modelContainer.Children.Add(modelPart.Visual); 
                    modelPart.UpdateMaterial(); // Apply initial texture
                }
            }

            partsListBox.ItemsSource = _modelParts;
            textureAssignmentListBox.ItemsSource = _modelParts;
            modelContainer.Children.Add(SceneElements.CreateGroundPlane(LoadTexture, LogService.LogError)); // Add the ground plane
            modelContainer.Children.Add(CreateSidePlanes(LoadTexture, LogService.LogError)); // Add the side planes
            
            LogService.LogError("--- Finished displaying model ---");
        }

        private BitmapSource? LoadTexture(string textureFilePath)
        {
            try
            {
                LogService.LogError($"Attempting to load texture from: {textureFilePath}");
                string extension = Path.GetExtension(textureFilePath);

                if (extension.Equals(".tex", StringComparison.OrdinalIgnoreCase) || extension.Equals(".dds", StringComparison.OrdinalIgnoreCase))
                {
                    Stream? resourceStream = null;
                    if (textureFilePath.StartsWith("pack://application:"))
                    {
                        Uri resourceUri = new Uri(textureFilePath);
                        resourceStream = Application.GetResourceStream(resourceUri)?.Stream;
                        if (resourceStream == null)
                        {
                            LogService.LogError($"Failed to get resource stream for: {textureFilePath}");
                            return null;
                        }
                    }
                    else
                    {
                        resourceStream = File.OpenRead(textureFilePath);
                    }

                    using (resourceStream)
                    {
                        Texture? tex = Texture.Load(resourceStream);
                        if (tex == null)
                        {
                            LogService.LogError($"Failed to load texture object from {textureFilePath}. Texture.Load returned null.");
                            return null;
                        }
                        // Ensure Mips is not null before accessing Count
                        if (tex.Mips == null)
                        {
                            LogService.LogError($"No Mips collection found for texture: {textureFilePath}");
                            return null;
                        }
                        LogService.LogError($"Texture.Load successful for {textureFilePath}. Mipmap count: {tex.Mips.Length.ToString()}");
                        if (tex.Mips.Length == 0)
                        {
                            LogService.LogError($"No mipmaps found for texture: {textureFilePath}");
                            return null;
                        }
                        CommunityToolkit.HighPerformance.Memory2D<BCnEncoder.Shared.ColorRgba32> mipmap = tex.Mips[0];
                        LogService.LogError($"Attempting to convert mipmap to ImageSharp for {textureFilePath}");
                        Image<Rgba32> imageSharp = mipmap.ToImage();
                        LogService.LogError($"ImageSharp conversion successful for {textureFilePath}");

                        using (MemoryStream ms = new MemoryStream())
                        {
                            imageSharp.SaveAsPng(ms);
                            ms.Position = 0;
                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = ms;
                            bitmapImage.EndInit();
                            bitmapImage.Freeze();
                            return bitmapImage;
                        }
                    }
                }
                else // Assume it's a standard image format (png, jpg, etc.)
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(textureFilePath, UriKind.Absolute);
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"Failed to load texture: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show("Failed to load texture. Details logged to app.log", "Error");
                return null;
            }
        }

        private void ModelPart_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            ModelPart? part = sender as ModelPart;
            if (part == null) return;

            if (e.PropertyName == nameof(ModelPart.IsVisible))
            {
                if (part.Visual == null) return;

                if (part.IsVisible)
                {
                    if (!modelContainer.Children.Contains(part.Visual))
                    {
                        modelContainer.Children.Add(part.Visual);
                    }
                }
                else
                {
                    modelContainer.Children.Remove(part.Visual);
                }
            }
        }
    }
}
