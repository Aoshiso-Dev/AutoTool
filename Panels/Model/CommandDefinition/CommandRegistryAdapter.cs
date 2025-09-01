using System;
using System.Collections.Generic;
using System.Linq;
using MacroPanels.Model.List.Interface;
using Microsoft.Extensions.Logging;

namespace MacroPanels.Model.CommandDefinition
{
    /// <summary>
    /// CommandRegistryの静的クラスをDI対応にするアダプター
    /// </summary>
    public class CommandRegistryAdapter : ICommandRegistry
    {
        private readonly ILogger<CommandRegistryAdapter> _logger;

        public CommandRegistryAdapter(ILogger<CommandRegistryAdapter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("CommandRegistryAdapterを初期化しています");
            Initialize();
        }

        public void Initialize()
        {
            try
            {
                _logger.LogDebug("CommandRegistryの初期化を開始します");
                CommandRegistry.Initialize();
                _logger.LogDebug("CommandRegistryの初期化が完了しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CommandRegistryの初期化中にエラーが発生しました");
                throw;
            }
        }

        public IEnumerable<string> GetAllTypeNames()
        {
            try
            {
                var typeNames = CommandRegistry.GetAllTypeNames();
                _logger.LogDebug("すべてのタイプ名を取得: {Count}件", typeNames.Count());
                return typeNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "タイプ名取得中にエラーが発生しました");
                return Enumerable.Empty<string>();
            }
        }

        public IEnumerable<string> GetOrderedTypeNames()
        {
            try
            {
                var typeNames = CommandRegistry.GetOrderedTypeNames();
                _logger.LogDebug("順序付けられたタイプ名を取得: {Count}件", typeNames.Count());
                return typeNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "順序付けタイプ名取得中にエラーが発生しました");
                return Enumerable.Empty<string>();
            }
        }

        public IEnumerable<string> GetTypeNamesByCategory(CommandCategory category)
        {
            try
            {
                var typeNames = CommandRegistry.GetTypeNamesByCategory(category);
                _logger.LogDebug("カテゴリ別タイプ名を取得: {Category}, {Count}件", category, typeNames.Count());
                return typeNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "カテゴリ別タイプ名取得中にエラーが発生しました: {Category}", category);
                return Enumerable.Empty<string>();
            }
        }

        public IEnumerable<string> GetTypeNamesByDisplayPriority(int priority)
        {
            try
            {
                var typeNames = CommandRegistry.GetTypeNamesByDisplayPriority(priority);
                _logger.LogDebug("優先度別タイプ名を取得: {Priority}, {Count}件", priority, typeNames.Count());
                return typeNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "優先度別タイプ名取得中にエラーが発生しました: {Priority}", priority);
                return Enumerable.Empty<string>();
            }
        }

        public ICommandListItem? CreateCommandItem(string typeName)
        {
            try
            {
                var item = CommandRegistry.CreateCommandItem(typeName);
                if (item != null)
                {
                    _logger.LogDebug("コマンドアイテムを作成しました: {TypeName}", typeName);
                }
                else
                {
                    _logger.LogWarning("コマンドアイテムの作成に失敗しました: {TypeName}", typeName);
                }
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンドアイテム作成中にエラーが発生しました: {TypeName}", typeName);
                return null;
            }
        }

        public bool IsIfCommand(string typeName)
        {
            try
            {
                return CommandRegistry.IsIfCommand(typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ifコマンド判定中にエラーが発生しました: {TypeName}", typeName);
                return false;
            }
        }

        public bool IsLoopCommand(string typeName)
        {
            try
            {
                return CommandRegistry.IsLoopCommand(typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ループコマンド判定中にエラーが発生しました: {TypeName}", typeName);
                return false;
            }
        }

        public bool IsEndCommand(string typeName)
        {
            try
            {
                return CommandRegistry.IsEndCommand(typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "終了コマンド判定中にエラーが発生しました: {TypeName}", typeName);
                return false;
            }
        }

        public bool IsStartCommand(string typeName)
        {
            try
            {
                return CommandRegistry.IsStartCommand(typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "開始コマンド判定中にエラーが発生しました: {TypeName}", typeName);
                return false;
            }
        }

        public Type? GetItemType(string typeName)
        {
            try
            {
                return CommandRegistry.GetItemType(typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテムタイプ取得中にエラーが発生しました: {TypeName}", typeName);
                return null;
            }
        }

        public IEnumerable<CommandDefinitionItem> GetCommandDefinitions()
        {
            try
            {
                var definitions = GetOrderedTypeNames()
                    .Select(typeName => new CommandDefinitionItem
                    {
                        TypeName = typeName,
                        DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = GetCategoryFromPriority(CommandRegistry.DisplayOrder.GetPriority(typeName)),
                        Description = CommandRegistry.DisplayOrder.GetDescription(typeName),
                        Priority = CommandRegistry.DisplayOrder.GetPriority(typeName),
                        SubPriority = CommandRegistry.DisplayOrder.GetSubPriority(typeName),
                        IsIfCommand = IsIfCommand(typeName),
                        IsLoopCommand = IsLoopCommand(typeName)
                    })
                    .ToList();

                _logger.LogDebug("コマンド定義一覧を取得: {Count}件", definitions.Count);
                return definitions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド定義一覧取得中にエラーが発生しました");
                return Enumerable.Empty<CommandDefinitionItem>();
            }
        }

        private static CommandCategory GetCategoryFromPriority(int priority)
        {
            return priority switch
            {
                1 => CommandCategory.Action,   // クリック操作
                2 => CommandCategory.Action,   // 基本操作
                3 => CommandCategory.Control,  // ループ制御
                4 => CommandCategory.Control,  // 条件制御
                5 => CommandCategory.Variable, // 変数操作
                6 => CommandCategory.System,   // システム操作
                _ => CommandCategory.Action    // その他
            };
        }
    }
}