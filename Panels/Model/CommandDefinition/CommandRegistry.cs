using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MacroPanels.List.Class;
using MacroPanels.Command.Class;
using MacroPanels.Command.Interface;
using MacroPanels.Model.List.Interface;

namespace MacroPanels.Model.CommandDefinition
{
    /// <summary>
    /// コマンド定義から自動的にタイプやファクトリを生成するクラス
    /// </summary>
    public static class CommandRegistry
    {
        private static readonly Dictionary<string, CommandInfo> _commands = new();
        private static bool _initialized = false;

        /// <summary>
        /// コマンド情報
        /// </summary>
        private class CommandInfo
        {
            public string TypeName { get; set; } = string.Empty;
            public Type ItemType { get; set; } = null!;
            public Type CommandType { get; set; } = null!;
            public Type SettingsType { get; set; } = null!;
            public CommandCategory Category { get; set; }
            public bool IsIfCommand { get; set; }
            public bool IsLoopCommand { get; set; }
            public Func<ICommandListItem> ItemFactory { get; set; } = null!;
        }

        /// <summary>
        /// 初期化（アプリ起動時に1回だけ実行）
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            // アセンブリ内のCommandDefinition属性が付いたクラスを探す
            var assembly = Assembly.GetExecutingAssembly();
            var commandTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<CommandDefinitionAttribute>() != null)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"CommandRegistry: Found {commandTypes.Count} command types");

            foreach (var type in commandTypes)
            {
                var attr = type.GetCustomAttribute<CommandDefinitionAttribute>()!;
                var hasSimpleBinding = type.GetCustomAttribute<MacroPanels.Model.CommandDefinition.SimpleCommandBindingAttribute>() != null;
                
                // ファクトリメソッドを作成
                var factory = CreateFactory(type);
                
                var commandInfo = new CommandInfo
                {
                    TypeName = attr.TypeName,
                    ItemType = type,
                    CommandType = attr.CommandType,
                    SettingsType = attr.SettingsType,
                    Category = attr.Category,
                    IsIfCommand = attr.IsIfCommand,
                    IsLoopCommand = attr.IsLoopCommand,
                    ItemFactory = factory
                };

                _commands[attr.TypeName] = commandInfo;
                System.Diagnostics.Debug.WriteLine($"CommandRegistry: Registered {attr.TypeName} -> {type.Name} (SimpleBinding: {hasSimpleBinding})");
            }

            _initialized = true;
            System.Diagnostics.Debug.WriteLine($"CommandRegistry: Initialization complete with {_commands.Count} commands");
        }

        /// <summary>
        /// ファクトリメソッドを作成
        /// </summary>
        private static Func<ICommandListItem> CreateFactory(Type itemType)
        {
            return () => (ICommandListItem)Activator.CreateInstance(itemType)!;
        }

        /// <summary>
        /// 全てのコマンドタイプ名を取得
        /// </summary>
        public static IEnumerable<string> GetAllTypeNames()
        {
            Initialize();
            return _commands.Keys;
        }

        /// <summary>
        /// カテゴリ別のコマンドタイプ名を取得
        /// </summary>
        public static IEnumerable<string> GetTypeNamesByCategory(CommandCategory category)
        {
            Initialize();
            return _commands.Values
                .Where(c => c.Category == category)
                .Select(c => c.TypeName);
        }

        /// <summary>
        /// コマンドアイテムを作成
        /// </summary>
        public static ICommandListItem? CreateCommandItem(string typeName)
        {
            Initialize();
            
            if (_commands.TryGetValue(typeName, out var info))
            {
                var item = info.ItemFactory();
                item.ItemType = typeName;
                return item;
            }
            
            return null;
        }

        /// <summary>
        /// If系コマンドかどうか判定
        /// </summary>
        public static bool IsIfCommand(string typeName)
        {
            Initialize();
            return _commands.TryGetValue(typeName, out var info) && info.IsIfCommand;
        }

        /// <summary>
        /// ループ系コマンドかどうか判定
        /// </summary>
        public static bool IsLoopCommand(string typeName)
        {
            Initialize();
            return _commands.TryGetValue(typeName, out var info) && info.IsLoopCommand;
        }

        /// <summary>
        /// 指定されたタイプからコマンドを作成（SimpleCommandBinding用）
        /// </summary>
        public static bool TryCreateSimple(ICommand parent, ICommandListItem item, out ICommand? command)
        {
            Initialize();
            command = null;

            System.Diagnostics.Debug.WriteLine($"TryCreateSimple: ItemType={item.ItemType}, Type={item.GetType().Name}");

            if (!_commands.TryGetValue(item.ItemType, out var info))
            {
                System.Diagnostics.Debug.WriteLine($"TryCreateSimple: ItemType {item.ItemType} not found in registry");
                System.Diagnostics.Debug.WriteLine($"TryCreateSimple: Available commands: {string.Join(", ", _commands.Keys)}");
                return false;
            }

            // SimpleCommandBinding属性をチェック（完全修飾名を使用）
            var bindingAttr = info.ItemType.GetCustomAttribute<MacroPanels.Model.CommandDefinition.SimpleCommandBindingAttribute>();
            if (bindingAttr == null)
            {
                System.Diagnostics.Debug.WriteLine($"TryCreateSimple: No SimpleCommandBinding attribute found for {info.ItemType.Name}");
                return false;
            }

            try
            {
                // 設定オブジェクトとしてアイテム自体を使用
                command = (ICommand)Activator.CreateInstance(bindingAttr.CommandType, parent, item)!;
                command.LineNumber = item.LineNumber;
                command.IsEnabled = item.IsEnable;
                System.Diagnostics.Debug.WriteLine($"TryCreateSimple: Successfully created {bindingAttr.CommandType.Name}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryCreateSimple: Failed to create command {bindingAttr.CommandType.Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"TryCreateSimple: Exception details: {ex}");
                return false;
            }
        }

        /// <summary>
        /// デシリアライゼーション用のタイプマッピングを取得
        /// </summary>
        public static Type? GetItemType(string typeName)
        {
            Initialize();
            return _commands.TryGetValue(typeName, out var info) ? info.ItemType : null;
        }
    }
}