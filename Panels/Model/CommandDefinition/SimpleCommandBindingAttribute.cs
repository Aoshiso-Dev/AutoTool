using System;

namespace MacroPanels.Model.CommandDefinition
{
    /// <summary>
    /// 単純なコマンドバインディング用の属性
    /// この属性が付いたアイテムは、自動的にコマンドファクトリで処理される
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SimpleCommandBindingAttribute : Attribute
    {
        /// <summary>
        /// コマンド実装クラス型
        /// </summary>
        public Type CommandType { get; }
        
        /// <summary>
        /// 設定インターフェース型
        /// </summary>
        public Type SettingsType { get; }

        public SimpleCommandBindingAttribute(Type commandType, Type settingsType)
        {
            CommandType = commandType;
            SettingsType = settingsType;
        }
    }
}