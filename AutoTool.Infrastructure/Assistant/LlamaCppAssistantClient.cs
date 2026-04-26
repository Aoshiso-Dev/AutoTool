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

    public async Task<AssistantMacroGenerationResponse> GenerateMacroAsync(
        AssistantRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var settings = request.Settings.Normalize();
        if (!settings.IsEnabled)
        {
            return AssistantMacroGenerationResponse.Failure("AI相談が無効です。設定で有効にしてください。");
        }

        if (string.IsNullOrWhiteSpace(request.UserMessage))
        {
            return AssistantMacroGenerationResponse.Failure("生成したいマクロの内容を入力してください。");
        }

        if (!_serverProcess.EnsureStarted(settings, out var processError))
        {
            return AssistantMacroGenerationResponse.Failure(processError);
        }

        var payload = CreateMacroGenerationPayload(request);
        var response = await SendChatAsync(settings, payload, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess)
        {
            return AssistantMacroGenerationResponse.Failure(response.ErrorMessage ?? "マクロ生成に失敗しました。");
        }

        if (TryParseGeneratedMacro(response.Message, out var commands, out var parseError))
        {
            return AssistantMacroGenerationResponse.Success(commands, BuildGeneratedMacroSummary(commands));
        }

        var retryPayload = CreateMacroGenerationRepairPayload(request, response.Message, parseError);
        var retryResponse = await SendChatAsync(settings, retryPayload, cancellationToken).ConfigureAwait(false);
        if (!retryResponse.IsSuccess)
        {
            return AssistantMacroGenerationResponse.Failure(retryResponse.ErrorMessage ?? parseError);
        }

        return TryParseGeneratedMacro(retryResponse.Message, out commands, out parseError)
            ? AssistantMacroGenerationResponse.Success(commands, BuildGeneratedMacroSummary(commands))
            : AssistantMacroGenerationResponse.Failure(parseError);
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

    private static ChatCompletionRequest CreateMacroGenerationPayload(AssistantRequest request)
    {
        return new ChatCompletionRequest(
            request.Settings.ModelName,
            [
                new("system", BuildMacroGenerationSystemPrompt()),
                new("user", BuildMacroGenerationUserPrompt(request))
            ],
            Math.Max(request.Settings.MaxOutputTokens, 1024),
            false,
            0.2);
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

    private static string BuildMacroGenerationSystemPrompt()
    {
        return """
            あなたはAutoToolのマクロ生成アシスタントです。
            回答は必ずJSONのみで返してください。説明文、Markdown、コードフェンスは禁止です。
            JSON形式は {"commands":[{"type":"ItemType","comment":"日本語の短い説明","enabled":true,"parameters":{"PropertyName":"value"},"warnings":["注意点"]}]} です。
            type には利用可能コマンド一覧にある ItemType だけを使ってください。
            parameters には、そのコマンドの設定プロパティ名と値だけを入れてください。不明な設定は入れないでください。
            EndIf、EndLoop、RetryEnd などの終了コマンドは出力しないでください。
            設定値が不明な操作は、近いコマンドを選び、必要な設定内容を comment に日本語で書いてください。
            危険な削除や大量操作は、確認を促すコメントを含めてください。
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

    private static string BuildMacroGenerationUserPrompt(AssistantRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("生成したいマクロ:");
        builder.AppendLine(request.UserMessage.Trim());
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(request.Context.AvailableCommandSummary))
        {
            builder.AppendLine("利用可能コマンド一覧:");
            builder.AppendLine(request.Context.AvailableCommandSummary);
            builder.AppendLine();
        }

        if (request.Settings.IncludeMacroContext && !string.IsNullOrWhiteSpace(request.Context.MacroSummary))
        {
            builder.AppendLine("現在のマクロ:");
            builder.AppendLine(request.Context.MacroSummary);
            builder.AppendLine();
        }

        builder.AppendLine("出力ルール:");
        builder.AppendLine("JSONのみを返してください。comment と warnings は必ず日本語にしてください。");
        return builder.ToString();
    }

    private static ChatCompletionRequest CreateMacroGenerationRepairPayload(
        AssistantRequest request,
        string invalidResponse,
        string parseError)
    {
        return new ChatCompletionRequest(
            request.Settings.ModelName,
            [
                new("system", BuildMacroGenerationSystemPrompt()),
                new("user", BuildMacroGenerationUserPrompt(request)),
                new("assistant", invalidResponse),
                new("user", $"直前の応答はJSONとして利用できません。理由: {parseError}\n説明文やMarkdownを含めず、指定形式のJSONだけを再出力してください。")
            ],
            Math.Max(request.Settings.MaxOutputTokens, 1024),
            false,
            0.1);
    }

    private static bool TryParseGeneratedMacro(
        string responseText,
        out IReadOnlyList<AssistantGeneratedMacroCommand> commands,
        out string errorMessage)
    {
        commands = [];
        errorMessage = string.Empty;

        var json = ExtractJson(responseText);
        if (string.IsNullOrWhiteSpace(json))
        {
            errorMessage = "AIの応答からマクロ生成用JSONを取得できませんでした。";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var commandElements = EnumerateCommandElements(document.RootElement).ToArray();
            if (commandElements.Length == 0)
            {
                errorMessage = "AIの応答に commands が含まれていませんでした。";
                return false;
            }

            List<AssistantGeneratedMacroCommand> parsed = [];
            foreach (var element in commandElements)
            {
                var type = ReadString(element, "type")
                    ?? ReadString(element, "itemType")
                    ?? ReadString(element, "ItemType");
                if (string.IsNullOrWhiteSpace(type))
                {
                    continue;
                }

                var comment = ReadString(element, "comment")
                    ?? ReadString(element, "description")
                    ?? "AI生成コマンド";
                var enabled = ReadBoolean(element, "enabled") ?? true;
                var parameters = ReadStringDictionary(element, "parameters");
                var warnings = ReadStringArray(element, "warnings");
                parsed.Add(new(type.Trim(), comment.Trim(), enabled, parameters, warnings));
            }

            if (parsed.Count == 0)
            {
                errorMessage = "AIの応答に有効なコマンド種別が含まれていませんでした。";
                return false;
            }

            commands = parsed;
            return true;
        }
        catch (JsonException ex)
        {
            errorMessage = $"AIの応答JSONを解析できませんでした。{ex.Message}";
            return false;
        }
    }

    private static IEnumerable<JsonElement> EnumerateCommandElements(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root.EnumerateArray().ToArray();
        }

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("commands", out var commands)
            && commands.ValueKind == JsonValueKind.Array)
        {
            return commands.EnumerateArray().ToArray();
        }

        return [];
    }

    private static string ExtractJson(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            return trimmed;
        }

        var objectStart = trimmed.IndexOf('{', StringComparison.Ordinal);
        var arrayStart = trimmed.IndexOf('[', StringComparison.Ordinal);
        var start = objectStart < 0 ? arrayStart : arrayStart < 0 ? objectStart : Math.Min(objectStart, arrayStart);
        if (start < 0)
        {
            return string.Empty;
        }

        var end = trimmed.LastIndexOf(trimmed[start] == '{' ? '}' : ']');
        return end <= start ? string.Empty : trimmed[start..(end + 1)];
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static bool? ReadBoolean(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static IReadOnlyDictionary<string, string>? ReadStringDictionary(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (var member in property.EnumerateObject())
        {
            var value = member.Value.ValueKind switch
            {
                JsonValueKind.String => member.Value.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => member.Value.GetRawText(),
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(member.Name) && !string.IsNullOrWhiteSpace(value))
            {
                values[member.Name] = value;
            }
        }

        return values.Count == 0 ? null : values;
    }

    private static IReadOnlyList<string>? ReadStringArray(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            var value = property.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : [value];
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var values = property
            .EnumerateArray()
            .Where(static x => x.ValueKind == JsonValueKind.String)
            .Select(static x => x.GetString())
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x!)
            .ToList();

        return values.Count == 0 ? null : values;
    }

    private static string BuildGeneratedMacroSummary(IReadOnlyList<AssistantGeneratedMacroCommand> commands)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"マクロ案を {commands.Count} 件生成しました。コマンド一覧へ追加します。");
        foreach (var command in commands.Take(20))
        {
            builder.Append("- ")
                .Append(command.ItemType)
                .Append(": ")
                .AppendLine(command.Comment);
        }

        if (commands.Count > 20)
        {
            builder.AppendLine($"... 残り {commands.Count - 20} 件");
        }

        return builder.ToString().TrimEnd();
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
