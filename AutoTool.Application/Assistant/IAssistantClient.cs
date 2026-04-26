namespace AutoTool.Application.Assistant;

/// <summary>
/// AI相談プロバイダーへ質問と接続確認を行うポートです。
/// </summary>
public interface IAssistantClient
{
    Task<AssistantResponse> AskAsync(AssistantRequest request, CancellationToken cancellationToken);
    Task<AssistantMacroGenerationResponse> GenerateMacroAsync(AssistantRequest request, CancellationToken cancellationToken);
    Task<AssistantResponse> TestConnectionAsync(AssistantSettings settings, CancellationToken cancellationToken);
}
