namespace AutoTool.Plugin.Template;

internal sealed record WriteVariableParameters
{
    public string TargetVariable { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}
