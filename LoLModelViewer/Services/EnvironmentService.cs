using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using LoLModelViewer.Models;

namespace LoLModelViewer.Services
{
    public static class EnvironmentService
    {
        public static void Load(ICollection<ModelVisual3D> containerChildren, ITextureLoadingService textureService, Action<string> logErrorFunc)
        {
            Func<string, System.Windows.Media.Imaging.BitmapSource?> loadTextureFunc = (path) => textureService.LoadTexture(path, logErrorFunc);

            containerChildren.Add(SceneElements.CreateGroundPlane(loadTextureFunc, logErrorFunc));
            containerChildren.Add(SceneElements.CreateSidePlanes(loadTextureFunc, logErrorFunc));
        }
    }
}