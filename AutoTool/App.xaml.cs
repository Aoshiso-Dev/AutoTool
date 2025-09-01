using System;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AutoTool.Services;
using AutoTool.Services.Safety;
using AutoTool.Services.Configuration;

namespace AutoTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal IHost? _host;
        private ILogger<App>? _logger;

        /// <summary>
        /// アプリケーション開始時の処理
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== App.OnStartup 開始 ===");
                
                // 例外ハンドラーを早期に設定
                this.DispatcherUnhandledException += Application_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                
                // 安全な起動チェック
                if (!SafeActivator.CanActivateApplication())
                {
                    MessageBox.Show("アプリケーションの起動に失敗しました。",
                        "起動エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }

                // ホストビルダーの作成
                var hostBuilder = CreateHostBuilder();
                _host = hostBuilder.Build();

                // サービスプロバイダーから必要なサービスを取得
                var serviceProvider = _host.Services;
                _logger = serviceProvider.GetRequiredService<ILogger<App>>();

                _logger.LogInformation("AutoTool アプリケーション開始");

                // ホストを開始
                await _host.StartAsync();

                // プラグインシステムを初期化
                await serviceProvider.InitializePluginSystemAsync();

                // メインウィンドウを作成して表示
                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                mainWindow.Show();

                System.Diagnostics.Debug.WriteLine("=== App.OnStartup 完了 ===");
            }
            catch (Exception ex)
            {
                var errorMessage = $"アプリケーション起動中にエラーが発生しました: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                _logger?.LogCritical(ex, "アプリケーション起動エラー");
                
                MessageBox.Show(errorMessage, "起動エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }

            base.OnStartup(e);
        }

        /// <summary>
        /// アプリケーション終了時の処理
        /// </summary>
        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                _logger?.LogInformation("AutoTool アプリケーション終了");

                if (_host != null)
                {
                    await _host.StopAsync();
                    _host.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "アプリケーション終了中にエラーが発生しました");
            }
            finally
            {
                base.OnExit(e);
            }
        }

        /// <summary>
        /// ハンドルされていない例外の処理
        /// </summary>
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                var errorDetails = $"UI Thread Exception: {e.Exception}";
                System.Diagnostics.Debug.WriteLine(errorDetails);
                _logger?.LogCritical(e.Exception, "ハンドルされていないUI例外が発生しました");

                // DefaultBinderエラーの特別処理
                if (e.Exception is System.Reflection.TargetParameterCountException ||
                    e.Exception.Message.Contains("DefaultBinder") ||
                    e.Exception.Message.Contains("Object reference not set"))
                {
                    System.Diagnostics.Debug.WriteLine("DefaultBinder関連エラーを検出しました");
                    _logger?.LogWarning("DefaultBinder関連エラーが発生しました。継続して実行します。");
                    
                    var message = "アプリケーションで軽微なエラーが発生しました。\n" +
                                "継続して実行します。\n\n" +
                                "エラーの詳細:\n" + e.Exception.Message;

                    MessageBox.Show(message, "情報", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    e.Handled = true; // エラーを処理済みとしてアプリケーションを継続
                    return;
                }

                // その他の一般的なエラー
                var userMessage = $"予期しないエラーが発生しました。\n\n{e.Exception.Message}\n\nアプリケーションを継続しますか？";

                var result = MessageBox.Show(userMessage, "予期しないエラー", 
                    MessageBoxButton.YesNo, MessageBoxImage.Error);

                if (result == MessageBoxResult.Yes)
                {
                    e.Handled = true;
                    _logger?.LogInformation("ユーザーがアプリケーション継続を選択しました");
                    System.Diagnostics.Debug.WriteLine("エラー処理: 継続選択");
                }
                else
                {
                    _logger?.LogInformation("ユーザーがアプリケーション終了を選択しました");
                    System.Diagnostics.Debug.WriteLine("エラー処理: 終了選択");
                    Current?.Shutdown(1);
                }
            }
            catch (Exception ex)
            {
                // 最後の砦
                System.Diagnostics.Debug.WriteLine($"例外ハンドラーでさらに例外が発生: {ex}");
                MessageBox.Show(
                    "重大なエラーが発生しました。アプリケーションを強制終了します。", 
                    "システムエラー", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Stop);
                Current?.Shutdown(1);
            }
        }

        /// <summary>
        /// アプリケーションドメインレベルの例外処理
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                var errorDetails = $"AppDomain Exception: {exception}";
                System.Diagnostics.Debug.WriteLine(errorDetails);
                _logger?.LogCritical(exception, "ハンドルされていないAppDomain例外が発生しました");

                if (exception != null)
                {
                    MessageBox.Show(
                        $"重大なエラーが発生しました。アプリケーションを終了します。\n\n{exception.Message}",
                        "重大なエラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AppDomain例外ハンドラーでエラー: {ex}");
            }
        }

        /// <summary>
        /// ホストビルダーの作成
        /// </summary>
        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .ConfigureServices((context, services) =>
                {
                    // AutoToolのサービスを登録
                    services.AddAutoToolServices();

                    // ViewModelの登録
                    services.AddTransient<MainWindowViewModel>();

                    // その他の必要なサービスをここに追加
                });
        }
    }
}
