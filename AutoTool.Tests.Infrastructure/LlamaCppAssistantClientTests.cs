using AutoTool.Application.Assistant;
using AutoTool.Infrastructure.Assistant;

namespace AutoTool.Automation.Runtime.Tests;

/// <summary>
/// llama.cpp AI相談クライアントの安全側エラー処理を確認します。
/// </summary>
public class LlamaCppAssistantClientTests
{
    [Fact]
    public async Task AskAsync_DisabledSettings_ReturnsUserFriendlyFailure()
    {
        using var client = new LlamaCppAssistantClient();
        var request = new AssistantRequest(
            "テスト",
            AssistantContext.Empty,
            new AssistantSettings { IsEnabled = false });

        var response = await client.AskAsync(request, CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Contains("AI相談が無効", response.ErrorMessage);
    }

    [Fact]
    public async Task GenerateMacroAsync_DisabledSettings_ReturnsUserFriendlyFailure()
    {
        using var client = new LlamaCppAssistantClient();
        var request = new AssistantRequest(
            "メモ帳を開いて待機する",
            AssistantContext.Empty,
            new AssistantSettings { IsEnabled = false });

        var response = await client.GenerateMacroAsync(request, CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Contains("AI相談が無効", response.ErrorMessage);
    }

    [Fact]
    public async Task TestConnectionAsync_AutoStartWithoutServerPath_ReturnsUserFriendlyFailure()
    {
        using var client = new LlamaCppAssistantClient();
        var settings = new AssistantSettings
        {
            StartServerAutomatically = true,
            ModelPath = "model.gguf"
        };

        var response = await client.TestConnectionAsync(settings, CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Contains("llama-server.exe", response.ErrorMessage);
    }
}
