using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using AutoTool.Command.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoTool.Command.Definition;
using AutoTool.ViewModel.Shared;

namespace AutoTool.Command.Definition
{
    /// <summary>
    /// 設定コントロールのタイプ
    /// </summary>
    public enum SettingControlType
    {
        TextBox,
        PasswordBox,
        NumberBox,
        CheckBox,
        ComboBox,
        Slider,
        FilePicker,
        FolderPicker,
        OnnxPicker,
        ColorPicker,
        DatePicker,
        TimePicker,
        KeyPicker,
        CoordinatePicker,
        WindowPicker
    }

    /// <summary>
    /// 動的コマンドのカテゴリ
    /// </summary>
    public enum DynamicCommandCategory
    {
        Basic,
        Mouse,
        Keyboard,
        Image,
        AI,
        Window,
        File,
        Control,
        Advanced
    }

    /// <summary>
    /// Command直接登録レジストリ
    /// </summary>
    public static class DirectCommandRegistry
    {
        private static readonly ConcurrentDictionary<string, CommandRegistration> _commands = new();
        private static readonly ConcurrentDictionary<string, List<SettingDefinition>> _settingDefinitions = new();
        private static readonly ConcurrentDictionary<string, object[]> _sourceCollections = new();
        private static bool _initialized = false;
        private static readonly object _lockObject = new();
        private static ILogger? _logger;

        private record CommandRegistration(
            string CommandId,
            Type CommandType,
            string DisplayName,
            DynamicCommandCategory Category,
            string Description,
            List<SettingDefinition> Settings,
            Func<ICommand?, UniversalCommandItem, IServiceProvider, ICommand> Factory
        );

        /// <summary>
        /// よく使用するコマンドタイプ名の定数
        /// </summary>
        public static class CommandTypes
        {
            public const string Click = "Click";
            public const string ClickImage = "ClickImage";
            public const string ClickImageAI = "ClickImageAI";
            public const string Hotkey = "Hotkey";
            public const string TypeText = "TypeText";
            public const string Wait = "Wait";
            public const string WaitImage = "WaitImage";
            public const string Execute = "Execute";
            public const string Screenshot = "Screenshot";
            public const string Loop = "Loop";
            public const string LoopEnd = "LoopEnd";
            public const string LoopBreak = "LoopBreak";
            public const string IfImageExist = "IfImageExist";
            public const string IfImageNotExist = "IfImageNotExist";
            public const string IfImageExistAI = "IfImageExist_AI";
            public const string IfImageNotExistAI = "IfImageNotExistAI";
            public const string IfVariable = "IfVariable";
            public const string IfEnd = "IfEnd";
            public const string SetVariable = "SetVariable";
            public const string SetVariableAI = "SetVariableAI";
            
            // 画像処理コマンド
            public const string SearchImage = "SearchImage";
            public const string SearchColor = "SearchColor";
            public const string SearchImageWithColorFilter = "SearchImageWithColorFilter";
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
                    CommandTypes.TypeText => 2,
                    CommandTypes.Wait => 2,
                    CommandTypes.WaitImage => 2,

                    // 3. ループ制御
                    CommandTypes.Loop => 3,
                    CommandTypes.LoopEnd => 3,
                    CommandTypes.LoopBreak => 3,

                    // 4. IF制御
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

                    // 9. その他・拡張機能
                    _ => 9
                };
            }

            /// <summary>
            /// 同一優先度グループでの詳細な順序を取得
            /// </summary>
            public static int GetSubPriority(string commandType)
            {
                return commandType switch
                {
                    // クリック操作グループでの順序
                    CommandTypes.Click => 1,          // 通常クリック
                    CommandTypes.ClickImage => 2,     // 画像クリック
                    CommandTypes.ClickImageAI => 3,   // AIクリック

                    // 基本操作グループでの順序
                    CommandTypes.Hotkey => 1,         // ホットキー
                    CommandTypes.TypeText => 2,       // テキスト入力
                    CommandTypes.Wait => 3,           // 待機
                    CommandTypes.WaitImage => 4,      // 画像待機

                    // ループ制御グループでの順序
                    CommandTypes.Loop => 1,           // ループ開始
                    CommandTypes.LoopBreak => 2,      // ループ中断
                    CommandTypes.LoopEnd => 3,        // ループ終了

                    // IF制御グループでの順序
                    CommandTypes.IfImageExist => 1,      // 画像存在
                    CommandTypes.IfImageNotExist => 2,   // 画像非存在
                    CommandTypes.IfImageExistAI => 3,    // AI画像存在
                    CommandTypes.IfImageNotExistAI => 4, // AI画像非存在
                    CommandTypes.IfVariable => 5,        // 変数条件
                    CommandTypes.IfEnd => 6,             // IF終了

                    // 変数操作グループでの順序
                    CommandTypes.SetVariable => 1,    // 変数設定
                    CommandTypes.SetVariableAI => 2,  // AI変数設定

                    // システム操作グループでの順序
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
                    CommandTypes.TypeText => "テキスト入力",
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
                    CommandTypes.SearchImage => "画像検索",
                    CommandTypes.SearchColor => "色検索",
                    CommandTypes.SearchImageWithColorFilter => "画像&色検索",
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
                    CommandTypes.TypeText => "Type Text",
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
                    CommandTypes.SearchImage => "Search Image",
                    CommandTypes.SearchColor => "Search Color",
                    CommandTypes.SearchImageWithColorFilter => "Search Image with Color Filter",
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
                    4 => "条件制御",
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
                    CommandTypes.TypeText => "テキストを入力します",
                    CommandTypes.Wait => "指定した時間待機します",
                    CommandTypes.WaitImage => "画像が表示されるまで待機します",
                    CommandTypes.Loop => "指定回数ループを開始して実行します",
                    CommandTypes.LoopEnd => "ループを終了します",
                    CommandTypes.LoopBreak => "ループを中断します",
                    CommandTypes.IfImageExist => "画像が存在する場合に実行します",
                    CommandTypes.IfImageNotExist => "画像が存在しない場合に実行します",
                    CommandTypes.IfImageExistAI => "AIで画像が存在する場合に実行します",
                    CommandTypes.IfImageNotExistAI => "AIで画像が存在しない場合に実行します",
                    CommandTypes.IfVariable => "変数の条件が真の場合に実行します",
                    CommandTypes.IfEnd => "条件文を終了します",
                    CommandTypes.SetVariable => "変数に値を設定します",
                    CommandTypes.SetVariableAI => "AIの結果を変数に設定します",
                    CommandTypes.Execute => "外部プログラムを実行します",
                    CommandTypes.Screenshot => "スクリーンショットを撮影します",
                    CommandTypes.SearchImage => "画像を検索します",
                    CommandTypes.SearchColor => "色を検索します",
                    CommandTypes.SearchImageWithColorFilter => "画像と色を同時に検索します",
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
                    CommandTypes.TypeText => "Type text input",
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
                    CommandTypes.SearchImage => "Search for image",
                    CommandTypes.SearchColor => "Search for color",
                    CommandTypes.SearchImageWithColorFilter => "Search for image with color filter",
                    _ => $"{commandType} command"
                };
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public static void Initialize(IServiceProvider? serviceProvider)
        {
            if (_initialized) return;

            lock (_lockObject)
            {
                if (_initialized) return;

                _logger = serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger(nameof(DirectCommandRegistry));

                // DirectCommand属性が付けられたCommandクラスを検索して登録
                if (serviceProvider != null)
                {
                    RegisterDynamicCommands();
                }

                RegisterSourceCollections();
                _initialized = true;
                _logger?.LogInformation("DirectCommandRegistry初期化完了: {Count}個Command登録", _commands.Count);
                
                // 登録されたコマンドの詳細をログ出力
                foreach (var kvp in _commands)
                {
                    var reg = kvp.Value;
                    _logger?.LogDebug("登録済みコマンド: {CommandId} -> {CommandType}, 設定項目: {SettingCount}個", 
                        kvp.Key, reg.CommandType?.Name ?? "Built-in", reg.Settings.Count);
                }
            }
        }

        /// <summary>
        /// 動的コマンドを登録
        /// </summary>
        private static void RegisterDynamicCommands()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var commandTypes = assembly.GetTypes()
                .Where(t => typeof(ICommand).IsAssignableFrom(t) && 
                           !t.IsInterface && !t.IsAbstract &&
                           t.GetCustomAttribute<DirectCommandAttribute>() != null)
                .ToArray();

            _logger?.LogInformation("DirectCommandRegistry動的初期化開始: {Count}個Command検出", commandTypes.Length);

            // 検出されたコマンドタイプをログ出力
            foreach (var commandType in commandTypes)
            {
                var attr = commandType.GetCustomAttribute<DirectCommandAttribute>();
                _logger?.LogDebug("検出されたコマンド: {CommandType} -> {CommandId} ({DisplayName})", 
                    commandType.Name, attr?.CommandId, attr?.DisplayName);
            }

            // Dynamic command type detection and registration  
            foreach (var commandType in commandTypes)
            {
                RegisterCommand(commandType);
            }

            // 画像処理関連コマンド
            RegisterBuiltinCommand(CommandTypes.Screenshot, 
                "スクリーンショット", DynamicCommandCategory.Basic, "スクリーンショットを撮影します",
                typeof(AutoTool.Command.Commands.ScreenshotCommand));
        }

        /// <summary>
        /// Commandの登録（DirectCommand属性使用）
        /// </summary>
        private static void RegisterCommand(Type commandType)
        {
            var attr = commandType.GetCustomAttribute<DirectCommandAttribute>();
            if (attr == null) return;

            try
            {
                // CategoryをDynamicCommandCategoryに変換
                var category = Enum.TryParse<DynamicCommandCategory>(attr.Category, out var parsedCategory) 
                    ? parsedCategory 
                    : DynamicCommandCategory.Basic;

                var settings = ExtractSettingDefinitions(commandType);
                var factory = CreateCommandFactory(commandType);

                var registration = new CommandRegistration(
                    attr.CommandId,
                    commandType,
                    attr.DisplayName,
                    category,
                    attr.Description,
                    settings,
                    factory
                );

                _commands[attr.CommandId] = registration;
                _settingDefinitions[attr.CommandId] = settings;

                _logger?.LogDebug("Command登録: {CommandId} -> {CommandType}, 設定項目: {SettingCount}個",
                    attr.CommandId, commandType.Name, settings.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Command登録失敗: {CommandType}", commandType.Name);
                throw;
            }
        }

        /// <summary>
        /// ビルトインCommandの登録（属性なしコマンド用）
        /// </summary>
        private static void RegisterBuiltinCommand(string commandId, string displayName, DynamicCommandCategory category, string description, Type commandType)
        {
            try
            {
                var settings = ExtractSettingDefinitions(commandType);
                var factory = CreateCommandFactory(commandType);

                var registration = new CommandRegistration(
                    commandId,
                    commandType,
                    displayName,
                    category,
                    description,
                    settings,
                    factory
                );

                _commands[commandId] = registration;
                _settingDefinitions[commandId] = settings;

                _logger?.LogDebug("ビルトインCommand登録: {CommandId} -> {CommandType}, 設定項目: {SettingCount}個",
                    commandId, commandType.Name, settings.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ビルトインCommand登録失敗: {CommandType}", commandType.Name);
                throw;
            }
        }

        /// <summary>
        /// 設定定義の抽出
        /// </summary>
        private static List<SettingDefinition> ExtractSettingDefinitions(Type commandType)
        {
            var definitions = new List<SettingDefinition>();

            var properties = commandType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var settingAttr = property.GetCustomAttribute<SettingPropertyAttribute>();
                if (settingAttr != null)
                {
                    definitions.Add(new SettingDefinition
                    {
                        PropertyName = property.Name,
                        DisplayName = settingAttr.DisplayName,
                        ControlType = settingAttr.ControlType,
                        Category = settingAttr.Category ?? "基本設定",
                        Description = settingAttr.Description,
                        DefaultValue = settingAttr.DefaultValue,
                        IsRequired = settingAttr.IsRequired,
                        FileFilter = settingAttr.FileFilter,
                        ActionButtons = settingAttr.ActionButtons?.ToList() ?? new List<string>(),
                        SourceCollection = settingAttr.SourceCollection,
                        PropertyType = property.PropertyType,
                        ShowCurrentValue = settingAttr.ShowCurrentValue,
                        MinValue = settingAttr.MinValue,
                        MaxValue = settingAttr.MaxValue,
                        Unit = settingAttr.Unit
                    });
                }
            }

            return definitions;
        }

        /// <summary>
        /// Commandファクトリーの作成
        /// </summary>
        private static Func<ICommand?, UniversalCommandItem, IServiceProvider, ICommand> CreateCommandFactory(Type commandType)
        {
            return (parent, item, serviceProvider) =>
            {
                try
                {
                    // コンストラクターのパターンを検索
                    var constructors = commandType.GetConstructors();

                    // parent, item, serviceProviderを受け取るコンストラクター
                    var constructor = constructors.FirstOrDefault(c =>
                    {
                        var parameters = c.GetParameters();
                        return parameters.Length == 3 &&
                               typeof(ICommand).IsAssignableFrom(parameters[0].ParameterType) &&
                               typeof(UniversalCommandItem).IsAssignableFrom(parameters[1].ParameterType) &&
                               typeof(IServiceProvider).IsAssignableFrom(parameters[2].ParameterType);
                    });

                    if (constructor != null)
                    {
                        var command = (ICommand)Activator.CreateInstance(commandType, parent, item, serviceProvider)!;
                        InitializeCommand(command, parent, item);
                        return command;
                    }

                    // parent, serviceProviderのみのコンストラクター
                    constructor = constructors.FirstOrDefault(c =>
                    {
                        var parameters = c.GetParameters();
                        return parameters.Length == 2 &&
                               typeof(ICommand).IsAssignableFrom(parameters[0].ParameterType) &&
                               typeof(IServiceProvider).IsAssignableFrom(parameters[1].ParameterType);
                    });

                    if (constructor != null)
                    {
                        var command = (ICommand)Activator.CreateInstance(commandType, parent, serviceProvider)!;
                        InitializeCommand(command, parent, item);
                        return command;
                    }

                    // デフォルトコンストラクター
                    var defaultCommand = (ICommand)Activator.CreateInstance(commandType)!;
                    InitializeCommand(defaultCommand, parent, item);
                    return defaultCommand;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Command作成失敗: {CommandType}", commandType.Name);
                    throw;
                }
            };
        }

        /// <summary>
        /// Commandの初期化
        /// </summary>
        private static void InitializeCommand(ICommand command, ICommand? parent, UniversalCommandItem item)
        {
            command.LineNumber = item.LineNumber;
            command.IsEnabled = item.IsEnable;

            // 親コマンドの設定
            if (parent != null)
            {
                var parentProperty = command.GetType().GetProperty("Parent");
                if (parentProperty?.CanWrite == true)
                {
                    parentProperty.SetValue(command, parent);
                }
            }

            // 設定値をCommandのプロパティに反映
            ApplySettingsToCommand(command, item);
        }

        /// <summary>
        /// 設定値をCommandプロパティに適用
        /// </summary>
        private static void ApplySettingsToCommand(ICommand command, UniversalCommandItem item)
        {
            var commandType = command.GetType();
            var properties = commandType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var settingAttr = property.GetCustomAttribute<SettingPropertyAttribute>();
                if (settingAttr != null && property.CanWrite)
                {
                    var value = item.GetSetting(property.Name, settingAttr.DefaultValue);
                    if (value != null)
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(value, property.PropertyType);
                            property.SetValue(command, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "設定値変換失敗: {PropertyName} = {Value}", property.Name, value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ソースコレクションの登録
        /// </summary>
        private static void RegisterSourceCollections()
        {
            _sourceCollections["MouseButtons"] = Enum.GetValues(typeof(System.Windows.Input.MouseButton)).Cast<object>().ToArray();
            
            _sourceCollections["Keys"] = new object[]
            {
                System.Windows.Input.Key.Enter, System.Windows.Input.Key.Escape, System.Windows.Input.Key.Space, 
                System.Windows.Input.Key.Tab, System.Windows.Input.Key.Back, System.Windows.Input.Key.Delete,
                System.Windows.Input.Key.F1, System.Windows.Input.Key.F2, System.Windows.Input.Key.F3, 
                System.Windows.Input.Key.F4, System.Windows.Input.Key.F5, System.Windows.Input.Key.F6, 
                System.Windows.Input.Key.F7, System.Windows.Input.Key.F8, System.Windows.Input.Key.F9, 
                System.Windows.Input.Key.F10, System.Windows.Input.Key.F11, System.Windows.Input.Key.F12,
                System.Windows.Input.Key.A, System.Windows.Input.Key.B, System.Windows.Input.Key.C, 
                System.Windows.Input.Key.D, System.Windows.Input.Key.E, System.Windows.Input.Key.F, 
                System.Windows.Input.Key.G, System.Windows.Input.Key.H, System.Windows.Input.Key.I, 
                System.Windows.Input.Key.J, System.Windows.Input.Key.K, System.Windows.Input.Key.L, 
                System.Windows.Input.Key.M, System.Windows.Input.Key.N, System.Windows.Input.Key.O, 
                System.Windows.Input.Key.P, System.Windows.Input.Key.Q, System.Windows.Input.Key.R, 
                System.Windows.Input.Key.S, System.Windows.Input.Key.T, System.Windows.Input.Key.U, 
                System.Windows.Input.Key.V, System.Windows.Input.Key.W, System.Windows.Input.Key.X, 
                System.Windows.Input.Key.Y, System.Windows.Input.Key.Z,
                System.Windows.Input.Key.D0, System.Windows.Input.Key.D1, System.Windows.Input.Key.D2, 
                System.Windows.Input.Key.D3, System.Windows.Input.Key.D4, System.Windows.Input.Key.D5, 
                System.Windows.Input.Key.D6, System.Windows.Input.Key.D7, System.Windows.Input.Key.D8, 
                System.Windows.Input.Key.D9,
                System.Windows.Input.Key.Up, System.Windows.Input.Key.Down, System.Windows.Input.Key.Left, 
                System.Windows.Input.Key.Right, System.Windows.Input.Key.Home, System.Windows.Input.Key.End, 
                System.Windows.Input.Key.PageUp, System.Windows.Input.Key.PageDown
            };
            
            _sourceCollections["ExecutionModes"] = new object[]
            {
                "Normal", "Fast", "Slow", "Debug", "Silent", "Verbose", "Test", "Production"
            };
            
            _sourceCollections["BackgroundClickMethods"] = new object[]
            {
                "SendMessage", "PostMessage", "AutoDetectChild", "TryAll", 
                "GameDirectInput", "GameFullscreen", "GameLowLevel", "GameVirtualMouse"
            };
            
            _sourceCollections["ComparisonOperators"] = new object[]
            {
                "==", "!=", ">", "<", ">=", "<=", "Contains", "StartsWith", "EndsWith", 
                "IsEmpty", "IsNotEmpty"
            };
            
            _sourceCollections["AIDetectModes"] = new object[]
            {
                "Class", "Count"
            };
            
            _sourceCollections["LogLevels"] = new object[]
            {
                "Trace", "Debug", "Info", "Warning", "Error", "Critical"
            };
        }

        /// <summary>
        /// CommandIDからCommandを作成
        /// </summary>
        public static ICommand? CreateCommand(string commandId, ICommand? parent, UniversalCommandItem item, IServiceProvider serviceProvider)
        {
            if (!_initialized)
            {
                Initialize(serviceProvider);
            }

            if (_commands.TryGetValue(commandId, out var registration))
            {
                return registration.Factory(parent, item, serviceProvider);
            }

            return null;
        }

        /// <summary>
        /// UniversalCommandItem作成
        /// </summary>
        public static UniversalCommandItem CreateUniversalItem(string commandId, Dictionary<string, object?>? initialSettings = null)
        {
            if (!_initialized)
            {
                Initialize(null);
            }
            
            _logger?.LogDebug("UniversalCommandItem作成開始: {CommandId}", commandId);
            
            var item = new UniversalCommandItem
            {
                ItemType = commandId,
                IsEnable = true
            };

            // デフォルト値設定
            if (_settingDefinitions.TryGetValue(commandId, out var definitions))
            {
                _logger?.LogDebug("設定定義取得成功: {CommandId}, 項目数: {Count}", commandId, definitions.Count);
                
                foreach (var definition in definitions)
                {
                    if (definition.DefaultValue != null)
                    {
                        item.SetSetting(definition.PropertyName, definition.DefaultValue);
                        _logger?.LogTrace("デフォルト値設定: {PropertyName} = {DefaultValue}", 
                            definition.PropertyName, definition.DefaultValue);
                    }
                }
            }
            else
            {
                _logger?.LogWarning("設定定義が見つかりません: {CommandId}", commandId);
                _logger?.LogDebug("利用可能なコマンドID: {AvailableCommands}", 
                    string.Join(", ", _settingDefinitions.Keys));
            }

            // 初期設定値で上書き
            if (initialSettings != null)
            {
                _logger?.LogDebug("初期設定値適用: {Count}項目", initialSettings.Count);
                foreach (var kvp in initialSettings)
                {
                    item.SetSetting(kvp.Key, kvp.Value);
                    _logger?.LogTrace("初期設定値: {PropertyName} = {Value}", kvp.Key, kvp.Value);
                }
            }

            item.InitializeSettingDefinitions();
            _logger?.LogDebug("UniversalCommandItem作成完了: {CommandId}", commandId);
            return item;
        }

        /// <summary>
        /// 登録されているCommand一覧を取得
        /// </summary>
        public static IEnumerable<(string CommandId, string DisplayName, DynamicCommandCategory Category)> GetRegisteredCommands()
        {
            if (!_initialized)
            {
                Initialize(null);
            }
            return _commands.Values.Select(r => (r.CommandId, r.DisplayName, r.Category));
        }

        /// <summary>
        /// 全てのコマンドタイプ名を取得
        /// </summary>
        public static IEnumerable<string> GetAllTypeNames()
        {
            if (!_initialized)
            {
                Initialize(null);
            }
            return _commands.Keys.ToArray();
        }

        /// <summary>
        /// UI表示用に順序付けられたコマンドタイプ名を取得
        /// </summary>
        public static IEnumerable<string> GetOrderedTypeNames()
        {
            return GetAllTypeNames()
                .OrderBy(DisplayOrder.GetPriority)
                .ThenBy(DisplayOrder.GetSubPriority)
                .ThenBy(x => x);
        }

        /// <summary>
        /// コマンドアイテムを作成（後方互換性用）
        /// </summary>
        public static UniversalCommandItem? CreateCommandItem(string typeName)
        {
            try
            {
                var universalItem = CreateUniversalItem(typeName);
                return universalItem;
            }
            catch
            {
                // フォールバック：UniversalCommandItem
                return new UniversalCommandItem
                {
                    ItemType = typeName,
                    IsEnable = true
                };
            }
        }

        /// <summary>
        /// If系コマンドかどうかを判定
        /// </summary>
        public static bool IsIfCommand(string typeName)
        {
            return typeName switch
            {
                CommandTypes.IfImageExist or CommandTypes.IfImageNotExist or 
                CommandTypes.IfImageExistAI or CommandTypes.IfImageNotExistAI or 
                CommandTypes.IfVariable => true,
                _ => false
            };
        }

        /// <summary>
        /// ループ系コマンドかどうかを判定
        /// </summary>
        public static bool IsLoopCommand(string typeName)
        {
            return typeName == CommandTypes.Loop;
        }

        /// <summary>
        /// 終了系コマンド（ネストレベルを下げる）かどうかを判定
        /// </summary>
        public static bool IsEndCommand(string typeName)
        {
            return typeName is CommandTypes.LoopEnd or CommandTypes.IfEnd;
        }

        /// <summary>
        /// コマンドの設定定義を取得
        /// </summary>
        public static List<SettingDefinition> GetSettingDefinitions(string commandId)
        {
            if (!_initialized)
            {
                Initialize(null);
            }

            if (_settingDefinitions.TryGetValue(commandId, out var definitions))
            {
                return definitions;
            }

            _logger?.LogWarning("設定定義が見つかりません: {CommandId}", commandId);
            return new List<SettingDefinition>();
        }

        /// <summary>
        /// ソースコレクションを取得
        /// </summary>
        public static object[]? GetSourceCollection(string collectionName)
        {
            if (!_initialized)
            {
                Initialize(null);
            }

            if (_sourceCollections.TryGetValue(collectionName, out var collection))
            {
                return collection;
            }

            _logger?.LogWarning("ソースコレクションが見つかりません: {CollectionName}", collectionName);
            return null;
        }

        /// <summary>
        /// 指定されたコマンドタイプが開始コマンド（Loop、If系）かどうかを判定
        /// </summary>
        public static bool IsStartCommand(string commandType)
        {
            return commandType switch
            {
                "Loop" => true,
                "IfImageExist" => true,
                "IfImageNotExist" => true,
                "IfImageExist_AI" => true,
                "IfImageNotExist_AI" => true,
                "IfVariable" => true,
                _ => false
            };
        }
    }

    /// <summary>
    /// 設定項目定義
    /// </summary>
    public class SettingDefinition : INotifyPropertyChanged
    {
        private object? _currentValue;

        public string PropertyName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public SettingControlType ControlType { get; set; }
        public string Category { get; set; } = "基本設定";
        public string? Description { get; set; }
        public object? DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public string? FileFilter { get; set; }
        public List<string> ActionButtons { get; set; } = new();
        public string? SourceCollection { get; set; }
        public Type PropertyType { get; set; } = typeof(object);
        public bool ShowCurrentValue { get; set; } = true;
        public double MinValue { get; set; } = 0.0;
        public double MaxValue { get; set; } = 100.0;
        public string? Unit { get; set; }
        
        /// <summary>
        /// 現在値
        /// </summary>
        public object? CurrentValue
        {
            get => _currentValue;
            set
            {
                if (!Equals(_currentValue, value))
                {
                    _currentValue = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentValue)));
                }
            }
        }
        
        /// <summary>
        /// ComboBox等で使用するソースアイテム
        /// </summary>
        public List<object> SourceItems { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}