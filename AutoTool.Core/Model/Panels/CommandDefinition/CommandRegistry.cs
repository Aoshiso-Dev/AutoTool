using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using AutoTool.Panels.List.Class;
using AutoTool.Commands.Commands;
using AutoTool.Commands.Interface;
using AutoTool.Panels.Model.List.Interface;
using System.Collections.Concurrent;

namespace AutoTool.Panels.Model.CommandDefinition
{
    /// <summary>
    /// �R�}���h��`���玩���I�Ƀ^�C�v��t�@�N�g���𐶐�����N���X
    /// </summary>
    public static class CommandRegistry
    {
        private static readonly ConcurrentDictionary<string, CommandInfo> _commands = new();
        private static readonly Lazy<Dictionary<string, CommandInfo>> _initializedCommands = new(InitializeCommands);
        private static bool _initialized = false;
        private static readonly object _lockObject = new();

        /// <summary>
        /// �悭�g�p�����R�}���h�^�C�v���̒萔
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
        /// UI�\���p�̃R�}���h������`
        /// </summary>
        public static class DisplayOrder
        {
            /// <summary>
            /// �R�}���h�̕\���D��x��擾�i���l���������قǏ�ʂɕ\���j
            /// </summary>
            public static int GetPriority(string commandType)
            {
                return commandType switch
                {
                    // 1. ��{�N���b�N����
                    CommandTypes.Click => 1,
                    CommandTypes.ClickImage => 1,
                    CommandTypes.ClickImageAI => 1,
                    
                    // 2. ���̑��̊�{����
                    CommandTypes.Hotkey => 2,
                    CommandTypes.Wait => 2,
                    CommandTypes.WaitImage => 2,
                    
                    // 3. ���[�v����
                    CommandTypes.Loop => 3,
                    CommandTypes.LoopEnd => 3,
                    CommandTypes.LoopBreak => 3,
                    
                    // 4. IF������
                    CommandTypes.IfImageExist => 4,
                    CommandTypes.IfImageNotExist => 4,
                    CommandTypes.IfImageExistAI => 4,
                    CommandTypes.IfImageNotExistAI => 4,
                    CommandTypes.IfVariable => 4,
                    CommandTypes.IfEnd => 4,
                    
                    // 5. �ϐ�����
                    CommandTypes.SetVariable => 5,
                    CommandTypes.SetVariableAI => 5,
                    
                    // 6. �V�X�e������
                    CommandTypes.Execute => 6,
                    CommandTypes.Screenshot => 6,
                    
                    // 9. ���̑��E������
                    _ => 9
                };
            }

            /// <summary>
            /// �����D��x�O���[�v��ł̏ڍׂȏ�����擾
            /// </summary>
            public static int GetSubPriority(string commandType)
            {
                return commandType switch
                {
                    // �N���b�N����O���[�v��ł̏���
                    CommandTypes.Click => 1,          // �ʏ�N���b�N
                    CommandTypes.ClickImage => 2,     // �摜�N���b�N
                    CommandTypes.ClickImageAI => 3,   // AI�N���b�N
                    
                    // ��{����O���[�v��ł̏���
                    CommandTypes.Hotkey => 1,         // �z�b�g�L�[
                    CommandTypes.Wait => 2,           // �ҋ@
                    CommandTypes.WaitImage => 3,      // �摜�ҋ@
                    
                    // ���[�v����O���[�v��ł̏���
                    CommandTypes.Loop => 1,           // ���[�v�J�n
                    CommandTypes.LoopBreak => 2,      // ���[�v���f
                    CommandTypes.LoopEnd => 3,        // ���[�v�I��
                    
                    // IF������O���[�v��ł̏���
                    CommandTypes.IfImageExist => 1,      // �摜����
                    CommandTypes.IfImageNotExist => 2,   // �摜�񑶍�
                    CommandTypes.IfImageExistAI => 3,    // AI�摜����
                    CommandTypes.IfImageNotExistAI => 4, // AI�摜�񑶍�
                    CommandTypes.IfVariable => 5,        // �ϐ����
                    CommandTypes.IfEnd => 6,             // IF�I��
                    
                    // �ϐ�����O���[�v��ł̏���
                    CommandTypes.SetVariable => 1,    // �ϐ��ݒ�
                    CommandTypes.SetVariableAI => 2,  // AI�ϐ��ݒ�
                    
                    // �V�X�e������O���[�v��ł̏���
                    CommandTypes.Execute => 1,        // �v���O�������s
                    CommandTypes.Screenshot => 2,     // �X�N���[���V���b�g
                    
                    // �f�t�H���g
                    _ => 0
                };
            }

            /// <summary>
            /// �R�}���h�̕\������擾�i������Ή��j
            /// </summary>
            public static string GetDisplayName(string commandType, string language = "ja")
            {
                return language switch
                {
                    "en" => GetEnglishDisplayName(commandType),
                    "ja" => GetJapaneseDisplayName(commandType),
                    _ => GetJapaneseDisplayName(commandType) // �f�t�H���g�͓��{��
                };
            }

            /// <summary>
            /// ���{��\������擾
            /// </summary>
            public static string GetJapaneseDisplayName(string commandType)
            {
                return commandType switch
                {
                    CommandTypes.Click => "�N���b�N",
                    CommandTypes.ClickImage => "�摜�N���b�N",
                    CommandTypes.ClickImageAI => "�摜�N���b�N(AI���o)",
                    CommandTypes.Hotkey => "�z�b�g�L�[",
                    CommandTypes.Wait => "�ҋ@",
                    CommandTypes.WaitImage => "�摜�ҋ@",
                    CommandTypes.Loop => "���[�v - �J�n",
                    CommandTypes.LoopEnd => "���[�v - �I��",
                    CommandTypes.LoopBreak => "���[�v - ���f",
                    CommandTypes.IfImageExist => "��� - �摜���ݔ���",
                    CommandTypes.IfImageNotExist => "��� - �摜�񑶍ݔ���",
                    CommandTypes.IfImageExistAI => "��� - �摜���ݔ���(AI���o)",
                    CommandTypes.IfImageNotExistAI => "��� - �摜�񑶍ݔ���(AI���o)",
                    CommandTypes.IfVariable => "��� - �ϐ�����",
                    CommandTypes.IfEnd => "��� - �I��",
                    CommandTypes.SetVariable => "�ϐ��ݒ�",
                    CommandTypes.SetVariableAI => "�ϐ��ݒ�(AI���o)",
                    CommandTypes.Execute => "�v���O�������s",
                    CommandTypes.Screenshot => "�X�N���[���V���b�g",
                    _ => commandType
                };
            }

            /// <summary>
            /// �p��\������擾
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
            /// �J�e�S������擾�i������Ή��j
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
                    1 => "�N���b�N����",
                    2 => "��{����",
                    3 => "���[�v����",
                    4 => "�������",
                    5 => "�ϐ�����",
                    6 => "�V�X�e������",
                    _ => "���̑�"
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
            /// �R�}���h�̐����擾
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
                    CommandTypes.Click => "クリック",
                    CommandTypes.ClickImage => "画像クリック",
                    CommandTypes.ClickImageAI => "AI画像クリック",
                    CommandTypes.Hotkey => "ホットキー",
                    CommandTypes.Wait => "待機",
                    CommandTypes.WaitImage => "画像待機",
                    CommandTypes.Loop => "ループ開始",
                    CommandTypes.LoopEnd => "ループ終了",
                    CommandTypes.LoopBreak => "ループ中断",
                    CommandTypes.IfImageExist => "画像あり分岐",
                    CommandTypes.IfImageNotExist => "画像なし分岐",
                    CommandTypes.IfImageExistAI => "AI画像あり分岐",
                    CommandTypes.IfImageNotExistAI => "AI画像なし分岐",
                    CommandTypes.IfVariable => "変数分岐",
                    CommandTypes.IfEnd => "分岐終了",
                    CommandTypes.SetVariable => "変数設定",
                    CommandTypes.SetVariableAI => "AI変数設定",
                    CommandTypes.Execute => "実行",
                    CommandTypes.Screenshot => "スクリーンショット",
                    _ => $"{commandType} コマンド"
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
        /// �R�}���h���
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
        /// �����I�ȏ������i�x���]���j
        /// </summary>
        private static Dictionary<string, CommandInfo> InitializeCommands()
        {
            var commands = new Dictionary<string, CommandInfo>();
            
            System.Diagnostics.Debug.WriteLine("CommandRegistry: Starting initialization...");
            
            // �A�Z���u�����CommandDefinition�������t�����N���X��T��
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
                    
                    // �t�@�N�g�����\�b�h��쐬�i�������j
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
        /// �������i�A�v���N������1�񂾂����s�j
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
        /// �œK�����ꂽ�t�@�N�g�����\�b�h��쐬
        /// </summary>
        private static Func<ICommandListItem> CreateOptimizedFactory(Type itemType)
        {
            // �R���p�C���ςݎ��c���[�Ńp�t�H�[�}���X����
            var constructor = itemType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw new InvalidOperationException($"Type {itemType.Name} does not have a parameterless constructor");

            return () => (ICommandListItem)Activator.CreateInstance(itemType)!;
        }

        /// <summary>
        /// �f�o�b�O�p�F�o�^����Ă���R�}���h�̏ڍ׏���o��
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
        /// �f�o�b�O�p�F����̃R�}���h�^�C�v�̍쐬��e�X�g
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
        /// �S�ẴR�}���h�^�C�v����擾�i�L���b�V���ς݁j
        /// </summary>
        private static readonly Lazy<IReadOnlyCollection<string>> _allTypeNames = new(() =>
        {
            var _ = _initializedCommands.Value; // �����������
            return _commands.Keys.ToArray();
        });

        public static IEnumerable<string> GetAllTypeNames()
        {
            Initialize();
            return _allTypeNames.Value;
        }

        /// <summary>
        /// UI�\���p�ɏ����t�����ꂽ�R�}���h�^�C�v����擾
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
        /// �J�e�S���ʂ̃R�}���h�^�C�v����擾�i�L���b�V���ς݁j
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
        /// �\���D��x�ʂ̃R�}���h�^�C�v����擾
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
        /// �R�}���h�A�C�e����쐬
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
        /// If�n�R�}���h���ǂ�������i�������j
        /// </summary>
        public static bool IsIfCommand(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;
            Initialize();
            return _commands.TryGetValue(typeName, out var info) && info.IsIfCommand;
        }

        /// <summary>
        /// ���[�v�n�R�}���h���ǂ�������i�������j
        /// </summary>
        public static bool IsLoopCommand(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;
            Initialize();
            return _commands.TryGetValue(typeName, out var info) && info.IsLoopCommand;
        }

        /// <summary>
        /// �I���n�R�}���h�i�l�X�g���x������炷�j���ǂ�������i�œK���j
        /// </summary>
        public static bool IsEndCommand(string typeName)
        {
            return typeName is CommandTypes.LoopEnd or CommandTypes.IfEnd;
        }

        /// <summary>
        /// �J�n�n�R�}���h�i�l�X�g���x���𑝂₷�j���ǂ�������
        /// </summary>
        public static bool IsStartCommand(string typeName)
        {
            return IsLoopCommand(typeName) || IsIfCommand(typeName);
        }

        /// <summary>
        /// �w�肳�ꂽ�^�C�v����R�}���h��쐬�iSimpleCommandBinding�p�j
        /// </summary>
        public static bool TryCreateSimple(ICommand parent, ICommandListItem item, out ICommand? command)
        {
            return TryCreateSimple(parent, item, null, out command);
        }

        /// <summary>
        /// Item��ExecuteAsync��I�[�o�[���C�h���Ă��邩�`�F�b�N
        /// </summary>
        private static bool HasExecuteAsyncOverride(Type itemType)
        {
            var method = itemType.GetMethod("ExecuteAsync", 
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(ICommandExecutionContext), typeof(CancellationToken) },
                null);
            
            return method != null && method.DeclaringType != typeof(CommandListItem);
        }

        /// <summary>
        /// �w�肳�ꂽ�^�C�v����R�}���h��쐬�iSimpleCommandBinding�p�A�T�[�r�X�v���o�C�_�[�t���j
        /// </summary>
        public static bool TryCreateSimple(ICommand parent, ICommandListItem item, IServiceProvider? serviceProvider, out ICommand? command)
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

            try
            {
                // Item��ExecuteAsync���I�[�o�[���C�h����Ă��āA�T�[�r�X�v���o�C�_�[������ꍇ��SimpleCommand��g�p
                if (serviceProvider != null && HasExecuteAsyncOverride(item.GetType()))
                {
                    var variableStore = serviceProvider.GetService(typeof(AutoTool.Commands.Services.IVariableStore)) as AutoTool.Commands.Services.IVariableStore;
                    var pathService = serviceProvider.GetService(typeof(AutoTool.Commands.Services.IPathService)) as AutoTool.Commands.Services.IPathService;
                    var mouseService = serviceProvider.GetService(typeof(AutoTool.Commands.Services.IMouseService)) as AutoTool.Commands.Services.IMouseService;
                    var keyboardService = serviceProvider.GetService(typeof(AutoTool.Commands.Services.IKeyboardService)) as AutoTool.Commands.Services.IKeyboardService;
                    var processService = serviceProvider.GetService(typeof(AutoTool.Commands.Services.IProcessService)) as AutoTool.Commands.Services.IProcessService;
                    var screenCaptureService = serviceProvider.GetService(typeof(AutoTool.Commands.Services.IScreenCaptureService)) as AutoTool.Commands.Services.IScreenCaptureService;
                    var imageSearchService = serviceProvider.GetService(typeof(AutoTool.Commands.Services.IImageSearchService)) as AutoTool.Commands.Services.IImageSearchService;
                    var aiDetectionService = serviceProvider.GetService(typeof(AutoTool.Commands.Services.IAIDetectionService)) as AutoTool.Commands.Services.IAIDetectionService;

                    if (variableStore != null && pathService != null && mouseService != null && keyboardService != null)
                    {
                        command = new SimpleCommand(parent, item as ICommandSettings ?? new CommandSettings(), item, variableStore, pathService, mouseService, keyboardService, processService, screenCaptureService, imageSearchService, aiDetectionService);
                        command.LineNumber = item.LineNumber;
                        command.IsEnabled = item.IsEnable;
                        System.Diagnostics.Debug.WriteLine($"TryCreateSimple: Created SimpleCommand for {item.GetType().Name}");
                        return true;
                    }
                }

                // SimpleCommandBinding������`�F�b�N�i���K�V�[�Ή��j
                var bindingAttr = info.ItemType.GetCustomAttribute<SimpleCommandBindingAttribute>();
                if (bindingAttr == null)
                {
                    System.Diagnostics.Debug.WriteLine($"TryCreateSimple: No SimpleCommandBinding attribute and no ExecuteAsync override for {info.ItemType.Name}");
                    return false;
                }

                // �]���̕��@�ŃR�}���h��쐬
                var constructors = bindingAttr.CommandType.GetConstructors();
                
                // �܂�2�����̃R���X�g���N�^����� (parent, settings)
                var twoArgConstructor = constructors.FirstOrDefault(c => 
                    c.GetParameters().Length == 2);
                
                if (twoArgConstructor != null)
                {
                    command = (ICommand)twoArgConstructor.Invoke(new object?[] { parent, item })!;
                }
                else if (serviceProvider != null)
                {
                    // �T�[�r�X��K�v�Ƃ���R���X�g���N�^��T��
                    foreach (var ctor in constructors.OrderByDescending(c => c.GetParameters().Length))
                    {
                        var parameters = ctor.GetParameters();
                        if (parameters.Length < 2) continue;
                        
                        var args = new object?[parameters.Length];
                        args[0] = parent;
                        args[1] = item;
                        
                        bool allServicesFound = true;
                        for (int i = 2; i < parameters.Length; i++)
                        {
                            var service = serviceProvider.GetService(parameters[i].ParameterType);
                            if (service == null)
                            {
                                allServicesFound = false;
                                break;
                            }
                            args[i] = service;
                        }
                        
                        if (allServicesFound)
                        {
                            command = (ICommand)ctor.Invoke(args)!;
                            break;
                        }
                    }
                    
                    if (command == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"TryCreateSimple: Could not find suitable constructor for {bindingAttr.CommandType.Name}");
                        return false;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"TryCreateSimple: No suitable constructor found for {bindingAttr.CommandType.Name}");
                    return false;
                }
                
                command.LineNumber = item.LineNumber;
                command.IsEnabled = item.IsEnable;
                System.Diagnostics.Debug.WriteLine($"TryCreateSimple: Successfully created {bindingAttr.CommandType.Name}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryCreateSimple: Failed to create command: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// �f�V���A�[�[�V�����p�̃^�C�v�}�b�s���O��擾
        /// </summary>
        public static Type? GetItemType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            Initialize();
            return _commands.TryGetValue(typeName, out var info) ? info.ItemType : null;
        }

        /// <summary>
        /// ���ׂẴR�}���h����擾�i�f�o�b�O�p�j
        /// </summary>
        public static IReadOnlyDictionary<string, (Type ItemType, Type CommandType, CommandCategory Category)> GetAllCommandInfo()
        {
            Initialize();
            return _commands.ToDictionary(
                kvp => kvp.Key,
                kvp => (kvp.Value.ItemType, kvp.Value.CommandType, kvp.Value.Category));
        }

        /// <summary>
        /// ���v����擾�i�f�o�b�O�p�j
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


