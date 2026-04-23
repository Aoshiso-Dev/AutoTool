using System.IO;
using System.Reflection;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;
using AutoTool.Plugin.Host.Services;

namespace AutoTool.Plugin.Host.Tests;

/// <summary>
/// DLL 由来のコマンド定義がカタログへ反映されることを確認するテストです。
/// </summary>
public sealed class PluginCommandCatalogTests : IDisposable
{
    private readonly string _rootDirectoryPath = Path.Combine(Path.GetTempPath(), "AutoTool.PluginCommandCatalogTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void GetCommandDefinitions_IncludesProviderDefinitions()
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
                  "order": 5
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

        var definitions = commandCatalog.GetCommandDefinitions();

        Assert.Contains(definitions, static x => x.CommandType == "Sample.Plugin.ManifestCommand");
        var providerCommand = Assert.Single(definitions, static x => x.CommandType == "Sample.Plugin.ProviderCommand");
        Assert.Equal("指定した変数へ文字列を設定します。", providerCommand.Description);
        Assert.Collection(
            providerCommand.Properties.OrderBy(static x => x.Order),
            property =>
            {
                Assert.Equal("targetVariable", property.Name);
                Assert.Equal("TextBox", property.EditorType);
                Assert.True(property.IsRequired);
            },
            property =>
            {
                Assert.Equal("value", property.Name);
                Assert.Equal("TextBox", property.EditorType);
            });
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
