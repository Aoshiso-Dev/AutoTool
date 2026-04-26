namespace AutoTool.Application.Assistant;

/// <summary>
/// AIマクロ生成の履歴1件分を表します。
/// </summary>
public sealed record AssistantMacroGenerationHistoryEntry(
    DateTimeOffset CreatedAt,
    string RequestText,
    IReadOnlyList<AssistantGeneratedMacroCommand> Commands,
    string Summary);
