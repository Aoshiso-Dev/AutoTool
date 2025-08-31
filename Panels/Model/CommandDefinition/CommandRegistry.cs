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
            
            // �A�Z���u������CommandDefinition�������t�����N���X��T��
            var assembly = Assembly.GetExecutingAssembly();
            var commandTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<CommandDefinitionAttribute>() != null)
                .ToArray();

            System.Diagnostics.Debug.WriteLine($"CommandRegistry: Found {commandTypes.Length} command types");

            foreach (var type in commandTypes)
            {
                var attr = type.GetCustomAttribute<CommandDefinitionAttribute>()!;
                var hasSimpleBinding = type.GetCustomAttribute<SimpleCommandBindingAttribute>() != null;
                
                // �t�@�N�g�����\�b�h���쐬�i�������j
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
                System.Diagnostics.Debug.WriteLine($"CommandRegistry: Registered {attr.TypeName} -> {type.Name} (SimpleBinding: {hasSimpleBinding})");
            }

            System.Diagnostics.Debug.WriteLine($"CommandRegistry: Initialization complete with {commands.Count} commands");
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
        /// �œK�����ꂽ�t�@�N�g�����\�b�h���쐬
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
        /// �S�ẴR�}���h�^�C�v�����擾�i�L���b�V���ς݁j
        /// </summary>
        private static readonly Lazy<IReadOnlyCollection<string>> _allTypeNames = new(() =>
        {
            var _ = _initializedCommands.Value; // ������������
            return _commands.Keys.ToArray();
        });

        public static IEnumerable<string> GetAllTypeNames()
        {
            Initialize();
            return _allTypeNames.Value;
        }

        /// <summary>
        /// �J�e�S���ʂ̃R�}���h�^�C�v�����擾�i�L���b�V���ς݁j
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
        /// �R�}���h�A�C�e�����쐬
        /// </summary>
        public static ICommandListItem? CreateCommandItem(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            
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
        /// �I���n�R�}���h�i�l�X�g���x�������炷�j���ǂ�������i�œK���j
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
        /// �w�肳�ꂽ�^�C�v����R�}���h���쐬�iSimpleCommandBinding�p�j
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

            // SimpleCommandBinding�������`�F�b�N
            var bindingAttr = info.ItemType.GetCustomAttribute<SimpleCommandBindingAttribute>();
            if (bindingAttr == null)
            {
                System.Diagnostics.Debug.WriteLine($"TryCreateSimple: No SimpleCommandBinding attribute found for {info.ItemType.Name}");
                return false;
            }

            try
            {
                // �ݒ�I�u�W�F�N�g�Ƃ��ăA�C�e�����̂��g�p
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
        /// �f�V���A���C�[�[�V�����p�̃^�C�v�}�b�s���O���擾
        /// </summary>
        public static Type? GetItemType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            Initialize();
            return _commands.TryGetValue(typeName, out var info) ? info.ItemType : null;
        }

        /// <summary>
        /// ���ׂẴR�}���h�����擾�i�f�o�b�O�p�j
        /// </summary>
        public static IReadOnlyDictionary<string, (Type ItemType, Type CommandType, CommandCategory Category)> GetAllCommandInfo()
        {
            Initialize();
            return _commands.ToDictionary(
                kvp => kvp.Key,
                kvp => (kvp.Value.ItemType, kvp.Value.CommandType, kvp.Value.Category));
        }

        /// <summary>
        /// ���v�����擾�i�f�o�b�O�p�j
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