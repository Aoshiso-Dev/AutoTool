using System.Windows;
using Microsoft.Extensions.Hosting;

namespace AutoTool.Desktop.Hosting;

/// <summary>
/// ホスト起動時にメインウィンドウを生成・表示し、アプリの初期表示を開始します。
/// </summary>
public sealed class MainWindowHostedService(MainWindow mainWindow) : IHostedService
{
    private readonly MainWindow _mainWindow = EnsureNotNull(mainWindow);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (System.Windows.Application.Current.MainWindow is null)
        {
            System.Windows.Application.Current.MainWindow = _mainWindow;
        }

        if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static MainWindow EnsureNotNull(MainWindow value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }
}

