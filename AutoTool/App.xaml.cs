using System.Windows;
using System.Windows.Threading;
using AutoTool.Hosting;
using AutoTool.Commands.Services;
using AutoTool.Commands.Infrastructure;
using AutoTool.Core.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoTool;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    /// <summary>
    /// アプリケーションのサービスプロバイダー
    /// </summary>
    /// <remarks>
    /// 可能な限りコンストラクタインジェクションを使用してください。
    /// このプロパティはMainWindowなど、DIコンテナから直接作成できないクラスでのみ使用してください。
    /// </remarks>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// DIコンテナからサービスを取得します
    /// </summary>
    /// <remarks>
    /// 可能な限りコンストラクタインジェクションを使用してください。
    /// </remarks>
    public static T GetService<T>() where T : notnull => Services.GetRequiredService<T>();

    protected override void OnStartup(StartupEventArgs e)
    {
        // ホストを構築して初期化
        _host = AppHostBuilder.BuildAndInitialize(e.Args);
        Services = _host.Services;

        base.OnStartup(e);

        // UIスレッドで発生した未処理の例外をキャッチ
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        // ホストを開始
        _host.Start();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // グローバルフックが残っていても必ず解除する
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
                Services?.GetService<ILogWriter>()?.Write(ex);
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
        var notifier = Services?.GetService<INotifier>();
        var logWriter = Services?.GetService<ILogWriter>();

        var exception = e.Exception;
        logWriter?.Write(exception);

        if (IsCriticalException(exception))
        {
            notifier?.ShowError("致命的なエラーが発生したためアプリケーションを終了します。", "致命的エラー");
            e.Handled = false;
            return;
        }

        notifier?.ShowError("予期しないエラーが発生しました: " + exception.Message, "エラー");

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


