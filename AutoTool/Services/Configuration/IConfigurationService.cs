using System;
using System.Threading.Tasks;

namespace AutoTool.Services.Configuration
{
    /// <summary>
    /// アプリケーション設定管理サービスのインターフェース
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// 設定値を取得
        /// </summary>
        T GetValue<T>(string key, T defaultValue = default);

        /// <summary>
        /// 設定値を設定
        /// </summary>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// 設定をファイルに保存
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// 設定をファイルから読み込み
        /// </summary>
        Task LoadAsync();

        /// <summary>
        /// 設定値の変更を監視
        /// </summary>
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    }

    /// <summary>
    /// 設定変更イベント引数
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