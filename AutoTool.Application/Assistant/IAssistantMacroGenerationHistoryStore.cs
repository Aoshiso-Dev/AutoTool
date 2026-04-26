namespace AutoTool.Application.Assistant;

/// <summary>
/// AIマクロ生成履歴の読み書きを行います。
/// </summary>
public interface IAssistantMacroGenerationHistoryStore
{
    IReadOnlyList<AssistantMacroGenerationHistoryEntry> Load();

    void Append(AssistantMacroGenerationHistoryEntry entry, int maxCount = 20);
}
