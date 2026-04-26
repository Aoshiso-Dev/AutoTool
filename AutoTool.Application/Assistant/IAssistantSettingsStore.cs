namespace AutoTool.Application.Assistant;

/// <summary>
/// AI相談設定の永続化を担当するポートです。
/// </summary>
public interface IAssistantSettingsStore
{
    AssistantSettings Load();
    void Save(AssistantSettings settings);
}
