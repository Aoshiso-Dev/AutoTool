using System;

namespace AutoTool.Automation.Runtime.Definitions;

/// <summary>
/// コマンド定義メタデータを表す属性。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CommandDefinitionAttribute : Attribute
{
    /// <summary>
    /// コマンド種別名
    /// </summary>
    public string TypeName { get; }
    
    /// <summary>
    /// 実行コマンド型
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
    /// If 開始コマンドかどうか
    /// </summary>
    public bool IsIfCommand { get; }
    
    /// <summary>
    /// Loop 開始コマンドかどうか
    /// </summary>
    public bool IsLoopCommand { get; }
    
    /// <summary>
    /// ネスト終了コマンドかどうか
    /// </summary>
    public bool IsEndCommand { get; }

    /// <summary>
    /// 表示優先度
    /// </summary>
    public int DisplayPriority { get; }

    /// <summary>
    /// 同一優先度内の表示順
    /// </summary>
    public int DisplaySubPriority { get; }

    /// <summary>
    /// 日本語表示名
    /// </summary>
    public string DisplayNameJa { get; }

    /// <summary>
    /// 英語表示名
    /// </summary>
    public string DisplayNameEn { get; }

    public CommandDefinitionAttribute(
        string typeName, 
        Type commandType, 
        Type settingsType, 
        CommandCategory category = CommandCategory.Action,
        bool isIfCommand = false,
        bool isLoopCommand = false,
        bool isEndCommand = false,
        int displayPriority = 9,
        int displaySubPriority = 0,
        string? displayNameJa = null,
        string? displayNameEn = null)
    {
        TypeName = typeName;
        CommandType = commandType;
        SettingsType = settingsType;
        Category = category;
        IsIfCommand = isIfCommand;
        IsLoopCommand = isLoopCommand;
        IsEndCommand = isEndCommand;
        DisplayPriority = displayPriority;
        DisplaySubPriority = displaySubPriority;
        DisplayNameJa = string.IsNullOrWhiteSpace(displayNameJa) ? typeName : displayNameJa;
        DisplayNameEn = string.IsNullOrWhiteSpace(displayNameEn) ? typeName : displayNameEn;
    }
}

/// <summary>
/// コマンドカテゴリ
/// </summary>
public enum CommandCategory
{
    Action,   // 基本アクション
    Control,  // 制御構文
    AI,       // AI 関連
    System,   // システム操作
    Variable  // 変数操作
}

