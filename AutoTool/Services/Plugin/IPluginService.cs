using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoTool.Services.Plugin
{
    /// <summary>
    /// Phase 5統合版：プラグインシステムのインターフェース
    /// </summary>
    public interface IPluginService
    {
        /// <summary>
        /// プラグインが読み込まれた時のイベント
        /// </summary>
        event EventHandler<PluginLoadedEventArgs>? PluginLoaded;
        
        /// <summary>
        /// プラグインがアンロードされた時のイベント
        /// </summary>
        event EventHandler<PluginUnloadedEventArgs>? PluginUnloaded;

        /// <summary>
        /// プラグインを読み込み
        /// </summary>
        Task LoadPluginAsync(string pluginPath);
        
        /// <summary>
        /// 全てのプラグインを読み込み
        /// </summary>
        Task LoadAllPluginsAsync();
        
        /// <summary>
        /// プラグインをアンロード
        /// </summary>
        Task UnloadPluginAsync(string pluginId);
        
        /// <summary>
        /// 読み込み済みプラグイン一覧を取得
        /// </summary>
        IEnumerable<IPluginInfo> GetLoadedPlugins();
        
        /// <summary>
        /// プラグインを取得
        /// </summary>
        T? GetPlugin<T>(string pluginId) where T : class, IPlugin;
        
        /// <summary>
        /// コマンドプラグインからコマンドを作成（Phase 5統合版）
        /// </summary>
        object? CreatePluginCommand(string pluginId, string commandId, object? parent, object? settings);
        
        /// <summary>
        /// 利用可能なプラグインコマンドを取得
        /// </summary>
        IEnumerable<IPluginCommandInfo> GetAvailablePluginCommands();
    }

    /// <summary>
    /// Phase 5統合版：プラグイン情報インターフェース
    /// </summary>
    public interface IPluginInfo
    {
        string Id { get; }
        string Name { get; }
        string Version { get; }
        string Description { get; }
        string Author { get; }
        DateTime LoadedAt { get; set; }
        PluginStatus Status { get; set; }
    }

    /// <summary>
    /// Phase 5統合版：プラグインステータス
    /// </summary>
    public enum PluginStatus
    {
        NotLoaded,
        Loading,
        Loaded,
        Initializing,
        Active,
        Error,
        Unloading
    }

    /// <summary>
    /// Phase 5統合版：プラグイン読み込みイベント引数
    /// </summary>
    public class PluginLoadedEventArgs : EventArgs
    {
        public IPluginInfo PluginInfo { get; }
        public PluginLoadedEventArgs(IPluginInfo pluginInfo) => PluginInfo = pluginInfo;
    }

    /// <summary>
    /// Phase 5統合版：プラグインアンロードイベント引数
    /// </summary>
    public class PluginUnloadedEventArgs : EventArgs
    {
        public string PluginId { get; }
        public PluginUnloadedEventArgs(string pluginId) => PluginId = pluginId;
    }

    /// <summary>
    /// Phase 5統合版：プラグインコマンド情報
    /// </summary>
    public interface IPluginCommandInfo
    {
        /// <summary>
        /// コマンドID
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// 表示名
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// 説明
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// カテゴリ
        /// </summary>
        string Category { get; }
        
        /// <summary>
        /// プラグインID
        /// </summary>
        string PluginId { get; }
        
        /// <summary>
        /// コマンド型
        /// </summary>
        Type CommandType { get; }
        
        /// <summary>
        /// 設定型
        /// </summary>
        Type? SettingsType { get; }
        
        /// <summary>
        /// アイコンパス（オプション）
        /// </summary>
        string? IconPath { get; }
    }

    /// <summary>
    /// Phase 5統合版：プラグインの基底インターフェース
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// プラグイン情報
        /// </summary>
        IPluginInfo Info { get; }
        
        /// <summary>
        /// プラグイン初期化
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// プラグイン終了
        /// </summary>
        Task ShutdownAsync();
    }
}