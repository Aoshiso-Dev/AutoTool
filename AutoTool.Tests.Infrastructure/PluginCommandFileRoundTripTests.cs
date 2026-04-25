using System.IO;
using System.Reflection;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Application.Ports;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Infrastructure.DependencyInjection;
using AutoTool.Plugin.Host.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.Plugin.Host.Tests;

/// <summary>
/// プラグインコマンドを含む macro の保存と再読込ができることを確認します。
/// </summary>
public sealed class PluginCommandFileRoundTripTests : IDisposable
{
    private readonly string _rootDirectoryPath = Path.Combine(Path.GetTempPath(), "AutoTool.PluginCommandFileRoundTripTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_WithPluginCommandItem_RestoresPluginItem()
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
              "entryType": "AutoTool.Tests.Plugin.Sample.SamplePlugin"
            }
            """);

        var macroPath = Path.Combine(_rootDirectoryPath, "sample.macro");

        var services = new ServiceCollection();
        services.AddCommandServices();
        services.AddMacroRuntimeCoreServices();
        services.AddPluginHostServices(options =>
        {
            options.RootDirectoryPath = _rootDirectoryPath;
        });

        using var provider = services.BuildServiceProvider();
        var gateway = provider.GetRequiredService<ICommandListFileGateway>();

        gateway.Save(
        [
            new PluginCommandListItem
            {
                LineNumber = 1,
                ItemType = "Sample.Plugin.ProviderCommand",
                PluginId = "Sample.Plugin",
                ParameterJson = """{"targetVariable":"result","value":"ok"}"""
            }
        ],
        macroPath);

        var loaded = gateway.Load(macroPath);

        var item = Assert.Single(loaded);
        var pluginItem = Assert.IsType<PluginCommandListItem>(item);
        Assert.Equal("Sample.Plugin.ProviderCommand", pluginItem.ItemType);
        Assert.Equal("Sample.Plugin", pluginItem.PluginId);
        Assert.Contains("targetVariable", pluginItem.ParameterJson, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        TryDeleteRootDirectory();
    }

    private void TryDeleteRootDirectory()
    {
        try
        {
            if (Directory.Exists(_rootDirectoryPath))
            {
                Directory.Delete(_rootDirectoryPath, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
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
