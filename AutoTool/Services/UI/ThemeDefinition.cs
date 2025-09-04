using System;
using System.Collections.Generic;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// �A�v���P�[�V�����ŗ��p�\�ȃe�[�}
    /// </summary>
    public enum AppTheme
    {
        Light,
        Dark,
        Auto,       // �V�X�e���ݒ�ɒǏ]
        HighContrast // ���R���g���X�g
    }

    /// <summary>
    /// �e�[�}��`���
    /// </summary>
    public class ThemeDefinition
    {
        public AppTheme Theme { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string ResourceDictionary { get; set; } = string.Empty;
        public string AccentColor { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSystemTheme { get; set; }
    }

    /// <summary>
    /// �e�[�}��`�̃t�@�N�g���[
    /// </summary>
    public static class ThemeDefinitionFactory
    {
        private static readonly Dictionary<AppTheme, ThemeDefinition> _themeDefinitions = new()
        {
            {
                AppTheme.Light,
                new ThemeDefinition
                {
                    Theme = AppTheme.Light,
                    DisplayName = "���C�g�e�[�}",
                    ResourceDictionary = "pack://application:,,,/AutoTool;component/Themes/LightTheme.xaml",
                    AccentColor = "#0066CC",
                    Description = "���邢�F���̃e�[�}�ł��B�����̎g�p�ɍœK�ł��B",
                    IsSystemTheme = false
                }
            },
            {
                AppTheme.Dark,
                new ThemeDefinition
                {
                    Theme = AppTheme.Dark,
                    DisplayName = "�_�[�N�e�[�}",
                    ResourceDictionary = "pack://application:,,,/AutoTool;component/Themes/DarkTheme.xaml",
                    AccentColor = "#66B2FF",
                    Description = "�Â��F���̃e�[�}�ł��B��ԂⒷ���Ԃ̎g�p�ɖڂɗD�����ł��B",
                    IsSystemTheme = false
                }
            },
            {
                AppTheme.Auto,
                new ThemeDefinition
                {
                    Theme = AppTheme.Auto,
                    DisplayName = "�����i�V�X�e���Ǐ]�j",
                    ResourceDictionary = "", // ���s���Ɍ���
                    AccentColor = "", // ���s���Ɍ���
                    Description = "Windows�̃V�X�e���ݒ�ɍ��킹�ăe�[�}�������ύX���܂��B",
                    IsSystemTheme = true
                }
            },
            {
                AppTheme.HighContrast,
                new ThemeDefinition
                {
                    Theme = AppTheme.HighContrast,
                    DisplayName = "���R���g���X�g",
                    ResourceDictionary = "pack://application:,,,/AutoTool;component/Themes/HighContrastTheme.xaml",
                    AccentColor = "#FFFF00",
                    Description = "���F�������߂����R���g���X�g�e�[�}�ł��B�A�N�Z�V�r���e�B�ɔz�����Ă��܂��B",
                    IsSystemTheme = false
                }
            }
        };

        /// <summary>
        /// �w�肳�ꂽ�e�[�}�̒�`���擾
        /// </summary>
        public static ThemeDefinition GetThemeDefinition(AppTheme theme)
        {
            return _themeDefinitions.TryGetValue(theme, out var definition) 
                ? definition 
                : _themeDefinitions[AppTheme.Light];
        }

        /// <summary>
        /// ���ׂẴe�[�}��`���擾
        /// </summary>
        public static IEnumerable<ThemeDefinition> GetAllThemeDefinitions()
        {
            return _themeDefinitions.Values;
        }

        /// <summary>
        /// �����񂩂�AppTheme�ɕϊ�
        /// </summary>
        public static AppTheme ParseTheme(string themeString)
        {
            if (Enum.TryParse<AppTheme>(themeString, true, out var theme))
            {
                return theme;
            }
            return AppTheme.Light; // �f�t�H���g
        }

        /// <summary>
        /// AppTheme���當����ɕϊ�
        /// </summary>
        public static string ThemeToString(AppTheme theme)
        {
            return theme.ToString();
        }
    }
}