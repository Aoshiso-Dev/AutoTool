using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.Plugin;

namespace AutoTool.Services.Plugin
{
    /// <summary>
    /// プラグインコマンドの基底クラス
    /// プラグイン開発者が継承して独自のコマンドを作成する
    /// </summary>
    public abstract class PluginCommandBase : BaseCommand
    {
        /// <summary>
        /// プラグイン情報
        /// </summary>
        public MacroPanels.Plugin.IPluginInfo PluginInfo { get; }

        /// <summary>
        /// コマンド情報
        /// </summary>
        public MacroPanels.Plugin.IPluginCommandInfo CommandInfo { get; }

        protected PluginCommandBase(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
            : base(parent, settings)
        {
            PluginInfo = pluginInfo ?? throw new ArgumentNullException(nameof(pluginInfo));
            CommandInfo = commandInfo ?? throw new ArgumentNullException(nameof(commandInfo));
            Description = $"[{pluginInfo.Name}] {commandInfo.Name}";
        }

        /// <summary>
        /// プラグインコマンドの実際の実行処理
        /// 派生クラスで実装する
        /// </summary>
        protected abstract Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken);

        /// <summary>
        /// BaseCommandの実行処理をオーバーライド
        /// </summary>
        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                LogMessage($"プラグインコマンド開始: {PluginInfo.Name}.{CommandInfo.Name}");
                
                var result = await DoExecutePluginAsync(cancellationToken);
                
                LogMessage($"プラグインコマンド終了: {PluginInfo.Name}.{CommandInfo.Name} - {(result ? "成功" : "失敗")}");
                
                return result;
            }
            catch (Exception ex)
            {
                LogMessage($"? プラグインコマンドエラー: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// プラグイン設定を取得（型安全）
        /// </summary>
        protected T? GetPluginSettings<T>() where T : class
        {
            return Settings as T;
        }

        /// <summary>
        /// プラグイン設定の検証
        /// </summary>
        protected virtual void ValidatePluginSettings()
        {
            // 基底クラスでは何もしない（派生クラスでオーバーライド）
        }

        /// <summary>
        /// ファイル検証をオーバーライド
        /// </summary>
        protected override void ValidateFiles()
        {
            base.ValidateFiles();
            ValidatePluginSettings();
        }
    }

    /// <summary>
    /// プラグインコマンドファクトリーの基底クラス
    /// プラグイン開発者が継承してコマンド作成ロジックを実装する
    /// </summary>
    public abstract class PluginCommandFactoryBase
    {
        /// <summary>
        /// プラグイン情報
        /// </summary>
        public MacroPanels.Plugin.IPluginInfo PluginInfo { get; }

        protected PluginCommandFactoryBase(MacroPanels.Plugin.IPluginInfo pluginInfo)
        {
            PluginInfo = pluginInfo ?? throw new ArgumentNullException(nameof(pluginInfo));
        }

        /// <summary>
        /// 利用可能なコマンド一覧を取得
        /// 派生クラスで実装する
        /// </summary>
        public abstract IEnumerable<MacroPanels.Plugin.IPluginCommandInfo> GetAvailableCommands();

        /// <summary>
        /// コマンドを作成
        /// 派生クラスで実装する
        /// </summary>
        public abstract MacroPanels.Command.Interface.ICommand CreateCommand(string commandId, MacroPanels.Command.Interface.ICommand? parent, object? settings);

        /// <summary>
        /// コマンド設定の型を取得
        /// 派生クラスで実装する
        /// </summary>
        public abstract Type? GetCommandSettingsType(string commandId);

        /// <summary>
        /// プラグインコマンドが利用可能かどうか
        /// 派生クラスでオーバーライド可
        /// </summary>
        public virtual bool IsCommandAvailable(string commandId)
        {
            return GetAvailableCommands().Any(c => c.Id == commandId);
        }
    }

    /// <summary>
    /// 簡単なプラグインコマンド情報作成のためのヘルパー
    /// </summary>
    public static class PluginCommandHelper
    {
        /// <summary>
        /// プラグインコマンド情報を作成
        /// </summary>
        public static MacroPanels.Plugin.IPluginCommandInfo CreateCommandInfo(
            string id, 
            string name, 
            string description, 
            string pluginId,
            Type commandType,
            Type? settingsType = null,
            string category = "プラグイン",
            string? iconPath = null)
        {
            return new PluginCommandInfoImpl
            {
                Id = id,
                Name = name,
                Description = description,
                Category = category,
                PluginId = pluginId,
                CommandType = commandType,
                SettingsType = settingsType,
                IconPath = iconPath
            };
        }

        /// <summary>
        /// デフォルト設定オブジェクトを作成
        /// </summary>
        public static T? CreateDefaultSettings<T>() where T : class, new()
        {
            try
            {
                return new T();
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// プラグインコマンド情報の内部実装
    /// </summary>
    internal class PluginCommandInfoImpl : MacroPanels.Plugin.IPluginCommandInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string PluginId { get; set; } = string.Empty;
        public Type CommandType { get; set; } = typeof(object);
        public Type? SettingsType { get; set; }
        public string? IconPath { get; set; }
    }
}