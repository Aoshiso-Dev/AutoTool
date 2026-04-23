namespace AutoTool.Plugin.Abstractions.PluginModel;

public sealed record PluginArtifactRequest
{
    public required string ArtifactType { get; init; }
    public required string RelativePath { get; init; }
    public string? ContentType { get; init; }
    public byte[]? BinaryContent { get; init; }
    public string? TextContent { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}


