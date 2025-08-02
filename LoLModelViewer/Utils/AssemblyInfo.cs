using System;
using System.Reflection;

namespace LoLModelViewer.Info
{
  public static class AssemblyInfo
  {
    public static string Version
    {
      get
      {
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return informationalVersionAttribute?.InformationalVersion ?? assembly.GetName().Version?.ToString() ?? "Unknown";
      }
    }
  }
}