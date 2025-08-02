using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using LeagueToolkit.Core.Mesh;
using LeagueToolkit.Core.Memory;
using LeagueToolkit.Core.Renderer;
using LeagueToolkit.Toolkit;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Input;
using LoLModelViewer.Models;

namespace LoLModelViewer.Views
{
    public partial class MainWindow : Window
    {
        private System.Windows.Point _lastMousePosition;
        private System.Windows.Point _lastPanPosition;
        private double _rotationX = 0;
        private double _rotationY = 0;
        private double _zoom = 1.0;
        private const double _initialCameraZ = 100.0;
        private List<ModelPart> _modelParts = new List<ModelPart>();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            modelViewport.MouseDown += modelViewport_MouseDown;
            modelViewport.MouseMove += modelViewport_MouseMove;
            modelViewport.MouseWheel += modelViewport_MouseWheel;
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

                _rotationY += deltaX * 0.5;
                _rotationX -= deltaY * 0.5;

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
            _zoom += e.Delta * 0.002;
            if (_zoom < 0.1) _zoom = 0.1;
            if (_zoom > 20.0) _zoom = 20.0;
            UpdateCameraTransform();
        }

        private void UpdateCameraTransform()
        {
            ((AxisAngleRotation3D)cameraRotationX.Rotation).Angle = _rotationX;
            ((AxisAngleRotation3D)cameraRotationY.Rotation).Angle = _rotationY;
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
                    SkinnedMesh skinnedMesh = SkinnedMesh.ReadFromSimpleSkin(openFileDialog.FileName);
                    string? modelDirectory = Path.GetDirectoryName(openFileDialog.FileName);

                    if (string.IsNullOrEmpty(modelDirectory))
                    {
                        LogError("Could not determine the model directory.");
                        MessageBox.Show("Could not determine the model directory.", "Error");
                        return;
                    }

                    string[] textureFiles = Directory.GetFiles(modelDirectory, "*.tex", SearchOption.TopDirectoryOnly);
                    Dictionary<string, BitmapSource> loadedTextures = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
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
                    LogError($"Failed to load model: {ex.Message}\n{ex.StackTrace}");
                    MessageBox.Show("Failed to load model. Details logged to app.log", "Error");
                }
            }
        }

        private void DisplayModel(SkinnedMesh skinnedMesh, Dictionary<string, BitmapSource> loadedTextures)
        {
            modelContainer.Children.Clear();
            _modelParts.Clear();
            partsListBox.ItemsSource = null;

            LogError("---" + "Displaying Model" + "---");
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
                string? matchingKey = null;

                if (textureName.Equals("Banner", StringComparison.OrdinalIgnoreCase))
                {
                    matchingKey = loadedTextures.Keys.FirstOrDefault(key => key.IndexOf("wings", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (matchingKey != null)
                    {
                        LogError($"  -> Special Case: Found texture '{matchingKey}' for Banner.");
                    }
                    else
                    {
                        LogError("  -> Special Case Error: Could not find wings texture for Banner.");
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

                        if (matchingKey == null) 
                        {
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
            LogError("---" + "Finished displaying model" + "---");
        }

        private void ModelPart_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ModelPart.IsVisible) && sender is ModelPart part)
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

        private BitmapSource? LoadTexture(string textureFilePath)
        {
            try
            {
                LogError($"Attempting to load texture from: {textureFilePath}");
                using (FileStream fs = File.OpenRead(textureFilePath))
                {
                    LogError($"File stream opened for: {textureFilePath}");
                    LeagueToolkit.Core.Renderer.Texture tex = LeagueToolkit.Core.Renderer.Texture.Load(fs);
                    LogError("LeagueToolkit.Core.Renderer.Texture loaded.");

                    CommunityToolkit.HighPerformance.Memory2D<BCnEncoder.Shared.ColorRgba32> mipmap = tex.Mips[0];
                    LogError($"Mipmap extracted. Width: {mipmap.Width}, Height: {mipmap.Height}");

                    Image<Rgba32> imageSharp = mipmap.ToImage();
                    LogError("Converted to SixLabors.ImageSharp.Image<Rgba32>.");

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
                        LogError("Converted to BitmapSource successfully.");
                        return bitmapImage;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to load texture: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show("Failed to load texture. Details logged to app.log", "Error");
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