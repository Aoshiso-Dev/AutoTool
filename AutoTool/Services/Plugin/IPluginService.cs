using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoTool.Services.Plugin
{
    /// <summary>
    /// Phase 5�����ŁF�v���O�C���V�X�e���̃C���^�[�t�F�[�X
    /// </summary>
    public interface IPluginService
    {
        /// <summary>
        /// �v���O�C�����ǂݍ��܂ꂽ���̃C�x���g
        /// </summary>
        event EventHandler<PluginLoadedEventArgs>? PluginLoaded;
        
        /// <summary>
        /// �v���O�C�����A�����[�h���ꂽ���̃C�x���g
        /// </summary>
        event EventHandler<PluginUnloadedEventArgs>? PluginUnloaded;

        /// <summary>
        /// �v���O�C����ǂݍ���
        /// </summary>
        Task LoadPluginAsync(string pluginPath);
        
        /// <summary>
        /// �S�Ẵv���O�C����ǂݍ���
        /// </summary>
        Task LoadAllPluginsAsync();
        
        /// <summary>
        /// �v���O�C�����A�����[�h
        /// </summary>
        Task UnloadPluginAsync(string pluginId);
        
        /// <summary>
        /// �ǂݍ��ݍς݃v���O�C���ꗗ���擾
        /// </summary>
        IEnumerable<IPluginInfo> GetLoadedPlugins();
        
        /// <summary>
        /// �v���O�C�����擾
        /// </summary>
        T? GetPlugin<T>(string pluginId) where T : class, IPlugin;
        
        /// <summary>
        /// �R�}���h�v���O�C������R�}���h���쐬�iPhase 5�����Łj
        /// </summary>
        object? CreatePluginCommand(string pluginId, string commandId, object? parent, object? settings);
        
        /// <summary>
        /// ���p�\�ȃv���O�C���R�}���h���擾
        /// </summary>
        IEnumerable<IPluginCommandInfo> GetAvailablePluginCommands();
    }

    /// <summary>
    /// Phase 5�����ŁF�v���O�C�����C���^�[�t�F�[�X
    /// </summary>
    public interface IPluginInfo
    {
        string Id { get; }
        string Name { get; }
        string Version { get; }
        string Description { get; }
        string Author { get; }
        DateTime LoadedAt { get; set; }
        PluginStatus Status { get; set; }
    }

    /// <summary>
    /// Phase 5�����ŁF�v���O�C���X�e�[�^�X
    /// </summary>
    public enum PluginStatus
    {
        NotLoaded,
        Loading,
        Loaded,
        Initializing,
        Active,
        Error,
        Unloading
    }

    /// <summary>
    /// Phase 5�����ŁF�v���O�C���ǂݍ��݃C�x���g����
    /// </summary>
    public class PluginLoadedEventArgs : EventArgs
    {
        public IPluginInfo PluginInfo { get; }
        public PluginLoadedEventArgs(IPluginInfo pluginInfo) => PluginInfo = pluginInfo;
    }

    /// <summary>
    /// Phase 5�����ŁF�v���O�C���A�����[�h�C�x���g����
    /// </summary>
    public class PluginUnloadedEventArgs : EventArgs
    {
        public string PluginId { get; }
        public PluginUnloadedEventArgs(string pluginId) => PluginId = pluginId;
    }

    /// <summary>
    /// Phase 5�����ŁF�v���O�C���R�}���h���
    /// </summary>
    public interface IPluginCommandInfo
    {
        /// <summary>
        /// �R�}���hID
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// �\����
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// ����
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// �J�e�S��
        /// </summary>
        string Category { get; }
        
        /// <summary>
        /// �v���O�C��ID
        /// </summary>
        string PluginId { get; }
        
        /// <summary>
        /// �R�}���h�^
        /// </summary>
        Type CommandType { get; }
        
        /// <summary>
        /// �ݒ�^
        /// </summary>
        Type? SettingsType { get; }
        
        /// <summary>
        /// �A�C�R���p�X�i�I�v�V�����j
        /// </summary>
        string? IconPath { get; }
    }

    /// <summary>
    /// Phase 5�����ŁF�v���O�C���̊��C���^�[�t�F�[�X
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// �v���O�C�����
        /// </summary>
        IPluginInfo Info { get; }
        
        /// <summary>
        /// �v���O�C��������
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// �v���O�C���I��
        /// </summary>
        Task ShutdownAsync();
    }
}