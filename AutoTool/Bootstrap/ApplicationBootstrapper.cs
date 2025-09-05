using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using AutoTool.Services;
using AutoTool.Model.CommandDefinition;

namespace AutoTool.Bootstrap
{
    /// <summary>
    /// アプリケーション起動処理（DirectCommandRegistry対応）
    /// </summary>
    public class ApplicationBootstrapper : IApplicationBootstrapper
    {
        private IHost? _host;
        private ILogger<ApplicationBootstrapper>? _logger;

        public IHost? Host => _host;

        public async Task<bool> InitializeAsync()
        {
            try
            {
                // ホストの構築
                _host = CreateHostBuilder().Build();

                // ログサービスの取得
                _logger = _host.Services.GetRequiredService<ILogger<ApplicationBootstrapper>>();
                _logger.LogInformation("ApplicationBootstrapper 初期化開始");

                // ホストの開始
                await _host.StartAsync();

                // DirectCommandRegistry の初期化
                DirectCommandRegistry.Initialize(_host.Services);
                _logger.LogInformation("DirectCommandRegistry 初期化完了");

                _logger.LogInformation("ApplicationBootstrapper 初期化完了");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "ApplicationBootstrapper 初期化中に致命的エラー");
                
                // フォールバック：緊急エラー表示
                System.Windows.MessageBox.Show(
                    $"アプリケーションの初期化に失敗しました:\n{ex.Message}",
                    "起動エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                return false;
            }
        }

        public async Task ShutdownAsync()
        {
            try
            {
                _logger?.LogInformation("ApplicationBootstrapper シャットダウン開始");

                if (_host != null)
                {
                    await _host.StopAsync();
                    _host.Dispose();
                }

                _logger?.LogInformation("ApplicationBootstrapper シャットダウン完了");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ApplicationBootstrapper シャットダウン中にエラー");
            }
        }

        private IHostBuilder CreateHostBuilder()
        {
            var exeDir = AppContext.BaseDirectory;
            var settingsPath = Path.Combine(exeDir, "Settings.json");

            return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(exeDir);
                    if (File.Exists(settingsPath))
                    {
                        config.AddJsonFile("Settings.json", optional: true, reloadOnChange: true);
                    }
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddDebug();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureServices((context, services) =>
                {
                    // AutoToolのすべてのサービスを登録
                    services.AddAutoToolServices();
                })
                .UseConsoleLifetime();
        }
    }

    /// <summary>
    /// アプリケーション起動処理のインターフェース
    /// </summary>
    public interface IApplicationBootstrapper
    {
        IHost? Host { get; }
        Task<bool> InitializeAsync();
        Task ShutdownAsync();
    }
}