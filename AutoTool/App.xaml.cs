using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using MacroPanels.Model.CommandDefinition;
using AutoTool.Services;

namespace AutoTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Log Logger { get; private set; } = new Log();

        protected override void OnStartup(StartupEventArgs e)
        {
            // コマンドレジストリを初期化
            CommandRegistry.Initialize();
            
            base.OnStartup(e);

            // UIスレッドで発生した未処理の例外をキャッチ
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 例外を処理（例: ログに記録、ユーザーに通知など）
            AppServices.NotificationService.ShowError("予期しないエラーが発生しました: " + e.Exception.Message, "エラー");

            // ログに例外を記録
            AppServices.LogService.Write(e.Exception);

            // 例外をハンドル済みとして設定（アプリケーションが終了しないようにする）
            e.Handled = true;
        }
    }

}
