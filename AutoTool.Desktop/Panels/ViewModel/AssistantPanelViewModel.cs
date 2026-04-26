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
    TimeProvider timeProvider)
    : ObservableObject, IAssistantPanelViewModel
{
    private Func<AssistantContext> _contextProvider = static () => AssistantContext.Empty;
    private CancellationTokenSource? _requestCts;

    public event Action<string>? StatusMessageRequested;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendQuestionCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExplainMacroCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelRequestCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendQuestionCommand))]
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

    private bool CanRunAssistantAction() => !IsBusy;

    private void AddUserMessage(string text) => AddMessage("あなた", text, isUser: true);

    private void AddAssistantMessage(string text) => AddMessage("AI相談", text, isUser: false);

    private AssistantMessage AddPendingAssistantMessage()
    {
        var message = CreateMessage("AI相談", "回答を作成中...", isUser: false);
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
