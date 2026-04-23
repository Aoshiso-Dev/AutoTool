namespace AutoTool.Plugin.Abstractions.PluginModel;

public sealed record PluginCommandExecutionRequest
{
    public required string PluginId { get; init; }

    public required string CommandType { get; init; }

    public string ParameterJson { get; init; } = "{}";
}

