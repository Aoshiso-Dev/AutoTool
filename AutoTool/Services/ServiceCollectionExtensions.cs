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

            // ViewModelの登録（シングルトン）
            services.AddSingleton<EditPanelViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<FavoritePanelViewModel>();
            services.AddSingleton<ListPanelViewModel>();

            // モデルの登録
            services.AddTransient<CommandList>();
            services.AddTransient<BasicCommandItem>();

            // JSON設定の登録
            services.AddSingleton<JsonSerializerOptions>(provider =>
            {
                return new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
            });

            // CommandListItemファクトリーパターンの追加
            services.AddSingleton<ICommandListItemFactory, CommandListItemFactory>();

            // JsonSerializerHelperにロガーを設定
            services.AddSingleton<IServiceProvider>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("JsonSerializerHelper");
                JsonSerializerHelper.SetLogger(logger);
                return provider;
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