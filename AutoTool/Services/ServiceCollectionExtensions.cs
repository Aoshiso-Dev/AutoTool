using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Plugin;
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

            // プラグインサービス
            services.AddSingleton<IPluginService, PluginService>();

            // ファイルサービス
            services.AddSingleton<IRecentFileService, RecentFileService>();

            // 変数ストアサービス（AutoTool版を登録）
            services.AddSingleton<AutoTool.Services.IVariableStore, AutoTool.Services.VariableStore>();

            // マクロ実行サービス
            services.AddSingleton<AutoTool.Services.Execution.IMacroExecutionService, AutoTool.Services.Execution.MacroExecutionService>();

            // 設定管理サービス
            services.AddSingleton<AutoTool.Services.Configuration.IEnhancedConfigurationService, AutoTool.Services.Configuration.EnhancedConfigurationService>();

            // UI関連サービス（View-ViewModel DI対応）
            services.AddSingleton<AutoTool.Services.UI.IDataContextLocator, AutoTool.Services.UI.DataContextLocator>();
            services.AddSingleton<AutoTool.Services.UI.IUIStateService, AutoTool.Services.UI.UIStateService>();
            services.AddSingleton<AutoTool.Services.UI.IWindowSettingsService, AutoTool.Services.UI.WindowSettingsService>();
            services.AddSingleton<AutoTool.Services.UI.IEnhancedThemeService, AutoTool.Services.UI.EnhancedThemeService>();
            services.AddSingleton<AutoTool.Services.UI.IAppSettingsService, AutoTool.Services.UI.AppSettingsService>();
            
            // EditPanelPropertyService (廃止予定) - 後方互換性のため一時的に保持
            services.AddSingleton<AutoTool.Services.UI.IEditPanelPropertyService, AutoTool.Services.UI.EditPanelPropertyService>();
            
            // MainWindow関連サービス
            services.AddSingleton<AutoTool.Services.UI.IMainWindowMenuService, AutoTool.Services.UI.MainWindowMenuService>();
            services.AddSingleton<AutoTool.Services.UI.IMainWindowButtonService, AutoTool.Services.UI.MainWindowButtonService>();

            // アプリケーション初期化サービス
            services.AddSingleton<AutoTool.Bootstrap.IApplicationBootstrapper, AutoTool.Bootstrap.ApplicationBootstrapper>();

            // ViewModel登録（シングルトン） - 標準MVVM方式に統一
            services.AddSingleton<EditPanelViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<FavoritePanelViewModel>();
            services.AddSingleton<ListPanelViewModel>();

            // モデル登録
            services.AddTransient<CommandList>();
            services.AddTransient<BasicCommandItem>();

            // JSON設定登録
            services.AddSingleton<JsonSerializerOptions>(provider =>
            {
                return new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
            });

            // CommandListItemファクトリーパターン
            services.AddSingleton<ICommandListItemFactory, CommandListItemFactory>();

            // JsonSerializerHelperにロガーを設定（循環参照を避けるため初期化時に処理）
            services.AddSingleton<Action<IServiceProvider>>(provider =>
            {
                return (sp) =>
                {
                    try
                    {
                        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger("JsonSerializerHelper");
                        JsonSerializerHelper.SetLogger(logger);
                        
                        // CommandFactoryにもServiceProviderを設定
                        AutoTool.Command.Class.CommandFactory.SetServiceProvider(sp);
                    }
                    catch (Exception ex)
                    {
                        // フォールバック: コンソールに出力
                        Console.WriteLine($"JsonSerializerHelper初期化エラー: {ex.Message}");
                    }
                };
            });

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
                
                // CommandRegistryから型マッピングを取得
                var itemTypes = CommandRegistry.GetTypeMapping();
                if (itemTypes.TryGetValue(itemType, out var type))
                {
                    _logger.LogDebug("CommandRegistryから型取得: {Type}", type.Name);
                    
                    // DIコンテナからインスタンスを取得を試行
                    var serviceInstance = _serviceProvider.GetService(type);
                    if (serviceInstance is ICommandListItem item)
                    {
                        item.ItemType = itemType;
                        item.IsEnable = true;
                        
                        _logger.LogDebug("DIコンテナで作成成功: {ActualType}", item.GetType().Name);
                        return item;
                    }
                    
                    // DIで取得できない場合はActivatorで作成
                    if (Activator.CreateInstance(type) is ICommandListItem fallbackItem)
                    {
                        fallbackItem.ItemType = itemType;
                        fallbackItem.IsEnable = true;
                        
                        _logger.LogDebug("Activatorで作成成功: {ActualType}", fallbackItem.GetType().Name);
                        return fallbackItem;
                    }
                }

                _logger.LogWarning("CommandRegistryで作成失敗、BasicCommandItemで代替: {ItemType}", itemType);

                // 最終フォールバック：BasicCommandItem
                var basicItem = _serviceProvider.GetService<BasicCommandItem>();
                if (basicItem == null)
                {
                    basicItem = new BasicCommandItem();
                    _logger.LogWarning("BasicCommandItemもDIから取得できなかったため、直接作成しました");
                }
                
                basicItem.ItemType = itemType;
                basicItem.IsEnable = true;
                
                _logger.LogDebug("BasicCommandItemで作成完了");
                return basicItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CommandListItemFactory.CreateItem中にエラー発生: {ItemType}", itemType);
                
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