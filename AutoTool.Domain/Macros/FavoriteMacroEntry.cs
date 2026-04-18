using System.Diagnostics.CodeAnalysis;

namespace AutoTool.Domain.Macros;

public sealed record FavoriteMacroEntry
{
    [SetsRequiredMembers]
    public FavoriteMacroEntry()
    {
        Name = string.Empty;
        SnapshotPath = string.Empty;
    }

    public required string Name { get; set; }
    public required string SnapshotPath { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static FavoriteMacroEntry Create(string name, string snapshotPath, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("お気に入り名は必須です。", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(snapshotPath))
        {
            throw new ArgumentException("スナップショットパスは必須です。", nameof(snapshotPath));
        }

        return new FavoriteMacroEntry
        {
            Name = name.Trim(),
            SnapshotPath = snapshotPath.Trim(),
            CreatedAt = createdAt
        };
    }

    public FavoriteMacroEntry Normalize()
    {
        Name = Name.Trim();
        SnapshotPath = SnapshotPath.Trim();
        return this;
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(SnapshotPath);
    }
}
