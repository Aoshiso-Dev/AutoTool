using AutoTool.Core.Abstractions;

public sealed record WhileSettings : AutoToolCommandSettings
{
    public int Version { get; init; } = 1;
    public string ConditionExpr { get; init; } = "true";
    public int MaxIterations { get; init; } = 10_000;
}