using System;

namespace AutoTool.Automation.Runtime.Definitions;

/// <summary>
/// コマンドアイテムと実行コマンドを結び付ける属性。
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

