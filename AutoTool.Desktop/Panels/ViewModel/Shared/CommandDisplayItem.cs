using System;

namespace AutoTool.Desktop.Panels.ViewModel.Shared;

/// <summary>
/// コマンド表示用のアイテムクラス
/// </summary>
public class CommandDisplayItem
{
    public string TypeName { get; init; } = string.Empty;      // 内部で使用する英語名
    public string DisplayName { get; init; } = string.Empty;   // UI表示用の日本語名
    public string Category { get; init; } = string.Empty;      // カテゴリ名
    public string Description { get; init; } = string.Empty;   // コマンドの説明文
    public string PluginId { get; init; } = string.Empty;      // プラグイン識別子
    public int DisplayPriority { get; init; } = 9;             // カテゴリの表示順
    public int DisplaySubPriority { get; init; } = 0;          // カテゴリ内の表示順
    public bool ShowInCommandList { get; init; } = true;       // コマンド追加一覧に表示するか
    public bool IsPluginCommand => !string.IsNullOrWhiteSpace(PluginId);

    /// <summary>
    /// デバッグ用の文字列表現（DisplayNameではなくTypeNameを返す）
    /// </summary>
    public override string ToString() => $"{DisplayName} ({TypeName})";
    
    public override bool Equals(object? obj)
    {
        return obj is CommandDisplayItem other && TypeName == other.TypeName;
    }
    
    public override int GetHashCode()
    {
        return TypeName.GetHashCode();
    }
}
