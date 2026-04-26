namespace AutoTool.Application.Assistant;

/// <summary>
/// AIが生成したマクロ候補の1行分を表します。
/// </summary>
public sealed record AssistantGeneratedMacroCommand(
    string ItemType,
    string Comment,
    bool IsEnabled = true,
    IReadOnlyDictionary<string, string>? Parameters = null,
    IReadOnlyList<string>? Warnings = null);
