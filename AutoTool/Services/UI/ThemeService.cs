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
    /// アプリケーション設定の管理サービス
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
    /// アプリケーションのテーマと外観を管理するサービス（強化版）
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
    /// テーマ変更イベント引数（強化版）
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
    /// 言語変更イベント引数
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
    /// 強化されたテーマサービス（WPF統合版）
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
                // 設定に保存
                _configService.SetValue(ConfigurationKeys.App.Theme, ThemeDefinitionFactory.ThemeToString(theme));
                _configService.Save();

                // WPFテーマの実際の適用
                ApplyWpfTheme(themeDefinition);

                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme, themeDefinition));
                _logger.LogInformation("テーマ変更: {OldTheme} → {NewTheme} ({DisplayName})", 
                    oldTheme, theme, themeDefinition.DisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テーマ設定保存エラー: {Theme}", theme);
                CurrentTheme = oldTheme; // ロールバック
                CurrentThemeDefinition = ThemeDefinitionFactory.GetThemeDefinition(oldTheme);
                throw;
            }
        }

        public void SetLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException("言語コードは必須です", nameof(language));

            var oldLanguage = CurrentLanguage;
            CurrentLanguage = language;

            try
            {
                _configService.SetValue(ConfigurationKeys.App.Language, language);
                _configService.Save();

                LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(oldLanguage, language));
                _logger.LogInformation("言語変更: {OldLanguage} → {NewLanguage}", oldLanguage, language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "言語設定保存エラー: {Language}", language);
                CurrentLanguage = oldLanguage; // ロールバック
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

                // 起動時にテーマを適用
                ApplyWpfTheme(CurrentThemeDefinition);

                _logger.LogDebug("テーマ設定読み込み完了: Theme={Theme}, Language={Language}", 
                    CurrentTheme, CurrentLanguage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テーマ設定読み込みエラー");
            }
        }

        private void ApplyWpfTheme(ThemeDefinition themeDefinition)
        {
            try
            {
                var app = Application.Current;
                if (app == null) return;

                // 現在のテーマリソースを削除
                RemoveCurrentThemeResources(app);

                // 新しいテーマリソースを追加
                if (!string.IsNullOrEmpty(themeDefinition.ResourceDictionary))
                {
                    var themeResourceDictionary = new ResourceDictionary
                    {
                        Source = new Uri(themeDefinition.ResourceDictionary, UriKind.RelativeOrAbsolute)
                    };
                    
                    app.Resources.MergedDictionaries.Insert(0, themeResourceDictionary);
                    _logger.LogDebug("テーマリソース適用: {ResourcePath}", themeDefinition.ResourceDictionary);
                }

                // システム自動テーマの場合の特別処理
                if (themeDefinition.Theme == AppTheme.Auto)
                {
                    ApplySystemTheme();
                }

                _logger.LogInformation("WPFテーマ適用完了: {Theme}", themeDefinition.DisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WPFテーマ適用中にエラー: {Theme}", themeDefinition.DisplayName);
            }
        }

        private void RemoveCurrentThemeResources(Application app)
        {
            try
            {
                // テーマ関連のリソース辞書を削除（AutoToolのテーマファイルのみ）
                var toRemove = app.Resources.MergedDictionaries
                    .Where(rd => rd.Source?.ToString().Contains("AutoTool") == true && 
                                rd.Source?.ToString().Contains("Themes") == true)
                    .ToList();

                foreach (var rd in toRemove)
                {
                    app.Resources.MergedDictionaries.Remove(rd);
                }

                _logger.LogDebug("既存テーマリソース削除完了: {Count}個", toRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "既存テーマリソース削除中に警告");
            }
        }

        private void SetupSystemThemeMonitoring()
        {
            try
            {
                // Windowsのテーマ変更を監視
                SystemEvents.UserPreferenceChanged += OnSystemUserPreferenceChanged;
                _logger.LogDebug("システムテーマ監視設定完了");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "システムテーマ監視設定中に警告");
            }
        }

        private void OnSystemUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General && CurrentTheme == AppTheme.Auto)
            {
                try
                {
                    // システムテーマ変更時に自動適用
                    Application.Current?.Dispatcher.BeginInvoke(() =>
                    {
                        ApplySystemTheme();
                        _logger.LogInformation("システムテーマ変更に追従しました");
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "システムテーマ変更追従中にエラー");
                }
            }
        }

        private void ApplySystemTheme()
        {
            try
            {
                // Windowsのダークモード設定を取得
                var isDarkMode = IsSystemDarkMode();
                var systemTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;
                var systemThemeDefinition = ThemeDefinitionFactory.GetThemeDefinition(systemTheme);

                // システムテーマを適用（ただし設定は Auto のまま）
                RemoveCurrentThemeResources(Application.Current);
                
                if (!string.IsNullOrEmpty(systemThemeDefinition.ResourceDictionary))
                {
                    var themeResourceDictionary = new ResourceDictionary
                    {
                        Source = new Uri(systemThemeDefinition.ResourceDictionary, UriKind.RelativeOrAbsolute)
                    };
                    
                    Application.Current.Resources.MergedDictionaries.Insert(0, themeResourceDictionary);
                }

                _logger.LogDebug("システム自動テーマ適用: {SystemTheme} (ダークモード: {IsDark})", 
                    systemTheme, isDarkMode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "システム自動テーマ適用中にエラー");
            }
        }

        private bool IsSystemDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0; // 0 = ダークモード, 1 = ライトモード
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "システムダークモード判定中に警告、ライトモードとして扱います");
                return false; // エラー時はライトモードをデフォルトとする
            }
        }
    }

    /// <summary>
    /// アプリケーション設定サービスの実装（言語機能統合版）
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
                _logger.LogDebug("アプリケーション設定保存完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アプリケーション設定保存エラー");
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

                _logger.LogDebug("アプリケーション設定読み込み完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アプリケーション設定読み込みエラー、デフォルト値使用");
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
                _logger.LogInformation("アプリケーション設定をデフォルト値にリセットしました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定リセット中にエラー");
                throw;
            }
        }

        // プロパティ変更時の自動保存
        partial void OnAutoSaveChanged(bool value)
        {
            SaveSettings();
        }

        partial void OnAutoSaveIntervalChanged(int value)
        {
            if (value < 60 || value > 3600) // 1分?1時間の範囲チェック
            {
                _logger.LogWarning("AutoSaveIntervalの値が範囲外です: {Value}", value);
                return;
            }
            SaveSettings();
        }

        partial void OnDefaultTimeoutChanged(int value)
        {
            if (value < 100 || value > 300000) // 100ms?5分の範囲チェック
            {
                _logger.LogWarning("DefaultTimeoutの値が範囲外です: {Value}", value);
                return;
            }
            SaveSettings();
        }

        partial void OnDefaultIntervalChanged(int value)
        {
            if (value < 10 || value > 60000) // 10ms?1分の範囲チェック
            {
                _logger.LogWarning("DefaultIntervalの値が範囲外です: {Value}", value);
                return;
            }
            SaveSettings();
        }
    }
}