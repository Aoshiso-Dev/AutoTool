namespace AutoTool.Plugin.Abstractions.PluginModel;

public sealed record PluginServiceRegistration
{
    public required Type ServiceType { get; init; }

    public Type? ImplementationType { get; init; }

    public object? Instance { get; init; }

    public required PluginServiceLifetime Lifetime { get; init; }
}

