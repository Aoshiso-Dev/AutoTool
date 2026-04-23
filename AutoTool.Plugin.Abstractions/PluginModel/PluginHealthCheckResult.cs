namespace AutoTool.Plugin.Abstractions.PluginModel;

public sealed record PluginHealthCheckResult
{
    public required bool IsHealthy { get; init; }
    public string? Summary { get; init; }
    public IReadOnlyList<string> Messages { get; init; } = [];
}


