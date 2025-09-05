using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using AutoTool.Command.Interface;
using AutoTool.Model.List.Class;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Model.CommandDefinition
{
    /// <summary>
    /// �ݒ�R���g���[���̃^�C�v
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
    /// ���I�R�}���h�̃J�e�S��
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
    /// Command���ړo�^���W�X�g��
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
        /// �悭�g�p����R�}���h�^�C�v���̒萔
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
            /// �R�}���h�̕\���D��x���擾�i���l���������قǏ�ʂɕ\���j
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

                    // 4. IF����
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

                    // 9. ���̑��E�g���@�\
                    _ => 9
                };
            }

            /// <summary>
            /// ����D��x�O���[�v�ł̏ڍׂȏ������擾
            /// </summary>
            public static int GetSubPriority(string commandType)
            {
                return commandType switch
                {
                    // �N���b�N����O���[�v�ł̏���
                    CommandTypes.Click => 1,          // �ʏ�N���b�N
                    CommandTypes.ClickImage => 2,     // �摜�N���b�N
                    CommandTypes.ClickImageAI => 3,   // AI�N���b�N

                    // ��{����O���[�v�ł̏���
                    CommandTypes.Hotkey => 1,         // �z�b�g�L�[
                    CommandTypes.Wait => 2,           // �ҋ@
                    CommandTypes.WaitImage => 3,      // �摜�ҋ@

                    // ���[�v����O���[�v�ł̏���
                    CommandTypes.Loop => 1,           // ���[�v�J�n
                    CommandTypes.LoopBreak => 2,      // ���[�v���f
                    CommandTypes.LoopEnd => 3,        // ���[�v�I��

                    // IF����O���[�v�ł̏���
                    CommandTypes.IfImageExist => 1,      // �摜����
                    CommandTypes.IfImageNotExist => 2,   // �摜�񑶍�
                    CommandTypes.IfImageExistAI => 3,    // AI�摜����
                    CommandTypes.IfImageNotExistAI => 4, // AI�摜�񑶍�
                    CommandTypes.IfVariable => 5,        // �ϐ�����
                    CommandTypes.IfEnd => 6,             // IF�I��

                    // �ϐ�����O���[�v�ł̏���
                    CommandTypes.SetVariable => 1,    // �ϐ��ݒ�
                    CommandTypes.SetVariableAI => 2,  // AI�ϐ��ݒ�

                    // �V�X�e������O���[�v�ł̏���
                    CommandTypes.Execute => 1,        // �v���O�������s
                    CommandTypes.Screenshot => 2,     // �X�N���[���V���b�g

                    // �f�t�H���g
                    _ => 0
                };
            }

            /// <summary>
            /// �R�}���h�̕\�������擾�i������Ή��j
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
            /// ���{��\�������擾
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
                    CommandTypes.IfImageExist => "���� - �摜���ݔ���",
                    CommandTypes.IfImageNotExist => "���� - �摜�񑶍ݔ���",
                    CommandTypes.IfImageExistAI => "���� - �摜���ݔ���(AI���o)",
                    CommandTypes.IfImageNotExistAI => "���� - �摜�񑶍ݔ���(AI���o)",
                    CommandTypes.IfVariable => "���� - �ϐ�����",
                    CommandTypes.IfEnd => "���� - �I��",
                    CommandTypes.SetVariable => "�ϐ��ݒ�",
                    CommandTypes.SetVariableAI => "�ϐ��ݒ�(AI���o)",
                    CommandTypes.Execute => "�v���O�������s",
                    CommandTypes.Screenshot => "�X�N���[���V���b�g",
                    _ => commandType
                };
            }

            /// <summary>
            /// �p��\�������擾
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
            /// �J�e�S�������擾�i������Ή��j
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
                    4 => "��������",
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
            /// �R�}���h�̐������擾
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
                    CommandTypes.Click => "�w�肵�����W���N���b�N���܂�",
                    CommandTypes.ClickImage => "�摜���������ăN���b�N���܂�",
                    CommandTypes.ClickImageAI => "AI�ŉ摜���������ăN���b�N���܂�",
                    CommandTypes.Hotkey => "�z�b�g�L�[�𑗐M���܂�",
                    CommandTypes.Wait => "�w�肵�����ԑҋ@���܂�",
                    CommandTypes.WaitImage => "�摜���\�������܂őҋ@���܂�",
                    CommandTypes.Loop => "�w��񐔃��[�v���������s���܂�",
                    CommandTypes.LoopEnd => "���[�v���I�����܂�",
                    CommandTypes.LoopBreak => "���[�v�𒆒f���܂�",
                    CommandTypes.IfImageExist => "�摜�����݂���ꍇ�Ɏ��s���܂�",
                    CommandTypes.IfImageNotExist => "�摜�����݂��Ȃ��ꍇ�Ɏ��s���܂�",
                    CommandTypes.IfImageExistAI => "AI�ŉ摜�����݂���ꍇ�Ɏ��s���܂�",
                    CommandTypes.IfImageNotExistAI => "AI�ŉ摜�����݂��Ȃ��ꍇ�Ɏ��s���܂�",
                    CommandTypes.IfVariable => "�ϐ��̏������^�̏ꍇ�Ɏ��s���܂�",
                    CommandTypes.IfEnd => "����������I�����܂�",
                    CommandTypes.SetVariable => "�ϐ��ɒl��ݒ肵�܂�",
                    CommandTypes.SetVariableAI => "AI�̌��ʂ�ϐ��ɐݒ肵�܂�",
                    CommandTypes.Execute => "�O���v���O���������s���܂�",
                    CommandTypes.Screenshot => "�X�N���[���V���b�g���B�e���܂�",
                    _ => $"{commandType}�R�}���h"
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
        /// ������
        /// </summary>
        public static void Initialize(IServiceProvider? serviceProvider)
        {
            if (_initialized) return;

            lock (_lockObject)
            {
                if (_initialized) return;

                _logger = serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger(nameof(DirectCommandRegistry));

                // �܂��r���g�C���R�}���h��o�^
                RegisterBuiltinCommands();

                // DirectCommand�������t����ꂽCommand�N���X���������ēo�^
                if (serviceProvider != null)
                {
                    RegisterDynamicCommands();
                }

                RegisterSourceCollections();
                _initialized = true;
                _logger?.LogInformation("DirectCommandRegistry����������: {Count}��Command�o�^", _commands.Count);
                
                // �o�^���ꂽ�R�}���h�̏ڍׂ����O�o��
                foreach (var kvp in _commands)
                {
                    var reg = kvp.Value;
                    _logger?.LogDebug("�o�^�ς݃R�}���h: {CommandId} -> {CommandType}, �ݒ荀��: {SettingCount}��", 
                        kvp.Key, reg.CommandType?.Name ?? "Built-in", reg.Settings.Count);
                }
            }
        }

        /// <summary>
        /// �r���g�C���R�}���h��o�^
        /// </summary>
        private static void RegisterBuiltinCommands()
        {
            var builtinCommands = new[]
            {
                (CommandTypes.Click, "�N���b�N", DynamicCommandCategory.Mouse, "�w�肵�����W���N���b�N���܂�"),
                (CommandTypes.ClickImage, "�摜�N���b�N", DynamicCommandCategory.Image, "�摜���������ăN���b�N���܂�"),
                (CommandTypes.ClickImageAI, "�摜�N���b�N(AI���o)", DynamicCommandCategory.AI, "AI�ŉ摜���������ăN���b�N���܂�"),
                (CommandTypes.Hotkey, "�z�b�g�L�[", DynamicCommandCategory.Keyboard, "�z�b�g�L�[�𑗐M���܂�"),
                (CommandTypes.Wait, "�ҋ@", DynamicCommandCategory.Basic, "�w�肵�����ԑҋ@���܂�"),
                (CommandTypes.WaitImage, "�摜�ҋ@", DynamicCommandCategory.Image, "�摜���\�������܂őҋ@���܂�"),
                (CommandTypes.Loop, "���[�v - �J�n", DynamicCommandCategory.Control, "�w��񐔃��[�v���������s���܂�"),
                (CommandTypes.LoopEnd, "���[�v - �I��", DynamicCommandCategory.Control, "���[�v���I�����܂�"),
                (CommandTypes.LoopBreak, "���[�v - ���f", DynamicCommandCategory.Control, "���[�v�𒆒f���܂�"),
                (CommandTypes.IfImageExist, "���� - �摜���ݔ���", DynamicCommandCategory.Control, "�摜�����݂���ꍇ�Ɏ��s���܂�"),
                (CommandTypes.IfImageNotExist, "���� - �摜�񑶍ݔ���", DynamicCommandCategory.Control, "�摜�����݂��Ȃ��ꍇ�Ɏ��s���܂�"),
                (CommandTypes.IfImageExistAI, "���� - �摜���ݔ���(AI���o)", DynamicCommandCategory.AI, "AI�ŉ摜�����݂���ꍇ�Ɏ��s���܂�"),
                (CommandTypes.IfImageNotExistAI, "���� - �摜�񑶍ݔ���(AI���o)", DynamicCommandCategory.AI, "AI�ŉ摜�����݂��Ȃ��ꍇ�Ɏ��s���܂�"),
                (CommandTypes.IfVariable, "���� - �ϐ�����", DynamicCommandCategory.Control, "�ϐ��̏������^�̏ꍇ�Ɏ��s���܂�"),
                (CommandTypes.IfEnd, "���� - �I��", DynamicCommandCategory.Control, "����������I�����܂�"),
                (CommandTypes.SetVariable, "�ϐ��ݒ�", DynamicCommandCategory.Basic, "�ϐ��ɒl��ݒ肵�܂�"),
                (CommandTypes.SetVariableAI, "�ϐ��ݒ�(AI���o)", DynamicCommandCategory.AI, "AI�̌��ʂ�ϐ��ɐݒ肵�܂�"),
                (CommandTypes.Execute, "�v���O�������s", DynamicCommandCategory.Basic, "�O���v���O���������s���܂�"),
                (CommandTypes.Screenshot, "�X�N���[���V���b�g", DynamicCommandCategory.Basic, "�X�N���[���V���b�g���B�e���܂�")
            };

            foreach (var (commandId, displayName, category, description) in builtinCommands)
            {
                var settings = CreateBuiltinSettings(commandId);
                var factory = CreateBuiltinFactory(commandId);

                var registration = new CommandRegistration(
                    commandId,
                    null, // �r���g�C���͌^�Ȃ�
                    displayName,
                    category,
                    description,
                    settings,
                    factory
                );

                _commands[commandId] = registration;
                _settingDefinitions[commandId] = settings;

                _logger?.LogDebug("�r���g�C��Command�o�^: {CommandId} -> {DisplayName}, �ݒ荀��: {SettingCount}��",
                    commandId, displayName, settings.Count);
            }
        }

        /// <summary>
        /// �r���g�C���ݒ���쐬
        /// </summary>
        private static List<SettingDefinition> CreateBuiltinSettings(string commandId)
        {
            var settings = new List<SettingDefinition>();

            switch (commandId)
            {
                case CommandTypes.Click:
                    settings.AddRange(new[]
                    {
                        new SettingDefinition { PropertyName = "X", DisplayName = "X���W", ControlType = SettingControlType.NumberBox, DefaultValue = 0, IsRequired = true },
                        new SettingDefinition { PropertyName = "Y", DisplayName = "Y���W", ControlType = SettingControlType.NumberBox, DefaultValue = 0, IsRequired = true },
                        new SettingDefinition { PropertyName = "Button", DisplayName = "�}�E�X�{�^��", ControlType = SettingControlType.ComboBox, DefaultValue = "Left", SourceCollection = "MouseButtons" },
                        new SettingDefinition { PropertyName = "UseBackgroundClick", DisplayName = "�o�b�N�O���E���h�N���b�N", ControlType = SettingControlType.CheckBox, DefaultValue = false }
                    });
                    break;

                case CommandTypes.ClickImage:
                case CommandTypes.WaitImage:
                    settings.AddRange(new[]
                    {
                        new SettingDefinition { PropertyName = "ImagePath", DisplayName = "�摜�t�@�C��", ControlType = SettingControlType.FilePicker, IsRequired = true, FileFilter = "�摜�t�@�C��|*.png;*.jpg;*.jpeg;*.bmp" },
                        new SettingDefinition { PropertyName = "Threshold", DisplayName = "臒l", ControlType = SettingControlType.Slider, DefaultValue = 0.8, MinValue = 0.0, MaxValue = 1.0 },
                        new SettingDefinition { PropertyName = "Timeout", DisplayName = "�^�C���A�E�g(ms)", ControlType = SettingControlType.NumberBox, DefaultValue = 5000 },
                        new SettingDefinition { PropertyName = "Interval", DisplayName = "�Ԋu(ms)", ControlType = SettingControlType.NumberBox, DefaultValue = 500 }
                    });
                    break;

                case CommandTypes.Hotkey:
                    settings.AddRange(new[]
                    {
                        new SettingDefinition { PropertyName = "Key", DisplayName = "�L�[", ControlType = SettingControlType.KeyPicker, IsRequired = true },
                        new SettingDefinition { PropertyName = "Ctrl", DisplayName = "Ctrl�L�[", ControlType = SettingControlType.CheckBox, DefaultValue = false },
                        new SettingDefinition { PropertyName = "Alt", DisplayName = "Alt�L�[", ControlType = SettingControlType.CheckBox, DefaultValue = false },
                        new SettingDefinition { PropertyName = "Shift", DisplayName = "Shift�L�[", ControlType = SettingControlType.CheckBox, DefaultValue = false }
                    });
                    break;

                case CommandTypes.Wait:
                    settings.Add(new SettingDefinition { PropertyName = "Wait", DisplayName = "�ҋ@����(ms)", ControlType = SettingControlType.NumberBox, DefaultValue = 1000, IsRequired = true });
                    break;

                case CommandTypes.Loop:
                    settings.Add(new SettingDefinition { PropertyName = "LoopCount", DisplayName = "���[�v��", ControlType = SettingControlType.NumberBox, DefaultValue = 1, IsRequired = true });
                    break;

                case CommandTypes.SetVariable:
                    settings.AddRange(new[]
                    {
                        new SettingDefinition { PropertyName = "Name", DisplayName = "�ϐ���", ControlType = SettingControlType.TextBox, IsRequired = true },
                        new SettingDefinition { PropertyName = "Value", DisplayName = "�l", ControlType = SettingControlType.TextBox, IsRequired = true }
                    });
                    break;

                case CommandTypes.IfVariable:
                    settings.AddRange(new[]
                    {
                        new SettingDefinition { PropertyName = "Name", DisplayName = "�ϐ���", ControlType = SettingControlType.TextBox, IsRequired = true },
                        new SettingDefinition { PropertyName = "Operator", DisplayName = "���Z�q", ControlType = SettingControlType.ComboBox, DefaultValue = "==", SourceCollection = "ComparisonOperators" },
                        new SettingDefinition { PropertyName = "Value", DisplayName = "��r�l", ControlType = SettingControlType.TextBox, IsRequired = true }
                    });
                    break;

                case CommandTypes.Execute:
                    settings.AddRange(new[]
                    {
                        new SettingDefinition { PropertyName = "ProgramPath", DisplayName = "�v���O�����p�X", ControlType = SettingControlType.FilePicker, IsRequired = true },
                        new SettingDefinition { PropertyName = "Arguments", DisplayName = "����", ControlType = SettingControlType.TextBox },
                        new SettingDefinition { PropertyName = "WorkingDirectory", DisplayName = "��ƃf�B���N�g��", ControlType = SettingControlType.FolderPicker },
                        new SettingDefinition { PropertyName = "WaitForExit", DisplayName = "�I����ҋ@", ControlType = SettingControlType.CheckBox, DefaultValue = false }
                    });
                    break;

                case CommandTypes.Screenshot:
                    settings.Add(new SettingDefinition { PropertyName = "SaveDirectory", DisplayName = "�ۑ��f�B���N�g��", ControlType = SettingControlType.FolderPicker, IsRequired = true });
                    break;
            }

            return settings;
        }

        /// <summary>
        /// �r���g�C���t�@�N�g���[���쐬
        /// </summary>
        private static Func<ICommand?, UniversalCommandItem, IServiceProvider, ICommand> CreateBuiltinFactory(string commandId)
        {
            return (parent, item, serviceProvider) =>
            {
                // TODO: �����Ŏ��ۂ�Command�N���X���쐬����
                // ���݂͉��̎�����Ԃ�
                return new DummyCommand(commandId, parent, item);
            };
        }

        /// <summary>
        /// ���I�R�}���h��o�^
        /// </summary>
        private static void RegisterDynamicCommands()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var commandTypes = assembly.GetTypes()
                .Where(t => typeof(ICommand).IsAssignableFrom(t) && 
                           !t.IsInterface && !t.IsAbstract &&
                           t.GetCustomAttribute<DirectCommandAttribute>() != null)
                .ToArray();

            _logger?.LogInformation("DirectCommandRegistry���I�������J�n: {Count}��Command���o", commandTypes.Length);

            // ���o���ꂽ�R�}���h�^�C�v�����O�o��
            foreach (var commandType in commandTypes)
            {
                var attr = commandType.GetCustomAttribute<DirectCommandAttribute>();
                _logger?.LogDebug("���o���ꂽ�R�}���h: {CommandType} -> {CommandId} ({DisplayName})", 
                    commandType.Name, attr?.CommandId, attr?.DisplayName);
            }

            foreach (var commandType in commandTypes)
            {
                RegisterCommand(commandType);
            }
        }

        /// <summary>
        /// Command�̓o�^
        /// </summary>
        private static void RegisterCommand(Type commandType)
        {
            var attr = commandType.GetCustomAttribute<DirectCommandAttribute>();
            if (attr == null) return;

            try
            {
                // Category��DynamicCommandCategory�ɕϊ�
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

                _logger?.LogDebug("Command�o�^: {CommandId} -> {CommandType}, �ݒ荀��: {SettingCount}��",
                    attr.CommandId, commandType.Name, settings.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Command�o�^���s: {CommandType}", commandType.Name);
                throw;
            }
        }

        /// <summary>
        /// �ݒ��`�̒��o
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
                        Category = settingAttr.Category ?? "��{�ݒ�",
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
        /// Command�t�@�N�g���[�̍쐬
        /// </summary>
        private static Func<ICommand?, UniversalCommandItem, IServiceProvider, ICommand> CreateCommandFactory(Type commandType)
        {
            return (parent, item, serviceProvider) =>
            {
                try
                {
                    // �R���X�g���N�^�[�̃p�^�[��������
                    var constructors = commandType.GetConstructors();

                    // parent, item, serviceProvider���󂯎��R���X�g���N�^�[
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

                    // parent, serviceProvider�݂̂̃R���X�g���N�^�[
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

                    // �����̃R���X�g���N�^�[�iparent, settings, serviceProvider�j
                    constructor = constructors.FirstOrDefault(c =>
                    {
                        var parameters = c.GetParameters();
                        return parameters.Length == 3 &&
                               typeof(ICommand).IsAssignableFrom(parameters[0].ParameterType) &&
                               typeof(IServiceProvider).IsAssignableFrom(parameters[2].ParameterType);
                    });

                    if (constructor != null)
                    {
                        var settings = CreateSettingsFromItem(commandType, item);
                        var command = (ICommand)Activator.CreateInstance(commandType, parent, settings, serviceProvider)!;
                        InitializeCommand(command, parent, item);
                        return command;
                    }

                    // �f�t�H���g�R���X�g���N�^�[
                    var defaultCommand = (ICommand)Activator.CreateInstance(commandType)!;
                    InitializeCommand(defaultCommand, parent, item);
                    return defaultCommand;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Command�쐬���s: {CommandType}", commandType.Name);
                    throw;
                }
            };
        }

        /// <summary>
        /// UniversalCommandItem����ݒ�I�u�W�F�N�g���쐬
        /// </summary>
        private static object? CreateSettingsFromItem(Type commandType, UniversalCommandItem item)
        {
            try
            {
                // �ݒ�N���X��T���i�R�}���h�� + "Settings"�j
                var settingsTypeName = commandType.Name + "Settings";
                var settingsType = commandType.Assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == settingsTypeName);

                if (settingsType == null) return null;

                var settings = Activator.CreateInstance(settingsType);
                if (settings == null) return null;

                // �v���p�e�B��ݒ�l���畜��
                var properties = settingsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    if (prop.CanWrite && item.Settings.TryGetValue(prop.Name, out var value))
                    {
                        try
                        {
                            if (value != null)
                            {
                                var convertedValue = Convert.ChangeType(value, prop.PropertyType);
                                prop.SetValue(settings, convertedValue);
                            }
                        }
                        catch
                        {
                            // �ϊ����s���̓X�L�b�v
                        }
                    }
                }

                return settings;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Command�̏�����
        /// </summary>
        private static void InitializeCommand(ICommand command, ICommand? parent, UniversalCommandItem item)
        {
            command.LineNumber = item.LineNumber;
            command.IsEnabled = item.IsEnable;

            // �e�R�}���h�̐ݒ�
            if (parent != null)
            {
                var parentProperty = command.GetType().GetProperty("Parent");
                if (parentProperty?.CanWrite == true)
                {
                    parentProperty.SetValue(command, parent);
                }
            }

            // �ݒ�l��Command�̃v���p�e�B�ɔ��f
            ApplySettingsToCommand(command, item);
        }

        /// <summary>
        /// �ݒ�l��Command�v���p�e�B�ɓK�p
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
                    var value = item.GetSetting<object>(property.Name, settingAttr.DefaultValue);
                    if (value != null)
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(value, property.PropertyType);
                            property.SetValue(command, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "�ݒ�l�ϊ����s: {PropertyName} = {Value}", property.Name, value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// �\�[�X�R���N�V�����̓o�^
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
        /// CommandID����Command���쐬
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
        /// UniversalCommandItem�쐬
        /// </summary>
        public static UniversalCommandItem CreateUniversalItem(string commandId, Dictionary<string, object?>? initialSettings = null)
        {
            if (!_initialized)
            {
                Initialize(null);
            }
            
            _logger?.LogDebug("UniversalCommandItem�쐬�J�n: {CommandId}", commandId);
            
            var item = new UniversalCommandItem
            {
                ItemType = commandId,
                IsEnable = true
            };

            // �f�t�H���g�l�ݒ�
            if (_settingDefinitions.TryGetValue(commandId, out var definitions))
            {
                _logger?.LogDebug("�ݒ��`�擾����: {CommandId}, ���ڐ�: {Count}", commandId, definitions.Count);
                
                foreach (var definition in definitions)
                {
                    if (definition.DefaultValue != null)
                    {
                        item.SetSetting(definition.PropertyName, definition.DefaultValue);
                        _logger?.LogTrace("�f�t�H���g�l�ݒ�: {PropertyName} = {DefaultValue}", 
                            definition.PropertyName, definition.DefaultValue);
                    }
                }
            }
            else
            {
                _logger?.LogWarning("�ݒ��`��������܂���: {CommandId}", commandId);
                _logger?.LogDebug("���p�\�ȃR�}���hID: {AvailableCommands}", 
                    string.Join(", ", _settingDefinitions.Keys));
            }

            // �����ݒ�l�ŏ㏑��
            if (initialSettings != null)
            {
                _logger?.LogDebug("�����ݒ�l�K�p: {Count}����", initialSettings.Count);
                foreach (var kvp in initialSettings)
                {
                    item.SetSetting(kvp.Key, kvp.Value);
                    _logger?.LogTrace("�����ݒ�l: {PropertyName} = {Value}", kvp.Key, kvp.Value);
                }
            }

            item.InitializeSettingDefinitions();
            _logger?.LogDebug("UniversalCommandItem�쐬����: {CommandId}", commandId);
            return item;
        }

        /// <summary>
        /// �o�^����Ă���Command�ꗗ���擾
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
        /// �S�ẴR�}���h�^�C�v�����擾
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
        /// UI�\���p�ɏ����t����ꂽ�R�}���h�^�C�v�����擾
        /// </summary>
        public static IEnumerable<string> GetOrderedTypeNames()
        {
            return GetAllTypeNames()
                .OrderBy(DisplayOrder.GetPriority)
                .ThenBy(DisplayOrder.GetSubPriority)
                .ThenBy(x => x);
        }

        /// <summary>
        /// �R�}���h�A�C�e�����쐬�i����݊����p�j
        /// </summary>
        public static AutoTool.Model.List.Interface.ICommandListItem? CreateCommandItem(string typeName)
        {
            try
            {
                var universalItem = CreateUniversalItem(typeName);
                return universalItem;
            }
            catch
            {
                // �t�H�[���o�b�N�FBasicCommandItem
                return new AutoTool.Model.List.Type.BasicCommandItem
                {
                    ItemType = typeName,
                    IsEnable = true
                };
            }
        }

        /// <summary>
        /// If�n�R�}���h���ǂ����𔻒�
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
        /// ���[�v�n�R�}���h���ǂ����𔻒�
        /// </summary>
        public static bool IsLoopCommand(string typeName)
        {
            return typeName == CommandTypes.Loop;
        }

        /// <summary>
        /// �I���n�R�}���h�i�l�X�g���x����������j���ǂ����𔻒�
        /// </summary>
        public static bool IsEndCommand(string typeName)
        {
            return typeName is CommandTypes.LoopEnd or CommandTypes.IfEnd;
        }

        /// <summary>
        /// �R�}���h�̐ݒ��`���擾
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

            _logger?.LogWarning("�ݒ��`��������܂���: {CommandId}", commandId);
            return new List<SettingDefinition>();
        }

        /// <summary>
        /// �\�[�X�R���N�V�������擾
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

            _logger?.LogWarning("�\�[�X�R���N�V������������܂���: {CollectionName}", collectionName);
            return null;
        }

        /// <summary>
        /// �w�肳�ꂽ�R�}���h�^�C�v���J�n�R�}���h�iLoop�AIf�n�j���ǂ����𔻒�
        /// </summary>
        public static bool IsStartCommand(string commandType)
        {
            return commandType switch
            {
                "Loop" => true,
                "IF_ImageExist" => true,
                "IF_ImageNotExist" => true,
                "IF_ImageExist_AI" => true,
                "IF_ImageNotExist_AI" => true,
                "IF_Variable" => true,
                _ => false
            };
        }
    }
}

/// <summary>
/// �_�~�[Command�����i�r���g�C���R�}���h�p�̉������j
/// </summary>
internal class DummyCommand : ICommand
{
    public string CommandId { get; }
    public int LineNumber { get; set; }
    public bool IsEnabled { get; set; } = true;
    public ICommand? Parent { get; }
    public IEnumerable<ICommand> Children { get; private set; } = new List<ICommand>();
    public int NestLevel { get; set; }
    public object? Settings { get; set; }
    public string Description { get; }
    public event EventHandler? OnStartCommand;

    public DummyCommand(string commandId, ICommand? parent, AutoTool.Model.CommandDefinition.UniversalCommandItem item)
    {
        CommandId = commandId;
        Parent = parent;
        LineNumber = item.LineNumber;
        IsEnabled = item.IsEnable;
        Description = AutoTool.Model.CommandDefinition.DirectCommandRegistry.DisplayOrder.GetDescription(commandId);
    }

    public Task<bool> Execute(CancellationToken cancellationToken)
    {
        OnStartCommand?.Invoke(this, EventArgs.Empty);
        // �_�~�[�����F�������Ȃ�
        return Task.FromResult(true);
    }

    public void AddChild(ICommand child)
    {
        var children = Children.ToList();
        children.Add(child);
        Children = children;
    }

    public void RemoveChild(ICommand child)
    {
        var children = Children.ToList();
        children.Remove(child);
        Children = children;
    }

    public IEnumerable<ICommand> GetChildren()
    {
        return Children;
    }
}

/// <summary>
/// �ݒ荀�ڒ�`
/// </summary>
public class SettingDefinition : INotifyPropertyChanged
{
    private object? _currentValue;

    public string PropertyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AutoTool.Model.CommandDefinition.SettingControlType ControlType { get; set; }
    public string Category { get; set; } = "��{�ݒ�";
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
    /// ���ݒl
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
    /// ComboBox���Ŏg�p����\�[�X�A�C�e��
    /// </summary>
    public List<object> SourceItems { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
}