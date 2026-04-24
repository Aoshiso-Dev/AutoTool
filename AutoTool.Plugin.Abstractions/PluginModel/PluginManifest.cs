using System.Text.Json.Serialization;

namespace AutoTool.Plugin.Abstractions.PluginModel;

public sealed record PluginManifest
{
    public required string PluginId { get; init; }
    public required string DisplayName { get; init; }
    public required string Version { get; init; }
    public required string EntryAssembly { get; init; }
    public required string EntryType { get; init; }
    public string? MinHostVersion { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = [];
    public string? SignatureThumbprint { get; init; }
    public IReadOnlyList<PluginCommandDefinition> Commands { get; init; } = [];
    public IReadOnlyList<PluginQuickActionDefinition> QuickActions { get; init; } = [];
}

public sealed record PluginQuickActionDefinition
{
    public required string ActionId { get; init; }
    public required string DisplayName { get; init; }
    public required string CommandType { get; init; }
    public string? ToolTip { get; init; }
    public string? Icon { get; init; }
    public int Order { get; init; }
    public string? Location { get; init; }
    [JsonConverter(typeof(RawJsonStringConverter))]
    public string? ParameterJson { get; init; }
}
