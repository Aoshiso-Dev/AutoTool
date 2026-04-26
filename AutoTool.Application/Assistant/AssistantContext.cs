namespace AutoTool.Application.Assistant;

/// <summary>
/// AI相談へ渡すAutoTool側の現在状態を表します。
/// </summary>
public sealed record AssistantContext(
    string MacroSummary,
    string SelectedCommandSummary,
    string RecentLogSummary)
{
    public static AssistantContext Empty { get; } = new(string.Empty, string.Empty, string.Empty);
}
