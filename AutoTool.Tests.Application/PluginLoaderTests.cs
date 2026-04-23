using System.IO;
using System.Reflection;
using AutoTool.Plugin.Abstractions.Interfaces;
using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;
using AutoTool.Plugin.Host.Services;

namespace AutoTool.Plugin.Host.Tests;

/// <summary>
/// プラグイン DLL の読込と型解決を確認するテストです。
/// </summary>
public sealed class PluginLoaderTests : IDisposable
{
    private readonly string _rootDirectoryPath = Path.Combine(Path.GetTempPath(), "AutoTool.PluginLoaderTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Load_WithValidPluginAssembly_LoadsPluginInstance()
    {
        var pluginDirectoryPath = CreatePluginDirectory("Sample.Plugin");
        CopySamplePluginAssembly(pluginDirectoryPath);
        File.WriteAllText(
            Path.Combine(pluginDirectoryPath, "plugin.json"),
            """
            {
              "pluginId": "Sample.Plugin",
              "displayName": "Sample Plugin",
              "version": "1.0.0",
              "entryAssembly": "AutoTool.Tests.Plugin.Sample.dll",
              "entryType": "AutoTool.Tests.Plugin.Sample.SamplePlugin"
            }
            """);

        IPluginCatalogLoader catalogLoader = new PluginCatalogLoader(
            new PluginHostOptions { RootDirectoryPath = _rootDirectoryPath },
            new PluginManifestLoader(new PluginManifestValidator()));
        IPluginLoader loader = new PluginLoader(catalogLoader);

        var result = Assert.Single(loader.LoadAll());

        Assert.True(result.IsLoaded);
        Assert.NotNull(result.Plugin);
        Assert.IsAssignableFrom<IAutoToolPlugin>(result.Plugin.Instance);
        Assert.Equal("Sample.Plugin", result.Plugin.Manifest.PluginId);
        Assert.NotNull(result.Plugin.CommandDefinitionProvider);
        Assert.NotNull(result.Plugin.CommandExecutor);
        Assert.NotNull(result.Plugin.ServiceRegistrar);
        Assert.Contains(
            result.Plugin.ServiceRegistrations,
            static x => string.Equals(x.ServiceType.FullName, "AutoTool.Tests.Plugin.Sample.ISamplePluginService", StringComparison.Ordinal)
                && string.Equals(x.ImplementationType?.FullName, "AutoTool.Tests.Plugin.Sample.SamplePluginService", StringComparison.Ordinal)
                && x.Lifetime == PluginServiceLifetime.Singleton);

        await result.Plugin.Instance.DisposeAsync(CancellationToken.None);
        result.Plugin.LoadContext.Unload();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Fact]
    public void Load_WithMissingEntryType_ReturnsError()
    {
        var pluginDirectoryPath = CreatePluginDirectory("Missing.EntryType");
        CopySamplePluginAssembly(pluginDirectoryPath);
        File.WriteAllText(
            Path.Combine(pluginDirectoryPath, "plugin.json"),
            """
            {
              "pluginId": "Missing.EntryType",
              "displayName": "Missing EntryType",
              "version": "1.0.0",
              "entryAssembly": "AutoTool.Tests.Plugin.Sample.dll",
              "entryType": "AutoTool.Tests.Plugin.Sample.UnknownPlugin"
            }
            """);

        IPluginCatalogLoader catalogLoader = new PluginCatalogLoader(
            new PluginHostOptions { RootDirectoryPath = _rootDirectoryPath },
            new PluginManifestLoader(new PluginManifestValidator()));
        IPluginLoader loader = new PluginLoader(catalogLoader);

        var result = Assert.Single(loader.LoadAll());

        Assert.False(result.IsLoaded);
        Assert.Contains(result.Errors, static x => x.Contains("entryType が見つかりません", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_WithWrongEntryType_ReturnsError()
    {
        var pluginDirectoryPath = CreatePluginDirectory("Wrong.EntryType");
        CopySamplePluginAssembly(pluginDirectoryPath);
        File.WriteAllText(
            Path.Combine(pluginDirectoryPath, "plugin.json"),
            """
            {
              "pluginId": "Wrong.EntryType",
              "displayName": "Wrong EntryType",
              "version": "1.0.0",
              "entryAssembly": "AutoTool.Tests.Plugin.Sample.dll",
              "entryType": "AutoTool.Tests.Plugin.Sample.WrongType"
            }
            """);

        IPluginCatalogLoader catalogLoader = new PluginCatalogLoader(
            new PluginHostOptions { RootDirectoryPath = _rootDirectoryPath },
            new PluginManifestLoader(new PluginManifestValidator()));
        IPluginLoader loader = new PluginLoader(catalogLoader);

        var result = Assert.Single(loader.LoadAll());

        Assert.False(result.IsLoaded);
        Assert.Contains(result.Errors, static x => x.Contains("IAutoToolPlugin を実装していません", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        // 一時ディレクトリは GUID 単位で分離しているため、DLL ロックによるテスト不安定化を避けて削除しません。
    }

    private string CreatePluginDirectory(string pluginId)
    {
        var pluginDirectoryPath = Path.Combine(_rootDirectoryPath, pluginId);
        Directory.CreateDirectory(pluginDirectoryPath);
        return pluginDirectoryPath;
    }

    private static void CopySamplePluginAssembly(string pluginDirectoryPath)
    {
        var assemblyDirectoryPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(AutoTool.Tests.Plugin.Sample.SamplePlugin))!.Location)!;
        File.Copy(
            Path.Combine(assemblyDirectoryPath, "AutoTool.Tests.Plugin.Sample.dll"),
            Path.Combine(pluginDirectoryPath, "AutoTool.Tests.Plugin.Sample.dll"),
            overwrite: true);
    }
}


