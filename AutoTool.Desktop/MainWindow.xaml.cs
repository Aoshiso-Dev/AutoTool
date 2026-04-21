using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using AutoTool.Application.Ports;
using AutoTool.Commands.Infrastructure;
using AutoTool.Desktop.Model;
using AutoTool.Desktop.Panels.View;
using AutoTool.Desktop.Panels.ViewModel;
using AutoTool.Desktop.Ui;
using AutoTool.Desktop.View;
using Wpf.Ui.Controls;

namespace AutoTool.Desktop;

/// <summary>
/// MainWindow.xaml のコードビハインドです。
/// </summary>
public partial class MainWindow : FluentWindow
{
    private static readonly TimeSpan EmergencyStopHoldDuration = TimeSpan.FromMilliseconds(1200);
    private static readonly TimeSpan EmergencyStopPollInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan EmergencyStopReleaseGrace = TimeSpan.FromMilliseconds(220);
    private const int VkEscape = 0x1B;
    private readonly WindowSettings _windowSettings;
    private readonly MainWindowViewModel _viewModel;
    private readonly IUiStatePreferenceStore _uiStatePreferenceStore;
    private readonly IAppDialogService _appDialogService;
    private bool _restorePreviousSession;
    private bool _isCommandListSelectAllPending;
    private bool _isEmergencyStopTriggered;
    private bool _isEscapeKeyDownByHook;
    private long? _emergencyStopPressedAtTimestamp;
    private long? _lastEscapePressedObservedAtTimestamp;
    private CancellationTokenSource? _emergencyStopPollCts;

    public MainWindow(
        MainWindowViewModel viewModel,
        TimeProvider timeProvider,
        IUiStatePreferenceStore uiStatePreferenceStore,
        IAppDialogService appDialogService)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(uiStatePreferenceStore);
        ArgumentNullException.ThrowIfNull(appDialogService);

        InitializeComponent();
        _viewModel = viewModel;
        _uiStatePreferenceStore = uiStatePreferenceStore;
        _appDialogService = appDialogService;
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
        Deactivated += MainWindow_Deactivated;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _windowSettings.ApplyToWindow(this);
        Win32KeyboardHookHelper.KeyChanged += Win32KeyboardHookHelper_KeyChanged;
        Win32KeyboardHookHelper.StartHook();
        StartEmergencyStopPolling();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!ConfirmCloseWhenUnsaved())
        {
            e.Cancel = true;
            return;
        }

        ResetEmergencyStopState();
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

    private bool ConfirmCloseWhenUnsaved()
    {
        if (!_viewModel.HasUnsavedChanges)
        {
            return true;
        }

        var result = _appDialogService.Show(
            "未保存の変更",
            "未保存の変更があります。このまま閉じると変更内容は失われます。閉じますか？",
            [
                new("save", "保存して閉じる", IsDefault: true),
                new("discard", "保存せず閉じる"),
                new("cancel", "キャンセル", IsCancel: true)
            ],
            AppDialogTone.Warning,
            this);

        switch (result)
        {
            case "save":
                if (_viewModel.SaveFileCommand.CanExecute(null))
                {
                    _viewModel.SaveFileCommand.Execute(null);
                }

                return !_viewModel.HasUnsavedChanges;
            case "discard":
                return true;
            default:
                return false;
        }
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

    private void AddCommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.MacroPanelViewModel.ButtonPanelViewModel is not ButtonPanelViewModel buttonPanelViewModel)
        {
            return;
        }

        var commandSelectionWindow = new CommandSelectionWindow(
            buttonPanelViewModel.ItemTypes,
            buttonPanelViewModel.SelectedItemType)
        {
            Owner = this
        };

        if (commandSelectionWindow.ShowDialog() != true || commandSelectionWindow.SelectedCommand is null)
        {
            return;
        }

        buttonPanelViewModel.SelectedItemType = commandSelectionWindow.SelectedCommand;
        buttonPanelViewModel.Add();
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };

        _ = aboutWindow.ShowDialog();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        // 念のためグローバルフックを解除してから明示的に終了
        StopEmergencyStopPolling();
        Win32KeyboardHookHelper.KeyChanged -= Win32KeyboardHookHelper_KeyChanged;
        Win32KeyboardHookHelper.StopHook();
        Win32MouseHookHelper.StopHook();
        System.Windows.Application.Current?.Shutdown();

        // `Shutdown` が完了しない異常系に備えたフェイルセーフです。
        _ = ForceExitAsync();
    }

    private static async Task ForceExitAsync()
    {
        await Task.Delay(1500).ConfigureAwait(false);
        Environment.Exit(0);
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _isEscapeKeyDownByHook = true;
            e.Handled = true;
            return;
        }

        if (_isCommandListSelectAllPending && e.Key is not Key.Delete)
        {
            SetCommandListSelectAllPending(false);
        }

        if (!CanHandleMacroKeyboardShortcut())
        {
            return;
        }

        if (_viewModel.MacroPanelViewModel.ButtonPanelViewModel is not ButtonPanelViewModel buttonPanelViewModel)
        {
            return;
        }

        if (e.Key == Key.X && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (buttonPanelViewModel.CopyCommand.CanExecute(null))
            {
                buttonPanelViewModel.CopyCommand.Execute(null);
            }

            if (buttonPanelViewModel.DeleteCommand.CanExecute(null))
            {
                buttonPanelViewModel.DeleteCommand.Execute(null);
            }

            SetCommandListSelectAllPending(false);
            e.Handled = true;
            return;
        }

        var command = e.Key switch
        {
            Key.Delete when Keyboard.Modifiers == ModifierKeys.None
                => _isCommandListSelectAllPending ? buttonPanelViewModel.ClearCommand : buttonPanelViewModel.DeleteCommand,
            Key.A when Keyboard.Modifiers == ModifierKeys.Control => null,
            Key.C when Keyboard.Modifiers == ModifierKeys.Control => buttonPanelViewModel.CopyCommand,
            Key.V when Keyboard.Modifiers == ModifierKeys.Control => buttonPanelViewModel.PasteCommand,
            Key.Up when Keyboard.Modifiers == ModifierKeys.Alt => buttonPanelViewModel.UpCommand,
            Key.Down when Keyboard.Modifiers == ModifierKeys.Alt => buttonPanelViewModel.DownCommand,
            _ => null
        };

        if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SetCommandListSelectAllPending(true);
            _viewModel.StatusMessage = "全選択しました。Delete キーで全削除できます。";
            e.Handled = true;
            return;
        }

        if (command is null || !command.CanExecute(null))
        {
            return;
        }

        command.Execute(null);
        if (e.Key == Key.Delete)
        {
            SetCommandListSelectAllPending(false);
        }
        e.Handled = true;
    }

    private void MainWindow_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        _isEscapeKeyDownByHook = false;
    }

    private void MainWindow_Deactivated(object? sender, EventArgs e) => ResetEmergencyStopState();

    private bool CanHandleMacroKeyboardShortcut()
    {
        if (_viewModel.SelectedTabIndex != TabIndexes.Macro || _viewModel.IsRunning)
        {
            return false;
        }

        var focused = Keyboard.FocusedElement as DependencyObject;
        return !IsTextInputContext(focused);
    }

    private void SetCommandListSelectAllPending(bool isPending)
    {
        _isCommandListSelectAllPending = isPending;
        _viewModel.MacroPanelViewModel.ListPanelViewModel.IsAllSelectedVisual = isPending;
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

    private void HandleEmergencyStopPressState(bool isPressed, long nowTimestamp)
    {
        if (!_viewModel.MacroPanelViewModel.IsRunning)
        {
            ResetEmergencyStopState();
            return;
        }

        if (isPressed)
        {
            _lastEscapePressedObservedAtTimestamp = nowTimestamp;

            if (_emergencyStopPressedAtTimestamp is null)
            {
                _emergencyStopPressedAtTimestamp = nowTimestamp;
                _viewModel.StatusMessage = $"緊急停止: Esc を {EmergencyStopHoldDuration.TotalSeconds:0.0} 秒長押しで停止します。";
                return;
            }

            if (_isEmergencyStopTriggered)
            {
                return;
            }

            var elapsed = Stopwatch.GetElapsedTime(_emergencyStopPressedAtTimestamp.Value, nowTimestamp);
            if (elapsed >= EmergencyStopHoldDuration)
            {
                _isEmergencyStopTriggered = true;
                _viewModel.MacroPanelViewModel.ButtonPanelViewModel.RequestStop();
            }
            return;
        }

        if (_lastEscapePressedObservedAtTimestamp is null)
        {
            ResetEmergencyStopState();
            return;
        }

        var releaseElapsed = Stopwatch.GetElapsedTime(_lastEscapePressedObservedAtTimestamp.Value, nowTimestamp);
        if (releaseElapsed >= EmergencyStopReleaseGrace)
        {
            ResetEmergencyStopState();
        }
    }

    private void ResetEmergencyStopState()
    {
        _isEscapeKeyDownByHook = false;
        _emergencyStopPressedAtTimestamp = null;
        _lastEscapePressedObservedAtTimestamp = null;
        _isEmergencyStopTriggered = false;
    }

    private void Win32KeyboardHookHelper_KeyChanged(object? sender, Win32KeyboardHookHelper.KeyboardHookEventArgs e)
    {
        if (e.VirtualKey != VkEscape)
        {
            return;
        }

        _ = Dispatcher.BeginInvoke(() =>
        {
            _isEscapeKeyDownByHook = e.IsKeyDown;
            HandleEmergencyStopPressState(e.IsKeyDown, Stopwatch.GetTimestamp());
        });
    }

    private void StartEmergencyStopPolling()
    {
        _emergencyStopPollCts?.Cancel();
        _emergencyStopPollCts?.Dispose();
        _emergencyStopPollCts = new();
        _ = PollEmergencyStopKeyAsync(_emergencyStopPollCts.Token);
    }

    private void StopEmergencyStopPolling()
    {
        _emergencyStopPollCts?.Cancel();
        _emergencyStopPollCts?.Dispose();
        _emergencyStopPollCts = null;
    }

    private async Task PollEmergencyStopKeyAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(EmergencyStopPollInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _ = Dispatcher.BeginInvoke(() =>
            {
                var now = Stopwatch.GetTimestamp();
                var isEscapePressed = Win32KeyStateHelper.IsKeyPressed(VkEscape) || _isEscapeKeyDownByHook;
                HandleEmergencyStopPressState(isEscapePressed, now);
            });
        }
    }
}
