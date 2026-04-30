using System.IO;
using System.Reflection;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Commands.Commands;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;
using AutoTool.Plugin.Host.Services;

namespace AutoTool.Plugin.Host.Tests;

/// <summary>
/// プラグイン定義がコマンドレジストリへ統合されることを確認するテストです。
/// </summary>
public sealed class PluginCommandRegistryTests : IDisposable
{
    private readonly string _rootDirectoryPath = Path.Combine(Path.GetTempPath(), "AutoTool.PluginRegistryTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Initialize_WithPluginCommandDefinitions_RegistersPluginCommands()
    {
        var pluginDirectoryPath = Path.Combine(_rootDirectoryPath, "Sample.Plugin");
        Directory.CreateDirectory(pluginDirectoryPath);
        CopySamplePluginAssembly(pluginDirectoryPath);
        File.WriteAllText(
            Path.Combine(pluginDirectoryPath, "plugin.json"),
            """
            {
              "pluginId": "Sample.Plugin",
              "displayName": "Sample Plugin",
              "version": "1.0.0",
              "entryAssembly": "AutoTool.Tests.Plugin.Sample.dll",
              "entryType": "AutoTool.Tests.Plugin.Sample.SamplePlugin",
              "commands": [
                {
                  "commandType": "Sample.Plugin.ManifestCommand",
                  "displayName": "Manifest Command",
                  "category": "System",
                  "order": 3,
                  "showInCommandList": false
                }
              ]
            }
            """);

        IPluginCatalogLoader catalogLoader = new PluginCatalogLoader(
            new PluginHostOptions { RootDirectoryPath = _rootDirectoryPath },
            new PluginManifestLoader(new PluginManifestValidator()));
        IPluginLoader loader = new PluginLoader(catalogLoader);
        ILoadedPluginCatalog loadedPluginCatalog = new LoadedPluginCatalog(loader);
        IPluginCommandCatalog commandCatalog = new PluginCommandCatalog(loadedPluginCatalog);
        IExternalCommandMetadataProvider provider = new PluginRuntimeCommandMetadataProvider(commandCatalog);

        var registry = new ReflectionCommandRegistry(null, [provider]);
        registry.Initialize();

        Assert.Contains("Sample.Plugin.ManifestCommand", registry.GetAllTypeNames());
        Assert.Contains("Sample.Plugin.ProviderCommand", registry.GetAllTypeNames());
        Assert.Equal("Manifest Command", registry.GetDisplayName("Sample.Plugin.ManifestCommand"));
        Assert.Equal("System", registry.GetCategoryName("Sample.Plugin.ManifestCommand"));
        Assert.True(CommandMetadataCatalog.TryGetByTypeName("Sample.Plugin.ManifestCommand", out var manifestMetadata));
        Assert.False(manifestMetadata.ShowInCommandList);
        Assert.True(CommandMetadataCatalog.TryGetByTypeName("Sample.Plugin.ProviderCommand", out var providerMetadata));
        Assert.True(providerMetadata.ShowInCommandList);

        var item = registry.CreateCommandItem("Sample.Plugin.ProviderCommand");
        Assert.NotNull(item);
        var pluginItem = Assert.IsType<PluginCommandListItem>(item);
        Assert.Equal("Sample.Plugin.ProviderCommand", pluginItem.ItemType);
        Assert.Equal("Sample.Plugin", pluginItem.PluginId);

        var result = registry.CreateSimple(new RootCommand(), pluginItem);
        Assert.False(result.Success);
        Assert.Equal(CommandCreationFailureReason.CommandFactoryUnavailable, result.FailureReason);
    }

    public void Dispose()
    {
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

