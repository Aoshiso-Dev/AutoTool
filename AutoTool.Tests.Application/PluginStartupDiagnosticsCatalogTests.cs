using System.IO;
using System.Reflection;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;
using AutoTool.Plugin.Host.Services;
using AutoTool.Tests.Plugin.Sample;

namespace AutoTool.Plugin.Host.Tests;

/// <summary>
/// プラグイン起動時診断のヘルスチェックと権限整合を確認するテストです。
/// </summary>
public sealed class PluginStartupDiagnosticsCatalogTests : IDisposable
{
    private readonly string _rootDirectoryPath = Path.Combine(Path.GetTempPath(), "AutoTool.PluginStartupDiagnosticsCatalogTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void GetDiagnostics_WithHealthyPluginAndDeclaredPermissions_ReturnsHealthyResult()
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
              "entryType": "AutoTool.Tests.Plugin.Sample.SamplePlugin",
              "permissions": [ "sample.read" ],
              "commands": [
                {
                  "commandType": "Sample.Plugin.ManifestCommand",
                  "displayName": "Manifest Command",
                  "category": "System",
                  "requiredPermissions": [ "sample.read" ]
                }
              ]
            }
            """);

        var diagnostics = CreateCatalog().GetDiagnostics();
        var result = Assert.Single(diagnostics);

        Assert.True(result.IsHealthy);
        Assert.Equal("Sample.Plugin", result.PluginId);
        Assert.Equal(["sample.read"], result.RequestedPermissions);
        Assert.Equal(["sample.read"], result.CommandPermissions);
        Assert.Empty(result.MissingPermissions);
        Assert.NotNull(result.HealthCheckResult);
        Assert.True(result.HealthCheckResult!.IsHealthy);
    }

    [Fact]
    public void GetDiagnostics_WithUndeclaredCommandPermissions_ReturnsUnhealthyResult()
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
              "entryType": "AutoTool.Tests.Plugin.Sample.SamplePlugin",
              "permissions": [ "sample.read" ],
              "commands": [
                {
                  "commandType": "Sample.Plugin.ManifestCommand",
                  "displayName": "Manifest Command",
                  "category": "System",
                  "requiredPermissions": [ "sample.write" ]
                }
              ]
            }
            """);

        var diagnostics = CreateCatalog().GetDiagnostics();
        var result = Assert.Single(diagnostics);

        Assert.False(result.IsHealthy);
        Assert.Equal(["sample.read"], result.RequestedPermissions);
        Assert.Equal(["sample.read", "sample.write"], result.CommandPermissions);
        Assert.Equal(["sample.write"], result.MissingPermissions);
        Assert.Contains(result.Messages, static x => x.Contains("permissions に不足", StringComparison.Ordinal));
        Assert.Equal("権限定義不足", result.Summary);
    }

    public void Dispose()
    {
        // 一時ディレクトリは GUID 単位で分離しているため、DLL ロックによるテスト不安定化を避けて削除しません。
    }

    private IPluginStartupDiagnosticsCatalog CreateCatalog()
    {
        IPluginCatalogLoader catalogLoader = new PluginCatalogLoader(
            new PluginHostOptions { RootDirectoryPath = _rootDirectoryPath },
            new PluginManifestLoader(new PluginManifestValidator()));
        IPluginLoader loader = new PluginLoader(catalogLoader);
        ILoadedPluginCatalog loadedPluginCatalog = new LoadedPluginCatalog(loader);
        IPluginCommandCatalog commandCatalog = new PluginCommandCatalog(loadedPluginCatalog);
        return new PluginStartupDiagnosticsCatalog(loadedPluginCatalog, commandCatalog);
    }

    private string CreatePluginDirectory(string pluginId)
    {
        var pluginDirectoryPath = Path.Combine(_rootDirectoryPath, pluginId);
        Directory.CreateDirectory(pluginDirectoryPath);
        return pluginDirectoryPath;
    }

    private static void CopySamplePluginAssembly(string pluginDirectoryPath)
    {
        var assemblyDirectoryPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(SamplePlugin))!.Location)!;
        File.Copy(
            Path.Combine(assemblyDirectoryPath, "AutoTool.Tests.Plugin.Sample.dll"),
            Path.Combine(pluginDirectoryPath, "AutoTool.Tests.Plugin.Sample.dll"),
            overwrite: true);
    }
}


