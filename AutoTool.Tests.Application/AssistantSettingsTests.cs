using AutoTool.Application.Assistant;

namespace AutoTool.Automation.Runtime.Tests;

/// <summary>
/// AI相談設定の入力値正規化を確認します。
/// </summary>
public class AssistantSettingsTests
{
    [Fact]
    public void Normalize_ClampsNumericValues()
    {
        var settings = new AssistantSettings
        {
            Port = 1,
            ContextLength = 1,
            MaxOutputTokens = 1,
            TimeoutSeconds = 1,
            ModelName = "  "
        };

        settings.Normalize();

        Assert.Equal(1024, settings.Port);
        Assert.Equal(1024, settings.ContextLength);
        Assert.Equal(128, settings.MaxOutputTokens);
        Assert.Equal(10, settings.TimeoutSeconds);
        Assert.Equal("local-model", settings.ModelName);
    }

    [Fact]
    public void Normalize_TrimsPathsAndModelName()
    {
        var settings = new AssistantSettings
        {
            LlamaServerPath = "  C:/tools/llama-server.exe  ",
            ModelPath = "  C:/models/qwen.gguf  ",
            ModelName = "  qwen-local  "
        };

        settings.Normalize();

        Assert.Equal("C:/tools/llama-server.exe", settings.LlamaServerPath);
        Assert.Equal("C:/models/qwen.gguf", settings.ModelPath);
        Assert.Equal("qwen-local", settings.ModelName);
    }
}
