using System;
using System.Threading.Tasks;

namespace AutoTool.Services.Configuration
{
    /// <summary>
    /// �A�v���P�[�V�����ݒ�Ǘ��T�[�r�X�̃C���^�[�t�F�[�X
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// �ݒ�l���擾
        /// </summary>
        T GetValue<T>(string key, T defaultValue = default);

        /// <summary>
        /// �ݒ�l��ݒ�
        /// </summary>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// �ݒ���t�@�C���ɕۑ�
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// �ݒ���t�@�C������ǂݍ���
        /// </summary>
        Task LoadAsync();

        /// <summary>
        /// �ݒ�l�̕ύX���Ď�
        /// </summary>
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    }

    /// <summary>
    /// �ݒ�ύX�C�x���g����
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public string Key { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }

        public ConfigurationChangedEventArgs(string key, object? oldValue, object? newValue)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}