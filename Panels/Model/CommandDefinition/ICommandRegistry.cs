using System;
using System.Collections.Generic;
using MacroPanels.Model.List.Interface;

namespace MacroPanels.Model.CommandDefinition
{
    /// <summary>
    /// コマンドレジストリのインターフェース
    /// </summary>
    public interface ICommandRegistry
    {
        /// <summary>
        /// すべてのコマンドタイプ名を取得
        /// </summary>
        IEnumerable<string> GetAllTypeNames();

        /// <summary>
        /// UI表示用に順序付けられたコマンドタイプ名を取得
        /// </summary>
        IEnumerable<string> GetOrderedTypeNames();

        /// <summary>
        /// カテゴリ別のコマンドタイプ名を取得
        /// </summary>
        IEnumerable<string> GetTypeNamesByCategory(CommandCategory category);

        /// <summary>
        /// 表示優先度別のコマンドタイプ名を取得
        /// </summary>
        IEnumerable<string> GetTypeNamesByDisplayPriority(int priority);

        /// <summary>
        /// コマンドアイテムを作成
        /// </summary>
        ICommandListItem? CreateCommandItem(string typeName);

        /// <summary>
        /// Ifコマンドかどうか判定
        /// </summary>
        bool IsIfCommand(string typeName);

        /// <summary>
        /// ループコマンドかどうか判定
        /// </summary>
        bool IsLoopCommand(string typeName);

        /// <summary>
        /// 終了コマンド（ネストレベルを減らす）かどうか判定
        /// </summary>
        bool IsEndCommand(string typeName);

        /// <summary>
        /// 開始コマンド（ネストレベルを増やす）かどうか判定
        /// </summary>
        bool IsStartCommand(string typeName);

        /// <summary>
        /// 指定されたタイプ名のアイテムタイプを取得
        /// </summary>
        Type? GetItemType(string typeName);

        /// <summary>
        /// UI表示用のコマンド定義一覧を取得
        /// </summary>
        IEnumerable<CommandDefinitionItem> GetCommandDefinitions();

        /// <summary>
        /// 初期化
        /// </summary>
        void Initialize();
    }

    /// <summary>
    /// UI表示用のコマンド定義項目
    /// </summary>
    public class CommandDefinitionItem
    {
        /// <summary>
        /// タイプ名（内部識別子）
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// 表示名（日本語）
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// カテゴリ
        /// </summary>
        public CommandCategory Category { get; set; }

        /// <summary>
        /// 説明文
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 表示優先度
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// サブ優先度
        /// </summary>
        public int SubPriority { get; set; }

        /// <summary>
        /// Ifコマンドフラグ
        /// </summary>
        public bool IsIfCommand { get; set; }

        /// <summary>
        /// ループコマンドフラグ
        /// </summary>
        public bool IsLoopCommand { get; set; }
    }
}