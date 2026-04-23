namespace AutoTool.Plugin.Abstractions.PluginModel;

public sealed record PluginCommandPropertyDefinition
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string EditorType { get; init; }
    public int Order { get; init; }
    public string? Group { get; init; }
    public string? Description { get; init; }
    public bool IsRequired { get; init; }
    public IReadOnlyList<string> Options { get; init; } = [];
}


