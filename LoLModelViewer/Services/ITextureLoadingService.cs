using System;
using System.Windows.Media.Imaging;

namespace LoLModelViewer.Services
{
    public interface ITextureLoadingService
    {
        BitmapSource? LoadTexture(string textureFilePath, Action<string> logError);
    }
}
