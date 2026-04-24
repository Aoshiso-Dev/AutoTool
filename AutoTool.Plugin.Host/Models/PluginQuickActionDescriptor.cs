namespace AutoTool.Plugin.Host.Models;

public sealed record PluginQuickActionDescriptor
{
    public required string PluginId { get; init; }
    public required string ActionId { get; init; }
    public required string DisplayName { get; init; }
    public string? ToolTip { get; init; }
    public string? Icon { get; init; }
    public int Order { get; init; }
    public required string CommandType { get; init; }
    public string ParameterJson { get; init; } = "{}";
    public bool IsAvailable { get; init; }
    public string? UnavailableReason { get; init; }
}
