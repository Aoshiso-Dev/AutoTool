namespace AutoTool.Plugin.Abstractions.PluginModel;

public sealed record PluginUiRequest
{
    public required string RequestType { get; init; }
    public string? Title { get; init; }
    public string? Message { get; init; }
    public string? ResourcePath { get; init; }
    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}


