using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;
using AutoTool.Services.Plugin;
using AutoTool.Services.Theme;
using AutoTool.Services.Performance;
using AutoTool.ViewModel.Shared;
using AutoTool.ViewModel.Panels;

namespace AutoTool.Services
{
    /// <summary>
    /// Phase 5完全統合版：サービスコレクション拡張
    /// MacroPanels依存を削除し、AutoTool統合版のみ使用
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Phase 5完全統合版：AutoToolサービスを登録
        /// </summary>
        public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
        {
            // コア サービス
            services.AddSingleton<IConfigurationService, JsonConfigurationService>();
            services.AddSingleton<IThemeService, WpfThemeService>();
            services.AddSingleton<IPerformanceService, PerformanceService>();
            
            // Phase 5統合版プラグインシステム
            services.AddSingleton<AutoTool.Services.Plugin.IPluginService, AutoTool.Services.Plugin.PluginService>();
            
            // Phase 5統合版共有サービス
            services.AddSingleton<CommandHistoryManager>();
            
            // Phase 5追加：メッセージサービス
            services.AddSingleton<IMessageService, MessageBoxService>();
            
            // ファクトリーサービス
            services.AddSingleton<AutoTool.ViewModel.IViewModelFactory, ViewModelFactory>();
            
            // Phase 5統合版メインViewModel
            services.AddTransient<ViewModel.MacroPanelViewModel>();
            
            // Phase 5統合版パネルViewModels
            services.AddTransient<AutoTool.ViewModel.Panels.ButtonPanelViewModel>();
            services.AddTransient<AutoTool.ViewModel.Panels.ListPanelViewModel>();
            services.AddTransient<AutoTool.ViewModel.Panels.EditPanelViewModel>();
            services.AddTransient<AutoTool.ViewModel.Panels.LogPanelViewModel>();
            services.AddTransient<AutoTool.ViewModel.Panels.FavoritePanelViewModel>();
            
            return services;
        }
    }
}