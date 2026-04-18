using System.Windows;
using Microsoft.Extensions.Hosting;

namespace AutoTool.Hosting;

public sealed class MainWindowHostedService(MainWindow mainWindow) : IHostedService
{
    private readonly MainWindow _mainWindow = EnsureNotNull(mainWindow);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Application.Current.MainWindow is null)
        {
            Application.Current.MainWindow = _mainWindow;
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

