namespace AutoTool.Model;

public class FavoriteMacroEntry
{
    public string Name { get; set; } = string.Empty;
    public string SnapshotPath { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
}

