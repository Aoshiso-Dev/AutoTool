namespace AutoTool.Plugin.Host.Models;

public sealed record VideoStreamRegistryIssue
{
    public required string SourceId { get; init; }

    public required string ProviderPluginId { get; init; }

    public required string Message { get; init; }
}
