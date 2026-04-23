using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Host.Models;

public sealed record PluginStartupDiagnostics
{
    public required string PluginId { get; init; }

    public required string DisplayName { get; init; }

    public required string Version { get; init; }

    public required bool IsHealthy { get; init; }

    public string? Summary { get; init; }

    public IReadOnlyList<string> RequestedPermissions { get; init; } = [];

    public IReadOnlyList<string> CommandPermissions { get; init; } = [];

    public IReadOnlyList<string> MissingPermissions { get; init; } = [];

    public IReadOnlyList<string> Messages { get; init; } = [];

    public PluginHealthCheckResult? HealthCheckResult { get; init; }
}

