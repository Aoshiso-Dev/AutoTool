namespace AutoTool.Model;

public class FavoriteMacroEntry
{
    public string Name { get; set; } = string.Empty;
    public string SnapshotPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
