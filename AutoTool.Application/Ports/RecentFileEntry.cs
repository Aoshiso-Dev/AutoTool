using System.Diagnostics.CodeAnalysis;

namespace AutoTool.Application.Ports;

/// <summary>
/// 一覧の 1 件分として扱うデータを保持し、保存・表示で共通利用できるようにします。
/// </summary>
public sealed class RecentFileEntry
{
    [SetsRequiredMembers]
    public RecentFileEntry()
    {
        FileName = string.Empty;
        FilePath = string.Empty;
    }

    public required string FileName { get; set; }
    public required string FilePath { get; set; }
}
