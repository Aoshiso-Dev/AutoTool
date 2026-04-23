namespace AutoTool.Plugin.Abstractions.PluginModel;

public sealed record PluginInitializationResult
{
    public static PluginInitializationResult Success() => new() { IsSuccess = true };

    public static PluginInitializationResult Failure(string message) => new()
    {
        IsSuccess = false,
        Message = message,
    };

    public required bool IsSuccess { get; init; }
    public string? Message { get; init; }
}


