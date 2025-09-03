using System;
using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AutoTool.Services;
using AutoTool.Services.Safety;
using AutoTool.Services.Configuration;
using AutoTool.Logging; // 追加: ファイルロガー
using AutoTool.ViewModel; // MainWindowViewModelの名前空間
using AutoTool.Helpers;

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
                // Settings.json (全体設定 + Logging) を生成/確保
                EnsureSettingsFile();
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
                var pluginService = serviceProvider.GetService<AutoTool.Services.Plugin.IPluginService>();
                if (pluginService != null)
                {
                    await pluginService.LoadAllPluginsAsync();
                    _logger.LogInformation("プラグインシステム初期化完了");
                }

                // MacroFactoryとCommandRegistryの初期化
                AutoTool.Model.MacroFactory.MacroFactory.SetServiceProvider(serviceProvider);
                AutoTool.Model.CommandDefinition.CommandRegistry.Initialize();

                // JsonSerializerHelperのロガーを初期化
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var jsonLogger = loggerFactory.CreateLogger("JsonSerializerHelper");
                JsonSerializerHelper.SetLogger(jsonLogger);

                // メインウィンドウを作成して表示
                var mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
                var mainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel
                };
                MainWindow = mainWindow;
                mainWindow.Show();

                System.Diagnostics.Debug.WriteLine("=== App.OnStartup 完了 ===");
                _logger.LogInformation("MainWindow作成とDataContext設定完了");
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
        /// Settings.json (全設定 + Logging) を exe ディレクトリに生成。旧 appsettings.json (AppData) が存在し新設定が無い場合は移行。
        /// </summary>
        private void EnsureSettingsFile()
        {
            try
            {
                var exeDir = AppContext.BaseDirectory;
                var settingsPath = Path.Combine(exeDir, "Settings.json");

                // 旧形式 (AppData) から移行
                var oldPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoTool", "appsettings.json");
                if (!File.Exists(settingsPath) && File.Exists(oldPath))
                {
                    try
                    {
                        File.Copy(oldPath, settingsPath, overwrite: false);
                    }
                    catch { /* ignore */ }
                }

                if (!File.Exists(settingsPath))
                {
                    var sample = "{\n  \"Logging\": {\n    \"LogLevel\": {\n      \"Default\": \"Information\",\n      \"Microsoft\": \"Warning\",\n      \"Microsoft.Hosting.Lifetime\": \"Information\",\n      \"AutoTool\": \"Debug\"\n    }\n  },\n  \"App\": {\n    \"Theme\": \"Light\",\n    \"Language\": \"ja-JP\",\n    \"AutoSave\": true,\n    \"AutoSaveInterval\": 300\n  },\n  \"Macro\": {\n    \"DefaultTimeout\": 5000,\n    \"DefaultInterval\": 100\n  },\n  \"UI\": {\n    \"GridSplitterPosition\": 0.5,\n    \"TabIndex\": { \n      \"List\": 0,\n      \"Edit\": 0\n    }\n  }\n}";
                    File.WriteAllText(settingsPath, sample);
                }
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// ホストビルダー (Settings.json を統合設定として使用)
        /// </summary>
        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.SetBasePath(AppContext.BaseDirectory);
                    cfg.AddJsonFile("Settings.json", optional: true, reloadOnChange: true);
                    // 環境変数/コマンドラインも反映可能に
                    cfg.AddEnvironmentVariables(prefix: "AUTOTOOL_");
                })
                .ConfigureLogging((ctx, logging) =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddSimpleFile();
                    var configured = ctx.Configuration["Logging:LogLevel:Default"] ?? "(null)";
                    System.Diagnostics.Debug.WriteLine($"[Logging] Configured Default LogLevel = {configured}");
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddAutoToolServices();
                    services.AddTransient<MainWindowViewModel>();
                });
        }
    }
}
