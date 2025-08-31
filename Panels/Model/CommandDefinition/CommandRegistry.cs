using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MacroPanels.List.Class;
using MacroPanels.Command.Class;
using MacroPanels.Command.Interface;
using MacroPanels.Model.List.Interface;
using System.Collections.Concurrent;

namespace MacroPanels.Model.CommandDefinition
{
    /// <summary>
    /// コマンド定義から自動的にタイプやファクトリを生成するクラス
    /// </summary>
    public static class CommandRegistry
    {
        private static readonly ConcurrentDictionary<string, CommandInfo> _commands = new();
        private static readonly Lazy<Dictionary<string, CommandInfo>> _initializedCommands = new(InitializeCommands);
        private static bool _initialized = false;
        private static readonly object _lockObject = new();

        /// <summary>
        /// よく使用されるコマンドタイプ名の定数
        /// </summary>
        public static class CommandTypes
        {
            public const string Click = "Click";
            public const string ClickImage = "Click_Image";
            public const string ClickImageAI = "Click_Image_AI";
            public const string Hotkey = "Hotkey";
            public const string Wait = "Wait";
            public const string WaitImage = "Wait_Image";
            public const string Execute = "Execute";
            public const string Screenshot = "Screenshot";
            public const string Loop = "Loop";
            public const string LoopEnd = "Loop_End";
            public const string LoopBreak = "Loop_Break";
            public const string IfImageExist = "IF_ImageExist";
            public const string IfImageNotExist = "IF_ImageNotExist";
            public const string IfImageExistAI = "IF_ImageExist_AI";
            public const string IfImageNotExistAI = "IF_ImageNotExist_AI";
            public const string IfVariable = "IF_Variable";
            public const string IfEnd = "IF_End";
            public const string SetVariable = "SetVariable";
            public const string SetVariableAI = "SetVariable_AI";
        }

        /// <summary>
        /// UI表示用のコマンド順序定義
        /// </summary>
        public static class DisplayOrder
        {
            /// <summary>
            /// コマンドの表示優先度を取得（数値が小さいほど上位に表示）
            /// </summary>
            public static int GetPriority(string commandType)
            {
                return commandType switch
                {
                    // 1. 基本クリック操作
                    CommandTypes.Click => 1,
                    CommandTypes.ClickImage => 1,
                    CommandTypes.ClickImageAI => 1,
                    
                    // 2. その他の基本操作
                    CommandTypes.Hotkey => 2,
                    CommandTypes.Wait => 2,
                    CommandTypes.WaitImage => 2,
                    
                    // 3. ループ制御
                    CommandTypes.Loop => 3,
                    CommandTypes.LoopEnd => 3,
                    CommandTypes.LoopBreak => 3,
                    
                    // 4. IF文制御
                    CommandTypes.IfImageExist => 4,
                    CommandTypes.IfImageNotExist => 4,
                    CommandTypes.IfImageExistAI => 4,
                    CommandTypes.IfImageNotExistAI => 4,
                    CommandTypes.IfVariable => 4,
                    CommandTypes.IfEnd => 4,
                    
                    // 5. 変数操作
                    CommandTypes.SetVariable => 5,
                    CommandTypes.SetVariableAI => 5,
                    
                    // 6. システム操作
                    CommandTypes.Execute => 6,
                    CommandTypes.Screenshot => 6,
                    
                    // 9. その他・未分類
                    _ => 9
                };
            }

            /// <summary>
            /// 同じ優先度グループ内での詳細な順序を取得
            /// </summary>
            public static int GetSubPriority(string commandType)
            {
                return commandType switch
                {
                    // クリック操作グループ内での順序
                    CommandTypes.Click => 1,          // 通常クリック
                    CommandTypes.ClickImage => 2,     // 画像クリック
                    CommandTypes.ClickImageAI => 3,   // AIクリック
                    
                    // 基本操作グループ内での順序
                    CommandTypes.Hotkey => 1,         // ホットキー
                    CommandTypes.Wait => 2,           // 待機
                    CommandTypes.WaitImage => 3,      // 画像待機
                    
                    // ループ制御グループ内での順序
                    CommandTypes.Loop => 1,           // ループ開始
                    CommandTypes.LoopBreak => 2,      // ループ中断
                    CommandTypes.LoopEnd => 3,        // ループ終了
                    
                    // IF文制御グループ内での順序
                    CommandTypes.IfImageExist => 1,      // 画像存在
                    CommandTypes.IfImageNotExist => 2,   // 画像非存在
                    CommandTypes.IfImageExistAI => 3,    // AI画像存在
                    CommandTypes.IfImageNotExistAI => 4, // AI画像非存在
                    CommandTypes.IfVariable => 5,        // 変数条件
                    CommandTypes.IfEnd => 6,             // IF終了
                    
                    // 変数操作グループ内での順序
                    CommandTypes.SetVariable => 1,    // 変数設定
                    CommandTypes.SetVariableAI => 2,  // AI変数設定
                    
                    // システム操作グループ内での順序
                    CommandTypes.Execute => 1,        // プログラム実行
                    CommandTypes.Screenshot => 2,     // スクリーンショット
                    
                    // デフォルト
                    _ => 0
                };
            }

            /// <summary>
            /// コマンドの表示名を取得（多言語対応）
            /// </summary>
            public static string GetDisplayName(string commandType, string language = "ja")
            {
                return language switch
                {
                    "en" => GetEnglishDisplayName(commandType),
                    "ja" => GetJapaneseDisplayName(commandType),
                    _ => GetJapaneseDisplayName(commandType) // デフォルトは日本語
                };
            }

            /// <summary>
            /// 日本語表示名を取得
            /// </summary>
            public static string GetJapaneseDisplayName(string commandType)
            {
                return commandType switch
                {
                    CommandTypes.Click => "クリック",
                    CommandTypes.ClickImage => "画像クリック",
                    CommandTypes.ClickImageAI => "画像クリック(AI検出)",
                    CommandTypes.Hotkey => "ホットキー",
                    CommandTypes.Wait => "待機",
                    CommandTypes.WaitImage => "画像待機",
                    CommandTypes.Loop => "ループ - 開始",
                    CommandTypes.LoopEnd => "ループ - 終了",
                    CommandTypes.LoopBreak => "ループ - 中断",
                    CommandTypes.IfImageExist => "条件 - 画像存在判定",
                    CommandTypes.IfImageNotExist => "条件 - 画像非存在判定",
                    CommandTypes.IfImageExistAI => "条件 - 画像存在判定(AI検出)",
                    CommandTypes.IfImageNotExistAI => "条件 - 画像非存在判定(AI検出)",
                    CommandTypes.IfVariable => "条件 - 変数判定",
                    CommandTypes.IfEnd => "条件 - 終了",
                    CommandTypes.SetVariable => "変数設定",
                    CommandTypes.SetVariableAI => "変数設定(AI検出)",
                    CommandTypes.Execute => "プログラム実行",
                    CommandTypes.Screenshot => "スクリーンショット",
                    _ => commandType
                };
            }

            /// <summary>
            /// 英語表示名を取得
            /// </summary>
            public static string GetEnglishDisplayName(string commandType)
            {
                return commandType switch
                {
                    CommandTypes.Click => "Click",
                    CommandTypes.ClickImage => "Image Click",
                    CommandTypes.ClickImageAI => "AI Click",
                    CommandTypes.Hotkey => "Hotkey",
                    CommandTypes.Wait => "Wait",
                    CommandTypes.WaitImage => "Wait for Image",
                    CommandTypes.Loop => "Loop Start",
                    CommandTypes.LoopEnd => "Loop End",
                    CommandTypes.LoopBreak => "Loop Break",
                    CommandTypes.IfImageExist => "If Image Exists",
                    CommandTypes.IfImageNotExist => "If Image Not Exists",
                    CommandTypes.IfImageExistAI => "If AI Image Exists",
                    CommandTypes.IfImageNotExistAI => "If AI Image Not Exists",
                    CommandTypes.IfVariable => "If Variable",
                    CommandTypes.IfEnd => "If End",
                    CommandTypes.SetVariable => "Set Variable",
                    CommandTypes.SetVariableAI => "Set AI Variable",
                    CommandTypes.Execute => "Execute Program",
                    CommandTypes.Screenshot => "Screenshot",
                    _ => commandType
                };
            }

            /// <summary>
            /// カテゴリ名を取得（多言語対応）
            /// </summary>
            public static string GetCategoryName(string commandType, string language = "ja")
            {
                var priority = GetPriority(commandType);
                return language switch
                {
                    "en" => GetEnglishCategoryName(priority),
                    "ja" => GetJapaneseCategoryName(priority),
                    _ => GetJapaneseCategoryName(priority)
                };
            }

            private static string GetJapaneseCategoryName(int priority)
            {
                return priority switch
                {
                    1 => "クリック操作",
                    2 => "基本操作",
                    3 => "ループ制御",
                    4 => "条件分岐",
                    5 => "変数操作",
                    6 => "システム操作",
                    _ => "その他"
                };
            }

            private static string GetEnglishCategoryName(int priority)
            {
                return priority switch
                {
                    1 => "Click Operations",
                    2 => "Basic Operations",
                    3 => "Loop Control",
                    4 => "Conditional",
                    5 => "Variable Operations",
                    6 => "System Operations",
                    _ => "Others"
                };
            }

            /// <summary>
            /// コマンドの説明を取得
            /// </summary>
            public static string GetDescription(string commandType, string language = "ja")
            {
                return language switch
                {
                    "en" => GetEnglishDescription(commandType),
                    "ja" => GetJapaneseDescription(commandType),
                    _ => GetJapaneseDescription(commandType)
                };
            }

            private static string GetJapaneseDescription(string commandType)
            {
                return commandType switch
                {
                    CommandTypes.Click => "指定した座標をクリックします",
                    CommandTypes.ClickImage => "画像を検索してクリックします",
                    CommandTypes.ClickImageAI => "AIで画像を検索してクリックします",
                    CommandTypes.Hotkey => "ホットキーを送信します",
                    CommandTypes.Wait => "指定した時間待機します",
                    CommandTypes.WaitImage => "画像が表示されるまで待機します",
                    CommandTypes.Loop => "指定回数ループを実行します",
                    CommandTypes.LoopEnd => "ループを終了します",
                    CommandTypes.LoopBreak => "ループを中断します",
                    CommandTypes.IfImageExist => "画像が存在する場合に実行します",
                    CommandTypes.IfImageNotExist => "画像が存在しない場合に実行します",
                    CommandTypes.IfImageExistAI => "AIで画像が存在する場合に実行します",
                    CommandTypes.IfImageNotExistAI => "AIで画像が存在しない場合に実行します",
                    CommandTypes.IfVariable => "変数の条件が真の場合に実行します",
                    CommandTypes.IfEnd => "条件分岐を終了します",
                    CommandTypes.SetVariable => "変数に値を設定します",
                    CommandTypes.SetVariableAI => "AIの結果を変数に設定します",
                    CommandTypes.Execute => "外部プログラムを実行します",
                    CommandTypes.Screenshot => "スクリーンショットを撮影します",
                    _ => $"{commandType}コマンド"
                };
            }

            private static string GetEnglishDescription(string commandType)
            {
                return commandType switch
                {
                    CommandTypes.Click => "Click at specified coordinates",
                    CommandTypes.ClickImage => "Search for image and click",
                    CommandTypes.ClickImageAI => "Search for image using AI and click",
                    CommandTypes.Hotkey => "Send hotkey combination",
                    CommandTypes.Wait => "Wait for specified duration",
                    CommandTypes.WaitImage => "Wait until image appears",
                    CommandTypes.Loop => "Execute loop for specified count",
                    CommandTypes.LoopEnd => "End loop",
                    CommandTypes.LoopBreak => "Break from loop",
                    CommandTypes.IfImageExist => "Execute if image exists",
                    CommandTypes.IfImageNotExist => "Execute if image does not exist",
                    CommandTypes.IfImageExistAI => "Execute if AI detects image",
                    CommandTypes.IfImageNotExistAI => "Execute if AI does not detect image",
                    CommandTypes.IfVariable => "Execute if variable condition is true",
                    CommandTypes.IfEnd => "End conditional statement",
                    CommandTypes.SetVariable => "Set variable value",
                    CommandTypes.SetVariableAI => "Set variable from AI result",
                    CommandTypes.Execute => "Execute external program",
                    CommandTypes.Screenshot => "Take screenshot",
                    _ => $"{commandType} command"
                };
            }
        }

        /// <summary>
        /// コマンド情報
        /// </summary>
        private sealed class CommandInfo
        {
            public string TypeName { get; init; } = string.Empty;
            public Type ItemType { get; init; } = null!;
            public Type CommandType { get; init; } = null!;
            public Type SettingsType { get; init; } = null!;
            public CommandCategory Category { get; init; }
            public bool IsIfCommand { get; init; }
            public bool IsLoopCommand { get; init; }
            public Func<ICommandListItem> ItemFactory { get; init; } = null!;
        }

        /// <summary>
        /// 効率的な初期化（遅延評価）
        /// </summary>
        private static Dictionary<string, CommandInfo> InitializeCommands()
        {
            var commands = new Dictionary<string, CommandInfo>();
            
            System.Diagnostics.Debug.WriteLine("CommandRegistry: Starting initialization...");
            
            // アセンブリ内のCommandDefinition属性が付いたクラスを探す
            var assembly = Assembly.GetExecutingAssembly();
            var commandTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<CommandDefinitionAttribute>() != null)
                .ToArray();

            System.Diagnostics.Debug.WriteLine($"CommandRegistry: Found {commandTypes.Length} command types");

            foreach (var type in commandTypes)
            {
                try
                {
                    var attr = type.GetCustomAttribute<CommandDefinitionAttribute>()!;
                    var hasSimpleBinding = type.GetCustomAttribute<SimpleCommandBindingAttribute>() != null;
                    
                    System.Diagnostics.Debug.WriteLine($"CommandRegistry: Processing type {type.Name} with TypeName '{attr.TypeName}'");
                    
                    // ファクトリメソッドを作成（効率化）
                    var factory = CreateOptimizedFactory(type);
                    
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

                    commands[attr.TypeName] = commandInfo;
                    System.Diagnostics.Debug.WriteLine($"CommandRegistry: Successfully registered {attr.TypeName} -> {type.Name} (SimpleBinding: {hasSimpleBinding})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CommandRegistry: Failed to register type {type.Name}: {ex.Message}");
                    throw;
                }
            }

            System.Diagnostics.Debug.WriteLine($"CommandRegistry: Initialization complete with {commands.Count} commands");
            System.Diagnostics.Debug.WriteLine($"CommandRegistry: Registered types: {string.Join(", ", commands.Keys)}");
            
            return commands;
        }

        /// <summary>
        /// 初期化（アプリ起動時に1回だけ実行）
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            lock (_lockObject)
            {
                if (_initialized) return;

                var commands = _initializedCommands.Value;
                foreach (var kvp in commands)
                {
                    _commands[kvp.Key] = kvp.Value;
                }

                _initialized = true;
            }
        }

        /// <summary>
        /// 最適化されたファクトリメソッドを作成
        /// </summary>
        private static Func<ICommandListItem> CreateOptimizedFactory(Type itemType)
        {
            // コンパイル済み式ツリーでパフォーマンス向上
            var constructor = itemType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw new InvalidOperationException($"Type {itemType.Name} does not have a parameterless constructor");

            return () => (ICommandListItem)Activator.CreateInstance(itemType)!;
        }

        /// <summary>
        /// デバッグ用：登録されているコマンドの詳細情報を出力
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void PrintDebugInfo()
        {
            Initialize();
            
            System.Diagnostics.Debug.WriteLine("=== CommandRegistry Debug Info ===");
            System.Diagnostics.Debug.WriteLine($"Total registered commands: {_commands.Count}");
            System.Diagnostics.Debug.WriteLine($"Initialization status: {_initialized}");
            
            foreach (var kvp in _commands)
            {
                var info = kvp.Value;
                System.Diagnostics.Debug.WriteLine($"TypeName: {kvp.Key}");
                System.Diagnostics.Debug.WriteLine($"  ItemType: {info.ItemType.FullName}");
                System.Diagnostics.Debug.WriteLine($"  CommandType: {info.CommandType.FullName}");
                System.Diagnostics.Debug.WriteLine($"  SettingsType: {info.SettingsType.FullName}");
                System.Diagnostics.Debug.WriteLine($"  Category: {info.Category}");
                System.Diagnostics.Debug.WriteLine($"  IsIfCommand: {info.IsIfCommand}");
                System.Diagnostics.Debug.WriteLine($"  IsLoopCommand: {info.IsLoopCommand}");
                System.Diagnostics.Debug.WriteLine($"  Factory: {info.ItemFactory != null}");
                System.Diagnostics.Debug.WriteLine("---");
            }
            System.Diagnostics.Debug.WriteLine("=== End Debug Info ===");
        }

        /// <summary>
        /// デバッグ用：特定のコマンドタイプの作成をテスト
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void TestCreateCommand(string typeName)
        {
            System.Diagnostics.Debug.WriteLine($"=== Testing CreateCommandItem for '{typeName}' ===");
            
            try
            {
                var item = CreateCommandItem(typeName);
                if (item != null)
                {
                    System.Diagnostics.Debug.WriteLine($"SUCCESS: Created {item.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"  ItemType: {item.ItemType}");
                    System.Diagnostics.Debug.WriteLine($"  Description: {item.Description}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("FAILURE: CreateCommandItem returned null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
            }
            
            System.Diagnostics.Debug.WriteLine("=== End Test ===");
        }
        
        /// <summary>
        /// 全てのコマンドタイプ名を取得（キャッシュ済み）
        /// </summary>
        private static readonly Lazy<IReadOnlyCollection<string>> _allTypeNames = new(() =>
        {
            var _ = _initializedCommands.Value; // 初期化を強制
            return _commands.Keys.ToArray();
        });

        public static IEnumerable<string> GetAllTypeNames()
        {
            Initialize();
            return _allTypeNames.Value;
        }

        /// <summary>
        /// UI表示用に順序付けされたコマンドタイプ名を取得
        /// </summary>
        public static IEnumerable<string> GetOrderedTypeNames()
        {
            Initialize();
            return _allTypeNames.Value
                .OrderBy(DisplayOrder.GetPriority)
                .ThenBy(DisplayOrder.GetSubPriority)
                .ThenBy(x => x);
        }

        /// <summary>
        /// カテゴリ別のコマンドタイプ名を取得（キャッシュ済み）
        /// </summary>
        private static readonly ConcurrentDictionary<CommandCategory, IReadOnlyCollection<string>> _categoryCache = new();

        public static IEnumerable<string> GetTypeNamesByCategory(CommandCategory category)
        {
            Initialize();
            return _categoryCache.GetOrAdd(category, cat =>
                _commands.Values
                    .Where(c => c.Category == cat)
                    .Select(c => c.TypeName)
                    .ToArray());
        }

        /// <summary>
        /// 表示優先度別のコマンドタイプ名を取得
        /// </summary>
        public static IEnumerable<string> GetTypeNamesByDisplayPriority(int priority)
        {
            Initialize();
            return _allTypeNames.Value
                .Where(type => DisplayOrder.GetPriority(type) == priority)
                .OrderBy(DisplayOrder.GetSubPriority)
                .ThenBy(x => x);
        }

        /// <summary>
        /// コマンドアイテムを作成
        /// </summary>
        public static ICommandListItem? CreateCommandItem(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) 
            {
                System.Diagnostics.Debug.WriteLine("CreateCommandItem: typeName is null or empty");
                return null;
            }
            
            Initialize();
            
            System.Diagnostics.Debug.WriteLine($"CreateCommandItem: Attempting to create item for type '{typeName}'");
            
            if (_commands.TryGetValue(typeName, out var info))
            {
                try
                {
                    var item = info.ItemFactory();
                    item.ItemType = typeName;
                    System.Diagnostics.Debug.WriteLine($"CreateCommandItem: Successfully created item of type '{item.GetType().Name}' for typeName '{typeName}'");
                    return item;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CreateCommandItem: Failed to create item for type '{typeName}': {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"CreateCommandItem: Exception details: {ex}");
                    return null;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"CreateCommandItem: Type '{typeName}' not found in registry");
                System.Diagnostics.Debug.WriteLine($"CreateCommandItem: Available types: {string.Join(", ", _commands.Keys)}");
                return null;
            }
        }

        /// <summary>
        /// If系コマンドかどうか判定（高速化）
        /// </summary>
        public static bool IsIfCommand(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;
            Initialize();
            return _commands.TryGetValue(typeName, out var info) && info.IsIfCommand;
        }

        /// <summary>
        /// ループ系コマンドかどうか判定（高速化）
        /// </summary>
        public static bool IsLoopCommand(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;
            Initialize();
            return _commands.TryGetValue(typeName, out var info) && info.IsLoopCommand;
        }

        /// <summary>
        /// 終了系コマンド（ネストレベルを減らす）かどうか判定（最適化）
        /// </summary>
        public static bool IsEndCommand(string typeName)
        {
            return typeName is CommandTypes.LoopEnd or CommandTypes.IfEnd;
        }

        /// <summary>
        /// 開始系コマンド（ネストレベルを増やす）かどうか判定
        /// </summary>
        public static bool IsStartCommand(string typeName)
        {
            return IsLoopCommand(typeName) || IsIfCommand(typeName);
        }

        /// <summary>
        /// 指定されたタイプからコマンドを作成（SimpleCommandBinding用）
        /// </summary>
        public static bool TryCreateSimple(ICommand parent, ICommandListItem item, out ICommand? command)
        {
            command = null;
            if (item?.ItemType == null) return false;

            Initialize();

            System.Diagnostics.Debug.WriteLine($"TryCreateSimple: ItemType={item.ItemType}, Type={item.GetType().Name}");

            if (!_commands.TryGetValue(item.ItemType, out var info))
            {
                System.Diagnostics.Debug.WriteLine($"TryCreateSimple: ItemType {item.ItemType} not found in registry");
                return false;
            }

            // SimpleCommandBinding属性をチェック
            var bindingAttr = info.ItemType.GetCustomAttribute<SimpleCommandBindingAttribute>();
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
                return false;
            }
        }

        /// <summary>
        /// デシリアゼーション用のタイプマッピングを取得
        /// </summary>
        public static Type? GetItemType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            Initialize();
            return _commands.TryGetValue(typeName, out var info) ? info.ItemType : null;
        }

        /// <summary>
        /// すべてのコマンド情報を取得（デバッグ用）
        /// </summary>
        public static IReadOnlyDictionary<string, (Type ItemType, Type CommandType, CommandCategory Category)> GetAllCommandInfo()
        {
            Initialize();
            return _commands.ToDictionary(
                kvp => kvp.Key,
                kvp => (kvp.Value.ItemType, kvp.Value.CommandType, kvp.Value.Category));
        }

        /// <summary>
        /// 統計情報を取得（デバッグ用）
        /// </summary>
        public static (int TotalCommands, int IfCommands, int LoopCommands, Dictionary<CommandCategory, int> ByCategory) GetStatistics()
        {
            Initialize();
            var byCategory = _commands.Values
                .GroupBy(c => c.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            return (
                TotalCommands: _commands.Count,
                IfCommands: _commands.Values.Count(c => c.IsIfCommand),
                LoopCommands: _commands.Values.Count(c => c.IsLoopCommand),
                ByCategory: byCategory
            );
        }
    }
}