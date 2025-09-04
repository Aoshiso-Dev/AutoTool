using System;
using System.Collections.Generic;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// アプリケーションで利用可能なテーマ
    /// </summary>
    public enum AppTheme
    {
        Light,
        Dark,
        Auto,       // システム設定に追従
        HighContrast // 高コントラスト
    }

    /// <summary>
    /// テーマ定義情報
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
    /// テーマ定義のファクトリー
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
                    DisplayName = "ライトテーマ",
                    ResourceDictionary = "pack://application:,,,/AutoTool;component/Themes/LightTheme.xaml",
                    AccentColor = "#0066CC",
                    Description = "明るい色調のテーマです。日中の使用に最適です。",
                    IsSystemTheme = false
                }
            },
            {
                AppTheme.Dark,
                new ThemeDefinition
                {
                    Theme = AppTheme.Dark,
                    DisplayName = "ダークテーマ",
                    ResourceDictionary = "pack://application:,,,/AutoTool;component/Themes/DarkTheme.xaml",
                    AccentColor = "#66B2FF",
                    Description = "暗い色調のテーマです。夜間や長時間の使用に目に優しいです。",
                    IsSystemTheme = false
                }
            },
            {
                AppTheme.Auto,
                new ThemeDefinition
                {
                    Theme = AppTheme.Auto,
                    DisplayName = "自動（システム追従）",
                    ResourceDictionary = "", // 実行時に決定
                    AccentColor = "", // 実行時に決定
                    Description = "Windowsのシステム設定に合わせてテーマを自動変更します。",
                    IsSystemTheme = true
                }
            },
            {
                AppTheme.HighContrast,
                new ThemeDefinition
                {
                    Theme = AppTheme.HighContrast,
                    DisplayName = "高コントラスト",
                    ResourceDictionary = "pack://application:,,,/AutoTool;component/Themes/HighContrastTheme.xaml",
                    AccentColor = "#FFFF00",
                    Description = "視認性を高めた高コントラストテーマです。アクセシビリティに配慮しています。",
                    IsSystemTheme = false
                }
            }
        };

        /// <summary>
        /// 指定されたテーマの定義を取得
        /// </summary>
        public static ThemeDefinition GetThemeDefinition(AppTheme theme)
        {
            return _themeDefinitions.TryGetValue(theme, out var definition) 
                ? definition 
                : _themeDefinitions[AppTheme.Light];
        }

        /// <summary>
        /// すべてのテーマ定義を取得
        /// </summary>
        public static IEnumerable<ThemeDefinition> GetAllThemeDefinitions()
        {
            return _themeDefinitions.Values;
        }

        /// <summary>
        /// 文字列からAppThemeに変換
        /// </summary>
        public static AppTheme ParseTheme(string themeString)
        {
            if (Enum.TryParse<AppTheme>(themeString, true, out var theme))
            {
                return theme;
            }
            return AppTheme.Light; // デフォルト
        }

        /// <summary>
        /// AppThemeから文字列に変換
        /// </summary>
        public static string ThemeToString(AppTheme theme)
        {
            return theme.ToString();
        }
    }
}