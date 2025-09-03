using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Plugin;

namespace AutoTool.SamplePlugins
{
    /// <summary>
    /// �T���v���R�}���h�v���O�C��
    /// MacroPanels�ˑ����폜���AAutoTool�����ł̂ݎg�p
    /// </summary>

    #region �T���v���ݒ�C���^�[�t�F�[�X

    /// <summary>
    /// �T���v���R�}���h�ݒ�C���^�[�t�F�[�X
    /// </summary>
    public interface ISampleCommandSettings
    {
        string SampleText { get; set; }
        int SampleNumber { get; set; }
        bool SampleFlag { get; set; }
    }

    /// <summary>
    /// �t�@�C������ݒ�C���^�[�t�F�[�X
    /// </summary>
    public interface IFileOperationSettings
    {
        string FilePath { get; set; }
        string Operation { get; set; }
        bool CreateIfNotExists { get; set; }
    }

    #endregion

    #region �T���v���R�}���h����

    /// <summary>
    /// �T���v���v���O�C���R�}���h
    /// </summary>
    public class SamplePluginCommand : PluginCommandBase
    {
        public ISampleCommandSettings? Settings { get; set; }

        public SamplePluginCommand(IPluginInfo pluginInfo, IPluginCommandInfo commandInfo)
            : base(pluginInfo, commandInfo)
        {
        }

        public override void Execute()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"�T���v���R�}���h���s: {Settings?.SampleText ?? "�f�t�H���g�e�L�X�g"}");
                
                if (Settings != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  - �T���v���ԍ�: {Settings.SampleNumber}");
                    System.Diagnostics.Debug.WriteLine($"  - �T���v���t���O: {Settings.SampleFlag}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"�T���v���R�}���h���s�G���[: {ex.Message}");
            }
        }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(Settings?.SampleText);
        }

        protected override string GetDescription()
        {
            return $"�T���v���R�}���h: {Settings?.SampleText ?? "���ݒ�"}";
        }
    }

    /// <summary>
    /// �t�@�C������R�}���h
    /// </summary>
    public class FileOperationCommand : PluginCommandBase
    {
        public IFileOperationSettings? Settings { get; set; }

        public FileOperationCommand(IPluginInfo pluginInfo, IPluginCommandInfo commandInfo)
            : base(pluginInfo, commandInfo)
        {
        }

        public override void Execute()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"�t�@�C��������s: {Settings?.Operation ?? "�s��"} - {Settings?.FilePath ?? "�p�X���w��"}");
                
                // �t�@�C������̊ȈՎ���
                if (Settings != null && !string.IsNullOrEmpty(Settings.FilePath))
                {
                    switch (Settings.Operation?.ToLower())
                    {
                        case "create":
                            if (Settings.CreateIfNotExists && !System.IO.File.Exists(Settings.FilePath))
                            {
                                System.IO.File.WriteAllText(Settings.FilePath, $"�쐬����: {DateTime.Now}");
                                System.Diagnostics.Debug.WriteLine($"  - �t�@�C���쐬����: {Settings.FilePath}");
                            }
                            break;
                        case "delete":
                            if (System.IO.File.Exists(Settings.FilePath))
                            {
                                System.IO.File.Delete(Settings.FilePath);
                                System.Diagnostics.Debug.WriteLine($"  - �t�@�C���폜����: {Settings.FilePath}");
                            }
                            break;
                        default:
                            System.Diagnostics.Debug.WriteLine($"  - ���T�|�[�g����: {Settings.Operation}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"�t�@�C������G���[: {ex.Message}");
            }
        }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(Settings?.FilePath) && !string.IsNullOrEmpty(Settings?.Operation);
        }

        protected override string GetDescription()
        {
            return $"�t�@�C������: {Settings?.Operation ?? "���ݒ�"} - {System.IO.Path.GetFileName(Settings?.FilePath) ?? "�t�@�C�����ݒ�"}";
        }
    }

    #endregion

    #region �v���O�C�����

    /// <summary>
    /// �T���v���v���O�C�����
    /// </summary>
    public class SamplePluginInfo : IPluginInfo
    {
        public string Id { get; } = "SampleCommandPlugin";
        public string Name { get; } = "�T���v���R�}���h�v���O�C��";
        public string Version { get; } = "1.0.0";
        public string Description { get; } = "�T���v���v���O�C���ł�";
        public string Author { get; } = "AutoTool Development Team";
        public DateTime LoadedAt { get; set; } = DateTime.Now;
        public PluginStatus Status { get; set; } = PluginStatus.NotLoaded;
    }

    #endregion

    #region �v���O�C�����C���N���X

    /// <summary>
    /// �T���v���R�}���h�v���O�C��
    /// </summary>
    public class SampleCommandPlugin : IPlugin
    {
        private readonly IPluginInfo _pluginInfo;

        public IPluginInfo Info => _pluginInfo;

        public SampleCommandPlugin()
        {
            _pluginInfo = new SamplePluginInfo();
        }

        public async Task InitializeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"{Info.Name} �������J�n");
                
                // �񓯊������������̃V�~�����[�V����
                await Task.Delay(100);
                
                _pluginInfo.Status = PluginStatus.Active;
                System.Diagnostics.Debug.WriteLine($"{Info.Name} ����������");
            }
            catch (Exception ex)
            {
                _pluginInfo.Status = PluginStatus.Error;
                System.Diagnostics.Debug.WriteLine($"{Info.Name} �������G���[: {ex.Message}");
                throw;
            }
        }

        public async Task ShutdownAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"{Info.Name} �I�������J�n");
                
                // �񓯊��I�������̃V�~�����[�V����
                await Task.Delay(50);
                
                _pluginInfo.Status = PluginStatus.NotLoaded;
                System.Diagnostics.Debug.WriteLine($"{Info.Name} �I����������");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{Info.Name} �I�������G���[: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// ���p�\�ȃR�}���h�ꗗ���擾
        /// </summary>
        public IEnumerable<IPluginCommandInfo> GetAvailableCommands()
        {
            return new[]
            {
                new PluginCommandInfo
                {
                    Id = "SampleCommand",
                    Name = "�T���v���R�}���h",
                    Description = "�T���v���R�}���h�ł�",
                    Category = "�T���v��",
                    PluginId = Info.Id,
                    CommandType = typeof(SamplePluginCommand),
                    SettingsType = typeof(ISampleCommandSettings)
                },
                new PluginCommandInfo
                {
                    Id = "FileOperation",
                    Name = "�t�@�C������",
                    Description = "�t�@�C������R�}���h�ł�",
                    Category = "�t�@�C��",
                    PluginId = Info.Id,
                    CommandType = typeof(FileOperationCommand),
                    SettingsType = typeof(IFileOperationSettings)
                }
            };
        }

        /// <summary>
        /// �R�}���h���쐬
        /// </summary>
        public object CreateCommand(string commandId, object? parent, object? settings)
        {
            var commandInfo = GetAvailableCommands().FirstOrDefault(c => c.Id == commandId);
            if (commandInfo == null)
            {
                throw new ArgumentException($"�R�}���h��������܂���: {commandId}");
            }

            var command = Activator.CreateInstance(commandInfo.CommandType, Info, commandInfo);
            
            // �ݒ�K�p
            if (settings != null && command != null)
            {
                var settingsProperty = commandInfo.CommandType.GetProperty("Settings");
                settingsProperty?.SetValue(command, settings);
            }

            return command ?? throw new InvalidOperationException($"�R�}���h�̍쐬�Ɏ��s���܂���: {commandId}");
        }

        /// <summary>
        /// �R�}���h�ݒ�̌^���擾
        /// </summary>
        public Type? GetCommandSettingsType(string commandId)
        {
            var commandInfo = GetAvailableCommands().FirstOrDefault(c => c.Id == commandId);
            return commandInfo?.SettingsType;
        }

        /// <summary>
        /// �R�}���h�����p�\���ǂ���
        /// </summary>
        public bool IsCommandAvailable(string commandId)
        {
            return GetAvailableCommands().Any(c => c.Id == commandId);
        }
    }

    #endregion
}