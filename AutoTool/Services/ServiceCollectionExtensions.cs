using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AutoTool.Services.Configuration;
using AutoTool.Services.Theme;
using AutoTool.Services.Performance;
using Microsoft.Extensions.Logging;
using MacroPanels.Plugin;
using System.Linq;
using System.Threading.Tasks;
using System;
using MacroPanels.Model.CommandDefinition;
using MacroPanels.ViewModel;
using AutoTool.ViewModel;

namespace AutoTool.Services
{
    /// <summary>
    /// サービスコレクション拡張メソッド
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// AutoToolのサービスを登録
        /// </summary>
        public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
        {
            // 設定サービス
            services.AddSingleton<IConfigurationService, JsonConfigurationService>();
            
            // テーマサービス
            services.AddSingleton<IThemeService, WpfThemeService>();
            
            // プラグインサービス（MacroPanelsを使用）
            services.AddSingleton<MacroPanels.Plugin.IPluginService, MacroPanels.Plugin.PluginService>();
            
            // パフォーマンスサービス
            services.AddSingleton<IPerformanceService, PerformanceService>();

            // コマンドレジストリ
            services.AddSingleton<ICommandRegistry, CommandRegistryAdapter>();

            // ViewModelファクトリ
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();

            // メッセージサービス
            services.AddSingleton<IMessageService, MessageBoxService>();

            // MacroPanelsのViewModels
            services.AddTransient<ButtonPanelViewModel>();
            services.AddTransient<ListPanelViewModel>();
            services.AddTransient<EditPanelViewModel>();
            services.AddTransient<LogPanelViewModel>();
            services.AddTransient<FavoritePanelViewModel>();
            
            // AutoToolのViewModels
            services.AddTransient<MacroPanelViewModel>();
            
            return services;
        }

        /// <summary>
        /// プラグインシステムを初期化
        /// </summary>
        public static async Task InitializePluginSystemAsync(this IServiceProvider serviceProvider)
        {
            var pluginService = serviceProvider.GetRequiredService<MacroPanels.Plugin.IPluginService>();
            var logger = serviceProvider.GetRequiredService<ILogger<MacroPanels.Plugin.PluginService>>();
            
            try
            {
                // プラグインを読み込み - DI,Pluginブランチの内容を使用
                await pluginService.LoadAllPluginsAsync();
                
                logger.LogInformation("プラグインシステムを初期化しました");
                
                // 読み込み済みプラグインの情報をログ出力
                var loadedPlugins = pluginService.GetLoadedPlugins();
                var availableCommands = pluginService.GetAvailablePluginCommands();
                
                logger.LogInformation("読み込み済みプラグイン: {PluginCount}個", loadedPlugins.Count());
                logger.LogInformation("利用可能なプラグインコマンド: {CommandCount}個", availableCommands.Count());
                
                foreach (var plugin in loadedPlugins)
                {
                    logger.LogDebug("プラグイン: {PluginId} - {PluginName} (v{Version})", 
                        plugin.Id, plugin.Name, plugin.Version);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "プラグインシステム初期化中にエラーが発生しました");
            }
        }
    }
}