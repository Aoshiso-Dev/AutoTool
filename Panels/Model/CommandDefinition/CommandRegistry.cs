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
    /// �R�}���h��`���玩���I�Ƀ^�C�v��t�@�N�g���𐶐�����N���X
    /// </summary>
    public static class CommandRegistry
    {
        private static readonly Dictionary<string, CommandInfo> _commands = new();
        private static bool _initialized = false;

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
        /// �������i�A�v���N������1�񂾂����s�j
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            // �A�Z���u������CommandDefinition�������t�����N���X��T��
            var assembly = Assembly.GetExecutingAssembly();
            var commandTypes = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<CommandDefinitionAttribute>() != null)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"CommandRegistry: Found {commandTypes.Count} command types");

            foreach (var type in commandTypes)
            {
                var attr = type.GetCustomAttribute<CommandDefinitionAttribute>()!;
                var hasSimpleBinding = type.GetCustomAttribute<MacroPanels.Model.CommandDefinition.SimpleCommandBindingAttribute>() != null;
                
                // �t�@�N�g�����\�b�h���쐬
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
        /// �t�@�N�g�����\�b�h���쐬
        /// </summary>
        private static Func<ICommandListItem> CreateFactory(Type itemType)
        {
            return () => (ICommandListItem)Activator.CreateInstance(itemType)!;
        }

        /// <summary>
        /// �S�ẴR�}���h�^�C�v�����擾
        /// </summary>
        public static IEnumerable<string> GetAllTypeNames()
        {
            Initialize();
            return _commands.Keys;
        }

        /// <summary>
        /// �J�e�S���ʂ̃R�}���h�^�C�v�����擾
        /// </summary>
        public static IEnumerable<string> GetTypeNamesByCategory(CommandCategory category)
        {
            Initialize();
            return _commands.Values
                .Where(c => c.Category == category)
                .Select(c => c.TypeName);
        }

        /// <summary>
        /// �R�}���h�A�C�e�����쐬
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
        /// If�n�R�}���h���ǂ�������
        /// </summary>
        public static bool IsIfCommand(string typeName)
        {
            Initialize();
            return _commands.TryGetValue(typeName, out var info) && info.IsIfCommand;
        }

        /// <summary>
        /// ���[�v�n�R�}���h���ǂ�������
        /// </summary>
        public static bool IsLoopCommand(string typeName)
        {
            Initialize();
            return _commands.TryGetValue(typeName, out var info) && info.IsLoopCommand;
        }

        /// <summary>
        /// �I���n�R�}���h�i�l�X�g���x�������炷�j���ǂ�������
        /// </summary>
        public static bool IsEndCommand(string typeName)
        {
            return typeName == CommandTypes.LoopEnd || typeName == CommandTypes.IfEnd;
        }

        /// <summary>
        /// �w�肳�ꂽ�^�C�v����R�}���h���쐬�iSimpleCommandBinding�p�j
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

            // SimpleCommandBinding�������`�F�b�N�i���S�C�������g�p�j
            var bindingAttr = info.ItemType.GetCustomAttribute<MacroPanels.Model.CommandDefinition.SimpleCommandBindingAttribute>();
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
                System.Diagnostics.Debug.WriteLine($"TryCreateSimple: Exception details: {ex}");
                return false;
            }
        }

        /// <summary>
        /// �f�V���A���C�[�[�V�����p�̃^�C�v�}�b�s���O���擾
        /// </summary>
        public static Type? GetItemType(string typeName)
        {
            Initialize();
            return _commands.TryGetValue(typeName, out var info) ? info.ItemType : null;
        }
    }
}