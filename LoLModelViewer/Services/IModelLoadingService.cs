using LoLModelViewer.Models;
using System;

namespace LoLModelViewer.Services
{
    public interface IModelLoadingService
    {
        SceneModel? LoadModel(string filePath, Action<string> logError);
    }
}
