namespace AutoTool.Core.Serialization;


public sealed class ScriptDto
{
    public int SchemaVersion { get; set; } = 1;
    public List<CommandDto> Root { get; set; } = new();
}