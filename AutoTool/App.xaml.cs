using System;
using System.Windows;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using AutoTool.Bootstrap;
using AutoTool.ViewModel;
using AutoTool.Command.Definition;

namespace AutoTool
{
    /// <summary>
    /// アプリケーションエントリポイント（クリーンアーキテクチャ対応）
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private IApplicationBootstrapper? _bootstrapper;
        private ILogger<App>? _logger;

        // Bootstrapper経由でHostにアクセス
        internal IServiceProvider? Services => _bootstrapper?.Host?.Services;

        /// <summary>
        /// アプリケーション開始時の処理
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // 設定ファイルの準備
                EnsureSettingsFile();
                
                // 例外ハンドラーの設定
                SetupExceptionHandling();
                
                // アプリケーションの初期化
                _bootstrapper = new ApplicationBootstrapper();
                var initSuccess = await _bootstrapper.InitializeAsync();
                
                if (!initSuccess)
                {
                    ShowErrorAndShutdown("アプリケーションの初期化に失敗しました。詳細はログを確認してください。");
                    return;
                }

                // ロガーを取得
                _logger = _bootstrapper.Host.Services.GetRequiredService<ILogger<App>>();
                _logger.LogInformation("AutoTool アプリケーション開始");

                // DirectCommandRegistryの初期化を追加
                DirectCommandRegistry.Initialize(_bootstrapper.Host.Services);
                _logger.LogInformation("DirectCommandRegistry初期化完了");

                // メインウィンドウの作成と表示
                CreateAndShowMainWindow();
                
                _logger.LogInformation("MainWindow作成と表示完了");
            }
            catch (Exception ex)
            {
                var detailMessage = $"アプリケーション起動中にエラーが発生しました:\n\n" +
                                   $"エラータイプ: {ex.GetType().Name}\n" +
                                   $"メッセージ: {ex.Message}\n\n" +
                                   $"スタックトレース:\n{ex.StackTrace}";
                
                ShowErrorAndShutdown(detailMessage);
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
                if (_bootstrapper != null)
                {
                    await _bootstrapper.ShutdownAsync();
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
        /// 例外ハンドリングの設定
        /// </summary>
        private void SetupExceptionHandling()
        {
            DispatcherUnhandledException += Application_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        /// <summary>
        /// メインウィンドウの作成と表示（エラーハンドリング強化）
        /// </summary>
        private void CreateAndShowMainWindow()
        {
            if (_bootstrapper?.Host == null)
            {
                throw new InvalidOperationException("Bootstrapper Host が初期化されていません");
            }

            try
            {
                _logger?.LogInformation("MainWindowViewModel の取得を開始します");
                
                // 依存関係の詳細チェック
                var services = _bootstrapper.Host.Services;
                
                // 必要な依存関係が登録されているかチェック
                var requiredServices = new[]
                {
                    typeof(ILogger<MainWindowViewModel>),
                    typeof(IServiceProvider),
                    typeof(AutoTool.Services.RecentFileService),
                    typeof(AutoTool.Services.Plugin.IPluginService),
                    typeof(AutoTool.Services.UI.IMainWindowMenuService),
                };

                foreach (var serviceType in requiredServices)
                {
                    var service = services.GetService(serviceType);
                    if (service == null)
                    {
                        _logger?.LogError("必須サービスが見つかりません: {ServiceType}", serviceType.Name);
                        throw new InvalidOperationException($"必須サービスが見つかりません: {serviceType.Name}");
                    }
                    else
                    {
                        _logger?.LogDebug("サービス確認OK: {ServiceType}", serviceType.Name);
                    }
                }

                var mainWindowViewModel = services.GetRequiredService<MainWindowViewModel>();
                _logger?.LogInformation("MainWindowViewModel の取得に成功しました");
                
                var mainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel
                };
                
                MainWindow = mainWindow;
                mainWindow.Show();
                
                _logger?.LogInformation("MainWindow の表示が完了しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MainWindow作成中にエラー");
                throw new InvalidOperationException($"MainWindow作成に失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// エラー表示と終了（詳細エラー情報付き）
        /// </summary>
        private void ShowErrorAndShutdown(string message)
        {
            // コンソールにも出力
            Console.WriteLine($"[FATAL ERROR] {DateTime.Now}: {message}");
            
            System.Windows.MessageBox.Show(message, "起動エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }

        /// <summary>
        /// UI例外ハンドラー
        /// </summary>
        private void Application_DispatcherUnhandledException(object sender, 
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                _logger?.LogCritical(e.Exception, "ハンドルされていないUI例外が発生しました");

                // 軽微なエラーは継続、重大なエラーは終了
                if (IsMinorError(e.Exception))
                {
                    HandleMinorError(e);
                }
                else
                {
                    HandleCriticalError(e);
                }
            }
            catch (Exception ex)
            {
                // 最後の砦
                System.Windows.MessageBox.Show("重大なエラーが発生しました。アプリケーションを強制終了します。", 
                    "システムエラー", MessageBoxButton.OK, MessageBoxImage.Stop);
                Shutdown(1);
            }
        }

        /// <summary>
        /// AppDomain例外ハンドラー
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                _logger?.LogCritical(exception, "ハンドルされていないAppDomain例外が発生しました");

                if (exception != null)
                {
                    var detailMessage = $"重大なエラーが発生しました:\n\n" +
                                       $"タイプ: {exception.GetType().Name}\n" +
                                       $"メッセージ: {exception.Message}\n\n" +
                                       $"スタックトレース:\n{exception.StackTrace}";
                    
                    System.Windows.MessageBox.Show(detailMessage, "重大なエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // 緊急フォールバック
                System.Windows.MessageBox.Show("システムエラーが発生しました。", "エラー", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 軽微なエラーかどうかを判定
        /// </summary>
        private static bool IsMinorError(Exception exception)
        {
            return exception is System.Reflection.TargetParameterCountException ||
                   exception.Message.Contains("DefaultBinder") ||
                   exception.Message.Contains("Object reference not set");
        }

        /// <summary>
        /// 軽微なエラーの処理
        /// </summary>
        private void HandleMinorError(System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            _logger?.LogWarning("軽微なエラーが発生しました。継続して実行します。");
            
            System.Windows.MessageBox.Show("アプリケーションで軽微なエラーが発生しました。\n継続して実行します。\n\n" +
                          $"エラーの詳細:\n{e.Exception.Message}", "情報", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
            
            e.Handled = true;
        }

        /// <summary>
        /// 重大なエラーの処理
        /// </summary>
        private void HandleCriticalError(System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var userMessage = $"予期しないエラーが発生しました。\n\n{e.Exception.Message}\n\n" +
                             "アプリケーションを継続しますか？";

            var result = System.Windows.MessageBox.Show(userMessage, "予期しないエラー", 
                MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                e.Handled = true;
                _logger?.LogInformation("ユーザーがアプリケーション継続を選択しました");
            }
            else
            {
                _logger?.LogInformation("ユーザーがアプリケーション終了を選択しました");
                Shutdown(1);
            }
        }

        /// <summary>
        /// Settings.jsonファイルの確保
        /// </summary>
        private static void EnsureSettingsFile()
        {
            try
            {
                var exeDir = AppContext.BaseDirectory;
                var settingsPath = Path.Combine(exeDir, "Settings.json");

                // 旧形式からの移行
                var oldPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "AutoTool", "appsettings.json");
                
                if (!File.Exists(settingsPath) && File.Exists(oldPath))
                {
                    File.Copy(oldPath, settingsPath, overwrite: false);
                }

                // ファイルが存在しない場合はEnhancedConfigurationServiceが自動作成するため、
                // ここでは最小限の処理のみ
                if (!File.Exists(settingsPath))
                {
                    CreateMinimalSettingsFile(settingsPath);
                }
            }
            catch (Exception ex)
            {
                // 設定ファイル作成に失敗してもアプリケーションは起動する
                // EnhancedConfigurationServiceが代替処理を行う
                System.Diagnostics.Debug.WriteLine($"Settings.json作成警告: {ex.Message}");
            }
        }

        /// <summary>
        /// 最小限の設定ファイルの作成（詳細はEnhancedConfigurationServiceに委譲）
        /// </summary>
        private static void CreateMinimalSettingsFile(string path)
        {
            var minimalSettings = """
                {
                  "Logging": {
                    "LogLevel": {
                      "Default": "Information",
                      "AutoTool": "Debug"
                    }
                  },
                  "App": {
                    "Language": "ja-JP",
                    "AutoSave": true,
                    "AutoSaveInterval": 300
                  },
                  "Macro": {
                    "DefaultTimeout": 5000,
                    "DefaultInterval": 100
                  }
                }
                """;
            
            File.WriteAllText(path, minimalSettings);
        }
    }
}
