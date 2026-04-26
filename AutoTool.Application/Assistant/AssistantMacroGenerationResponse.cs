namespace AutoTool.Application.Assistant;

/// <summary>
/// AIによるマクロ生成の結果を表します。
/// </summary>
public sealed record AssistantMacroGenerationResponse(
    bool IsSuccess,
    IReadOnlyList<AssistantGeneratedMacroCommand> Commands,
    string Message,
    string? ErrorMessage = null)
{
    public static AssistantMacroGenerationResponse Success(
        IReadOnlyList<AssistantGeneratedMacroCommand> commands,
        string message) =>
        new(true, commands, message);

    public static AssistantMacroGenerationResponse Failure(string errorMessage) =>
        new(false, [], string.Empty, errorMessage);
}
