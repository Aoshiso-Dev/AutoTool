namespace AutoTool.Application.Assistant;

/// <summary>
/// AI相談の応答結果を表します。
/// </summary>
public sealed record AssistantResponse(
    bool IsSuccess,
    string Message,
    string? ErrorMessage = null)
{
    public static AssistantResponse Success(string message) => new(true, message);

    public static AssistantResponse Failure(string errorMessage) => new(false, string.Empty, errorMessage);
}
