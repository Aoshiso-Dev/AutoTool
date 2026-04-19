using System.Windows;
using System.Windows.Threading;
using AutoTool.Desktop.Hosting;
using AutoTool.Commands.Services;
using AutoTool.Commands.Infrastructure;
using AutoTool.Application.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoTool.Bootstrap;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;
    private INotifier? _notifier;
    private ILogWriter? _logWriter;

    protected override void OnStartup(StartupEventArgs e)
    {
        // ホストを構築して初期化
        _host = AppHostBuilder.CreateHostBuilder(e.Args).Build();
        _notifier = _host.Services.GetRequiredService<INotifier>();
        _logWriter = _host.Services.GetRequiredService<ILogWriter>();

        base.OnStartup(e);

        // UIスレッドで発生した未処理の例外をキャッチ
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        // ホストを開始
        _host.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // グローバルフックが残っていても必ず解除する
        Win32KeyboardHookHelper.StopHook();
        Win32MouseHookHelper.StopHook();

        // UIスレッドの同期コンテキストに依存せず停止処理を完了させる
        if (_host is not null)
        {
            try
            {
                _host.StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logWriter?.Write(ex);
            }
            finally
            {
                _host.Dispose();
            }
        }

        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var exception = e.Exception;
        _logWriter?.Write(exception);

        if (IsCriticalException(exception))
        {
            _notifier?.ShowError("致命的なエラーが発生したためアプリケーションを終了します。", "致命的エラー");
            e.Handled = false;
            return;
        }

        _notifier?.ShowError("予期しないエラーが発生しました: " + exception.Message, "エラー");

        // 例外をハンドル済みとして設定
        e.Handled = true;
    }

    private static bool IsCriticalException(Exception exception) =>
        exception is OutOfMemoryException
            or AccessViolationException
            or AppDomainUnloadedException
            or BadImageFormatException
            or CannotUnloadAppDomainException
            or InvalidProgramException
            or StackOverflowException;
}


