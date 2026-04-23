using System.IO;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;
using AutoTool.Plugin.Host.Services;

namespace AutoTool.Plugin.Host.Tests;

/// <summary>
/// plugin.json の読み込みと検証を確認するテストです。
/// </summary>
public sealed class PluginManifestLoaderTests : IDisposable
{
    private readonly string _rootDirectoryPath = Path.Combine(Path.GetTempPath(), "AutoTool.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_ValidManifest_ReturnsValidResult()
    {
        var pluginDirectoryPath = CreatePluginDirectory("Example.Plugin");
        File.WriteAllText(
            Path.Combine(pluginDirectoryPath, "Example.Plugin.dll"),
            string.Empty);
        File.WriteAllText(
            Path.Combine(pluginDirectoryPath, "plugin.json"),
            """
            {
              "pluginId": "Example.Plugin",
              "displayName": "Example Plugin",
              "version": "1.0.0",
              "entryAssembly": "Example.Plugin.dll",
              "entryType": "Example.Plugin.PluginEntry",
              "permissions": [ "external.read" ],
              "commands": [
                {
                  "commandType": "ExampleCommand",
                  "displayName": "Example Command",
                  "category": "System",
                  "order": 10
                }
              ]
            }
            """);

        IPluginManifestLoader loader = new PluginManifestLoader(new PluginManifestValidator());
        var result = loader.Load(Path.Combine(pluginDirectoryPath, "plugin.json"));

        Assert.True(result.IsValid);
        Assert.NotNull(result.Manifest);
        Assert.Equal("Example.Plugin", result.Manifest.PluginId);
        Assert.Single(result.Manifest.Commands);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Load_MissingEntryAssembly_ReturnsValidationError()
    {
        var pluginDirectoryPath = CreatePluginDirectory("Broken.Plugin");
        File.WriteAllText(
            Path.Combine(pluginDirectoryPath, "plugin.json"),
            """
            {
              "pluginId": "Broken.Plugin",
              "displayName": "Broken Plugin",
              "version": "1.0.0",
              "entryAssembly": "Broken.Plugin.dll",
              "entryType": "Broken.Plugin.PluginEntry"
            }
            """);

        IPluginManifestLoader loader = new PluginManifestLoader(new PluginManifestValidator());
        var result = loader.Load(Path.Combine(pluginDirectoryPath, "plugin.json"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, static x => x.Contains("entryAssembly が見つかりません", StringComparison.Ordinal));
    }

    [Fact]
    public void LoadCatalog_EnumeratesPluginDirectories()
    {
        var validDirectoryPath = CreatePluginDirectory("Catalog.Valid");
        File.WriteAllText(Path.Combine(validDirectoryPath, "Catalog.Valid.dll"), string.Empty);
        File.WriteAllText(
            Path.Combine(validDirectoryPath, "plugin.json"),
            """
            {
              "pluginId": "Catalog.Valid",
              "displayName": "Catalog Valid",
              "version": "1.0.0",
              "entryAssembly": "Catalog.Valid.dll",
              "entryType": "Catalog.Valid.PluginEntry"
            }
            """);

        var invalidDirectoryPath = CreatePluginDirectory("Catalog.Invalid");
        File.WriteAllText(
            Path.Combine(invalidDirectoryPath, "plugin.json"),
            """
            {
              "pluginId": "",
              "displayName": "Catalog Invalid",
              "version": "1.0.0",
              "entryAssembly": "Catalog.Invalid.dll",
              "entryType": "Catalog.Invalid.PluginEntry"
            }
            """);

        IPluginCatalogLoader loader = new PluginCatalogLoader(
            new PluginHostOptions { RootDirectoryPath = _rootDirectoryPath },
            new PluginManifestLoader(new PluginManifestValidator()));

        var results = loader.LoadCatalog();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, static x => x.Manifest?.PluginId == "Catalog.Valid" && x.IsValid);
        Assert.Contains(results, static x => x.Manifest?.DisplayName == "Catalog Invalid" && !x.IsValid);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectoryPath))
        {
            Directory.Delete(_rootDirectoryPath, recursive: true);
        }
    }

    private string CreatePluginDirectory(string pluginId)
    {
        var pluginDirectoryPath = Path.Combine(_rootDirectoryPath, pluginId);
        Directory.CreateDirectory(pluginDirectoryPath);
        return pluginDirectoryPath;
    }
}


