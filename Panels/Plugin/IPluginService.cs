using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MacroPanels.Command.Interface;
using MacroCommand = MacroPanels.Command.Interface.ICommand;

namespace MacroPanels.Plugin
{
    /// <summary>
    /// プラグインシステムのインターフェース
    /// </summary>
    public interface IPluginService
    {
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
        /// コマンドプラグインからコマンドを作成
        /// </summary>
        MacroCommand? CreatePluginCommand(string pluginId, string commandId, MacroCommand? parent, object? settings);

        /// <summary>
        /// 利用可能なプラグインコマンドを取得
        /// </summary>
        IEnumerable<IPluginCommandInfo> GetAvailablePluginCommands();

        /// <summary>
        /// プラグイン読み込みイベント
        /// </summary>
        event EventHandler<PluginLoadedEventArgs> PluginLoaded;

        /// <summary>
        /// プラグインアンロードイベント
        /// </summary>
        event EventHandler<PluginUnloadedEventArgs> PluginUnloaded;
    }

    /// <summary>
    /// プラグインの基本インターフェース
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

    /// <summary>
    /// プラグイン情報インターフェース
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
    /// プラグインステータス
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
    /// プラグイン読み込みイベント引数
    /// </summary>
    public class PluginLoadedEventArgs : EventArgs
    {
        public IPluginInfo PluginInfo { get; }

        public PluginLoadedEventArgs(IPluginInfo pluginInfo)
        {
            PluginInfo = pluginInfo;
        }
    }

    /// <summary>
    /// プラグインアンロードイベント引数
    /// </summary>
    public class PluginUnloadedEventArgs : EventArgs
    {
        public string PluginId { get; }

        public PluginUnloadedEventArgs(string pluginId)
        {
            PluginId = pluginId;
        }
    }

    /// <summary>
    /// コマンドプラグインの専用インターフェース
    /// </summary>
    public interface ICommandPlugin : IPlugin
    {
        /// <summary>
        /// プラグインが提供するコマンド一覧
        /// </summary>
        IEnumerable<IPluginCommandInfo> GetAvailableCommands();

        /// <summary>
        /// コマンドを作成
        /// </summary>
        MacroCommand CreateCommand(string commandId, MacroCommand? parent, object? settings);

        /// <summary>
        /// コマンド設定の型を取得
        /// </summary>
        Type? GetCommandSettingsType(string commandId);

        /// <summary>
        /// プラグインコマンドが利用可能かどうか
        /// </summary>
        bool IsCommandAvailable(string commandId);
    }

    /// <summary>
    /// プラグインコマンド情報
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
    /// プラグインコマンド情報の実装クラス
    /// </summary>
    public class PluginCommandInfo : IPluginCommandInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "プラグイン";
        public string PluginId { get; set; } = string.Empty;
        public Type CommandType { get; set; } = typeof(object);
        public Type? SettingsType { get; set; }
        public string? IconPath { get; set; }
    }
}