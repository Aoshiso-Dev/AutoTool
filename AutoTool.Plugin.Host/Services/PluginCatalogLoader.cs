using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Services;

public sealed class PluginCatalogLoader(
    PluginHostOptions options,
    IPluginManifestLoader manifestLoader) : IPluginCatalogLoader
{
    private readonly PluginHostOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly IPluginManifestLoader _manifestLoader = manifestLoader ?? throw new ArgumentNullException(nameof(manifestLoader));

    public IReadOnlyList<PluginManifestLoadResult> LoadCatalog()
    {
        var rootDirectoryPath = _options.RootDirectoryPath;
        if (string.IsNullOrWhiteSpace(rootDirectoryPath) || !Directory.Exists(rootDirectoryPath))
        {
            return [];
        }

        List<PluginManifestLoadResult> results = [];

        foreach (var pluginDirectory in Directory.EnumerateDirectories(rootDirectoryPath))
        {
            var manifestPath = Path.Combine(pluginDirectory, "plugin.json");
            results.Add(_manifestLoader.Load(manifestPath));
        }

        return results
            .OrderBy(static x => x.Manifest?.DisplayName ?? x.PluginDirectoryPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

