using AutoTool.Automation.Runtime.Definitions;
using Microsoft.Extensions.Hosting;

namespace AutoTool.Desktop.Panels.Hosting;

/// <summary>
/// ホストの開始・停止に合わせて初期化や終了処理を実行します。
/// </summary>
public sealed class CommandRegistryInitializationHostedService(ICommandRegistry commandRegistry) : IHostedService
{
    private readonly ICommandRegistry _commandRegistry = EnsureNotNull(commandRegistry);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _commandRegistry.Initialize();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static ICommandRegistry EnsureNotNull(ICommandRegistry value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }
}

