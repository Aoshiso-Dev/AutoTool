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
}



