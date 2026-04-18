using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using AutoTool.Application.Ports;
using AutoTool.Commands.Infrastructure;
using AutoTool.Desktop.Model;
using AutoTool.Desktop.Panels.ViewModel;
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

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.C or Key.V) || Keyboard.Modifiers != ModifierKeys.Control)
        {
            return;
        }

        if (!CanHandleMacroClipboardShortcut())
        {
            return;
        }

        if (_viewModel.MacroPanelViewModel.ButtonPanelViewModel is not ButtonPanelViewModel buttonPanelViewModel)
        {
            return;
        }

        var command = e.Key switch
        {
            Key.C => buttonPanelViewModel.CopyCommand,
            Key.V => buttonPanelViewModel.PasteCommand,
            _ => null
        };

        if (command is null || !command.CanExecute(null))
        {
            return;
        }

        command.Execute(null);
        e.Handled = true;
    }

    private bool CanHandleMacroClipboardShortcut()
    {
        if (_viewModel.SelectedTabIndex != TabIndexes.Macro || _viewModel.IsRunning)
        {
            return false;
        }

        var focused = Keyboard.FocusedElement as DependencyObject;
        return !IsTextInputContext(focused);
    }

    private static bool IsTextInputContext(DependencyObject? focused)
    {
        for (var current = focused; current is not null; current = LogicalTreeHelper.GetParent(current))
        {
            if (current is TextBoxBase or System.Windows.Controls.PasswordBox or ComboBox { IsEditable: true })
            {
                return true;
            }
        }

        return false;
    }
}
