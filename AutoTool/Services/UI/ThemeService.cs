using System;
using System.Windows;
using System.Linq;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using System.Collections.Generic;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// �A�v���P�[�V�����ݒ�̊Ǘ��T�[�r�X
    /// </summary>
    public interface IAppSettingsService
    {
        bool AutoSave { get; set; }
        int AutoSaveInterval { get; set; }
        int DefaultTimeout { get; set; }
        int DefaultInterval { get; set; }
        
        void SaveSettings();
        void LoadSettings();
        void ResetToDefaults();
        
        event EventHandler SettingsChanged;
    }

    /// <summary>
    /// �A�v���P�[�V�����̃e�[�}�ƊO�ς��Ǘ�����T�[�r�X�i�����Łj
    /// </summary>
    public interface IEnhancedThemeService
    {
        AppTheme CurrentTheme { get; }
        string CurrentLanguage { get; }
        ThemeDefinition CurrentThemeDefinition { get; }
        
        void SetTheme(AppTheme theme);
        void SetLanguage(string language);
        AppTheme[] GetAvailableThemes();
        ThemeDefinition GetThemeDefinition(AppTheme theme);
        
        event EventHandler<ThemeChangedEventArgs> ThemeChanged;
        event EventHandler<LanguageChangedEventArgs> LanguageChanged;
    }

    /// <summary>
    /// �e�[�}�ύX�C�x���g�����i�����Łj
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        public AppTheme OldTheme { get; }
        public AppTheme NewTheme { get; }
        public ThemeDefinition NewThemeDefinition { get; }

        public ThemeChangedEventArgs(AppTheme oldTheme, AppTheme newTheme, ThemeDefinition newThemeDefinition)
        {
            OldTheme = oldTheme;
            NewTheme = newTheme;
            NewThemeDefinition = newThemeDefinition;
        }
    }

    /// <summary>
    /// ����ύX�C�x���g����
    /// </summary>
    public class LanguageChangedEventArgs : EventArgs
    {
        public string OldLanguage { get; }
        public string NewLanguage { get; }

        public LanguageChangedEventArgs(string oldLanguage, string newLanguage)
        {
            OldLanguage = oldLanguage;
            NewLanguage = newLanguage;
        }
    }

    /// <summary>
    /// �������ꂽ�e�[�}�T�[�r�X�iWPF�����Łj
    /// </summary>
    public partial class EnhancedThemeService : ObservableObject, IEnhancedThemeService
    {
        private readonly ILogger<EnhancedThemeService> _logger;
        private readonly IEnhancedConfigurationService _configService;

        [ObservableProperty]
        private AppTheme _currentTheme = AppTheme.Light;

        [ObservableProperty] 
        private string _currentLanguage = "ja-JP";

        [ObservableProperty]
        private ThemeDefinition _currentThemeDefinition;

        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
        public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

        public EnhancedThemeService(ILogger<EnhancedThemeService> logger, IEnhancedConfigurationService configService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));

            _currentThemeDefinition = ThemeDefinitionFactory.GetThemeDefinition(AppTheme.Light);
            
            LoadSettings();
            SetupSystemThemeMonitoring();
        }

        public void SetTheme(AppTheme theme)
        {
            var oldTheme = CurrentTheme;
            CurrentTheme = theme;

            var themeDefinition = ThemeDefinitionFactory.GetThemeDefinition(theme);
            CurrentThemeDefinition = themeDefinition;

            try
            {
                // �ݒ�ɕۑ�
                _configService.SetValue(ConfigurationKeys.App.Theme, ThemeDefinitionFactory.ThemeToString(theme));
                _configService.Save();

                // WPF�e�[�}�̎��ۂ̓K�p
                ApplyWpfTheme(themeDefinition);

                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme, themeDefinition));
                _logger.LogInformation("�e�[�}�ύX: {OldTheme} �� {NewTheme} ({DisplayName})", 
                    oldTheme, theme, themeDefinition.DisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�e�[�}�ݒ�ۑ��G���[: {Theme}", theme);
                CurrentTheme = oldTheme; // ���[���o�b�N
                CurrentThemeDefinition = ThemeDefinitionFactory.GetThemeDefinition(oldTheme);
                throw;
            }
        }

        public void SetLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException("����R�[�h�͕K�{�ł�", nameof(language));

            var oldLanguage = CurrentLanguage;
            CurrentLanguage = language;

            try
            {
                _configService.SetValue(ConfigurationKeys.App.Language, language);
                _configService.Save();

                LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(oldLanguage, language));
                _logger.LogInformation("����ύX: {OldLanguage} �� {NewLanguage}", oldLanguage, language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "����ݒ�ۑ��G���[: {Language}", language);
                CurrentLanguage = oldLanguage; // ���[���o�b�N
                throw;
            }
        }

        public AppTheme[] GetAvailableThemes()
        {
            return Enum.GetValues<AppTheme>();
        }

        public ThemeDefinition GetThemeDefinition(AppTheme theme)
        {
            return ThemeDefinitionFactory.GetThemeDefinition(theme);
        }

        private void LoadSettings()
        {
            try
            {
                var themeString = _configService.GetValue(ConfigurationKeys.App.Theme, "Light");
                var theme = ThemeDefinitionFactory.ParseTheme(themeString);
                
                CurrentTheme = theme;
                CurrentThemeDefinition = ThemeDefinitionFactory.GetThemeDefinition(theme);
                CurrentLanguage = _configService.GetValue(ConfigurationKeys.App.Language, "ja-JP");

                // �N�����Ƀe�[�}��K�p
                ApplyWpfTheme(CurrentThemeDefinition);

                _logger.LogDebug("�e�[�}�ݒ�ǂݍ��݊���: Theme={Theme}, Language={Language}", 
                    CurrentTheme, CurrentLanguage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�e�[�}�ݒ�ǂݍ��݃G���[");
            }
        }

        private void ApplyWpfTheme(ThemeDefinition themeDefinition)
        {
            try
            {
                var app = Application.Current;
                if (app == null) return;

                // ���݂̃e�[�}���\�[�X���폜
                RemoveCurrentThemeResources(app);

                // �V�����e�[�}���\�[�X��ǉ�
                if (!string.IsNullOrEmpty(themeDefinition.ResourceDictionary))
                {
                    var themeResourceDictionary = new ResourceDictionary
                    {
                        Source = new Uri(themeDefinition.ResourceDictionary, UriKind.RelativeOrAbsolute)
                    };
                    
                    app.Resources.MergedDictionaries.Insert(0, themeResourceDictionary);
                    _logger.LogDebug("�e�[�}���\�[�X�K�p: {ResourcePath}", themeDefinition.ResourceDictionary);
                }

                // �V�X�e�������e�[�}�̏ꍇ�̓��ʏ���
                if (themeDefinition.Theme == AppTheme.Auto)
                {
                    ApplySystemTheme();
                }

                _logger.LogInformation("WPF�e�[�}�K�p����: {Theme}", themeDefinition.DisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WPF�e�[�}�K�p���ɃG���[: {Theme}", themeDefinition.DisplayName);
            }
        }

        private void RemoveCurrentThemeResources(Application app)
        {
            try
            {
                // �e�[�}�֘A�̃��\�[�X�������폜�iAutoTool�̃e�[�}�t�@�C���̂݁j
                var toRemove = app.Resources.MergedDictionaries
                    .Where(rd => rd.Source?.ToString().Contains("AutoTool") == true && 
                                rd.Source?.ToString().Contains("Themes") == true)
                    .ToList();

                foreach (var rd in toRemove)
                {
                    app.Resources.MergedDictionaries.Remove(rd);
                }

                _logger.LogDebug("�����e�[�}���\�[�X�폜����: {Count}��", toRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�����e�[�}���\�[�X�폜���Ɍx��");
            }
        }

        private void SetupSystemThemeMonitoring()
        {
            try
            {
                // Windows�̃e�[�}�ύX���Ď�
                SystemEvents.UserPreferenceChanged += OnSystemUserPreferenceChanged;
                _logger.LogDebug("�V�X�e���e�[�}�Ď��ݒ芮��");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�V�X�e���e�[�}�Ď��ݒ蒆�Ɍx��");
            }
        }

        private void OnSystemUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General && CurrentTheme == AppTheme.Auto)
            {
                try
                {
                    // �V�X�e���e�[�}�ύX���Ɏ����K�p
                    Application.Current?.Dispatcher.BeginInvoke(() =>
                    {
                        ApplySystemTheme();
                        _logger.LogInformation("�V�X�e���e�[�}�ύX�ɒǏ]���܂���");
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "�V�X�e���e�[�}�ύX�Ǐ]���ɃG���[");
                }
            }
        }

        private void ApplySystemTheme()
        {
            try
            {
                // Windows�̃_�[�N���[�h�ݒ���擾
                var isDarkMode = IsSystemDarkMode();
                var systemTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;
                var systemThemeDefinition = ThemeDefinitionFactory.GetThemeDefinition(systemTheme);

                // �V�X�e���e�[�}��K�p�i�������ݒ�� Auto �̂܂܁j
                RemoveCurrentThemeResources(Application.Current);
                
                if (!string.IsNullOrEmpty(systemThemeDefinition.ResourceDictionary))
                {
                    var themeResourceDictionary = new ResourceDictionary
                    {
                        Source = new Uri(systemThemeDefinition.ResourceDictionary, UriKind.RelativeOrAbsolute)
                    };
                    
                    Application.Current.Resources.MergedDictionaries.Insert(0, themeResourceDictionary);
                }

                _logger.LogDebug("�V�X�e�������e�[�}�K�p: {SystemTheme} (�_�[�N���[�h: {IsDark})", 
                    systemTheme, isDarkMode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�V�X�e�������e�[�}�K�p���ɃG���[");
            }
        }

        private bool IsSystemDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0; // 0 = �_�[�N���[�h, 1 = ���C�g���[�h
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�V�X�e���_�[�N���[�h���蒆�Ɍx���A���C�g���[�h�Ƃ��Ĉ����܂�");
                return false; // �G���[���̓��C�g���[�h���f�t�H���g�Ƃ���
            }
        }
    }

    /// <summary>
    /// �A�v���P�[�V�����ݒ�T�[�r�X�̎����i����@�\�����Łj
    /// </summary>
    public partial class AppSettingsService : ObservableObject, IAppSettingsService
    {
        private readonly ILogger<AppSettingsService> _logger;
        private readonly IEnhancedConfigurationService _configService;

        [ObservableProperty]
        private bool _autoSave = true;

        [ObservableProperty]
        private int _autoSaveInterval = 300;

        [ObservableProperty]
        private int _defaultTimeout = 5000;

        [ObservableProperty]
        private int _defaultInterval = 100;

        public event EventHandler? SettingsChanged;

        public AppSettingsService(ILogger<AppSettingsService> logger, IEnhancedConfigurationService configService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));

            LoadSettings();
        }

        public void SaveSettings()
        {
            try
            {
                _configService.SetValue(ConfigurationKeys.App.AutoSave, AutoSave);
                _configService.SetValue(ConfigurationKeys.App.AutoSaveInterval, AutoSaveInterval);
                _configService.SetValue(ConfigurationKeys.Macro.DefaultTimeout, DefaultTimeout);
                _configService.SetValue(ConfigurationKeys.Macro.DefaultInterval, DefaultInterval);
                _configService.Save();

                SettingsChanged?.Invoke(this, EventArgs.Empty);
                _logger.LogDebug("�A�v���P�[�V�����ݒ�ۑ�����");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�v���P�[�V�����ݒ�ۑ��G���[");
                throw;
            }
        }

        public void LoadSettings()
        {
            try
            {
                AutoSave = _configService.GetValue(ConfigurationKeys.App.AutoSave, true);
                AutoSaveInterval = _configService.GetValue(ConfigurationKeys.App.AutoSaveInterval, 300);
                DefaultTimeout = _configService.GetValue(ConfigurationKeys.Macro.DefaultTimeout, 5000);
                DefaultInterval = _configService.GetValue(ConfigurationKeys.Macro.DefaultInterval, 100);

                _logger.LogDebug("�A�v���P�[�V�����ݒ�ǂݍ��݊���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�v���P�[�V�����ݒ�ǂݍ��݃G���[�A�f�t�H���g�l�g�p");
            }
        }

        public void ResetToDefaults()
        {
            try
            {
                AutoSave = true;
                AutoSaveInterval = 300;
                DefaultTimeout = 5000;
                DefaultInterval = 100;

                SaveSettings();
                _logger.LogInformation("�A�v���P�[�V�����ݒ���f�t�H���g�l�Ƀ��Z�b�g���܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ݒ胊�Z�b�g���ɃG���[");
                throw;
            }
        }

        // �v���p�e�B�ύX���̎����ۑ�
        partial void OnAutoSaveChanged(bool value)
        {
            SaveSettings();
        }

        partial void OnAutoSaveIntervalChanged(int value)
        {
            if (value < 60 || value > 3600) // 1��?1���Ԃ͈̔̓`�F�b�N
            {
                _logger.LogWarning("AutoSaveInterval�̒l���͈͊O�ł�: {Value}", value);
                return;
            }
            SaveSettings();
        }

        partial void OnDefaultTimeoutChanged(int value)
        {
            if (value < 100 || value > 300000) // 100ms?5���͈̔̓`�F�b�N
            {
                _logger.LogWarning("DefaultTimeout�̒l���͈͊O�ł�: {Value}", value);
                return;
            }
            SaveSettings();
        }

        partial void OnDefaultIntervalChanged(int value)
        {
            if (value < 10 || value > 60000) // 10ms?1���͈̔̓`�F�b�N
            {
                _logger.LogWarning("DefaultInterval�̒l���͈͊O�ł�: {Value}", value);
                return;
            }
            SaveSettings();
        }
    }
}