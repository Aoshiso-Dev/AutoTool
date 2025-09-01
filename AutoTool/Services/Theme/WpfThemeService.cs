using System;
using System.Windows;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;

namespace AutoTool.Services.Theme
{
    /// <summary>
    /// WPF�p�e�[�}�Ǘ��T�[�r�X����
    /// </summary>
    public class WpfThemeService : IThemeService
    {
        private readonly ILogger<WpfThemeService> _logger;
        private readonly IConfigurationService _configurationService;
        private AppTheme _currentTheme;

        public AppTheme CurrentTheme => _currentTheme;

        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        public WpfThemeService(ILogger<WpfThemeService> logger, IConfigurationService configurationService)
        {
            _logger = logger;
            _configurationService = configurationService;
            
            // �ݒ肩��e�[�}��ǂݍ���
            var savedTheme = _configurationService.GetValue("App.Theme", "Light");
            _currentTheme = Enum.TryParse<AppTheme>(savedTheme, out var theme) ? theme : AppTheme.Light;
            
            _logger.LogInformation("WpfThemeService �����������܂���: {Theme}", _currentTheme);
            
            // �����e�[�}��K�p
            ApplyTheme(_currentTheme);
        }

        /// <summary>
        /// �e�[�}��ύX
        /// </summary>
        public void SetTheme(AppTheme theme)
        {
            try
            {
                if (_currentTheme == theme)
                {
                    _logger.LogDebug("�����e�[�}���w�肳�ꂽ���ߕύX���X�L�b�v���܂�: {Theme}", theme);
                    return;
                }

                var oldTheme = _currentTheme;
                _currentTheme = theme;

                ApplyTheme(theme);
                
                // �ݒ�ɕۑ�
                _configurationService.SetValue("App.Theme", theme.ToString());
                
                _logger.LogInformation("�e�[�}��ύX���܂���: {OldTheme} -> {NewTheme}", oldTheme, theme);
                
                // �C�x���g����
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�e�[�}�̕ύX���ɃG���[���������܂���: {Theme}", theme);
                throw;
            }
        }

        /// <summary>
        /// ���p�\�ȃe�[�}�ꗗ���擾
        /// </summary>
        public AppTheme[] GetAvailableThemes()
        {
            return new[] { AppTheme.Light, AppTheme.Dark, AppTheme.Auto };
        }

        /// <summary>
        /// ���ۂɃe�[�}��K�p
        /// </summary>
        private void ApplyTheme(AppTheme theme)
        {
            try
            {
                _logger.LogDebug("�e�[�}��K�p���܂�: {Theme}", theme);
                
                // ���݂̃e�[�}���\�[�X���N���A
                ClearCurrentThemeResources();
                
                // �V�����e�[�}���\�[�X��K�p
                var actualTheme = ResolveActualTheme(theme);
                LoadThemeResources(actualTheme);
                
                _logger.LogDebug("�e�[�}�K�p����: {Theme}", actualTheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�e�[�}�K�p���ɃG���[���������܂���: {Theme}", theme);
                
                // �t�H�[���o�b�N�F���C�g�e�[�}��K�p
                if (theme != AppTheme.Light)
                {
                    try
                    {
                        LoadThemeResources(AppTheme.Light);
                        _logger.LogWarning("�t�H�[���o�b�N�Ń��C�g�e�[�}��K�p���܂���");
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogCritical(fallbackEx, "�t�H�[���o�b�N�e�[�}�̓K�p�����s���܂���");
                    }
                }
            }
        }

        /// <summary>
        /// ���ۂɓK�p����e�[�}�������iAuto �̏ꍇ�j
        /// </summary>
        private AppTheme ResolveActualTheme(AppTheme theme)
        {
            if (theme != AppTheme.Auto)
                return theme;

            try
            {
                // Windows �̃V�X�e���e�[�}���擾
                var registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                
                if (registryKey?.GetValue("AppsUseLightTheme") is int appsUseLightTheme)
                {
                    var resolvedTheme = appsUseLightTheme == 0 ? AppTheme.Dark : AppTheme.Light;
                    _logger.LogDebug("�V�X�e���e�[�}�����o: {Theme}", resolvedTheme);
                    return resolvedTheme;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�V�X�e���e�[�}�̌��o�Ɏ��s���܂���");
            }

            // �f�t�H���g�̓��C�g
            return AppTheme.Light;
        }

        /// <summary>
        /// ���݂̃e�[�}���\�[�X���N���A
        /// </summary>
        private void ClearCurrentThemeResources()
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources == null) return;

                // �e�[�}�֘A�̃��\�[�X�f�B�N�V���i�����폜
                for (int i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
                {
                    var dict = app.Resources.MergedDictionaries[i];
                    if (dict.Source?.ToString().Contains("Theme") == true)
                    {
                        app.Resources.MergedDictionaries.RemoveAt(i);
                        _logger.LogTrace("�e�[�}���\�[�X���폜: {Source}", dict.Source);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�e�[�}���\�[�X�̃N���A���ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// �e�[�}���\�[�X��ǂݍ���
        /// </summary>
        private void LoadThemeResources(AppTheme theme)
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources == null)
                {
                    _logger.LogWarning("Application.Current.Resources �� null �ł�");
                    return;
                }

                // �e�[�}�p�̃��\�[�X�f�B�N�V���i�����쐬
                var themeDict = new ResourceDictionary();
                
                switch (theme)
                {
                    case AppTheme.Light:
                        LoadLightTheme(themeDict);
                        break;
                    case AppTheme.Dark:
                        LoadDarkTheme(themeDict);
                        break;
                    default:
                        LoadLightTheme(themeDict);
                        break;
                }

                app.Resources.MergedDictionaries.Add(themeDict);
                _logger.LogDebug("�e�[�}���\�[�X��ǂݍ��݂܂���: {Theme}", theme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�e�[�}���\�[�X�̓ǂݍ��ݒ��ɃG���[���������܂���: {Theme}", theme);
                throw;
            }
        }

        /// <summary>
        /// ���C�g�e�[�}��ǂݍ���
        /// </summary>
        private void LoadLightTheme(ResourceDictionary dict)
        {
            // ���C�g�e�[�}�̐F��`
            dict["AppBackground"] = System.Windows.Media.Brushes.White;
            dict["AppForeground"] = System.Windows.Media.Brushes.Black;
            dict["PanelBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
            dict["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204));
            dict["AccentBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215));
            dict["ButtonBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
            dict["ButtonHoverBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
            
            _logger.LogTrace("���C�g�e�[�}���\�[�X��ݒ肵�܂���");
        }

        /// <summary>
        /// �_�[�N�e�[�}��ǂݍ���
        /// </summary>
        private void LoadDarkTheme(ResourceDictionary dict)
        {
            // �_�[�N�e�[�}�̐F��`
            dict["AppBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(32, 32, 32));
            dict["AppForeground"] = System.Windows.Media.Brushes.White;
            dict["PanelBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));
            dict["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));
            dict["AccentBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 180, 255));
            dict["ButtonBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
            dict["ButtonHoverBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));
            
            _logger.LogTrace("�_�[�N�e�[�}���\�[�X��ݒ肵�܂���");
        }
    }
}