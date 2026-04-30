namespace AutoTool.Plugin.Abstractions.PluginModel;

public sealed record PluginCommandDefinition
{
    public required string CommandType { get; init; }
    public required string DisplayName { get; init; }
    public required string Category { get; init; }
    public int Order { get; init; }
    public string? Description { get; init; }
    public bool ShowInCommandList { get; init; } = true;
    public string PluginId { get; init; } = string.Empty;
    public string? Version { get; init; }
    public IReadOnlyList<string> RequiredPermissions { get; init; } = [];
    public IReadOnlyList<PluginCommandPropertyDefinition> Properties { get; init; } = [];
}



