using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using AutoTool.Command.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Model.CommandDefinition
{
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
        /// ������
        /// </summary>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            if (_initialized) return;

            lock (_lockObject)
            {
                if (_initialized) return;

                _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(nameof(DirectCommandRegistry));

                // �������t�^���ꂽCommand�N���X���������o
                var assembly = Assembly.GetExecutingAssembly();
                var commandTypes = assembly.GetTypes()
                    .Where(t => typeof(ICommand).IsAssignableFrom(t) && 
                               !t.IsInterface && !t.IsAbstract &&
                               t.GetCustomAttribute<DirectCommandAttribute>() != null)
                    .ToArray();

                _logger?.LogInformation("DirectCommandRegistry�������J�n: {Count}��Command�����o", commandTypes.Length);

                foreach (var commandType in commandTypes)
                {
                    RegisterCommand(commandType);
                }

                RegisterSourceCollections();
                _initialized = true;
                _logger?.LogInformation("DirectCommandRegistry����������: {Count}��Command��o�^", _commands.Count);
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
                var settings = ExtractSettingDefinitions(commandType);
                var factory = CreateCommandFactory(commandType);

                var registration = new CommandRegistration(
                    attr.CommandId,
                    commandType,
                    attr.DisplayName,
                    attr.Category,
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
                throw new InvalidOperationException("DirectCommandRegistry is not initialized. Call Initialize() first.");
            }

            if (_commands.TryGetValue(commandId, out var registration))
            {
                return registration.Factory(parent, item, serviceProvider);
            }

            return null;
        }

        /// <summary>
        /// UniversalCommandItem���쐬
        /// </summary>
        public static UniversalCommandItem CreateUniversalItem(string commandId, Dictionary<string, object?>? initialSettings = null)
        {
            var item = new UniversalCommandItem
            {
                ItemType = commandId,
                IsEnable = true
            };

            // �f�t�H���g�l��ݒ�
            if (_settingDefinitions.TryGetValue(commandId, out var definitions))
            {
                foreach (var definition in definitions)
                {
                    if (definition.DefaultValue != null)
                    {
                        item.SetSetting(definition.PropertyName, definition.DefaultValue);
                    }
                }
            }

            // �����ݒ�l���㏑��
            if (initialSettings != null)
            {
                foreach (var kvp in initialSettings)
                {
                    item.SetSetting(kvp.Key, kvp.Value);
                }
            }

            item.InitializeSettingDefinitions();
            return item;
        }

        /// <summary>
        /// �o�^����Ă���Command�ꗗ���擾
        /// </summary>
        public static IEnumerable<(string CommandId, string DisplayName, DynamicCommandCategory Category)> GetRegisteredCommands()
        {
            return _commands.Values.Select(r => (r.CommandId, r.DisplayName, r.Category));
        }

        /// <summary>
        /// �ݒ��`���擾
        /// </summary>
        public static List<SettingDefinition> GetSettingDefinitions(string commandId)
        {
            var result = _settingDefinitions.TryGetValue(commandId, out var definitions) 
                ? definitions.ToList() 
                : new List<SettingDefinition>();
                
            System.Diagnostics.Debug.WriteLine($"[DirectCommandRegistry] GetSettingDefinitions: {commandId} -> {result.Count}�̐ݒ��`");
            
            if (result.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[DirectCommandRegistry] ���p�\�ȃR�}���h: {string.Join(", ", _settingDefinitions.Keys)}");
            }
            
            return result;
        }

        /// <summary>
        /// �\�[�X�R���N�V�������擾
        /// </summary>
        public static object[]? GetSourceCollection(string collectionName)
        {
            return _sourceCollections.TryGetValue(collectionName, out var collection) ? collection : null;
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
        public SettingControlType ControlType { get; set; }
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
}