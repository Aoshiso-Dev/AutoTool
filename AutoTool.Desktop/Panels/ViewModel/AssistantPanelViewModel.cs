using System.Collections.ObjectModel;
using AutoTool.Application.Assistant;
using AutoTool.Desktop.Panels.ViewModel.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoTool.Desktop.Panels.ViewModel;

/// <summary>
/// ローカルAIへ質問し、AutoToolの文脈を添えた相談履歴を管理します。
/// </summary>
public partial class AssistantPanelViewModel(
    IAssistantClient assistantClient,
    IAssistantSettingsStore settingsStore,
    IAssistantMacroGenerationHistoryStore macroHistoryStore,
    TimeProvider timeProvider)
    : ObservableObject, IAssistantPanelViewModel
{
    private Func<AssistantContext> _contextProvider = static () => AssistantContext.Empty;
    private CancellationTokenSource? _requestCts;

    public event Action<string>? StatusMessageRequested;
    public event Action<AssistantMacroGenerationResult>? MacroGenerated;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendQuestionCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExplainMacroCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateMacroCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowMacroHistoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReplayLatestMacroCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelRequestCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendQuestionCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateMacroCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReplayLatestMacroCommand))]
    private string _questionText = string.Empty;

    [ObservableProperty]
    private string _connectionStatus = "未接続";

    public ObservableCollection<AssistantMessage> Messages { get; } = [];

    public void SetContextProvider(Func<AssistantContext> contextProvider)
    {
        ArgumentNullException.ThrowIfNull(contextProvider);
        _contextProvider = contextProvider;
    }

    public void Prepare()
    {
    }

    public void SetRunningState(bool isRunning) => IsRunning = isRunning;

    public void RequestGenerateMacro(string requestText)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(requestText))
        {
            return;
        }

        QuestionText = requestText.Trim();
        _ = GenerateMacroAsync();
    }

    [RelayCommand(CanExecute = nameof(CanSendQuestion))]
    private async Task SendQuestionAsync()
    {
        var question = QuestionText.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            return;
        }

        QuestionText = string.Empty;
        await AskAsync(question);
    }

    [RelayCommand(CanExecute = nameof(CanRunAssistantAction))]
    private Task ExplainMacroAsync()
    {
        return AskAsync("現在のマクロが何をする内容か、実行順にわかりやすく説明してください。問題になりそうな点があれば短く指摘してください。");
    }

    [RelayCommand(CanExecute = nameof(CanGenerateMacro))]
    private async Task GenerateMacroAsync()
    {
        var requestText = QuestionText.Trim();
        if (string.IsNullOrWhiteSpace(requestText))
        {
            return;
        }

        QuestionText = string.Empty;
        AddUserMessage($"マクロ生成: {requestText}");
        var pendingMessage = AddPendingAssistantMessage("マクロ案を生成中...");

        await RunAssistantCallAsync(async cancellationToken =>
        {
            var settings = settingsStore.Load();
            var request = new AssistantRequest(requestText, _contextProvider(), settings);
            var response = await assistantClient.GenerateMacroAsync(request, cancellationToken);
            if (response.IsSuccess)
            {
                var result = new AssistantMacroGenerationResult(
                    requestText,
                    response.Commands,
                    timeProvider.GetUtcNow());
                SaveMacroGenerationHistory(result, response.Message);
                ConnectionStatus = "接続済み";
                CompletePendingMessage(pendingMessage, response.Message);
                MacroGenerated?.Invoke(result);
                StatusMessageRequested?.Invoke($"AIがマクロ案を {response.Commands.Count} 件生成しました。");
                return;
            }

            ConnectionStatus = "エラー";
            CompletePendingMessage(pendingMessage, response.ErrorMessage ?? "マクロ生成に失敗しました。");
            StatusMessageRequested?.Invoke("マクロ生成に失敗しました。");
        });

        if (pendingMessage.IsPending)
        {
            CompletePendingMessage(pendingMessage, "マクロ生成をキャンセルしました。");
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunAssistantAction))]
    private void ShowMacroHistory()
    {
        var entries = macroHistoryStore.Load();
        if (entries.Count == 0)
        {
            AddSystemMessage("マクロ生成履歴はまだありません。");
            return;
        }

        var lines = entries
            .Take(10)
            .Select((entry, index) =>
                $"{index + 1}. {entry.CreatedAt.LocalDateTime:MM/dd HH:mm} {entry.RequestText}（{entry.Commands.Count}件）");

        AddSystemMessage("マクロ生成履歴:\n" + string.Join('\n', lines));
    }

    [RelayCommand(CanExecute = nameof(CanReplayLatestMacro))]
    private void ReplayLatestMacro()
    {
        var entry = macroHistoryStore.Load().FirstOrDefault();
        if (entry is null)
        {
            AddSystemMessage("再追加できるマクロ生成履歴はありません。");
            return;
        }

        MacroGenerated?.Invoke(new AssistantMacroGenerationResult(
            entry.RequestText,
            entry.Commands,
            entry.CreatedAt));
        AddSystemMessage($"直近のマクロ生成履歴を再追加します: {entry.RequestText}");
    }

    [RelayCommand(CanExecute = nameof(CanRunAssistantAction))]
    private async Task TestConnectionAsync()
    {
        await RunAssistantCallAsync(async cancellationToken =>
        {
            AddSystemMessage("接続テストを開始しました。");
            var settings = settingsStore.Load();
            var response = await assistantClient.TestConnectionAsync(settings, cancellationToken);
            if (response.IsSuccess)
            {
                ConnectionStatus = "接続済み";
                AddSystemMessage($"接続できました。{response.Message}");
                StatusMessageRequested?.Invoke("AI相談の接続テストに成功しました。");
                return;
            }

            ConnectionStatus = "接続失敗";
            AddSystemMessage(response.ErrorMessage ?? "接続テストに失敗しました。");
            StatusMessageRequested?.Invoke("AI相談の接続テストに失敗しました。");
        });
    }

    [RelayCommand(CanExecute = nameof(IsBusy))]
    private void CancelRequest()
    {
        RequestCancel();
    }

    public void RequestCancel()
    {
        _requestCts?.Cancel();
    }

    private async Task AskAsync(string question)
    {
        AddUserMessage(question);
        var pendingMessage = AddPendingAssistantMessage();
        await RunAssistantCallAsync(async cancellationToken =>
        {
            var settings = settingsStore.Load();
            var request = new AssistantRequest(question, _contextProvider(), settings);
            var response = await assistantClient.AskAsync(request, cancellationToken);
            if (response.IsSuccess)
            {
                ConnectionStatus = "接続済み";
                CompletePendingMessage(pendingMessage, response.Message);
                StatusMessageRequested?.Invoke("AI相談の回答を受信しました。");
                return;
            }

            ConnectionStatus = "エラー";
            CompletePendingMessage(pendingMessage, response.ErrorMessage ?? "AI相談に失敗しました。");
            StatusMessageRequested?.Invoke("AI相談に失敗しました。");
        });

        if (pendingMessage.IsPending)
        {
            CompletePendingMessage(pendingMessage, "AI相談をキャンセルしました。");
        }
    }

    private async Task RunAssistantCallAsync(Func<CancellationToken, Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        _requestCts?.Dispose();
        _requestCts = new();

        try
        {
            await action(_requestCts.Token);
        }
        catch (OperationCanceledException)
        {
            AddSystemMessage("AI相談をキャンセルしました。");
            StatusMessageRequested?.Invoke("AI相談をキャンセルしました。");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSendQuestion()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(QuestionText);
    }

    private bool CanGenerateMacro() => CanSendQuestion();

    private bool CanRunAssistantAction() => !IsBusy;

    private bool CanReplayLatestMacro() => !IsBusy;

    private void AddUserMessage(string text) => AddMessage("あなた", text, isUser: true);

    private void AddAssistantMessage(string text) => AddMessage("AI相談", text, isUser: false);

    private AssistantMessage AddPendingAssistantMessage(string text = "回答を作成中...")
    {
        var message = CreateMessage("AI相談", text, isUser: false);
        message.IsPending = true;
        RunOnUiThread(() => Messages.Add(message));
        return message;
    }

    private static void CompletePendingMessage(AssistantMessage message, string text)
    {
        RunOnUiThread(() =>
        {
            message.Text = text;
            message.IsPending = false;
        });
    }

    private void AddSystemMessage(string text) => AddMessage("システム", text, isUser: false);

    private void AddMessage(string sender, string text, bool isUser)
    {
        var message = CreateMessage(sender, text, isUser);
        RunOnUiThread(() => Messages.Add(message));
    }

    private AssistantMessage CreateMessage(string sender, string text, bool isUser)
    {
        var message = new AssistantMessage
        {
            Sender = sender,
            Text = text,
            Timestamp = timeProvider.GetLocalNow().ToString("HH:mm:ss"),
            IsUser = isUser
        };

        return message;
    }

    private void SaveMacroGenerationHistory(AssistantMacroGenerationResult result, string summary)
    {
        var entry = new AssistantMacroGenerationHistoryEntry(
            result.CreatedAt,
            result.RequestText,
            result.Commands,
            summary);
        macroHistoryStore.Append(entry);
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }
}
