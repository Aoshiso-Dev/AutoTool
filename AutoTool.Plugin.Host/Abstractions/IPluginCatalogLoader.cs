using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Abstractions;

public interface IPluginCatalogLoader
{
    IReadOnlyList<PluginManifestLoadResult> LoadCatalog();
}

