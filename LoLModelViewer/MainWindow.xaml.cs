using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using LeagueToolkit.Core.Mesh; // Correct namespace for SkinnedMesh
using LeagueToolkit.Core.Memory; // For ElementName
using LeagueToolkit.Core.Renderer; // For Texture
using LeagueToolkit.Toolkit; // For TextureExtensions
using System.Windows.Media.Media3D; // For 3D models
using System.Windows.Media; // For colors and brushes
using SixLabors.ImageSharp; // For Image<Rgba32>
using SixLabors.ImageSharp.PixelFormats; // For Rgba32
using System.Windows.Media.Imaging; // For BitmapSource
using System.Runtime.InteropServices; // For MemoryMarshal
using System.Windows.Input; // For Mouse events

namespace LoLModelViewer
{
    public class ModelPart : INotifyPropertyChanged
    {
        public string Name { get; set; }
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
        public ModelVisual3D Visual { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public partial class MainWindow : Window
    {
        private System.Windows.Point _lastMousePosition;
        private System.Windows.Point _lastPanPosition;
        private double _rotationX = 0;
        private double _rotationY = 0;
        private double _zoom = 1.0;
        private const double _initialCameraZ = 100.0; // Initial Z position of the camera
        private List<ModelPart> _modelParts = new List<ModelPart>();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Attach mouse event handlers to the Viewport3D container
            modelViewport.MouseDown += modelViewport_MouseDown;
            modelViewport.MouseMove += modelViewport_MouseMove;
            modelViewport.MouseWheel += modelViewport_MouseWheel;

            // Initial camera setup (optional, can be done in XAML)
            UpdateCameraTransform();
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

                _rotationY += deltaX * 0.5; // Adjust sensitivity as needed
                _rotationX -= deltaY * 0.5; // Adjust sensitivity as needed

                UpdateCameraTransform();

                _lastMousePosition = currentMousePosition;
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                System.Windows.Point currentPanPosition = e.GetPosition(modelViewport);
                double deltaX = currentPanPosition.X - _lastPanPosition.X;
                double deltaY = currentPanPosition.Y - _lastPanPosition.Y;

                cameraTranslation.OffsetX += deltaX * 0.1; // Adjust sensitivity as needed
                cameraTranslation.OffsetY -= deltaY * 0.1; // Adjust sensitivity as needed

                UpdateCameraTransform(); // Call UpdateCameraTransform after panning

                _lastPanPosition = currentPanPosition;
            }
        }

        private void modelViewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _zoom += e.Delta * 0.002; // Adjusted sensitivity
            if (_zoom < 0.1) _zoom = 0.1; // Prevent zooming too far in
            if (_zoom > 20.0) _zoom = 20.0; // Increased max zoom out

            UpdateCameraTransform();
        }

        private void UpdateCameraTransform()
        {
            // Update X-axis rotation
            ((AxisAngleRotation3D)cameraRotationX.Rotation).Angle = _rotationX;

            // Update Y-axis rotation
            ((AxisAngleRotation3D)cameraRotationY.Rotation).Angle = _rotationY;

            // cameraTranslation.OffsetX and cameraTranslation.OffsetY are updated in MouseMove

            // Apply zoom (by adjusting camera position)
            mainCamera.Position = new Point3D(mainCamera.Position.X, mainCamera.Position.Y, _initialCameraZ * (1 / _zoom));
        }

        private void LoadModel_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "League of Legends Models (*.skn)|*.skn|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Cargar el modelo 3D (.skn)
                    SkinnedMesh skinnedMesh = SkinnedMesh.ReadFromSimpleSkin(openFileDialog.FileName);
                    string modelDirectory = Path.GetDirectoryName(openFileDialog.FileName);

                    // Cargar todas las texturas (.tex) del directorio en un diccionario, ignorando las de la pantalla de carga
                    string[] textureFiles = Directory.GetFiles(modelDirectory, "*.tex", SearchOption.TopDirectoryOnly);
                    Dictionary<string, BitmapSource> loadedTextures = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
                    foreach (string texPath in textureFiles)
                    {
                        // Ignorar texturas que contengan "loadscreen"
                        if (texPath.IndexOf("loadscreen", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            continue;
                        }

                        BitmapSource loadedTex = LoadTexture(texPath);
                        if (loadedTex != null)
                        {
                            loadedTextures[Path.GetFileNameWithoutExtension(texPath)] = loadedTex;
                        }
                    }

                    // Visualizar el modelo con las texturas cargadas
                    DisplayModel(skinnedMesh, loadedTextures);
                    MessageBox.Show($"Successfully loaded model with {skinnedMesh.Ranges.Count} submeshes.", "Success");
                }
                catch (Exception ex)
                {
                    LogError($"Failed to load model: {ex.Message}\n{ex.StackTrace}");
                    MessageBox.Show($"Failed to load model. Details logged to app.log", "Error");
                }
            }
        }

        private void DisplayModel(SkinnedMesh skinnedMesh, Dictionary<string, BitmapSource> loadedTextures)
        {
            modelContainer.Children.Clear();
            _modelParts.Clear();
            partsListBox.ItemsSource = null;

            LogError("--- Displaying Model ---");
            LogError($"Available texture keys: {string.Join(", ", loadedTextures.Keys)}");

            foreach (var rangeObj in skinnedMesh.Ranges)
            {
                string textureName = rangeObj.Material.TrimEnd('\0');
                LogError($"Submesh material name: '{textureName}'");

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

                Material material;
                string matchingKey = null;

                if (textureName.Equals("Banner", StringComparison.OrdinalIgnoreCase))
                {
                    matchingKey = loadedTextures.Keys.FirstOrDefault(key => key.IndexOf("wings", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (matchingKey != null)
                    {
                        LogError($"  -> Special Case: Found texture '{matchingKey}' for Banner.");
                    }
                    else
                    {
                        LogError($"  -> Special Case Error: Could not find wings texture for Banner.");
                    }
                }

                if (matchingKey == null)
                {
                    matchingKey = loadedTextures.Keys.FirstOrDefault(key => key.IndexOf(textureName, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (matchingKey == null)
                    {
                        LogError($"  -> Info: No specific texture for '{textureName}'. Attempting fallback to a more specific base texture.");
                        matchingKey = loadedTextures.Keys.FirstOrDefault(key => 
                            key.IndexOf("_base_tx_cm", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            key.IndexOf("sword", StringComparison.OrdinalIgnoreCase) < 0 &&
                            key.IndexOf("wings", StringComparison.OrdinalIgnoreCase) < 0 &&
                            key.IndexOf("banner", StringComparison.OrdinalIgnoreCase) < 0
                        );

                        if (matchingKey == null) {
                            LogError("  -> Info: Could not find a specific base texture. Falling back to any base texture.");
                            matchingKey = loadedTextures.Keys.FirstOrDefault(key => key.IndexOf("_base_tx_cm", StringComparison.OrdinalIgnoreCase) >= 0);
                        }
                    }
                }

                if (matchingKey != null)
                {
                    material = new DiffuseMaterial(new ImageBrush(loadedTextures[matchingKey]));
                    LogError($"  -> Success: Applying texture '{matchingKey}' to material '{textureName}'.");
                }
                else
                {
                    material = new DiffuseMaterial(new SolidColorBrush(Colors.Magenta));
                    LogError($"  -> Error: Could not find any texture for '{textureName}' (specific or base). Applying debug material.");
                }

                GeometryModel3D geometryModel = new GeometryModel3D(meshGeometry, material);
                ModelVisual3D partVisual = new ModelVisual3D { Content = geometryModel };

                var modelPart = new ModelPart
                {
                    Name = string.IsNullOrEmpty(textureName) ? "Default" : textureName,
                    Visual = partVisual
                };
                modelPart.PropertyChanged += ModelPart_PropertyChanged;
                _modelParts.Add(modelPart);

                modelContainer.Children.Add(partVisual);
            }

            partsListBox.ItemsSource = _modelParts;
            LogError("--- Finished displaying model ---");
        }

        private void ModelPart_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ModelPart.IsVisible))
            {
                var part = (ModelPart)sender;
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

        private BitmapSource LoadTexture(string textureFilePath)
        {
            try
            {
                LogError($"Attempting to load texture from: {textureFilePath}");
                using (FileStream fs = File.OpenRead(textureFilePath))
                {
                    LogError($"File stream opened for: {textureFilePath}");
                    LeagueToolkit.Core.Renderer.Texture tex = LeagueToolkit.Core.Renderer.Texture.Load(fs);
                    LogError($"LeagueToolkit.Core.Renderer.Texture loaded.");

                    CommunityToolkit.HighPerformance.Memory2D<BCnEncoder.Shared.ColorRgba32> mipmap = tex.Mips[0];
                    LogError($"Mipmap extracted. Width: {mipmap.Width}, Height: {mipmap.Height}");

                    Image<Rgba32> imageSharp = mipmap.ToImage();
                    LogError($"Converted to SixLabors.ImageSharp.Image<Rgba32>.");

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
                        LogError($"Converted to BitmapSource successfully.");
                        return bitmapImage;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to load texture: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Failed to load texture. Details logged to app.log", "Error");
                return null;
            }
        }

        private void LogError(string message)
        {
            try
            {
                string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                File.AppendAllText(logFilePath, $"[{DateTime.Now}] {message}\n");
            }
            catch (Exception logEx)
            {
                MessageBox.Show($"Failed to write to log file: {logEx.Message}", "Logging Error");
            }
        }
    }
}