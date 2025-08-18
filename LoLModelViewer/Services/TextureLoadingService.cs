using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using LeagueToolkit.Core.Renderer;
using LeagueToolkit.Toolkit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace LoLModelViewer.Services
{
    public class TextureLoadingService : ITextureLoadingService
    {
        public BitmapSource? LoadTexture(string textureFilePath, Action<string> logError)
        {
            try
            {
                string extension = Path.GetExtension(textureFilePath);
                using (Stream? resourceStream = textureFilePath.StartsWith("pack://application:")
                    ? Application.GetResourceStream(new Uri(textureFilePath))?.Stream
                    : File.OpenRead(textureFilePath))
                {
                    if (resourceStream == null) { return null; }

                    if (extension.Equals(".tex", StringComparison.OrdinalIgnoreCase) || extension.Equals(".dds", StringComparison.OrdinalIgnoreCase))
                    {
                        Texture? tex = Texture.Load(resourceStream);
                        if (tex?.Mips?.Length > 0)
                        {
                            Image<Rgba32> imageSharp = tex.Mips[0].ToImage();
                            using (MemoryStream ms = new MemoryStream())
                            {
                                imageSharp.SaveAsBmp(ms);
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
                        return null;
                    }
                    else
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
            }
            catch (Exception ex)
            {
                logError($"Failed to load texture: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
    }
}