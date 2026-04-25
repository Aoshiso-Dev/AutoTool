using System.IO;
using System.Reflection;
using AutoTool.Commands.Commands;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Services;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Runtime.MacroFactory;
using AutoTool.Infrastructure.DependencyInjection;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.Plugin.Host.Tests;

/// <summary>
/// プラグインコマンドが DI 構成上も生成・実行できることを確認します。
/// </summary>
public sealed class PluginCommandExecutionIntegrationTests : IDisposable
{
    private readonly string _rootDirectoryPath = Path.Combine(Path.GetTempPath(), "AutoTool.PluginExecutionIntegrationTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task CreateMacro_WithPluginCommand_ExecutesSuccessfully()
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

        var services = new ServiceCollection();
        services.AddCommandServices();
        services.AddMacroRuntimeCoreServices();
        services.AddPluginHostServices(options =>
        {
            options.RootDirectoryPath = _rootDirectoryPath;
        });

        using var provider = services.BuildServiceProvider();
        var dependencyResolver = provider.GetRequiredService<ICommandDependencyResolver>();
        Assert.True(dependencyResolver.TryResolve(typeof(IPluginCommandDispatcher), out var dispatcher));
        Assert.NotNull(dispatcher);

        var registry = provider.GetRequiredService<ICommandRegistry>();
        registry.Initialize();

        var item = new PluginCommandListItem
        {
            LineNumber = 1,
            ItemType = "Sample.Plugin.ProviderCommand",
            PluginId = "Sample.Plugin",
            ParameterJson = """{"targetVariable":"result","value":"ok"}"""
        };

        var command = registry.CreateSimple(new RootCommand(), item);
        Assert.True(command.Success, command.Message);
        Assert.Equal(CommandCreationFailureReason.None, command.FailureReason);

        var macroFactory = provider.GetRequiredService<IMacroFactory>();
        var variableStore = provider.GetRequiredService<IVariableStore>();
        var macro = macroFactory.CreateMacro([item]);

        await macro.Execute(CancellationToken.None);

        Assert.Equal("ok", variableStore.Get("result"));
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
