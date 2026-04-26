using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoTool.Application.Assistant;

namespace AutoTool.Infrastructure.Assistant;

/// <summary>
/// llama.cpp のOpenAI互換APIを利用してAI相談を実行します。
/// </summary>
public sealed class LlamaCppAssistantClient : IAssistantClient, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient = new();
    private readonly LlamaCppServerProcess _serverProcess = new();

    public async Task<AssistantResponse> AskAsync(AssistantRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var settings = request.Settings.Normalize();
        if (!settings.IsEnabled)
        {
            return AssistantResponse.Failure("AI相談が無効です。設定で有効にしてください。");
        }

        if (string.IsNullOrWhiteSpace(request.UserMessage))
        {
            return AssistantResponse.Failure("質問を入力してください。");
        }

        if (!_serverProcess.EnsureStarted(settings, out var processError))
        {
            return AssistantResponse.Failure(processError);
        }

        var payload = CreateChatPayload(request);
        return await SendChatAsync(settings, payload, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AssistantResponse> TestConnectionAsync(AssistantSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var normalized = settings.Normalize();
        if (!_serverProcess.EnsureStarted(normalized, out var processError))
        {
            return AssistantResponse.Failure(processError);
        }

        var payload = new ChatCompletionRequest(
            normalized.ModelName,
            [
                new("system", "あなたはAutoToolの接続確認に短く日本語で答えるアシスタントです。"),
                new("user", "接続確認です。短く返答してください。")
            ],
            64,
            false,
            0.2);

        return await SendChatAsync(normalized, payload, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AssistantResponse> SendChatAsync(
        AssistantSettings settings,
        ChatCompletionRequest payload,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        try
        {
            var endpoint = new Uri($"http://127.0.0.1:{settings.Port}/v1/chat/completions");
            using var response = await _httpClient
                .PostAsJsonAsync(endpoint, payload, SerializerOptions, timeoutCts.Token)
                .ConfigureAwait(false);

            var responseText = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return AssistantResponse.Failure($"llama.cpp がエラーを返しました。HTTP {(int)response.StatusCode}: {TrimForDisplay(responseText, 300)}");
            }

            var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseText, SerializerOptions);
            var message = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
            return string.IsNullOrWhiteSpace(message)
                ? AssistantResponse.Failure("llama.cpp から回答本文を取得できませんでした。")
                : AssistantResponse.Success(message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AssistantResponse.Failure("llama.cpp の応答がタイムアウトしました。モデルが重いか、サーバーが起動中の可能性があります。");
        }
        catch (HttpRequestException ex)
        {
            return AssistantResponse.Failure($"llama.cpp に接続できません。サーバーの起動状態とポート番号を確認してください。{ex.Message}");
        }
        catch (Exception ex)
        {
            return AssistantResponse.Failure($"AI相談の呼び出しに失敗しました。{ex.Message}");
        }
    }

    private static ChatCompletionRequest CreateChatPayload(AssistantRequest request)
    {
        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(request);
        return new ChatCompletionRequest(
            request.Settings.ModelName,
            [
                new("system", systemPrompt),
                new("user", userPrompt)
            ],
            request.Settings.MaxOutputTokens,
            false,
            0.4);
    }

    private static string BuildSystemPrompt()
    {
        return """
            あなたはAutoToolのローカルAI相談アシスタントです。
            回答本文は必ず日本語で書いてください。
            ユーザーが英語や他の言語で質問しても、日本語で回答してください。
            コード、ファイルパス、API名、コマンド名、ログ引用など、英語表記が必要な固有名詞だけはそのまま使ってかまいません。
            回答を返す前に、説明文が日本語になっていることを確認してください。
            簡潔かつ具体的に回答してください。
            AutoToolのマクロやログについて、説明・原因候補・改善案を提示します。
            不確かな内容は断定せず、「可能性があります」と表現してください。
            マクロを自動変更したとは言わず、提案として回答してください。
            危険な操作や削除につながる内容は、実行前の確認を促してください。
            """;
    }

    private static string BuildUserPrompt(AssistantRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("ユーザーの質問:");
        builder.AppendLine(request.UserMessage.Trim());
        builder.AppendLine();

        if (request.Settings.IncludeMacroContext && !string.IsNullOrWhiteSpace(request.Context.MacroSummary))
        {
            builder.AppendLine("現在のマクロ:");
            builder.AppendLine(request.Context.MacroSummary);
            builder.AppendLine();
        }

        if (request.Settings.IncludeSelectedCommandContext && !string.IsNullOrWhiteSpace(request.Context.SelectedCommandSummary))
        {
            builder.AppendLine("選択中コマンド:");
            builder.AppendLine(request.Context.SelectedCommandSummary);
            builder.AppendLine();
        }

        if (request.Settings.IncludeLogContext && !string.IsNullOrWhiteSpace(request.Context.RecentLogSummary))
        {
            builder.AppendLine("直近ログ:");
            builder.AppendLine(request.Context.RecentLogSummary);
            builder.AppendLine();
        }

        builder.AppendLine("回答ルール:");
        builder.AppendLine("説明文は必ず日本語で回答してください。英語で質問されても日本語で答えてください。");

        return builder.ToString();
    }

    private static string TrimForDisplay(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text.Length <= maxLength ? text : text[..maxLength] + "...";
    }

    public void Dispose()
    {
        _serverProcess.Dispose();
        _httpClient.Dispose();
    }

    private sealed record ChatCompletionRequest(
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        bool Stream,
        double Temperature);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ChatCompletionResponse(IReadOnlyList<ChatChoice>? Choices);

    private sealed record ChatChoice(ChatMessageResponse? Message);

    private sealed record ChatMessageResponse(string? Content);
}
