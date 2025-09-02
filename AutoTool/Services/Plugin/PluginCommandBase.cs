using System;
using System.Collections.Generic;
using System.Linq;
using AutoTool.Model.List.Interface;
using AutoTool.Services.Plugin;

namespace AutoTool.Services.Plugin
{
    /// <summary>
    /// Phase 5���S�����ŁF�v���O�C���R�}���h�x�[�X�N���X
    /// MacroPanels�ˑ����폜���AAutoTool�����ł̂ݎg�p
    /// </summary>
    public abstract class PluginCommandBase : ICommandListItem
    {
        // ICommandListItem�̎���
        public string ItemType { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public bool IsEnable { get; set; } = true;
        public int LineNumber { get; set; }
        public int NestLevel { get; set; }
        public virtual string Description { get; set; } = string.Empty;
        
        // Phase 5: ICommandListItem�̕s���v���p�e�B��ǉ�
        public bool IsRunning { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public bool IsInLoop { get; set; } = false;
        public bool IsInIf { get; set; } = false;
        public int Progress { get; set; } = 0;

        // �v���O�C�����
        public IPluginInfo PluginInfo { get; }
        public IPluginCommandInfo CommandInfo { get; }

        protected PluginCommandBase(IPluginInfo pluginInfo, IPluginCommandInfo commandInfo)
        {
            PluginInfo = pluginInfo ?? throw new ArgumentNullException(nameof(pluginInfo));
            CommandInfo = commandInfo ?? throw new ArgumentNullException(nameof(commandInfo));
            ItemType = commandInfo.Id;
        }

        protected virtual string GetDescription()
        {
            return $"[{PluginInfo.Name}] {CommandInfo.Name}";
        }

        public virtual ICommandListItem Clone()
        {
            // Phase 5: ��{�I�ȃN���[������
            var clone = (PluginCommandBase)Activator.CreateInstance(GetType(), PluginInfo, CommandInfo)!;
            clone.ItemType = this.ItemType;
            clone.Comment = this.Comment;
            clone.IsEnable = this.IsEnable;
            clone.LineNumber = this.LineNumber;
            clone.NestLevel = this.NestLevel;
            return clone;
        }

        /// <summary>
        /// �v���O�C���R�}���h�̎��s�����i�h���N���X�Ŏ����j
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// �v���O�C���R�}���h�̌��؏����i�h���N���X�Ŏ����j
        /// </summary>
        public virtual bool Validate()
        {
            return true;
        }

        /// <summary>
        /// �v���O�C���R�}���h�̏���������
        /// </summary>
        public virtual void Initialize()
        {
            // �f�t�H���g�ł͉������Ȃ�
        }

        /// <summary>
        /// �v���O�C���R�}���h�̃N���[���A�b�v����
        /// </summary>
        public virtual void Cleanup()
        {
            // �f�t�H���g�ł͉������Ȃ�
        }

        public override string ToString()
        {
            return $"[{LineNumber}] {Description}: {Comment}";
        }
    }

    /// <summary>
    /// Phase 5���S�����ŁF�v���O�C���R�}���h�t�@�N�g���[�x�[�X�N���X
    /// </summary>
    public abstract class PluginCommandFactoryBase
    {
        /// <summary>
        /// �v���O�C�����
        /// </summary>
        public IPluginInfo PluginInfo { get; }

        protected PluginCommandFactoryBase(IPluginInfo pluginInfo)
        {
            PluginInfo = pluginInfo ?? throw new ArgumentNullException(nameof(pluginInfo));
        }

        /// <summary>
        /// ���p�\�ȃR�}���h�ꗗ���擾�i�h���N���X�Ŏ����j
        /// </summary>
        public abstract IEnumerable<IPluginCommandInfo> GetAvailableCommands();

        /// <summary>
        /// �R�}���h���쐬�i�h���N���X�Ŏ����j
        /// </summary>
        public abstract object CreateCommand(string commandId, object? parent, object? settings);

        /// <summary>
        /// �R�}���h�ݒ�̌^���擾
        /// </summary>
        public virtual Type? GetCommandSettingsType(string commandId)
        {
            var commandInfo = GetAvailableCommands().FirstOrDefault(c => c.Id == commandId);
            return commandInfo?.SettingsType;
        }

        /// <summary>
        /// �R�}���h�����p�\���ǂ���
        /// </summary>
        public virtual bool IsCommandAvailable(string commandId)
        {
            return GetAvailableCommands().Any(c => c.Id == commandId);
        }

        /// <summary>
        /// �v���O�C���R�}���h�����쐬����w���p�[���\�b�h
        /// </summary>
        protected static IPluginCommandInfo CreateCommandInfo(
            string id,
            string name,
            string description,
            string category,
            string pluginId,
            Type commandType,
            Type? settingsType = null,
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
    }

    /// <summary>
    /// Phase 5���S�����ŁF�v���O�C���R�}���h�������N���X
    /// </summary>
    internal class PluginCommandInfoImpl : IPluginCommandInfo
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