using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;
using AutoTool.Services;
using AutoTool.Services.Mouse;
using AutoTool.Services.Capture;
using AutoTool.Services.Configuration;
using AutoTool.Services.Plugin;
using AutoTool.Services.Theme;
using AutoTool.Services.UI;
using AutoTool.Model.List.Class;
using AutoTool.Model.CommandDefinition;
using CommunityToolkit.Mvvm.Messaging;

namespace AutoTool.Services
{
    /// <summary>
    /// サービスコレクション拡張メソッド
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// AutoToolの全サービスを登録
        /// </summary>
        public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
        {
            // ロギング設定
            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Messaging設定
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

            // Configuration Services
            services.AddSingleton<IEnhancedConfigurationService, EnhancedConfigurationService>();

            // Core Services
            services.AddSingleton<IRecentFileService, RecentFileService>();
            services.AddSingleton<AutoTool.Command.Interface.IVariableStore, AutoTool.Command.Class.VariableStore>();

            // Plugin Services
            services.AddSingleton<IPluginService, PluginService>();

            // Theme Services
            services.AddSingleton<IEnhancedThemeService, EnhancedThemeService>();

            // UI Services
            services.AddTransient<IMainWindowMenuService, MainWindowMenuService>();
            services.AddTransient<IMainWindowButtonService, MainWindowButtonService>();
            services.AddTransient<IEditPanelPropertyService, EditPanelPropertyService>();
            services.AddTransient<IEditPanelIntegrationService, EditPanelIntegrationService>();

            // Mouse Services
            services.AddSingleton<IMouseService, MouseService>();

            // Capture Services
            services.AddSingleton<ICaptureService, CaptureService>();

            // Window Services
            services.AddSingleton<AutoTool.Services.Window.IWindowInfoService, AutoTool.Services.Window.WindowInfoService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<ListPanelViewModel>();
            services.AddTransient<EditPanelViewModel>();
            services.AddTransient<ButtonPanelViewModel>();

            // Command List Item Factory
            services.AddSingleton<IUniversalCommandItemFactory, UniversalCommandItemFactory>();

            // Undo/Redo System
            services.AddSingleton<AutoTool.ViewModel.Shared.CommandHistoryManager>();

            // Dummy services for missing dependencies
            services.AddSingleton<IDataContextLocator, DataContextLocator>();

            return services;
        }
    }

    /// <summary>
    /// UniversalCommandItemファクトリーインターフェース
    /// </summary>
    public interface IUniversalCommandItemFactory
    {
        UniversalCommandItem? CreateItem(string itemType);
    }

    /// <summary>
    /// UniversalCommandItemファクトリー実装（DirectCommandRegistry統一版）
    /// </summary>
    public class UniversalCommandItemFactory : IUniversalCommandItemFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UniversalCommandItemFactory> _logger;

        public UniversalCommandItemFactory(IServiceProvider serviceProvider, ILogger<UniversalCommandItemFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public UniversalCommandItem? CreateItem(string itemType)
        {
            try
            {
                _logger.LogDebug("UniversalCommandItemFactory.CreateItem開始: {ItemType}", itemType);

                // DirectCommandRegistryを使用してUniversalCommandItem を作成
                try
                {
                    var universalItem = DirectCommandRegistry.CreateUniversalItem(itemType);
                    if (universalItem != null)
                    {
                        _logger.LogDebug("DirectCommandRegistryでUniversalCommandItem作成成功: {ItemType}", itemType);
                        return universalItem;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "DirectCommandRegistryでの作成失敗、UniversalCommandItemで直接作成: {ItemType}", itemType);
                }

                // フォールバック: UniversalCommandItem を直接作成
                var fallbackItem = new UniversalCommandItem
                {
                    ItemType = itemType,
                    IsEnable = true
                };

                _logger.LogDebug("UniversalCommandItem で作成成功: {ItemType}", itemType);
                return fallbackItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UniversalCommandItemFactory.CreateItem 中にエラー発生: {ItemType}", itemType);

                // 緊急フォールバック
                return new UniversalCommandItem
                {
                    ItemType = itemType,
                    IsEnable = true
                };
            }
        }
    }

    /// <summary>
    /// DataContextLocatorインターフェース
    /// </summary>
    public interface IDataContextLocator
    {
        T? GetDataContext<T>() where T : class;
        void SetDataContext<T>(T dataContext) where T : class;
    }

    /// <summary>
    /// DataContextLocator実装
    /// </summary>
    public class DataContextLocator : IDataContextLocator
    {
        private readonly Dictionary<Type, object> _dataContexts = new();

        public T? GetDataContext<T>() where T : class
        {
            _dataContexts.TryGetValue(typeof(T), out var dataContext);
            return dataContext as T;
        }

        public void SetDataContext<T>(T dataContext) where T : class
        {
            _dataContexts[typeof(T)] = dataContext;
        }
    }
}