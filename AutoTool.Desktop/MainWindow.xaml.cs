using System.Windows;
using AutoTool.Commands.Infrastructure;
using AutoTool.Model;
using Wpf.Ui.Controls;

namespace AutoTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly WindowSettings _windowSettings;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        _windowSettings = WindowSettings.Load();
        SourceInitialized += MainWindow_SourceInitialized;
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _windowSettings.ApplyToWindow(this);
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _windowSettings.UpdateFromWindow(this);
        _windowSettings.Save();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        // 念のためグローバルフックを解除してから明示的に終了
        Win32MouseHookHelper.StopHook();
        Application.Current?.Shutdown();

        // Shutdown が何らかの理由で完了しないケース向けのフェイルセーフ
        _ = Task.Run(async () =>
        {
            await Task.Delay(1500).ConfigureAwait(false);
            Environment.Exit(0);
        });
    }
}
