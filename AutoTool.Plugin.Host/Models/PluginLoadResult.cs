namespace AutoTool.Plugin.Host.Models;

public sealed record PluginLoadResult
{
    public required PluginManifestLoadResult ManifestLoadResult { get; init; }

    public LoadedPlugin? Plugin { get; init; }

    public bool IsLoaded { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];
}
