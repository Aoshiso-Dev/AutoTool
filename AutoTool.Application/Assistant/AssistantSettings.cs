namespace AutoTool.Application.Assistant;

/// <summary>
/// AI相談機能の接続設定を保持します。
/// </summary>
public sealed class AssistantSettings
{
    public bool IsEnabled { get; set; }
    public AssistantProviderKind ProviderKind { get; set; } = AssistantProviderKind.LlamaCpp;
    public string LlamaServerPath { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
    public string ModelName { get; set; } = "local-model";
    public int Port { get; set; } = 8088;
    public int ContextLength { get; set; } = 4096;
    public int MaxOutputTokens { get; set; } = 512;
    public int TimeoutSeconds { get; set; } = 60;
    public bool StartServerAutomatically { get; set; }
    public bool IncludeMacroContext { get; set; } = true;
    public bool IncludeSelectedCommandContext { get; set; } = true;
    public bool IncludeLogContext { get; set; }

    public AssistantSettings Normalize()
    {
        Port = Math.Clamp(Port, 1024, 65535);
        ContextLength = Math.Clamp(ContextLength, 1024, 131072);
        MaxOutputTokens = Math.Clamp(MaxOutputTokens, 128, 8192);
        TimeoutSeconds = Math.Clamp(TimeoutSeconds, 10, 600);
        ModelName = string.IsNullOrWhiteSpace(ModelName) ? "local-model" : ModelName.Trim();
        LlamaServerPath = LlamaServerPath.Trim();
        ModelPath = ModelPath.Trim();
        return this;
    }
}
