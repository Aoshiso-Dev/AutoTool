using AutoTool.Desktop.CommandLine;
using Microsoft.Extensions.Hosting;

namespace AutoTool.Desktop.Hosting;

/// <summary>
/// ホスト起動時にメインウィンドウを生成し、起動引数の初期操作を適用します。
/// </summary>
public sealed class MainWindowHostedService(
    MainWindow mainWindow,
    CommandLineControlService commandLineControlService,
    CommandLineInvocation startupInvocation) : IHostedService
{
    private readonly MainWindow _mainWindow = EnsureNotNull(mainWindow);
    private readonly CommandLineControlService _commandLineControlService = EnsureNotNull(commandLineControlService);
    private readonly CommandLineInvocation _startupInvocation = EnsureNotNull(startupInvocation);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (System.Windows.Application.Current.MainWindow is null)
        {
            System.Windows.Application.Current.MainWindow = _mainWindow;
        }

        if (!_startupInvocation.ShouldStartHidden && !_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        _ = _commandLineControlService.ExecuteAsync(_startupInvocation, fromIpc: false, cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static T EnsureNotNull<T>(T value) where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }
}



