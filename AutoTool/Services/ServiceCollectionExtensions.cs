using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Plugin;
using AutoTool.Services.Mouse;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;
using AutoTool.List.Class;
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
            services.AddSingleton<CommandListService>();

            // Plugin Services
            services.AddSingleton<IPluginService, PluginService>();

            // Theme Services
            services.AddSingleton<IEnhancedThemeService, EnhancedThemeService>();

            // UI Services
            services.AddTransient<IMainWindowMenuService, MainWindowMenuService>();
            services.AddTransient<IMainWindowButtonService, MainWindowButtonService>();

            // Mouse Services
            services.AddSingleton<IMouseService, MouseService>();

            // Capture Services
            services.AddSingleton<ICaptureService, CaptureService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<ListPanelViewModel>();
            services.AddTransient<EditPanelViewModel>();

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
    /// CommandListItemファクトリー実装
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

                // 1. 動的システムでUniversalCommandItemを作成
                try
                {
                    var universalItem = DirectCommandRegistry.CreateUniversalItem(itemType);
                    if (universalItem != null)
                    {
                        _logger.LogDebug("動的システムでUniversalCommandItem作成成功: {ItemType}", itemType);
                        return universalItem;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "動的システムでの作成失敗、従来システムにフォールバック: {ItemType}", itemType);
                }

                // 2. 従来のCommandRegistry（後方互換性）
                var itemTypes = CommandRegistry.GetTypeMapping();
                if (itemTypes.TryGetValue(itemType, out var type))
                {
                    _logger.LogDebug("CommandRegistry から型取得: {Type}", type.Name);

                    // DI コンテナから インスタンス を取得を試行
                    var serviceInstance = _serviceProvider.GetService(type);
                    if (serviceInstance is ICommandListItem item)
                    {
                        item.ItemType = itemType;
                        item.IsEnable = true;

                        _logger.LogDebug("DI コンテナで作成成功: {ActualType}", item.GetType().Name);
                        return item;
                    }

                    // DI で取得できない場合は Activator で作成
                    if (Activator.CreateInstance(type) is ICommandListItem fallbackItem)
                    {
                        fallbackItem.ItemType = itemType;
                        fallbackItem.IsEnable = true;

                        _logger.LogDebug("Activator で作成成功: {ActualType}", fallbackItem.GetType().Name);
                        return fallbackItem;
                    }
                }

                _logger.LogWarning("CommandRegistry で作成失敗、BasicCommandItem で代用: {ItemType}", itemType);

                // 3. 最終フォールバック：BasicCommandItem
                var basicItem = _serviceProvider.GetService<BasicCommandItem>();
                if (basicItem == null)
                {
                    basicItem = new BasicCommandItem();
                    _logger.LogWarning("BasicCommandItem を DI から取得できなかったため、直接作成しました");
                }

                basicItem.ItemType = itemType;
                basicItem.IsEnable = true;

                _logger.LogDebug("BasicCommandItem で作成完了");
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
}