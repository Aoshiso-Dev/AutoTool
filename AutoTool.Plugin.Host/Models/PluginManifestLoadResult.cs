using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Host.Models;

public sealed record PluginManifestLoadResult
{
    public string PluginDirectoryPath { get; init; } = string.Empty;

    public string ManifestPath { get; init; } = string.Empty;

    public PluginManifest? Manifest { get; init; }

    public bool IsValid { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];
}

