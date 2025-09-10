using AutoTool.Core.Abstractions;

public sealed record IfSettings : AutoToolCommandSettings
{
    public int Version { get; init; } = 1;
    public string ConditionExpr { get; init; } = "true";
}
