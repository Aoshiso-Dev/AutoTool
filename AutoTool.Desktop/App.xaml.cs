using System;
using System.Threading.Tasks;
using System.Windows;
using AutoTool.Desktop.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoTool.Desktop
{
    /// <summary>
    /// AutoTool.Desktop WPF Application with IHost support
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        private ILogger<App>? _logger;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 先にbase.OnStartup()を呼び出す
            base.OnStartup(e);
            
            // 非同期での初期化を開始
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // グローバル例外ハンドラーを設定
                SetupExceptionHandling();

                _logger?.LogInformation("🚀 AutoTool.Desktop アプリケーション開始 (IHost対応版)");

                // IHostを構築
                _host = ServiceRegistration.BuildHost();
                
                // ロガーを取得
                _logger = _host.Services.GetService<ILogger<App>>();
                _logger?.LogInformation("IHost構築完了");

                // Attribute-based Command Registrationのデモを実行
                if (_logger?.IsEnabled(LogLevel.Information) ?? false)
                {
                    RunAttributeCommandRegistrationDemo();
                }

                // Hostを開始（これによりWpfApplicationServiceが実行される）
                await _host.StartAsync();

                _logger?.LogInformation("✅ IHost開始完了 - WPFアプリケーション実行中");
            }
            catch (Exception ex)
            {
                var errorMessage = $"アプリケーション起動中にエラーが発生しました:\n\n" +
                                  $"エラータイプ: {ex.GetType().Name}\n" +
                                  $"メッセージ: {ex.Message}\n\n" +
                                  $"詳細:\n{ex}";

                MessageBox.Show(errorMessage, "起動エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                _logger?.LogInformation("AutoTool.Desktop アプリケーション終了開始");

                if (_host != null)
                {
                    await _host.StopAsync();
                    _host.Dispose();
                }

                _logger?.LogInformation("AutoTool.Desktop アプリケーション終了完了");
            }
            catch (Exception ex)
            {
                // ログが使えない可能性があるため、コンソールにも出力
                Console.WriteLine($"アプリケーション終了中にエラー: {ex}");
                MessageBox.Show($"終了中にエラーが発生しました: {ex.Message}", "終了エラー", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                base.OnExit(e);
            }
        }

        /// <summary>
        /// グローバル例外ハンドラーの設定
        /// </summary>
        private void SetupExceptionHandling()
        {
            DispatcherUnhandledException += (sender, args) =>
            {
                _logger?.LogError(args.Exception, "ハンドルされていない例外が発生しました");
                
                var result = MessageBox.Show(
                    $"予期しないエラーが発生しました:\n{args.Exception.Message}\n\nアプリケーションを継続しますか?",
                    "エラー",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error);

                if (result == MessageBoxResult.Yes)
                {
                    args.Handled = true;
                    _logger?.LogInformation("ユーザーがアプリケーション継続を選択");
                }
                else
                {
                    _logger?.LogInformation("ユーザーがアプリケーション終了を選択");
                    Shutdown(1);
                }
            };

            // AppDomain例外ハンドラー
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                _logger?.LogCritical(exception, "ハンドルされていないAppDomain例外が発生しました");

                if (exception != null)
                {
                    var detailMessage = $"重大なエラーが発生しました:\n\n" +
                                       $"タイプ: {exception.GetType().Name}\n" +
                                       $"メッセージ: {exception.Message}";
                    
                    MessageBox.Show(detailMessage, "重大なエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
                Environment.Exit(1);
            };
        }

        /// <summary>
        /// Attribute-based Command Registrationのデモを実行
        /// </summary>
        private void RunAttributeCommandRegistrationDemo()
        {
            try
            {
                _logger?.LogInformation("📋 Attribute-based Command Registration デモを実行中...");
                
                if (_host?.Services != null)
                {
                    Examples.AttributeCommandRegistrationDemo.RunDemo(_host.Services);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "⚠️ Command Registration デモ実行中にエラー（アプリケーション継続）");
            }
        }
    }
}
