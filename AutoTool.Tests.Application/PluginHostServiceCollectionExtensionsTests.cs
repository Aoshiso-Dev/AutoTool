using System.IO;
using System.Reflection;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.DependencyInjection;
using AutoTool.Plugin.Abstractions.Video;
using AutoTool.Tests.Plugin.Sample;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.Plugin.Host.Tests;

/// <summary>
/// プラグインのサービス登録がアプリ DI に反映されることを確認するテストです。
/// </summary>
public sealed class PluginHostServiceCollectionExtensionsTests : IDisposable
{
    private readonly string _rootDirectoryPath = Path.Combine(Path.GetTempPath(), "AutoTool.PluginHostServiceCollectionExtensionsTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void AddPluginHostServices_RegistersPluginServicesIntoApplicationContainer()
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

        var services = new ServiceCollection();
        services.AddPluginHostServices(options => options.RootDirectoryPath = _rootDirectoryPath);

        using var serviceProvider = services.BuildServiceProvider();
        var loadedPluginCatalog = serviceProvider.GetRequiredService<ILoadedPluginCatalog>();
        var loadedPlugin = Assert.Single(loadedPluginCatalog.GetLoadedPlugins());
        var registration = Assert.Single(loadedPlugin.ServiceRegistrations);

        var first = serviceProvider.GetRequiredService(registration.ServiceType);
        var second = serviceProvider.GetRequiredService(registration.ServiceType);

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Equal(registration.ImplementationType, first.GetType());

        var value = Assert.IsType<string>(
            registration.ServiceType.GetMethod(nameof(ISamplePluginService.GetValue))!.Invoke(first, null));
        Assert.Equal("sample", value);
    }

    [Fact]
    public void AddPluginHostServices_RegistersVideoStreamRegistryIntoApplicationContainer()
    {
        var services = new ServiceCollection();
        services.AddPluginHostServices(options => options.RootDirectoryPath = _rootDirectoryPath);

        using var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IVideoStreamRegistry>();

        Assert.Empty(registry.GetSources());
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
        var assemblyDirectoryPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(SamplePlugin))!.Location)!;
        File.Copy(
            Path.Combine(assemblyDirectoryPath, "AutoTool.Tests.Plugin.Sample.dll"),
            Path.Combine(pluginDirectoryPath, "AutoTool.Tests.Plugin.Sample.dll"),
            overwrite: true);
    }
}

