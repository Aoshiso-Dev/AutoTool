using System;
using System.Windows;

namespace AutoTool.Services.Theme
{
    /// <summary>
    /// �e�[�}�Ǘ��T�[�r�X�̃C���^�[�t�F�[�X
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// ���݂̃e�[�}
        /// </summary>
        AppTheme CurrentTheme { get; }

        /// <summary>
        /// �e�[�}��ύX
        /// </summary>
        void SetTheme(AppTheme theme);

        /// <summary>
        /// �e�[�}�ύX�C�x���g
        /// </summary>
        event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        /// <summary>
        /// ���p�\�ȃe�[�}�ꗗ���擾
        /// </summary>
        AppTheme[] GetAvailableThemes();
    }

    /// <summary>
    /// �A�v���P�[�V�����e�[�}�̎��
    /// </summary>
    public enum AppTheme
    {
        Light,
        Dark,
        Auto // �V�X�e���ݒ�ɏ]��
    }

    /// <summary>
    /// �e�[�}�ύX�C�x���g����
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        public AppTheme OldTheme { get; }
        public AppTheme NewTheme { get; }

        public ThemeChangedEventArgs(AppTheme oldTheme, AppTheme newTheme)
        {
            OldTheme = oldTheme;
            NewTheme = newTheme;
        }
    }
}