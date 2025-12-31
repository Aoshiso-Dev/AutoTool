using System.Windows;
using System.Windows.Threading;
using AutoTool.Hosting;
using MacroPanels.Command.Services;
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
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // ホストを停止して破棄
        _host?.StopAsync().Wait();
        _host?.Dispose();

        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var notificationService = Services?.GetService<INotificationService>();
        var logService = Services?.GetService<AutoTool.Services.Interfaces.ILogService>();

        notificationService?.ShowError("予期しないエラーが発生しました: " + e.Exception.Message, "エラー");
        logService?.Write(e.Exception);

        // 例外をハンドル済みとして設定
        e.Handled = true;
    }
}
