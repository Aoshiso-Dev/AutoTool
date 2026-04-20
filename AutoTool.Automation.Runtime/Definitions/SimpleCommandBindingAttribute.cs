using System;

namespace AutoTool.Automation.Runtime.Definitions;

/// <summary>
/// 一覧アイテム型と実行コマンド型を関連付け、UI 編集結果から実行インスタンスを生成できるようにします。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SimpleCommandBindingAttribute : Attribute
{
    /// <summary>
    /// 実行コマンド型
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

