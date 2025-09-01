using System;
using System.Windows;

namespace AutoTool.Services.Theme
{
    /// <summary>
    /// テーマ管理サービスのインターフェース
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// 現在のテーマ
        /// </summary>
        AppTheme CurrentTheme { get; }

        /// <summary>
        /// テーマを変更
        /// </summary>
        void SetTheme(AppTheme theme);

        /// <summary>
        /// テーマ変更イベント
        /// </summary>
        event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        /// <summary>
        /// 利用可能なテーマ一覧を取得
        /// </summary>
        AppTheme[] GetAvailableThemes();
    }

    /// <summary>
    /// アプリケーションテーマの種類
    /// </summary>
    public enum AppTheme
    {
        Light,
        Dark,
        Auto // システム設定に従う
    }

    /// <summary>
    /// テーマ変更イベント引数
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