using System.Diagnostics.CodeAnalysis;

namespace AutoTool.Application.Ports;

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
