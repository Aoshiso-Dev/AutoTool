using System;
using System.Windows;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;

namespace AutoTool.Services.Theme
{
    /// <summary>
    /// WPF用テーマ管理サービス実装
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
            
            // 設定からテーマを読み込み
            var savedTheme = _configurationService.GetValue("App.Theme", "Light");
            _currentTheme = Enum.TryParse<AppTheme>(savedTheme, out var theme) ? theme : AppTheme.Light;
            
            _logger.LogInformation("WpfThemeService を初期化しました: {Theme}", _currentTheme);
            
            // 初期テーマを適用
            ApplyTheme(_currentTheme);
        }

        /// <summary>
        /// テーマを変更
        /// </summary>
        public void SetTheme(AppTheme theme)
        {
            try
            {
                if (_currentTheme == theme)
                {
                    _logger.LogDebug("同じテーマが指定されたため変更をスキップします: {Theme}", theme);
                    return;
                }

                var oldTheme = _currentTheme;
                _currentTheme = theme;

                ApplyTheme(theme);
                
                // 設定に保存
                _configurationService.SetValue("App.Theme", theme.ToString());
                
                _logger.LogInformation("テーマを変更しました: {OldTheme} -> {NewTheme}", oldTheme, theme);
                
                // イベント発火
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テーマの変更中にエラーが発生しました: {Theme}", theme);
                throw;
            }
        }

        /// <summary>
        /// 利用可能なテーマ一覧を取得
        /// </summary>
        public AppTheme[] GetAvailableThemes()
        {
            return new[] { AppTheme.Light, AppTheme.Dark, AppTheme.Auto };
        }

        /// <summary>
        /// 実際にテーマを適用
        /// </summary>
        private void ApplyTheme(AppTheme theme)
        {
            try
            {
                _logger.LogDebug("テーマを適用します: {Theme}", theme);
                
                // 現在のテーマリソースをクリア
                ClearCurrentThemeResources();
                
                // 新しいテーマリソースを適用
                var actualTheme = ResolveActualTheme(theme);
                LoadThemeResources(actualTheme);
                
                _logger.LogDebug("テーマ適用完了: {Theme}", actualTheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テーマ適用中にエラーが発生しました: {Theme}", theme);
                
                // フォールバック：ライトテーマを適用
                if (theme != AppTheme.Light)
                {
                    try
                    {
                        LoadThemeResources(AppTheme.Light);
                        _logger.LogWarning("フォールバックでライトテーマを適用しました");
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogCritical(fallbackEx, "フォールバックテーマの適用も失敗しました");
                    }
                }
            }
        }

        /// <summary>
        /// 実際に適用するテーマを解決（Auto の場合）
        /// </summary>
        private AppTheme ResolveActualTheme(AppTheme theme)
        {
            if (theme != AppTheme.Auto)
                return theme;

            try
            {
                // Windows のシステムテーマを取得
                var registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                
                if (registryKey?.GetValue("AppsUseLightTheme") is int appsUseLightTheme)
                {
                    var resolvedTheme = appsUseLightTheme == 0 ? AppTheme.Dark : AppTheme.Light;
                    _logger.LogDebug("システムテーマを検出: {Theme}", resolvedTheme);
                    return resolvedTheme;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "システムテーマの検出に失敗しました");
            }

            // デフォルトはライト
            return AppTheme.Light;
        }

        /// <summary>
        /// 現在のテーマリソースをクリア
        /// </summary>
        private void ClearCurrentThemeResources()
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources == null) return;

                // テーマ関連のリソースディクショナリを削除
                for (int i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
                {
                    var dict = app.Resources.MergedDictionaries[i];
                    if (dict.Source?.ToString().Contains("Theme") == true)
                    {
                        app.Resources.MergedDictionaries.RemoveAt(i);
                        _logger.LogTrace("テーマリソースを削除: {Source}", dict.Source);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "テーマリソースのクリア中にエラーが発生しました");
            }
        }

        /// <summary>
        /// テーマリソースを読み込み
        /// </summary>
        private void LoadThemeResources(AppTheme theme)
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources == null)
                {
                    _logger.LogWarning("Application.Current.Resources が null です");
                    return;
                }

                // テーマ用のリソースディクショナリを作成
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
                _logger.LogDebug("テーマリソースを読み込みました: {Theme}", theme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テーマリソースの読み込み中にエラーが発生しました: {Theme}", theme);
                throw;
            }
        }

        /// <summary>
        /// ライトテーマを読み込み
        /// </summary>
        private void LoadLightTheme(ResourceDictionary dict)
        {
            // ライトテーマの色定義
            dict["AppBackground"] = System.Windows.Media.Brushes.White;
            dict["AppForeground"] = System.Windows.Media.Brushes.Black;
            dict["PanelBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
            dict["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204));
            dict["AccentBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215));
            dict["ButtonBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
            dict["ButtonHoverBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
            
            _logger.LogTrace("ライトテーマリソースを設定しました");
        }

        /// <summary>
        /// ダークテーマを読み込み
        /// </summary>
        private void LoadDarkTheme(ResourceDictionary dict)
        {
            // ダークテーマの色定義
            dict["AppBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(32, 32, 32));
            dict["AppForeground"] = System.Windows.Media.Brushes.White;
            dict["PanelBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));
            dict["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));
            dict["AccentBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 180, 255));
            dict["ButtonBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
            dict["ButtonHoverBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));
            
            _logger.LogTrace("ダークテーマリソースを設定しました");
        }
    }
}