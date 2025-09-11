using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoTool.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.Desktop.Services
{
    /// <summary>
    /// WPFアプリケーションをIHostedServiceとして実行するサービス
    /// </summary>
    public sealed class WpfApplicationService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<WpfApplicationService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private MainWindow? _mainWindow;

        public WpfApplicationService(
            IServiceProvider services, 
            ILogger<WpfApplicationService> logger,
            IHostApplicationLifetime appLifetime)
        {
            _services = services;
            _logger = logger;
            _appLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WPFアプリケーションサービスを開始しています...");

            try
            {
                // UIスレッドでWPFアプリケーションを開始
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        _logger.LogDebug("メインウィンドウを作成中...");
                        
                        // メインウィンドウ作成
                        _mainWindow = _services.GetRequiredService<MainWindow>();
                        
                        // アプリケーション終了時にHostも停止
                        _mainWindow.Closed += (s, e) =>
                        {
                            _logger.LogInformation("メインウィンドウが閉じられました");
                            _appLifetime.StopApplication();
                        };

                        _logger.LogInformation("メインウィンドウを表示中...");
                        _mainWindow.Show();
                        
                        _logger.LogInformation("? メインウィンドウ表示完了");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "メインウィンドウ作成中にエラーが発生しました");
                        throw;
                    }
                });

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WPFアプリケーション開始中にエラーが発生しました");
                _appLifetime.StopApplication();
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WPFアプリケーションサービスを停止しています...");
            
            try
            {
                // メインウィンドウを閉じる
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    _mainWindow?.Close();
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WPFアプリケーション停止中に警告");
            }

            return Task.CompletedTask;
        }
    }
}