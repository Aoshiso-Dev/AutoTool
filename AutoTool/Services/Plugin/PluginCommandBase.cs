using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.Plugin;

namespace AutoTool.Services.Plugin
{
    /// <summary>
    /// �v���O�C���R�}���h�̊��N���X
    /// �v���O�C���J���҂��p�����ēƎ��̃R�}���h���쐬����
    /// </summary>
    public abstract class PluginCommandBase : BaseCommand
    {
        /// <summary>
        /// �v���O�C�����
        /// </summary>
        public MacroPanels.Plugin.IPluginInfo PluginInfo { get; }

        /// <summary>
        /// �R�}���h���
        /// </summary>
        public MacroPanels.Plugin.IPluginCommandInfo CommandInfo { get; }

        protected PluginCommandBase(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
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

    /// <summary>
    /// �v���O�C���R�}���h�t�@�N�g���[�̊��N���X
    /// �v���O�C���J���҂��p�����ăR�}���h�쐬���W�b�N����������
    /// </summary>
    public abstract class PluginCommandFactoryBase
    {
        /// <summary>
        /// �v���O�C�����
        /// </summary>
        public MacroPanels.Plugin.IPluginInfo PluginInfo { get; }

        protected PluginCommandFactoryBase(MacroPanels.Plugin.IPluginInfo pluginInfo)
        {
            PluginInfo = pluginInfo ?? throw new ArgumentNullException(nameof(pluginInfo));
        }

        /// <summary>
        /// ���p�\�ȃR�}���h�ꗗ���擾
        /// �h���N���X�Ŏ�������
        /// </summary>
        public abstract IEnumerable<MacroPanels.Plugin.IPluginCommandInfo> GetAvailableCommands();

        /// <summary>
        /// �R�}���h���쐬
        /// �h���N���X�Ŏ�������
        /// </summary>
        public abstract MacroPanels.Command.Interface.ICommand CreateCommand(string commandId, MacroPanels.Command.Interface.ICommand? parent, object? settings);

        /// <summary>
        /// �R�}���h�ݒ�̌^���擾
        /// �h���N���X�Ŏ�������
        /// </summary>
        public abstract Type? GetCommandSettingsType(string commandId);

        /// <summary>
        /// �v���O�C���R�}���h�����p�\���ǂ���
        /// �h���N���X�ŃI�[�o�[���C�h��
        /// </summary>
        public virtual bool IsCommandAvailable(string commandId)
        {
            return GetAvailableCommands().Any(c => c.Id == commandId);
        }
    }

    /// <summary>
    /// �ȒP�ȃv���O�C���R�}���h���쐬�̂��߂̃w���p�[
    /// </summary>
    public static class PluginCommandHelper
    {
        /// <summary>
        /// �v���O�C���R�}���h�����쐬
        /// </summary>
        public static MacroPanels.Plugin.IPluginCommandInfo CreateCommandInfo(
            string id, 
            string name, 
            string description, 
            string pluginId,
            Type commandType,
            Type? settingsType = null,
            string category = "�v���O�C��",
            string? iconPath = null)
        {
            return new PluginCommandInfoImpl
            {
                Id = id,
                Name = name,
                Description = description,
                Category = category,
                PluginId = pluginId,
                CommandType = commandType,
                SettingsType = settingsType,
                IconPath = iconPath
            };
        }

        /// <summary>
        /// �f�t�H���g�ݒ�I�u�W�F�N�g���쐬
        /// </summary>
        public static T? CreateDefaultSettings<T>() where T : class, new()
        {
            try
            {
                return new T();
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// �v���O�C���R�}���h���̓�������
    /// </summary>
    internal class PluginCommandInfoImpl : MacroPanels.Plugin.IPluginCommandInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string PluginId { get; set; } = string.Empty;
        public Type CommandType { get; set; } = typeof(object);
        public Type? SettingsType { get; set; }
        public string? IconPath { get; set; }
    }
}