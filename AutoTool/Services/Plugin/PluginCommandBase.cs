using System;
using System.Collections.Generic;
using System.Linq;
using AutoTool.Services.Plugin;
using AutoTool.ViewModel.Shared;

namespace AutoTool.Services.Plugin
{
    /// <summary>
    /// Phase 5完全統合版：プラグインコマンドベースクラス
    /// MacroPanels依存を削除し、AutoTool統合版のみ使用
    /// </summary>
    public abstract class PluginCommandBase : UniversalCommandItem
    {
        // UniversalCommandItemの実装
        public string ItemType { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public bool IsEnable { get; set; } = true;
        public int LineNumber { get; set; }
        public int NestLevel { get; set; }
        public virtual string Description { get; set; } = string.Empty;
        
        // Phase 5: UniversalCommandItemの不足プロパティを追加
        public bool IsRunning { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public bool IsInLoop { get; set; } = false;
        public bool IsInIf { get; set; } = false;
        public int Progress { get; set; } = 0;

        // プラグイン情報
        public IPluginInfo PluginInfo { get; }
        public IPluginCommandInfo CommandInfo { get; }

        protected PluginCommandBase(IPluginInfo pluginInfo, IPluginCommandInfo commandInfo)
        {
            PluginInfo = pluginInfo ?? throw new ArgumentNullException(nameof(pluginInfo));
            CommandInfo = commandInfo ?? throw new ArgumentNullException(nameof(commandInfo));
            ItemType = commandInfo.Id;
        }

        protected virtual string GetDescription()
        {
            return $"[{PluginInfo.Name}] {CommandInfo.Name}";
        }

        public virtual UniversalCommandItem Clone()
        {
            // Phase 5: 基本的なクローン実装
            var clone = (PluginCommandBase)Activator.CreateInstance(GetType(), PluginInfo, CommandInfo)!;
            clone.ItemType = this.ItemType;
            clone.Comment = this.Comment;
            clone.IsEnable = this.IsEnable;
            clone.LineNumber = this.LineNumber;
            clone.NestLevel = this.NestLevel;
            return clone;
        }

        /// <summary>
        /// プラグインコマンドの実行処理（派生クラスで実装）
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// プラグインコマンドの検証処理（派生クラスで実装）
        /// </summary>
        public virtual bool Validate()
        {
            return true;
        }

        /// <summary>
        /// プラグインコマンドの初期化処理
        /// </summary>
        public virtual void Initialize()
        {
            // デフォルトでは何もしない
        }

        /// <summary>
        /// プラグインコマンドのクリーンアップ処理
        /// </summary>
        public virtual void Cleanup()
        {
            // デフォルトでは何もしない
        }

        public override string ToString()
        {
            return $"[{LineNumber}] {Description}: {Comment}";
        }

        protected virtual UniversalCommandItem CreateCommandItem()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Phase 5完全統合版：プラグインコマンドファクトリーベースクラス
    /// </summary>
    public abstract class PluginCommandFactoryBase
    {
        /// <summary>
        /// プラグイン情報
        /// </summary>
        public IPluginInfo PluginInfo { get; }

        protected PluginCommandFactoryBase(IPluginInfo pluginInfo)
        {
            PluginInfo = pluginInfo ?? throw new ArgumentNullException(nameof(pluginInfo));
        }

        /// <summary>
        /// 利用可能なコマンド一覧を取得（派生クラスで実装）
        /// </summary>
        public abstract IEnumerable<IPluginCommandInfo> GetAvailableCommands();

        /// <summary>
        /// コマンドを作成（派生クラスで実装）
        /// </summary>
        public abstract object CreateCommand(String commandId, object? parent, object? settings);

        /// <summary>
        /// コマンド設定の型を取得
        /// </summary>
        public virtual Type? GetCommandSettingsType(string commandId)
        {
            var commandInfo = GetAvailableCommands().FirstOrDefault(c => c.Id == commandId);
            return commandInfo?.SettingsType;
        }

        /// <summary>
        /// コマンドが利用可能かどうか
        /// </summary>
        public virtual bool IsCommandAvailable(string commandId)
        {
            return GetAvailableCommands().Any(c => c.Id == commandId);
        }

        /// <summary>
        /// プラグインコマンド情報を作成するヘルパーメソッド
        /// </summary>
        protected static IPluginCommandInfo CreateCommandInfo(
            string id,
            string name,
            string description,
            string category,
            string pluginId,
            Type commandType,
            Type? settingsType = null,
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
    }

    /// <summary>
    /// Phase 5完全統合版：プラグインコマンド情報実装クラス
    /// </summary>
    internal class PluginCommandInfoImpl : IPluginCommandInfo
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