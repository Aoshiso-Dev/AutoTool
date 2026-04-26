namespace AutoTool.Application.Assistant;

/// <summary>
/// AIが生成したマクロ案と元の依頼内容をまとめて表します。
/// </summary>
public sealed record AssistantMacroGenerationResult(
    string RequestText,
    IReadOnlyList<AssistantGeneratedMacroCommand> Commands,
    DateTimeOffset CreatedAt);
