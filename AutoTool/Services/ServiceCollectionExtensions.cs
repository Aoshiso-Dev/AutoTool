using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Plugin;
using AutoTool.Services.Mouse;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;
using AutoTool.Model.List.Type;
using AutoTool.Model.CommandDefinition;
using AutoTool.Model.List.Interface;
using AutoTool.Helpers;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using System;
using AutoTool.Services.UI;
using AutoTool.Services.Capture;
using AutoTool.Services.Configuration;

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
            services.AddSingleton<ICommandListItemFactory, CommandListItemFactory>();

            // Dummy services for missing dependencies
            services.AddSingleton<IDataContextLocator, DataContextLocator>();

            return services;
        }
    }

    /// <summary>
    /// CommandListItemファクトリーインターフェース
    /// </summary>
    public interface ICommandListItemFactory
    {
        ICommandListItem? CreateItem(string itemType);
    }

    /// <summary>
    /// CommandListItemファクトリー実装（DirectCommandRegistry統一版）
    /// </summary>
    public class CommandListItemFactory : ICommandListItemFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CommandListItemFactory> _logger;

        public CommandListItemFactory(IServiceProvider serviceProvider, ILogger<CommandListItemFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public ICommandListItem? CreateItem(string itemType)
        {
            try
            {
                _logger.LogDebug("CommandListItemFactory.CreateItem開始: {ItemType}", itemType);

                // DirectCommandRegistryを使用してUniversalCommandItemを作成
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
                    _logger.LogDebug(ex, "DirectCommandRegistryでの作成失敗、BasicCommandItemにフォールバック: {ItemType}", itemType);
                }

                // フォールバック：BasicCommandItem
                var basicItem = _serviceProvider.GetService<BasicCommandItem>();
                if (basicItem == null)
                {
                    basicItem = new BasicCommandItem();
                    _logger.LogWarning("BasicCommandItem を DI から取得できなかったため、直接作成しました");
                }

                basicItem.ItemType = itemType;
                basicItem.IsEnable = true;

                _logger.LogDebug("BasicCommandItem で作成完了: {ItemType}", itemType);
                return basicItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CommandListItemFactory.CreateItem 中にエラー発生: {ItemType}", itemType);

                // 緊急フォールバック
                return new BasicCommandItem
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