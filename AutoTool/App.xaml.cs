﻿using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using LogHelper;

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
            base.OnStartup(e);

            // UIスレッドで発生した未処理の例外をキャッチ
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 例外を処理（例: ログに記録、ユーザーに通知など）
            MessageBox.Show("予期しないエラーが発生しました: " + e.Exception.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);

            // ログに例外を記録
            GlobalLogger.Instance.Write(e.Exception);

            // 例外をハンドル済みとして設定（アプリケーションが終了しないようにする）
            e.Handled = true;
        }
    }

}
