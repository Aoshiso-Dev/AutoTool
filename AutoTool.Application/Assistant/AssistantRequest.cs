namespace AutoTool.Application.Assistant;

/// <summary>
/// AI相談への質問内容と文脈を表します。
/// </summary>
public sealed record AssistantRequest(
    string UserMessage,
    AssistantContext Context,
    AssistantSettings Settings);
