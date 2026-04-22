using System.Windows;
using AutoTool.Application.Ports;
using AutoTool.Desktop.CommandLine;

namespace AutoTool.Desktop.Hosting;

/// <summary>
/// 起動引数と IPC 要求を UI 操作へ反映します。
/// </summary>
public sealed class CommandLineControlService(
    MainWindow mainWindow,
    MainWindowViewModel mainWindowViewModel,
    ILogWriter logWriter)
{
    private readonly MainWindow _mainWindow = EnsureNotNull(mainWindow);
    private readonly MainWindowViewModel _mainWindowViewModel = EnsureNotNull(mainWindowViewModel);
    private readonly ILogWriter _logWriter = EnsureNotNull(logWriter);
    private Action? _pendingExitOnCompleteHandler;

    public async Task<CommandLineIpcResponse> ExecuteAsync(
        CommandLineInvocation invocation,
        bool fromIpc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        if (!invocation.HasAnyOperation)
        {
            if (fromIpc)
            {
                await _mainWindow.Dispatcher.InvokeAsync(ShowAndActivateWindow, System.Windows.Threading.DispatcherPriority.Normal, cancellationToken);
            }

            return CommandLineIpcResponse.Ok();
        }

        return await _mainWindow.Dispatcher.InvokeAsync(
            () => ExecuteOnUiThread(invocation, fromIpc),
            System.Windows.Threading.DispatcherPriority.Normal,
            cancellationToken);
    }

    private CommandLineIpcResponse ExecuteOnUiThread(CommandLineInvocation invocation, bool fromIpc)
    {
        _logWriter.Write(
            "INFO",
            "CommandLineControl",
            $"起動要求=MacroPath:{invocation.MacroPath ?? "(null)"} Start:{invocation.Start} Stop:{invocation.Stop} Exit:{invocation.Exit} ExitOnComplete:{invocation.ExitOnComplete} Hide:{invocation.Hide} Show:{invocation.Show} SilentErrors:{invocation.SilentErrors}");

        var keepSuppressionUntilExecutionCompleted = invocation.SilentErrors && invocation.Start;
        if (invocation.SilentErrors)
        {
            _mainWindowViewModel.MacroPanelViewModel.SetCommandLineErrorNotificationSuppressed(
                suppress: true,
                clearOnExecutionCompleted: keepSuppressionUntilExecutionCompleted);
        }

        if (invocation.Show)
        {
            ShowAndActivateWindow();
        }

        if (invocation.Hide)
        {
            _mainWindow.Hide();
        }

        if (!string.IsNullOrWhiteSpace(invocation.MacroPath)
            && !_mainWindowViewModel.TryLoadMacroFromCommandLine(invocation.MacroPath, out var loadMessage))
        {
            ResetSuppressionIfNeeded(invocation, keepSuppressionUntilExecutionCompleted, forceReset: true);
            return Fail(CommandLineExitCodes.RuntimeFailure, loadMessage);
        }

        if (invocation.Stop
            && !_mainWindowViewModel.TryStopMacroFromCommandLine(out var stopMessage))
        {
            ResetSuppressionIfNeeded(invocation, keepSuppressionUntilExecutionCompleted, forceReset: true);
            return Fail(CommandLineExitCodes.TargetNotFound, stopMessage);
        }

        if (invocation.Start
            && !_mainWindowViewModel.TryStartMacroFromCommandLine(out var startMessage))
        {
            ResetSuppressionIfNeeded(invocation, keepSuppressionUntilExecutionCompleted, forceReset: true);
            return Fail(CommandLineExitCodes.RuntimeFailure, startMessage);
        }

        if (invocation.ExitOnComplete)
        {
            RegisterExitOnComplete();
        }

        if (invocation.Exit)
        {
            _logWriter.Write("INFO", "CommandLineControl", "終了要求を受け付けました。");
            if (fromIpc)
            {
                // IPC 応答送信後に終了できるよう、Dispatcher キューへ後段実行を積む。
                _ = _mainWindow.Dispatcher.BeginInvoke(ShutdownMainWindow);
            }
            else
            {
                ShutdownMainWindow();
            }
        }

        ResetSuppressionIfNeeded(invocation, keepSuppressionUntilExecutionCompleted);

        return CommandLineIpcResponse.Ok("要求を受け付けました。");
    }

    private void RegisterExitOnComplete()
    {
        if (_pendingExitOnCompleteHandler is not null)
        {
            _mainWindowViewModel.MacroPanelViewModel.ExecutionCompleted -= _pendingExitOnCompleteHandler;
            _pendingExitOnCompleteHandler = null;
        }

        void OnCompleted()
        {
            _mainWindowViewModel.MacroPanelViewModel.ExecutionCompleted -= OnCompleted;
            _pendingExitOnCompleteHandler = null;
            _logWriter.Write("INFO", "マクロ完了を検知したため自動終了します。");
            _mainWindow.RequestForceClose();
            if (_mainWindow.IsLoaded)
            {
                _mainWindow.Close();
            }
            else
            {
                System.Windows.Application.Current?.Shutdown();
            }
        }

        _pendingExitOnCompleteHandler = OnCompleted;
        _mainWindowViewModel.MacroPanelViewModel.ExecutionCompleted += OnCompleted;
    }

    private void ShowAndActivateWindow()
    {
        if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();
    }

    private CommandLineIpcResponse Fail(int exitCode, string message)
    {
        _logWriter.Write("WARN", $"コマンドライン処理失敗={message}");
        return new CommandLineIpcResponse(exitCode, message);
    }

    private void ShutdownMainWindow()
    {
        _mainWindow.RequestForceClose();
        if (_mainWindow.IsLoaded)
        {
            _mainWindow.Close();
        }
        else
        {
            System.Windows.Application.Current?.Shutdown();
        }
    }

    private static T EnsureNotNull<T>(T value) where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }

    private void ResetSuppressionIfNeeded(
        CommandLineInvocation invocation,
        bool keepSuppressionUntilExecutionCompleted,
        bool forceReset = false)
    {
        if (!invocation.SilentErrors)
        {
            return;
        }

        if (!forceReset && keepSuppressionUntilExecutionCompleted)
        {
            return;
        }

        _mainWindowViewModel.MacroPanelViewModel.SetCommandLineErrorNotificationSuppressed(
            suppress: false,
            clearOnExecutionCompleted: false);
    }

}
