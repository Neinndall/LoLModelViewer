using LeagueToolkit.Core.Mesh;
using LoLModelViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace LoLModelViewer.Services
{
    public class ModelLoadingService : IModelLoadingService
    {
        private readonly ITextureLoadingService _textureLoadingService;
        private readonly TextureMatcherService _textureMatcherService = new TextureMatcherService();

        public ModelLoadingService(ITextureLoadingService textureLoadingService)
        {
            _textureLoadingService = textureLoadingService;
        }

        public SceneModel? LoadModel(string filePath, Action<string> logError)
        {
            try
            {
                SkinnedMesh skinnedMesh = SkinnedMesh.ReadFromSimpleSkin(filePath);
                string? modelDirectory = Path.GetDirectoryName(filePath);

                if (string.IsNullOrEmpty(modelDirectory))
                {
                    logError("Could not determine the model directory.");
                    return null;
                }

                var loadedTextures = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
                string[] textureFiles = Directory.GetFiles(modelDirectory, "*.tex", SearchOption.TopDirectoryOnly);
                foreach (string texPath in textureFiles)
                {
                    BitmapSource? loadedTex = _textureLoadingService.LoadTexture(texPath, logError);
                    if (loadedTex != null)
                    {
                        loadedTextures[Path.GetFileNameWithoutExtension(texPath)] = loadedTex;
                    }
                }

                return CreateSceneModel(skinnedMesh, loadedTextures, Path.GetFileNameWithoutExtension(filePath), logError);
            }
            catch (Exception ex)
            {
                logError($"Failed to load model: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private SceneModel CreateSceneModel(SkinnedMesh skinnedMesh, Dictionary<string, BitmapSource> loadedTextures, string modelName, Action<string> logError)
        {
            logError("--- Displaying Model ---");
            logError($"Available texture keys: {string.Join(", ", loadedTextures.Keys)}");

            var sceneModel = new SceneModel { Name = modelName };
            var availableTextureNames = new ObservableCollection<string>(loadedTextures.Keys);

            string? defaultTextureKey = loadedTextures.Keys
                .Where(k => k.EndsWith("_tx_cm", StringComparison.OrdinalIgnoreCase))
                .OrderBy(k => k.Length)
                .FirstOrDefault();

            foreach (var rangeObj in skinnedMesh.Ranges)
            {
                string materialName = rangeObj.Material.TrimEnd('\0');
                MeshGeometry3D meshGeometry = new MeshGeometry3D();

                var positions = skinnedMesh.VerticesView.GetAccessor(LeagueToolkit.Core.Memory.ElementName.Position).AsVector3Array();
                meshGeometry.Positions = new Point3DCollection(positions.Select(p => new Point3D(p.X, p.Y, p.Z)));

                Int32Collection triangleIndices = new Int32Collection();
                for (int i = rangeObj.StartIndex; i < rangeObj.StartIndex + rangeObj.IndexCount; i++)
                {
                    triangleIndices.Add((int)skinnedMesh.Indices[i]);
                }
                meshGeometry.TriangleIndices = triangleIndices;

                var texCoords = skinnedMesh.VerticesView.GetAccessor(LeagueToolkit.Core.Memory.ElementName.Texcoord0).AsVector2Array();
                meshGeometry.TextureCoordinates = new PointCollection(texCoords.Select(uv => new System.Windows.Point(uv.X, uv.Y)));

                string? initialMatchingKey = _textureMatcherService.FindBestTextureMatch(materialName, loadedTextures.Keys, defaultTextureKey);

                var modelPart = new ModelPart
                {
                    Name = string.IsNullOrEmpty(materialName) ? "Default" : materialName,
                    Visual = new ModelVisual3D(),
                    AllTextures = loadedTextures,
                    AvailableTextureNames = availableTextureNames,
                    SelectedTextureName = initialMatchingKey
                };

                modelPart.Visual.Content = new GeometryModel3D(meshGeometry, new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.Magenta)));
                modelPart.UpdateMaterial();

                sceneModel.Parts.Add(modelPart);
                sceneModel.RootVisual.Children.Add(modelPart.Visual);
            }
            logError("--- Finished displaying model ---");
            return sceneModel;
        }
    }
}