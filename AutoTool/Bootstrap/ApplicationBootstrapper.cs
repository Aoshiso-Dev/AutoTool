using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AutoTool.Services;
using AutoTool.Services.Safety;
using AutoTool.ViewModel;
using AutoTool.Helpers;
using AutoTool.Logging;
using AutoTool.List.Class; // CommandListService用

namespace AutoTool.Bootstrap
{
    /// <summary>
    /// アプリケーション初期化を管理するサービス
    /// </summary>
    public interface IApplicationBootstrapper
    {
        Task<bool> InitializeAsync();
        Task ShutdownAsync();
        IHost Host { get; }
    }

    /// <summary>
    /// アプリケーション初期化の実装
    /// </summary>
    public class ApplicationBootstrapper : IApplicationBootstrapper
    {
        private IHost? _host;
        private readonly ILogger<ApplicationBootstrapper> _logger;

        public IHost Host => _host ?? throw new InvalidOperationException("Host is not initialized");

        public ApplicationBootstrapper()
        {
            // 一時的なロガーを作成（後で置き換える）
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug().AddConsole());
            _logger = loggerFactory.CreateLogger<ApplicationBootstrapper>();
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _logger.LogInformation("ApplicationBootstrapper 初期化開始");

                // 安全な起動チェック
                if (!SafeActivator.CanActivateApplication())
                {
                    _logger.LogError("アプリケーションの起動条件を満たしていません");
                    return false;
                }

                _logger.LogDebug("起動条件チェック完了");

                // DIコンテナの構築
                _logger.LogDebug("DIコンテナ構築開始");
                _host = CreateHost();
                _logger.LogDebug("DIコンテナ構築完了");
                
                // ロガーを正式なものに更新
                var realLogger = _host.Services.GetRequiredService<ILogger<ApplicationBootstrapper>>();
                
                realLogger.LogInformation("AutoTool アプリケーション初期化開始");

                // ホストを開始
                realLogger.LogDebug("Host開始");
                await _host.StartAsync();
                realLogger.LogDebug("Host開始完了");

                // 各種サービスの初期化
                realLogger.LogDebug("サービス初期化開始");
                await InitializeServicesAsync(_host.Services);
                realLogger.LogDebug("サービス初期化完了");

                // 登録されているサービス一覧をログ出力（デバッグ用）
                LogRegisteredServices(_host.Services, realLogger);

                realLogger.LogInformation("AutoTool アプリケーション初期化完了");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "アプリケーション初期化に失敗しました");
                return false;
            }
        }

        private void LogRegisteredServices(IServiceProvider services, ILogger logger)
        {
            try
            {
                logger.LogDebug("=== 登録サービス確認開始 ===");
                
                var serviceTypes = new[]
                {
                    typeof(ILogger<MainWindowViewModel>),
                    typeof(ILogger<CommandListService>), // CommandListService用のILoggerを追加
                    typeof(CommandListService), // CommandListServiceを追加
                    typeof(AutoTool.Services.IRecentFileService),
                    typeof(AutoTool.Services.Plugin.IPluginService),
                    typeof(AutoTool.Services.UI.IMainWindowMenuService),
                    typeof(AutoTool.Services.UI.IMainWindowButtonService),
                    typeof(AutoTool.Services.UI.IMainWindowCommandService),
                    typeof(MainWindowViewModel),
                    typeof(AutoTool.ViewModel.Panels.EditPanelViewModel),
                    typeof(AutoTool.ViewModel.Panels.ListPanelViewModel),
                };

                foreach (var serviceType in serviceTypes)
                {
                    var service = services.GetService(serviceType);
                    if (service != null)
                    {
                        logger.LogDebug("? {ServiceType} -> {ImplementationType}", 
                            serviceType.Name, service.GetType().Name);
                    }
                    else
                    {
                        logger.LogWarning("? {ServiceType} -> サービスが見つかりません", serviceType.Name);
                    }
                }
                
                logger.LogDebug("=== 登録サービス確認終了 ===");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "サービス確認時にエラー");
            }
        }

        public async Task ShutdownAsync()
        {
            try
            {
                if (_host != null)
                {
                    _logger.LogInformation("AutoTool アプリケーション終了開始");
                    await _host.StopAsync();
                    _host.Dispose();
                    _logger.LogInformation("AutoTool アプリケーション終了完了");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アプリケーション終了中にエラーが発生しました");
            }
        }

        private IHost CreateHost()
        {
            try
            {
                _logger.LogDebug("Host作成開始");
                
                var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration((ctx, cfg) =>
                    {
                        _logger.LogDebug("設定構築開始");
                        cfg.SetBasePath(AppContext.BaseDirectory);
                        cfg.AddJsonFile("Settings.json", optional: true, reloadOnChange: true);
                        cfg.AddEnvironmentVariables(prefix: "AUTOTOOL_");
                        _logger.LogDebug("設定構築完了");
                    })
                    .ConfigureLogging((ctx, logging) =>
                    {
                        _logger.LogDebug("ログ設定開始");
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Trace);
                        logging.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                        logging.AddConsole();
                        logging.AddDebug();
                        logging.AddSimpleFile();
                        _logger.LogDebug("ログ設定完了");
                    })
                    .ConfigureServices((context, services) =>
                    {
                        _logger.LogDebug("サービス登録開始");
                        
                        // AutoToolサービス登録
                        services.AddAutoToolServices();
                        
                        // アプリケーション固有のサービス
                        services.AddSingleton<IApplicationBootstrapper, ApplicationBootstrapper>();
                        
                        _logger.LogDebug("サービス登録完了");
                    })
                    .Build();
                
                _logger.LogDebug("Host作成完了");
                return host;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Host作成中にエラー");
                throw;
            }
        }

        private async Task InitializeServicesAsync(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ApplicationBootstrapper>>();

            try
            {
                // プラグインシステムを初期化
                logger.LogDebug("プラグインサービス初期化開始");
                var pluginService = serviceProvider.GetService<AutoTool.Services.Plugin.IPluginService>();
                if (pluginService != null)
                {
                    await pluginService.LoadAllPluginsAsync();
                    logger.LogInformation("プラグインシステム初期化完了");
                }
                else
                {
                    logger.LogWarning("プラグインサービスが見つかりません");
                }

                // ファクトリーサービスの初期化
                logger.LogDebug("ファクトリーサービス初期化開始");
                AutoTool.Model.MacroFactory.MacroFactory.SetServiceProvider(serviceProvider);
                AutoTool.Model.CommandDefinition.CommandRegistry.Initialize();
                logger.LogInformation("ファクトリーサービス初期化完了");

                // Helperサービスの初期化（遅延実行）
                logger.LogDebug("ヘルパーサービス初期化開始");
                try
                {
                    var helperInitializer = serviceProvider.GetService<Action<IServiceProvider>>();
                    if (helperInitializer != null)
                    {
                        helperInitializer(serviceProvider);
                        logger.LogDebug("JsonSerializerHelper初期化完了");
                    }

                    ViewModelLocator.Initialize(serviceProvider);
                    logger.LogDebug("ViewModelLocator初期化完了");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Helperサービス初期化で警告が発生しましたが、継続します");
                }
                
                logger.LogInformation("ヘルパーサービス初期化完了");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "サービス初期化中にエラーが発生しました");
                throw;
            }
        }
    }
}