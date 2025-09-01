using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Linq;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.Plugin;
using OpenCVHelper;
using KeyHelper;
using MouseHelper;
using WpfCommand = System.Windows.Input.ICommand;
using MacroCommand = MacroPanels.Command.Interface.ICommand;

namespace MacroPanels.Plugin.CommandPlugins
{
    /// <summary>
    /// �v���O�C���R�}���h�̊��N���X
    /// </summary>
    public abstract class PluginCommandBase : BaseCommand
    {
        /// <summary>
        /// �v���O�C�����
        /// </summary>
        public IPluginInfo PluginInfo { get; }

        /// <summary>
        /// �R�}���h���
        /// </summary>
        public IPluginCommandInfo CommandInfo { get; }

        protected PluginCommandBase(IPluginInfo pluginInfo, IPluginCommandInfo commandInfo, 
            MacroCommand? parent = null, object? settings = null) 
            : base(parent, settings)
        {
            PluginInfo = pluginInfo ?? throw new ArgumentNullException(nameof(pluginInfo));
            CommandInfo = commandInfo ?? throw new ArgumentNullException(nameof(commandInfo));
            Description = $"[{pluginInfo.Name}] {commandInfo.Name}";
        }

        /// <summary>
        /// �v���O�C���R�}���h�̎��ۂ̎��s����
        /// �h���N���X�Ŏ�������
        /// </summary>
        protected abstract Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken);

        /// <summary>
        /// BaseCommand�̎��s�������I�[�o�[���C�h
        /// </summary>
        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                LogMessage($"�v���O�C���R�}���h�J�n: {PluginInfo.Name}.{CommandInfo.Name}");
                
                var result = await DoExecutePluginAsync(cancellationToken);
                
                LogMessage($"�v���O�C���R�}���h�I��: {PluginInfo.Name}.{CommandInfo.Name} - {(result ? "����" : "���s")}");
                
                return result;
            }
            catch (Exception ex)
            {
                LogMessage($"? �v���O�C���R�}���h�G���[: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// �v���O�C���ݒ���擾�i�^���S�j
        /// </summary>
        protected T? GetPluginSettings<T>() where T : class
        {
            return Settings as T;
        }

        /// <summary>
        /// �v���O�C���ݒ�̌���
        /// </summary>
        protected virtual void ValidatePluginSettings()
        {
            // ���N���X�ł͉������Ȃ��i�h���N���X�ŃI�[�o�[���C�h�j
        }

        /// <summary>
        /// �t�@�C�����؂��I�[�o�[���C�h
        /// </summary>
        protected override void ValidateFiles()
        {
            base.ValidateFiles();
            ValidatePluginSettings();
        }
    }

    #region �T���v���R�}���h

    /// <summary>
    /// �T���v���v���O�C���R�}���h�̐ݒ�
    /// </summary>
    public interface ISampleCommandSettings : ICommandSettings
    {
        string Message { get; set; }
        int DelayMs { get; set; }
    }

    /// <summary>
    /// �T���v���ݒ�̎���
    /// </summary>
    public class SampleCommandSettings : ISampleCommandSettings
    {
        public string Message { get; set; } = "Hello from Plugin!";
        public int DelayMs { get; set; } = 1000;
    }

    /// <summary>
    /// �T���v���v���O�C���R�}���h
    /// </summary>
    public class SamplePluginCommand : PluginCommandBase
    {
        public new ISampleCommandSettings? Settings => base.Settings as ISampleCommandSettings;

        public SamplePluginCommand(IPluginInfo pluginInfo, IPluginCommandInfo commandInfo, 
            MacroCommand? parent = null, object? settings = null) 
            : base(pluginInfo, commandInfo, parent, settings)
        {
        }

        protected override async Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken)
        {
            var settings = Settings ?? new SampleCommandSettings();
            
            LogMessage($"�v���O�C���R�}���h�J�n: {settings.Message}");
            
            // �w�肵�����Ԃ����ҋ@
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalMilliseconds < settings.DelayMs)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                // �i����
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                ReportProgress(elapsed, settings.DelayMs);
                
                await Task.Delay(50, cancellationToken);
            }
            
            LogMessage($"�v���O�C���R�}���h����: {settings.Message}");
            return true;
        }

        protected override void ValidatePluginSettings()
        {
            var settings = Settings;
            if (settings != null)
            {
                if (settings.DelayMs < 0)
                    throw new ArgumentException("DelayMs��0�ȏ�ł���K�v������܂�");
                    
                if (string.IsNullOrEmpty(settings.Message))
                    throw new ArgumentException("Message�͕K�{�ł�");
            }
        }
    }

    #endregion

    #region �t�@�C������R�}���h

    /// <summary>
    /// �t�@�C������v���O�C���R�}���h�̐ݒ�
    /// </summary>
    public interface IFileOperationSettings : ICommandSettings
    {
        string SourcePath { get; set; }
        string DestinationPath { get; set; }
        string Operation { get; set; } // "Copy", "Move", "Delete"
    }

    /// <summary>
    /// �t�@�C������ݒ�̎���
    /// </summary>
    public class FileOperationSettings : IFileOperationSettings
    {
        public string SourcePath { get; set; } = "";
        public string DestinationPath { get; set; } = "";
        public string Operation { get; set; } = "Copy";
    }

    /// <summary>
    /// �t�@�C������v���O�C���R�}���h
    /// </summary>
    public class FileOperationCommand : PluginCommandBase
    {
        public new IFileOperationSettings? Settings => base.Settings as IFileOperationSettings;

        public FileOperationCommand(IPluginInfo pluginInfo, IPluginCommandInfo commandInfo, 
            MacroCommand? parent = null, object? settings = null) 
            : base(pluginInfo, commandInfo, parent, settings)
        {
        }

        protected override async Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            try
            {
                LogMessage($"�t�@�C������J�n: {settings.Operation}");

                await Task.Run(() =>
                {
                    switch (settings.Operation.ToLower())
                    {
                        case "copy":
                            File.Copy(settings.SourcePath, settings.DestinationPath, true);
                            LogMessage($"�t�@�C�����R�s�[���܂���: {settings.SourcePath} -> {settings.DestinationPath}");
                            break;

                        case "move":
                            if (File.Exists(settings.DestinationPath))
                                File.Delete(settings.DestinationPath);
                            File.Move(settings.SourcePath, settings.DestinationPath);
                            LogMessage($"�t�@�C�����ړ����܂���: {settings.SourcePath} -> {settings.DestinationPath}");
                            break;

                        case "delete":
                            File.Delete(settings.SourcePath);
                            LogMessage($"�t�@�C�����폜���܂���: {settings.SourcePath}");
                            break;

                        default:
                            throw new ArgumentException($"���Ή��̑���: {settings.Operation}");
                    }
                }, cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"? �t�@�C������G���[: {ex.Message}");
                return false;
            }
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null)
            {
                ValidateFileExists(settings.SourcePath, "����Ώۃt�@�C��");
                
                if (settings.Operation.ToLower() != "delete" && !string.IsNullOrEmpty(settings.DestinationPath))
                {
                    var destDir = Path.GetDirectoryName(settings.DestinationPath);
                    if (!string.IsNullOrEmpty(destDir))
                    {
                        ValidateDirectoryExists(destDir, "�o�͐�f�B���N�g��");
                    }
                }
            }
        }

        protected override void ValidatePluginSettings()
        {
            var settings = Settings;
            if (settings != null)
            {
                if (string.IsNullOrEmpty(settings.SourcePath))
                    throw new ArgumentException("SourcePath�͕K�{�ł�");

                var validOperations = new[] { "copy", "move", "delete" };
                if (!Array.Exists(validOperations, op => op.Equals(settings.Operation, StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException($"�����ȑ���: {settings.Operation}");

                if (settings.Operation.ToLower() != "delete" && string.IsNullOrEmpty(settings.DestinationPath))
                    throw new ArgumentException("Copy/Move����ɂ�DestinationPath���K�v�ł�");
            }
        }
    }

    #endregion

    /// <summary>
    /// �W���R�}���h�v���O�C���̎���
    /// </summary>
    public class StandardCommandPlugin : ICommandPlugin
    {
        private readonly IPluginInfo _pluginInfo;
        private readonly List<IPluginCommandInfo> _availableCommands;

        public IPluginInfo Info => _pluginInfo;

        public StandardCommandPlugin()
        {
            _pluginInfo = new StandardPluginInfo();
            _availableCommands = CreateCommandList();
        }

        public async Task InitializeAsync()
        {
            // �v���O�C������������
            await Task.CompletedTask;
        }

        public async Task ShutdownAsync()
        {
            // �v���O�C���I������
            await Task.CompletedTask;
        }

        public IEnumerable<IPluginCommandInfo> GetAvailableCommands()
        {
            return _availableCommands;
        }

        public MacroCommand CreateCommand(string commandId, MacroCommand? parent, object? settings)
        {
            var commandInfo = _availableCommands.FirstOrDefault(c => c.Id == commandId);
            if (commandInfo == null)
                throw new ArgumentException($"���m�̃R�}���hID: {commandId}");

            return commandId switch
            {
                "SampleMessage" => new SamplePluginCommand(_pluginInfo, commandInfo, parent, settings),
                "FileOperation" => new FileOperationCommand(_pluginInfo, commandInfo, parent, settings),
                _ => throw new ArgumentException($"���Ή��̃R�}���hID: {commandId}")
            };
        }

        public Type? GetCommandSettingsType(string commandId)
        {
            return commandId switch
            {
                "SampleMessage" => typeof(ISampleCommandSettings),
                "FileOperation" => typeof(IFileOperationSettings),
                _ => null
            };
        }

        public bool IsCommandAvailable(string commandId)
        {
            return _availableCommands.Any(c => c.Id == commandId);
        }

        private List<IPluginCommandInfo> CreateCommandList()
        {
            return new List<IPluginCommandInfo>
            {
                new PluginCommandInfo
                {
                    Id = "SampleMessage",
                    Name = "���b�Z�[�W�\��",
                    Description = "�J�X�^�����b�Z�[�W��\�����đҋ@����",
                    Category = "�T���v��",
                    PluginId = _pluginInfo.Id,
                    CommandType = typeof(SamplePluginCommand),
                    SettingsType = typeof(ISampleCommandSettings)
                },

                new PluginCommandInfo
                {
                    Id = "FileOperation",
                    Name = "�t�@�C������",
                    Description = "�t�@�C���̃R�s�[�A�ړ��A�폜���s��",
                    Category = "�t�@�C��",
                    PluginId = _pluginInfo.Id,
                    CommandType = typeof(FileOperationCommand),
                    SettingsType = typeof(IFileOperationSettings)
                }
            };
        }
    }

    /// <summary>
    /// �W���v���O�C�����
    /// </summary>
    public class StandardPluginInfo : IPluginInfo
    {
        public string Id => "StandardCommands";
        public string Name => "�W���R�}���h�v���O�C��";
        public string Version => "1.0.0";
        public string Description => "��{�I�ȃ}�N���R�}���h�i�t�@�C������A�T���v�����j";
        public string Author => "MacroPanels";
        public DateTime LoadedAt { get; set; } = DateTime.UtcNow;
        public PluginStatus Status { get; set; } = PluginStatus.NotLoaded;
    }
}