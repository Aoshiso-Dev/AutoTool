using System.Windows;
using AutoTool.Application.Ports;
using AutoTool.Commands.Infrastructure;
using AutoTool.Desktop.Model;
using AutoTool.Desktop.View;
using Wpf.Ui.Controls;

namespace AutoTool.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly WindowSettings _windowSettings;
    private readonly MainWindowViewModel _viewModel;
    private readonly IUiStatePreferenceStore _uiStatePreferenceStore;
    private bool _restorePreviousSession;

    public MainWindow(
        MainWindowViewModel viewModel,
        TimeProvider timeProvider,
        IUiStatePreferenceStore uiStatePreferenceStore)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(uiStatePreferenceStore);

        InitializeComponent();
        _viewModel = viewModel;
        _uiStatePreferenceStore = uiStatePreferenceStore;
        DataContext = _viewModel;
        _restorePreviousSession = _uiStatePreferenceStore.LoadRestorePreviousSession();

        _windowSettings = WindowSettings.Load(timeProvider);
        if (_restorePreviousSession)
        {
            _windowSettings.ApplyToViewModel(_viewModel);
        }

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
        if (_restorePreviousSession)
        {
            _windowSettings.UpdateFromViewModel(_viewModel);
        }
        else
        {
            _windowSettings.ClearSessionState();
        }

        _windowSettings.Save();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_restorePreviousSession, _windowSettings.WindowSizePreset)
        {
            Owner = this
        };

        if (settingsWindow.ShowDialog() != true)
        {
            return;
        }

        _restorePreviousSession = settingsWindow.RestorePreviousSession;
        _uiStatePreferenceStore.SaveRestorePreviousSession(_restorePreviousSession);
        _windowSettings.UpdateWindowSizePreset(settingsWindow.SelectedWindowSizePreset);
        _windowSettings.Save();
        _windowSettings.ApplyToWindow(this);
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        // 念のためグローバルフックを解除してから明示的に終了
        Win32MouseHookHelper.StopHook();
        System.Windows.Application.Current?.Shutdown();

        // Shutdown が何らかの理由で完了しないケース向けのフェイルセーフ
        _ = ForceExitAsync();
    }

    private static async Task ForceExitAsync()
    {
        await Task.Delay(1500).ConfigureAwait(false);
        Environment.Exit(0);
    }
}
