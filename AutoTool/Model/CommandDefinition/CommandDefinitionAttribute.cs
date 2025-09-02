using System;

namespace AutoTool.Model.CommandDefinition
{
    /// <summary>
    /// Phase 4統合版：コマンド定義用の属性。この属性を付けたクラスを自動的にコマンドタイプとして登録する
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandDefinitionAttribute : Attribute
    {
        /// <summary>
        /// コマンドタイプ名
        /// </summary>
        public string TypeName { get; }
        
        /// <summary>
        /// コマンド実装クラス型
        /// </summary>
        public Type CommandType { get; }
        
        /// <summary>
        /// 設定インターフェース型
        /// </summary>
        public Type SettingsType { get; }
        
        /// <summary>
        /// コマンドカテゴリ
        /// </summary>
        public CommandCategory Category { get; }
        
        /// <summary>
        /// If文系コマンドかどうか
        /// </summary>
        public bool IsIfCommand { get; }
        
        /// <summary>
        /// ループ系コマンドかどうか
        /// </summary>
        public bool IsLoopCommand { get; }

        public CommandDefinitionAttribute(
            string typeName, 
            Type commandType, 
            Type settingsType, 
            CommandCategory category = CommandCategory.Action,
            bool isIfCommand = false,
            bool isLoopCommand = false)
        {
            TypeName = typeName;
            CommandType = commandType;
            SettingsType = settingsType;
            Category = category;
            IsIfCommand = isIfCommand;
            IsLoopCommand = isLoopCommand;
        }
    }

    /// <summary>
    /// コマンドのカテゴリ（Phase 4統合版）
    /// </summary>
    public enum CommandCategory
    {
        Action,        // 基本アクション
        Control,       // 制御構造
        AI,           // AI関連
        System,       // システム
        Variable      // 変数
    }
}