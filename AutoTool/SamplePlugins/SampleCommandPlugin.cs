using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Linq;
using AutoTool.Services.Plugin;
using MacroPanels.Command.Interface;
using MacroPanels.Plugin;
using OpenCVHelper;
using KeyHelper;
using MouseHelper;
using System.Windows.Input;

namespace AutoTool.SamplePlugins
{
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

        public SamplePluginCommand(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
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

        public FileOperationCommand(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
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

    #region �����R�}���h�̃v���O�C���Ŏ���

    /// <summary>
    /// �摜�ҋ@�v���O�C���R�}���h
    /// </summary>
    public class WaitImagePluginCommand : PluginCommandBase
    {
        public new IWaitImageCommandSettings? Settings => base.Settings as IWaitImageCommandSettings;

        public WaitImagePluginCommand(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
            : base(pluginInfo, commandInfo, parent, settings)
        {
        }

        protected override void ValidatePluginSettings()
        {
            var settings = Settings;
            if (settings != null)
            {
                if (string.IsNullOrEmpty(settings.ImagePath))
                    throw new ArgumentException("ImagePath�͕K�{�ł�");
                
                ValidateFileExists(settings.ImagePath, "�摜�t�@�C��");
                
                if (settings.Timeout <= 0)
                    throw new ArgumentException("Timeout��0���傫���l�ł���K�v������܂�");
                    
                if (settings.Interval <= 0)
                    throw new ArgumentException("Interval��0���傫���l�ł���K�v������܂�");
            }
        }

        protected override async Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            LogMessage($"�摜�ҋ@�J�n: {settings.ImagePath}");
            
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Timeout)
            {
                var point = await ImageSearchHelper.SearchImage(
                    settings.ImagePath, cancellationToken, settings.Threshold, 
                    settings.SearchColor, settings.WindowTitle, settings.WindowClassName);

                if (point != null)
                {
                    LogMessage($"�摜��������܂����B({point.Value.X}, {point.Value.Y})");
                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, settings.Timeout);
                await Task.Delay(settings.Interval, cancellationToken);
            }

            LogMessage("�摜��������܂���ł����B");
            return false;
        }
    }

    /// <summary>
    /// �摜�N���b�N�v���O�C���R�}���h
    /// </summary>
    public class ClickImagePluginCommand : PluginCommandBase
    {
        public new IClickImageCommandSettings? Settings => base.Settings as IClickImageCommandSettings;

        public ClickImagePluginCommand(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
            : base(pluginInfo, commandInfo, parent, settings)
        {
        }

        protected override void ValidatePluginSettings()
        {
            var settings = Settings;
            if (settings != null)
            {
                if (string.IsNullOrEmpty(settings.ImagePath))
                    throw new ArgumentException("ImagePath�͕K�{�ł�");
                
                ValidateFileExists(settings.ImagePath, "�摜�t�@�C��");
                
                if (settings.Timeout <= 0)
                    throw new ArgumentException("Timeout��0���傫���l�ł���K�v������܂�");
                    
                if (settings.Interval <= 0)
                    throw new ArgumentException("Interval��0���傫���l�ł���K�v������܂�");
            }
        }

        protected override async Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            LogMessage($"�摜�N���b�N�J�n: {settings.ImagePath}");
            
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Timeout)
            {
                var point = await ImageSearchHelper.SearchImage(
                    settings.ImagePath, cancellationToken, settings.Threshold,
                    settings.SearchColor, settings.WindowTitle, settings.WindowClassName);

                if (point != null)
                {
                    await ExecuteMouseClick(point.Value.X, point.Value.Y, settings.Button, 
                        settings.WindowTitle, settings.WindowClassName);
                    
                    LogMessage($"�摜���N���b�N���܂����B({point.Value.X}, {point.Value.Y})");
                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, settings.Timeout);
                await Task.Delay(settings.Interval, cancellationToken);
            }

            LogMessage("�摜��������܂���ł����B");
            return false;
        }

        private static async Task ExecuteMouseClick(int x, int y, MouseButton button, 
            string windowTitle, string windowClassName)
        {
            switch (button)
            {
                case MouseButton.Left:
                    await MouseHelper.Input.ClickAsync(x, y, windowTitle, windowClassName);
                    break;
                case MouseButton.Right:
                    await MouseHelper.Input.RightClickAsync(x, y, windowTitle, windowClassName);
                    break;
                case MouseButton.Middle:
                    await MouseHelper.Input.MiddleClickAsync(x, y, windowTitle, windowClassName);
                    break;
                default:
                    throw new ArgumentException($"�T�|�[�g����Ă��Ȃ��}�E�X�{�^��: {button}");
            }
        }
    }

    /// <summary>
    /// �z�b�g�L�[�v���O�C���R�}���h
    /// </summary>
    public class HotkeyPluginCommand : PluginCommandBase
    {
        public new IHotkeyCommandSettings? Settings => base.Settings as IHotkeyCommandSettings;

        public HotkeyPluginCommand(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
            : base(pluginInfo, commandInfo, parent, settings)
        {
        }

        protected override async Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            var keyDescription = BuildKeyDescription(settings);
            LogMessage($"�z�b�g�L�[���s: {keyDescription}");
            
            await Task.Run(() => KeyHelper.Input.KeyPress(
                settings.Key, settings.Ctrl, settings.Alt, settings.Shift,
                settings.WindowTitle, settings.WindowClassName));

            LogMessage("�z�b�g�L�[�����s���܂����B");
            return true;
        }

        private string BuildKeyDescription(IHotkeyCommandSettings settings)
        {
            var parts = new List<string>();
            if (settings.Ctrl) parts.Add("Ctrl");
            if (settings.Alt) parts.Add("Alt");
            if (settings.Shift) parts.Add("Shift");
            parts.Add(settings.Key.ToString());
            
            return string.Join(" + ", parts);
        }
    }

    /// <summary>
    /// �ҋ@�v���O�C���R�}���h
    /// </summary>
    public class WaitPluginCommand : PluginCommandBase
    {
        public new IWaitCommandSettings? Settings => base.Settings as IWaitCommandSettings;

        public WaitPluginCommand(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
            : base(pluginInfo, commandInfo, parent, settings)
        {
        }

        protected override void ValidatePluginSettings()
        {
            var settings = Settings;
            if (settings != null)
            {
                if (settings.Wait < 0)
                    throw new ArgumentException("Wait��0�ȏ�ł���K�v������܂�");
            }
        }

        protected override async Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            LogMessage($"�ҋ@�J�n: {settings.Wait}ms");
            
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Wait)
            {
                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, settings.Wait);
                await Task.Delay(50, cancellationToken);
            }

            LogMessage("�ҋ@���������܂����B");
            return true;
        }
    }

    #endregion

    /// <summary>
    /// �T���v���R�}���h�v���O�C��
    /// </summary>
    public class SampleCommandPlugin : MacroPanels.Plugin.ICommandPlugin
    {
        private readonly MacroPanels.Plugin.IPluginInfo _pluginInfo;
        private readonly PluginCommandFactoryBase _commandFactory;

        public MacroPanels.Plugin.IPluginInfo Info => _pluginInfo;

        public SampleCommandPlugin()
        {
            _pluginInfo = new SamplePluginInfo();
            _commandFactory = new SampleCommandFactory(_pluginInfo);
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

        public IEnumerable<MacroPanels.Plugin.IPluginCommandInfo> GetAvailableCommands()
        {
            return _commandFactory.GetAvailableCommands();
        }

        public MacroPanels.Command.Interface.ICommand CreateCommand(string commandId, MacroPanels.Command.Interface.ICommand? parent, object? settings)
        {
            return _commandFactory.CreateCommand(commandId, parent, settings);
        }

        public Type? GetCommandSettingsType(string commandId)
        {
            return _commandFactory.GetCommandSettingsType(commandId);
        }

        public bool IsCommandAvailable(string commandId)
        {
            return _commandFactory.IsCommandAvailable(commandId);
        }
    }

    /// <summary>
    /// �T���v���v���O�C�����
    /// </summary>
    public class SamplePluginInfo : MacroPanels.Plugin.IPluginInfo
    {
        public string Id => "SamplePlugin";
        public string Name => "�T���v���v���O�C��";
        public string Version => "1.0.0";
        public string Description => "�v���O�C���V�X�e���̃T���v������";
        public string Author => "AutoTool";
        public DateTime LoadedAt { get; set; } = DateTime.UtcNow;
        public MacroPanels.Plugin.PluginStatus Status { get; set; } = MacroPanels.Plugin.PluginStatus.NotLoaded;
    }

    /// <summary>
    /// �T���v���R�}���h�t�@�N�g���[
    /// </summary>
    public class SampleCommandFactory : PluginCommandFactoryBase
    {
        private readonly List<MacroPanels.Plugin.IPluginCommandInfo> _availableCommands;

        public SampleCommandFactory(MacroPanels.Plugin.IPluginInfo pluginInfo) : base(pluginInfo)
        {
            _availableCommands = new List<MacroPanels.Plugin.IPluginCommandInfo>
            {
                PluginCommandHelper.CreateCommandInfo(
                    "SampleMessage", 
                    "���b�Z�[�W�\��", 
                    "�J�X�^�����b�Z�[�W��\�����đҋ@����", 
                    pluginInfo.Id,
                    typeof(SamplePluginCommand),
                    typeof(ISampleCommandSettings),
                    "�T���v��"),

                PluginCommandHelper.CreateCommandInfo(
                    "FileOperation", 
                    "�t�@�C������", 
                    "�t�@�C���̃R�s�[�A�ړ��A�폜���s��", 
                    pluginInfo.Id,
                    typeof(FileOperationCommand),
                    typeof(IFileOperationSettings),
                    "�t�@�C��"),

                PluginCommandHelper.CreateCommandInfo(
                    "WaitImage", 
                    "�摜�ҋ@", 
                    "�w�肵���摜��������܂őҋ@����", 
                    pluginInfo.Id,
                    typeof(WaitImagePluginCommand),
                    typeof(IWaitImageCommandSettings),
                    "�摜"),

                PluginCommandHelper.CreateCommandInfo(
                    "ClickImage", 
                    "�摜�N���b�N", 
                    "�w�肵���摜�������ăN���b�N����", 
                    pluginInfo.Id,
                    typeof(ClickImagePluginCommand),
                    typeof(IClickImageCommandSettings),
                    "�摜"),

                PluginCommandHelper.CreateCommandInfo(
                    "Hotkey", 
                    "�z�b�g�L�[", 
                    "�L�[�{�[�h�V���[�g�J�b�g�𑗐M����", 
                    pluginInfo.Id,
                    typeof(HotkeyPluginCommand),
                    typeof(IHotkeyCommandSettings),
                    "�L�[�{�[�h"),

                PluginCommandHelper.CreateCommandInfo(
                    "Wait", 
                    "�ҋ@", 
                    "�w�肵�����Ԃ����ҋ@����", 
                    pluginInfo.Id,
                    typeof(WaitPluginCommand),
                    typeof(IWaitCommandSettings),
                    "����")
            };
        }

        public override IEnumerable<MacroPanels.Plugin.IPluginCommandInfo> GetAvailableCommands()
        {
            return _availableCommands;
        }

        public override MacroPanels.Command.Interface.ICommand CreateCommand(string commandId, MacroPanels.Command.Interface.ICommand? parent, object? settings)
        {
            var commandInfo = _availableCommands.FirstOrDefault(c => c.Id == commandId);
            if (commandInfo == null)
                throw new ArgumentException($"���m�̃R�}���hID: {commandId}");

            return commandId switch
            {
                "SampleMessage" => new SamplePluginCommand(PluginInfo, commandInfo, parent, settings),
                "FileOperation" => new FileOperationCommand(PluginInfo, commandInfo, parent, settings),
                "WaitImage" => new WaitImagePluginCommand(PluginInfo, commandInfo, parent, settings),
                "ClickImage" => new ClickImagePluginCommand(PluginInfo, commandInfo, parent, settings),
                "Hotkey" => new HotkeyPluginCommand(PluginInfo, commandInfo, parent, settings),
                "Wait" => new WaitPluginCommand(PluginInfo, commandInfo, parent, settings),
                _ => throw new ArgumentException($"���Ή��̃R�}���hID: {commandId}")
            };
        }

        public override Type? GetCommandSettingsType(string commandId)
        {
            return commandId switch
            {
                "SampleMessage" => typeof(ISampleCommandSettings),
                "FileOperation" => typeof(IFileOperationSettings),
                "WaitImage" => typeof(IWaitImageCommandSettings),
                "ClickImage" => typeof(IClickImageCommandSettings),
                "Hotkey" => typeof(IHotkeyCommandSettings),
                "Wait" => typeof(IWaitCommandSettings),
                _ => null
            };
        }
    }
}