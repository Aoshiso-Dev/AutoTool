using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.List.Class;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MacroPanels.Model.List.Interface;
using MacroPanels.Model.CommandDefinition;
using MouseHelper;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Reflection;
using MacroPanels.Plugin;

namespace MacroPanels.Model.MacroFactory
{
    /// <summary>
    /// マクロファクトリ（属性ベース実装版）
    /// </summary>
    public static class MacroFactory
    {
        private static ILogger? _logger;
        private static IPluginService? _pluginService;
        private static readonly Dictionary<string, CommandDefinitionAttribute> _commandDefinitions = new();
        private static readonly Dictionary<Type, CommandDefinitionAttribute> _itemTypeToDefinition = new();

        static MacroFactory()
        {
            InitializeCommandDefinitions();
        }

        /// <summary>
        /// プラグインサービスを設定
        /// </summary>
        public static void SetPluginService(IPluginService pluginService)
        {
            _pluginService = pluginService;
            _logger?.LogInformation("プラグインサービスが設定されました");
        }

        /// <summary>
        /// CommandDefinition属性からコマンド定義を初期化
        /// </summary>
        private static void InitializeCommandDefinitions()
        {
            try
            {
                // 現在のアセンブリから CommandDefinition 属性を持つクラスを検索
                var assembly = Assembly.GetExecutingAssembly();
                var itemTypes = assembly.GetTypes()
                    .Where(t => typeof(ICommandListItem).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToList();

                foreach (var itemType in itemTypes)
                {
                    var attr = itemType.GetCustomAttribute<CommandDefinitionAttribute>();
                    if (attr != null)
                    {
                        _commandDefinitions[attr.TypeName] = attr;
                        _itemTypeToDefinition[itemType] = attr;
                        _logger?.LogDebug("コマンド定義登録: {ItemType} -> {CommandType}", attr.TypeName, attr.CommandType.Name);
                    }
                }

                _logger?.LogInformation("コマンド定義初期化完了: {Count}個の定義を登録", _commandDefinitions.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "コマンド定義の初期化に失敗しました");
            }
        }

        /// <summary>
        /// ロガーを設定（オプション）
        /// </summary>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// コマンドリストからマクロを作成
        /// </summary>
        public static ICommand CreateMacro(IEnumerable<ICommandListItem> items)
        {
            try
            {
                _logger?.LogDebug("マクロ作成を開始します");

                var itemList = items?.ToList() ?? new List<ICommandListItem>();
                if (!itemList.Any())
                {
                    _logger?.LogWarning("アイテムリストが空です");
                    return new RootCommand();
                }

                // ルートコマンドとしてLoopCommandを作成（1回実行）
                var rootLoop = new LoopCommand(null, new LoopCommandSettings { LoopCount = 1 });
                
                // 子コマンドを作成
                var childCommands = CreateCommandsFromItems(rootLoop, itemList);
                foreach (var command in childCommands)
                {
                    rootLoop.AddChild(command);
                }

                _logger?.LogInformation("マクロ作成完了: {Count}個のコマンドを生成", childCommands.Count());
                return rootLoop;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "マクロ作成中にエラーが発生しました");
                throw new InvalidOperationException("マクロの作成に失敗しました", ex);
            }
        }

        /// <summary>
        /// アイテムリストからコマンドリストを作成
        /// </summary>
        private static IEnumerable<ICommand> CreateCommandsFromItems(ICommand parent, IList<ICommandListItem> items)
        {
            var commands = new List<ICommand>();
            var processedIndices = new HashSet<int>();

            for (int i = 0; i < items.Count; i++)
            {
                if (processedIndices.Contains(i))
                    continue;

                var item = items[i];
                if (!item.IsEnable)
                {
                    _logger?.LogDebug("無効なアイテムをスキップ: {ItemType} (行 {LineNumber})", item.ItemType, item.LineNumber);
                    continue;
                }

                try
                {
                    var command = CreateSingleCommand(parent, item, items, i, processedIndices);
                    if (command != null)
                    {
                        commands.Add(command);
                        _logger?.LogDebug("コマンド作成成功: {CommandType} (行 {LineNumber})", 
                            command.GetType().Name, item.LineNumber);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "コマンド作成エラー: {ItemType} (行 {LineNumber})", 
                        item.ItemType, item.LineNumber);
                    throw new InvalidOperationException($"コマンド '{item.ItemType}' (行 {item.LineNumber}) の作成に失敗しました", ex);
                }
            }

            return commands;
        }

        /// <summary>
        /// 単一のコマンドを作成
        /// </summary>
        private static ICommand? CreateSingleCommand(ICommand parent, ICommandListItem item, 
            IList<ICommandListItem> allItems, int currentIndex, HashSet<int> processedIndices)
        {
            _logger?.LogDebug("コマンド作成開始: {ItemType} (行 {LineNumber})", item.ItemType, item.LineNumber);

            // プラグインコマンドかチェック
            if (IsPluginCommand(item.ItemType))
            {
                return CreatePluginCommand(parent, item);
            }

            // 複合コマンド（If文、ループ）の処理
            switch (item)
            {
                case IIfItem ifItem:
                    return CreateIfCommand(parent, ifItem, allItems, currentIndex, processedIndices);
                
                case ILoopItem loopItem:
                    return CreateLoopCommand(parent, loopItem, allItems, currentIndex, processedIndices);
                
                case IIfEndItem:
                case ILoopEndItem:
                    // 終了コマンドは親コマンドで処理済みなのでスキップ
                    return null;
                
                default:
                    // 単純コマンドの作成（属性ベース）
                    return CreateSimpleCommandFromAttributes(parent, item);
            }
        }

        /// <summary>
        /// プラグインコマンドかどうかを判定
        /// </summary>
        private static bool IsPluginCommand(string itemType)
        {
            // プラグインコマンドは "PluginId.CommandId" の形式
            return itemType.Contains('.') && _pluginService != null;
        }

        /// <summary>
        /// プラグインコマンドを作成
        /// </summary>
        private static ICommand? CreatePluginCommand(ICommand parent, ICommandListItem item)
        {
            if (_pluginService == null)
            {
                _logger?.LogWarning("プラグインサービスが設定されていません");
                return null;
            }

            try
            {
                // プラグインコマンドIDを分割 ("PluginId.CommandId")
                var parts = item.ItemType.Split('.', 2);
                if (parts.Length != 2)
                {
                    _logger?.LogWarning("無効なプラグインコマンド形式: {ItemType}", item.ItemType);
                    return null;
                }

                var pluginId = parts[0];
                var commandId = parts[1];

                _logger?.LogDebug("プラグインコマンド作成: {PluginId}.{CommandId}", pluginId, commandId);

                var command = _pluginService.CreatePluginCommand(pluginId, commandId, parent, item);
                if (command != null)
                {
                    // 基本プロパティを設定
                    command.LineNumber = item.LineNumber;
                    command.IsEnabled = item.IsEnable;
                    
                    _logger?.LogDebug("プラグインコマンド作成成功: {PluginId}.{CommandId}", pluginId, commandId);
                }
                else
                {
                    _logger?.LogWarning("プラグインコマンド作成失敗: {PluginId}.{CommandId}", pluginId, commandId);
                }

                return command;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "プラグインコマンド作成エラー: {ItemType}", item.ItemType);
                return null;
            }
        }

        /// <summary>
        /// If文コマンドを作成
        /// </summary>
        private static ICommand CreateIfCommand(ICommand parent, IIfItem ifItem, 
            IList<ICommandListItem> allItems, int startIndex, HashSet<int> processedIndices)
        {
            if (ifItem.Pair == null)
                throw new InvalidOperationException($"If文 (行 {ifItem.LineNumber}) に対応するEndIfがありません");

            var endLineNumber = ifItem.Pair.LineNumber;
            
            // If文内の子アイテムを取得
            var childItems = allItems
                .Where((item, index) => index > startIndex && 
                                      item.LineNumber > ifItem.LineNumber && 
                                      item.LineNumber < endLineNumber)
                .ToList();

            // If文コマンドを作成（属性ベース）
            var ifCommand = CreateSimpleCommandFromAttributes(parent, ifItem);
            
            // 子コマンドを作成して追加
            var childCommands = CreateCommandsFromItems(ifCommand, childItems);
            foreach (var childCommand in childCommands)
            {
                ifCommand.AddChild(childCommand);
            }

            // 処理済みインデックスをマーク
            for (int i = startIndex; i < allItems.Count; i++)
            {
                if (allItems[i].LineNumber > ifItem.LineNumber && allItems[i].LineNumber <= endLineNumber)
                {
                    processedIndices.Add(i);
                }
            }

            _logger?.LogDebug("If文コマンド作成完了: {Count}個の子コマンド", childCommands.Count());
            return ifCommand;
        }

        /// <summary>
        /// ループコマンドを作成
        /// </summary>
        private static ICommand CreateLoopCommand(ICommand parent, ILoopItem loopItem, 
            IList<ICommandListItem> allItems, int startIndex, HashSet<int> processedIndices)
        {
            if (loopItem.Pair == null)
                throw new InvalidOperationException($"ループ (行 {loopItem.LineNumber}) に対応するEndLoopがありません");

            var endLineNumber = loopItem.Pair.LineNumber;
            
            // ループ内の子アイテムを取得
            var childItems = allItems
                .Where((item, index) => index > startIndex && 
                                      item.LineNumber > loopItem.LineNumber && 
                                      item.LineNumber < endLineNumber)
                .ToList();

            // ループコマンドを作成（属性ベース）
            var loopCommand = CreateSimpleCommandFromAttributes(parent, loopItem);

            // 子コマンドを作成して追加
            var childCommands = CreateCommandsFromItems(loopCommand, childItems);
            foreach (var childCommand in childCommands)
            {
                loopCommand.AddChild(childCommand);
            }

            // 処理済みインデックスをマーク
            for (int i = startIndex; i < allItems.Count; i++)
            {
                if (allItems[i].LineNumber > loopItem.LineNumber && allItems[i].LineNumber <= endLineNumber)
                {
                    processedIndices.Add(i);
                }
            }

            _logger?.LogDebug("ループコマンド作成完了: {ChildCount}個の子コマンド", childCommands.Count());
            return loopCommand;
        }

        /// <summary>
        /// CommandDefinition属性を使用してコマンドを作成
        /// </summary>
        private static ICommand CreateSimpleCommandFromAttributes(ICommand parent, ICommandListItem item)
        {
            // 1. アイテムの型から属性を取得
            if (!_itemTypeToDefinition.TryGetValue(item.GetType(), out var definition))
            {
                // 2. ItemTypeからも検索
                if (!_commandDefinitions.TryGetValue(item.ItemType, out definition))
                {
                    throw new NotSupportedException($"未対応のアイテム型です: {item.GetType().Name} (ItemType: {item.ItemType})");
                }
            }

            try
            {
                _logger?.LogDebug("コマンド作成開始: {ItemType} -> {CommandType}, 設定型: {SettingsType}", 
                    item.ItemType, definition.CommandType.Name, definition.SettingsType.Name);

                // 3. CommandDefinition属性を使用してコマンドを作成
                var command = CreateCommandFromDefinition(parent, item, definition);
                
                // 4. 基本プロパティを設定
                command.LineNumber = item.LineNumber;
                command.IsEnabled = item.IsEnable;
                
                // 5. 設定内容をログ出力（デバッグ用）
                LogCommandSettings(command, item);
                
                _logger?.LogDebug("属性ベースコマンド作成成功: {ItemType} -> {CommandType}", 
                    item.ItemType, definition.CommandType.Name);
                
                return command;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "属性ベースコマンド作成エラー: {ItemType}", item.ItemType);
                throw new InvalidOperationException($"コマンド作成に失敗しました: {item.ItemType}", ex);
            }
        }

        /// <summary>
        /// コマンドの設定内容をログ出力（デバッグ用）
        /// </summary>
        private static void LogCommandSettings(ICommand command, ICommandListItem item)
        {
            if (_logger == null || !_logger.IsEnabled(LogLevel.Trace)) return;

            try
            {
                var settings = command.Settings;
                if (settings != null)
                {
                    var settingsType = settings.GetType();
                    var properties = settingsType.GetProperties()
                        .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                        .Take(5); // 最初の5つのプロパティのみ表示

                    foreach (var prop in properties)
                    {
                        try
                        {
                            var value = prop.GetValue(settings);
                            _logger.LogTrace("設定: {PropertyName} = {Value} (コマンド: {CommandType})", 
                                prop.Name, value ?? "null", command.GetType().Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogTrace(ex, "設定取得エラー: {PropertyName} (コマンド: {CommandType})", 
                                prop.Name, command.GetType().Name);
                        }
                    }
                }
                else
                {
                    _logger.LogTrace("設定がnullです (コマンド: {CommandType})", command.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "設定ログ出力中にエラー (コマンド: {CommandType})", command.GetType().Name);
            }
        }

        /// <summary>
        /// CommandDefinition属性からコマンドインスタンスを作成
        /// </summary>
        private static ICommand CreateCommandFromDefinition(ICommand parent, ICommandListItem item, CommandDefinitionAttribute definition)
        {
            // 1. コンストラクタを取得（親、設定オブジェクト）
            var constructor = definition.CommandType.GetConstructor(new[] { typeof(ICommand), typeof(object) });
            if (constructor == null)
            {
                throw new InvalidOperationException($"コマンド型 {definition.CommandType.Name} に適切なコンストラクタが見つかりません");
            }

            // 2. 設定オブジェクトを準備
            object? settings = null;
            
            // アイテムが設定インターフェースを実装している場合は、そのアイテム自体を設定として使用
            if (definition.SettingsType.IsInstanceOfType(item))
            {
                settings = item;
                _logger?.LogDebug("アイテム自体を設定として使用: {ItemType} -> {SettingsType}", 
                    item.GetType().Name, definition.SettingsType.Name);
            }
            else
            {
                // フォールバック：アイテムのプロパティを設定オブジェクトにコピー
                settings = CreateAndCopySettings(item, definition.SettingsType);
                _logger?.LogDebug("設定オブジェクトを作成してコピー: {ItemType} -> {SettingsType}", 
                    item.GetType().Name, definition.SettingsType.Name);
            }

            // 3. コマンドインスタンスを作成
            var command = (ICommand)constructor.Invoke(new object?[] { parent, settings });
            
            return command;
        }

        /// <summary>
        /// アイテムのプロパティを設定オブジェクトにコピー
        /// </summary>
        private static object CreateAndCopySettings(ICommandListItem item, Type settingsInterfaceType)
        {
            // 基本設定オブジェクトを作成
            var settings = CreateBasicSettings(settingsInterfaceType);
            
            try
            {
                // アイテムのプロパティを設定オブジェクトにコピー
                var itemType = item.GetType();
                var settingsType = settings.GetType();
                
                // 設定インターフェースのプロパティを取得
                var interfaceProperties = settingsInterfaceType.GetProperties();
                
                foreach (var interfaceProp in interfaceProperties)
                {
                    // アイテムに同名のプロパティがあるかチェック
                    var itemProp = itemType.GetProperty(interfaceProp.Name);
                    var settingsProp = settingsType.GetProperty(interfaceProp.Name);
                    
                    if (itemProp != null && settingsProp != null && 
                        itemProp.CanRead && settingsProp.CanWrite)
                    {
                        try
                        {
                            var value = itemProp.GetValue(item);
                            settingsProp.SetValue(settings, value);
                            _logger?.LogTrace("プロパティコピー: {PropertyName} = {Value}", 
                                interfaceProp.Name, value);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "プロパティコピー失敗: {PropertyName}", interfaceProp.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "設定オブジェクトのプロパティコピー中にエラーが発生しました");
            }
            
            return settings;
        }

        /// <summary>
        /// 基本設定オブジェクトを作成
        /// </summary>
        private static object CreateBasicSettings(Type settingsInterfaceType)
        {
            // 設定インターフェースに応じて適切な設定オブジェクトを作成
            object settings = settingsInterfaceType.Name switch
            {
                nameof(IWaitImageCommandSettings) => new WaitImageCommandSettings(),
                nameof(IClickImageCommandSettings) => new ClickImageCommandSettings(),
                nameof(IHotkeyCommandSettings) => new HotkeyCommandSettings(),
                nameof(IClickCommandSettings) => new ClickCommandSettings(),
                nameof(IWaitCommandSettings) => new WaitCommandSettings(),
                nameof(ILoopCommandSettings) => new LoopCommandSettings(),
                nameof(IIfImageCommandSettings) => new IfImageCommandSettings(),
                nameof(IIfImageExistAISettings) => new IfImageExistAISettings(),
                nameof(IIfImageNotExistAISettings) => new IfImageNotExistAISettings(),
                nameof(IIfVariableCommandSettings) => new IfVariableCommandSettings(),
                nameof(IExecuteCommandSettings) => new ExecuteCommandSettings(),
                nameof(ISetVariableCommandSettings) => new SetVariableCommandSettings(),
                nameof(ISetVariableAICommandSettings) => new SetVariableAICommandSettings(),
                nameof(IScreenshotCommandSettings) => new ScreenshotCommandSettings(),
                nameof(IClickImageAICommandSettings) => new ClickImageAICommandSettings(),
                _ => new object() // フォールバック
            };

            _logger?.LogTrace("設定オブジェクト作成: {InterfaceType} -> {SettingsType}", 
                settingsInterfaceType.Name, settings.GetType().Name);
            
            return settings;
        }

        #region ヘルパーメソッド

        /// <summary>
        /// 登録されているコマンド定義を取得
        /// </summary>
        public static IReadOnlyDictionary<string, CommandDefinitionAttribute> GetCommandDefinitions()
        {
            return _commandDefinitions;
        }

        /// <summary>
        /// ItemTypeからCommandDefinitionを取得
        /// </summary>
        public static CommandDefinitionAttribute? GetCommandDefinition(string itemType)
        {
            return _commandDefinitions.TryGetValue(itemType, out var definition) ? definition : null;
        }

        /// <summary>
        /// アイテム型からCommandDefinitionを取得
        /// </summary>
        public static CommandDefinitionAttribute? GetCommandDefinition(Type itemType)
        {
            return _itemTypeToDefinition.TryGetValue(itemType, out var definition) ? definition : null;
        }

        /// <summary>
        /// 利用可能なプラグインコマンドを取得
        /// </summary>
        public static IEnumerable<IPluginCommandInfo> GetAvailablePluginCommands()
        {
            return _pluginService?.GetAvailablePluginCommands() ?? Enumerable.Empty<IPluginCommandInfo>();
        }

        /// <summary>
        /// マウスボタンアクションを実行（互換性のため残す）
        /// </summary>
        public static async Task ExecuteMouseButtonAction(System.Windows.Input.MouseButton button, int x, int y, string windowTitle, string windowClassName)
        {
            try
            {
                switch (button)
                {
                    case System.Windows.Input.MouseButton.Left:
                        await MouseHelper.Input.ClickAsync(x, y, windowTitle, windowClassName);
                        break;
                    case System.Windows.Input.MouseButton.Right:
                        await MouseHelper.Input.RightClickAsync(x, y, windowTitle, windowClassName);
                        break;
                    case System.Windows.Input.MouseButton.Middle:
                        await MouseHelper.Input.MiddleClickAsync(x, y, windowTitle, windowClassName);
                        break;
                    default:
                        throw new ArgumentException("サポートされていないマウスボタンです。");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "マウスアクション実行エラー");
                throw;
            }
        }

        #endregion
    }

    #region 設定クラス（軽量版）

    /// <summary>
    /// 基本コマンド設定（フォールバック用）
    /// </summary>
    public class BasicCommandSettings : ICommandSettings
    {
        // 基本的な設定のみ
    }

    // 各設定クラスの基本実装（属性ベースで自動生成されるため簡略化）
    public class WaitImageCommandSettings : IWaitImageCommandSettings
    {
        public string ImagePath { get; set; } = "";
        public double Threshold { get; set; } = 0.8;
        public System.Windows.Media.Color? SearchColor { get; set; }
        public int Timeout { get; set; } = 5000;
        public int Interval { get; set; } = 500;
        public string WindowTitle { get; set; } = "";
        public string WindowClassName { get; set; } = "";
    }

    public class ClickImageCommandSettings : IClickImageCommandSettings
    {
        public string ImagePath { get; set; } = "";
        public double Threshold { get; set; } = 0.8;
        public System.Windows.Media.Color? SearchColor { get; set; }
        public int Timeout { get; set; } = 5000;
        public int Interval { get; set; } = 500;
        public System.Windows.Input.MouseButton Button { get; set; } = System.Windows.Input.MouseButton.Left;
        public string WindowTitle { get; set; } = "";
        public string WindowClassName { get; set; } = "";
    }

    public class HotkeyCommandSettings : IHotkeyCommandSettings
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public System.Windows.Input.Key Key { get; set; } = System.Windows.Input.Key.Escape;
        public string WindowTitle { get; set; } = "";
        public string WindowClassName { get; set; } = "";
    }

    public class ClickCommandSettings : IClickCommandSettings
    {
        public int X { get; set; }
        public int Y { get; set; }
        public System.Windows.Input.MouseButton Button { get; set; } = System.Windows.Input.MouseButton.Left;
        public string WindowTitle { get; set; } = "";
        public string WindowClassName { get; set; } = "";
    }

    public class WaitCommandSettings : IWaitCommandSettings
    {
        public int Wait { get; set; } = 1000;
    }

    public class ExecuteCommandSettings : IExecuteCommandSettings
    {
        public string ProgramPath { get; set; } = "";
        public string Arguments { get; set; } = "";
        public string WorkingDirectory { get; set; } = "";
        public bool WaitForExit { get; set; }
    }

    public class SetVariableCommandSettings : ISetVariableCommandSettings
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public class SetVariableAICommandSettings : ISetVariableAICommandSettings
    {
        public string WindowTitle { get; set; } = "";
        public string WindowClassName { get; set; } = "";
        public string AIDetectMode { get; set; } = "Class";
        public string ModelPath { get; set; } = "";
        public double ConfThreshold { get; set; } = 0.5;
        public double IoUThreshold { get; set; } = 0.25;
        public string Name { get; set; } = "";
    }

    public class ScreenshotCommandSettings : IScreenshotCommandSettings
    {
        public string SaveDirectory { get; set; } = "";
        public string WindowTitle { get; set; } = "";
        public string WindowClassName { get; set; } = "";
    }

    public class ClickImageAICommandSettings : IClickImageAICommandSettings
    {
        public string WindowTitle { get; set; } = "";
        public string WindowClassName { get; set; } = "";
        public string ModelPath { get; set; } = "";
        public int ClassID { get; set; }
        public double ConfThreshold { get; set; } = 0.5;
        public double IoUThreshold { get; set; } = 0.25;
        public System.Windows.Input.MouseButton Button { get; set; } = System.Windows.Input.MouseButton.Left;
    }

    public class IfImageCommandSettings : IIfImageCommandSettings
    {
        public string ImagePath { get; set; } = "";
        public double Threshold { get; set; } = 0.8;
        public System.Windows.Media.Color? SearchColor { get; set; }
        public string WindowTitle { get; set; } = "";
        public string WindowClassName { get; set; } = "";
    }

    public class IfImageExistAISettings : IIfImageExistAISettings
    {
        public string ModelPath { get; set; } = "";
        public int ClassID { get; set; }
        public double ConfThreshold { get; set; } = 0.5;
        public double IoUThreshold { get; set; } = 0.25;
        public string WindowTitle { get; set; } = "";
        public string WindowClassName { get; set; } = "";
    }

    public class IfImageNotExistAISettings : IIfImageNotExistAISettings
    {
        public string ModelPath { get; set; } = "";
        public int ClassID { get; set; }

        public double ConfThreshold { get; set; } = 0.5;
        public double IoUThreshold { get; set; } = 0.25;
        public string WindowTitle { get; set; } = "";
        public string WindowClassName { get; set; } = "";
    }

    public class IfVariableCommandSettings : IIfVariableCommandSettings
    {
        public string Name { get; set; } = "";
        public string Operator { get; set; } = "==";
        public string Value { get; set; } = "";
    }

    public class LoopCommandSettings : ILoopCommandSettings
    {
        public int LoopCount { get; set; } = 1;
        public ICommand? Pair { get; set; }
    }

    #endregion
}
