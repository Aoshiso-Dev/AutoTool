using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoTool.Commands.Services;
using AutoTool.Desktop.Panels.ViewModel;
using AutoTool.Automation.Runtime.MacroFactory;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Application.Files;
using AutoTool.Application.History;
using AutoTool.Application.Ports;
using AutoTool.Application.Assistant;
using AutoTool.Automation.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using AutoTool.Commands.Interface;

namespace AutoTool.Desktop.ViewModel;

/// <summary>
/// マクロ編集パネルの状態と操作を管理する ViewModel です。
/// </summary>
public partial class MacroPanelViewModel : ObservableObject, IDisposable
{
    private readonly INotifier _notifier;
    private readonly ILogWriter _logWriter;
    private readonly ICommandEventBus _commandEventBus;
    private readonly IMacroFactory _macroFactory;
    private readonly IMacroFileSerializer _macroFileSerializer;
    private readonly ICommandRegistry _commandRegistry;
    private readonly IPathResolver _pathResolver;
    private readonly IAppDialogService _appDialogService;
    private readonly TimeProvider _timeProvider;
    private readonly IListPanelViewModel _listPanel;
    private readonly IEditPanelViewModel _editPanel;
    private readonly IButtonPanelViewModel _buttonPanel;
    private readonly ILogPanelViewModel _logPanel;
    private readonly IVariablePanelViewModel _variablePanel;
    private readonly IAssistantPanelViewModel _assistantPanel;
    private readonly IFavoritePanelViewModel _favoritePanel;
    private CancellationTokenSource? _commandEventSubscriptionCts;
    private Task? _commandEventSubscriptionTask;
    private CancellationTokenSource? _cts;
    private CommandHistoryManager? _commandHistory;
    private long _lastObservedDroppedCommandEvents;
    private bool _suppressErrorNotificationsForCommandLine;
    private bool _clearSuppressionOnExecutionCompleted;
    private bool _disposed;
    private bool _isEditDialogOpen;
    public event Action<string>? StatusMessageRequested;
    public event Action? NewFileStateRequested;
    public event Action? ExecutionCompleted;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _selectedListTabIndex;

    [ObservableProperty]
    private bool _isFavoritePanelOpen;

    [ObservableProperty]
    private bool _isLogPanelOpen;

    [ObservableProperty]
    private bool _isVariablePanelOpen;

    [ObservableProperty]
    private bool _isAssistantPanelOpen;

    [ObservableProperty]
    private bool _isPreflightPanelOpen;

    [ObservableProperty]
    private string _preflightSummary = "実行前チェックは未実行です。";

    [ObservableProperty]
    private double _favoritePanelWidth = 340;

    public ObservableCollection<PreflightIssueItem> PreflightIssues { get; } = [];

    // View バインディング側で具象型キャスト不要にするため、インターフェースとして公開します。
    public IListPanelViewModel ListPanelViewModel => _listPanel;
    public IEditPanelViewModel EditPanelViewModel => _editPanel;
    public IButtonPanelViewModel ButtonPanelViewModel => _buttonPanel;
    public ILogPanelViewModel LogPanelViewModel => _logPanel;
    public IVariablePanelViewModel VariablePanelViewModel => _variablePanel;
    public IAssistantPanelViewModel AssistantPanelViewModel => _assistantPanel;
    public IFavoritePanelViewModel FavoritePanelViewModel => _favoritePanel;

    public bool IsBottomPanelOpen => IsLogPanelOpen || IsVariablePanelOpen;

    public MacroPanelViewModel(
        INotifier notifier,
        ILogWriter logWriter,
        ICommandEventBus commandEventBus,
        IMacroFactory macroFactory,
        IMacroFileSerializer macroFileSerializer,
        ICommandRegistry commandRegistry,
        IPathResolver pathResolver,
        IAppDialogService appDialogService,
        TimeProvider timeProvider,
        IListPanelViewModel listPanelViewModel,
        IEditPanelViewModel editPanelViewModel,
        IButtonPanelViewModel buttonPanelViewModel,
        ILogPanelViewModel logPanelViewModel,
        IVariablePanelViewModel variablePanelViewModel,
        IAssistantPanelViewModel assistantPanelViewModel,
        IFavoritePanelViewModel favoritePanelViewModel)
    {
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(logWriter);
        ArgumentNullException.ThrowIfNull(commandEventBus);
        ArgumentNullException.ThrowIfNull(macroFactory);
        ArgumentNullException.ThrowIfNull(macroFileSerializer);
        ArgumentNullException.ThrowIfNull(commandRegistry);
        ArgumentNullException.ThrowIfNull(pathResolver);
        ArgumentNullException.ThrowIfNull(appDialogService);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(listPanelViewModel);
        ArgumentNullException.ThrowIfNull(editPanelViewModel);
        ArgumentNullException.ThrowIfNull(buttonPanelViewModel);
        ArgumentNullException.ThrowIfNull(logPanelViewModel);
        ArgumentNullException.ThrowIfNull(variablePanelViewModel);
        ArgumentNullException.ThrowIfNull(assistantPanelViewModel);
        ArgumentNullException.ThrowIfNull(favoritePanelViewModel);

        _notifier = notifier;
        _logWriter = logWriter;
        _commandEventBus = commandEventBus;
        _macroFactory = macroFactory;
        _macroFileSerializer = macroFileSerializer;
        _commandRegistry = commandRegistry;
        _pathResolver = pathResolver;
        _appDialogService = appDialogService;
        _timeProvider = timeProvider;
        _listPanel = listPanelViewModel;
        _editPanel = editPanelViewModel;
        _buttonPanel = buttonPanelViewModel;
        _logPanel = logPanelViewModel;
        _variablePanel = variablePanelViewModel;
        _assistantPanel = assistantPanelViewModel;
        _favoritePanel = favoritePanelViewModel;
        _assistantPanel.SetContextProvider(BuildAssistantContext);

        SubscribeToChildViewModelEvents();
        RegisterCommandEventHandlers();
    }

    [RelayCommand]
    private void ToggleFavoritePanel()
    {
        IsFavoritePanelOpen = !IsFavoritePanelOpen;
        PublishStatusMessage(IsFavoritePanelOpen ? "お気に入りパネルを表示しました。" : "お気に入りパネルを閉じました。");
    }

    [RelayCommand]
    private void ToggleLogPanel()
    {
        IsLogPanelOpen = !IsLogPanelOpen;
        if (IsLogPanelOpen)
        {
            IsVariablePanelOpen = false;
            IsAssistantPanelOpen = false;
        }

        PublishStatusMessage(IsLogPanelOpen ? "ログパネルを表示しました。" : "ログパネルを閉じました。");
    }

    [RelayCommand]
    private void ToggleVariablePanel()
    {
        IsVariablePanelOpen = !IsVariablePanelOpen;
        if (IsVariablePanelOpen)
        {
            IsLogPanelOpen = false;
            IsAssistantPanelOpen = false;
            _variablePanel.Refresh();
        }

        PublishStatusMessage(IsVariablePanelOpen ? "変数パネルを表示しました。" : "変数パネルを閉じました。");
    }

    [RelayCommand]
    private void ToggleAssistantPanel()
    {
        IsAssistantPanelOpen = true;
        PublishStatusMessage("AI相談を表示しました。");
    }

    public void SetAssistantWindowOpen(bool isOpen)
    {
        IsAssistantPanelOpen = isOpen;
        PublishStatusMessage(isOpen ? "AI相談を表示しました。" : "AI相談を閉じました。");
    }

    [RelayCommand]
    private void TogglePreflightPanel()
    {
        IsPreflightPanelOpen = !IsPreflightPanelOpen;
        PublishStatusMessage(IsPreflightPanelOpen ? "診断パネルを表示しました。" : "診断パネルを閉じました。");
    }

    [RelayCommand]
    private void ClosePreflightPanel()
    {
        if (!IsPreflightPanelOpen)
        {
            return;
        }

        IsPreflightPanelOpen = false;
        PublishStatusMessage("診断パネルを閉じました。");
    }

    public void SetCommandHistory(CommandHistoryManager commandHistory)
    {
        _commandHistory = commandHistory;
    }

    partial void OnFavoritePanelWidthChanged(double value)
    {
        var normalizedWidth = Math.Clamp(value, 240, 700);
        if (Math.Abs(normalizedWidth - value) > 0.1)
        {
            FavoritePanelWidth = normalizedWidth;
        }
    }

    public void SaveMacroFile(string filePath) => _listPanel.Save(filePath);

    public void LoadMacroFile(string filePath)
    {
        _listPanel.Load(filePath);
        _editPanel.SetListCount(_listPanel.GetCount());
    }

    public void SetRunningState(bool isRunning)
    {
        IsRunning = isRunning;
        _buttonPanel.SetRunningState(isRunning);
        _editPanel.SetRunningState(isRunning);
        _favoritePanel.SetRunningState(isRunning);
        _listPanel.SetRunningState(isRunning);
        _logPanel.SetRunningState(isRunning);
        _variablePanel.SetRunningState(isRunning);
        _assistantPanel.SetRunningState(isRunning);
    }

    public bool TryStartExecution(out string message)
    {
        if (IsRunning)
        {
            message = "マクロはすでに実行中です。";
            return false;
        }

        System.Windows.Application.Current.Dispatcher.Invoke(PrepareAllPanels);

        if (!ValidateBeforeRun())
        {
            message = "実行前チェックで要修正項目が見つかりました。";
            _logWriter.Write("WARN", "Macro", message);
            PublishStatusMessage(message);
            return false;
        }

        System.Windows.Application.Current.Dispatcher.Invoke(() => SetRunningState(true));
        message = "マクロを実行開始しました。";
        _logWriter.WriteStructured(
            "Macro",
            "ExecutionStart",
            new Dictionary<string, object?>
            {
                ["Message"] = message,
                ["CommandCount"] = _listPanel.CommandList.Items.Count
            });
        _logPanel.WriteLog(string.Empty, "システム", message);
        PublishStatusMessage(message);
        _ = Run();
        return true;
    }

    public bool TryStopExecution(out string message)
    {
        if (!IsRunning)
        {
            message = "実行中のマクロはありません。";
            return false;
        }

        _cts?.Cancel();
        System.Windows.Application.Current.Dispatcher.Invoke(() => SetRunningState(false));
        message = "実行を停止しました。";
        _logWriter.Write("INFO", "Macro", message);
        _logPanel.WriteLog(string.Empty, "システム", message);
        PublishStatusMessage(message);
        return true;
    }

    public void SetCommandLineErrorNotificationSuppressed(bool suppress, bool clearOnExecutionCompleted)
    {
        _suppressErrorNotificationsForCommandLine = suppress;
        _clearSuppressionOnExecutionCompleted = suppress && clearOnExecutionCompleted;
    }

    private bool ShouldNotifyErrorToUser() => !_suppressErrorNotificationsForCommandLine;

    public void Dispose()
    {
        if (_disposed) return;

        UnsubscribeFromChildViewModelEvents();
        UnsubscribeCommandEventHandlers();
        _commandEventSubscriptionCts?.Cancel();
        _commandEventSubscriptionCts?.Dispose();
        _commandEventSubscriptionCts = null;
        if (_commandEventSubscriptionTask is not null)
        {
            try
            {
                _commandEventSubscriptionTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            _commandEventSubscriptionTask = null;
        }
        _cts?.Cancel();
        _cts?.Dispose();
        if (_variablePanel is IDisposable disposableVariablePanel)
        {
            disposableVariablePanel.Dispose();
        }
        if (_assistantPanel is IDisposable disposableAssistantPanel)
        {
            disposableAssistantPanel.Dispose();
        }
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    private void PublishStatusMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        StatusMessageRequested?.Invoke(message);
    }

    private void RequestNewFileState()
    {
        NewFileStateRequested?.Invoke();
    }

    private void ClosePreflightPanelOnly()
    {
        if (!IsPreflightPanelOpen)
        {
            return;
        }

        IsPreflightPanelOpen = false;
    }

    private void ClosePreflightPanelForListInteraction()
    {
        ClosePreflightPanelOnly();
    }

    partial void OnIsLogPanelOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBottomPanelOpen));
    }

    partial void OnIsVariablePanelOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBottomPanelOpen));
    }

    partial void OnIsAssistantPanelOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBottomPanelOpen));
    }

    private AssistantContext BuildAssistantContext()
    {
        return new AssistantContext(
            BuildMacroSummary(),
            BuildSelectedCommandSummary(),
            string.Empty,
            BuildAvailableCommandSummary());
    }

    private string BuildMacroSummary()
    {
        var items = _listPanel.CommandList.Items;
        if (items.Count == 0)
        {
            return "コマンドはまだありません。";
        }

        const int maxItems = 80;
        var builder = new StringBuilder();
        builder.AppendLine($"コマンド数: {items.Count}");
        foreach (var item in items.Take(maxItems))
        {
            builder.AppendLine(FormatCommandItem(item));
        }

        if (items.Count > maxItems)
        {
            builder.AppendLine($"... 残り {items.Count - maxItems} 件は省略");
        }

        return builder.ToString();
    }

    private string BuildSelectedCommandSummary()
    {
        return _listPanel.SelectedItem is null
            ? "選択中のコマンドはありません。"
            : FormatCommandItem(_listPanel.SelectedItem, includeSettings: true);
    }

    private string BuildAvailableCommandSummary()
    {
        _commandRegistry.Initialize();
        var builder = new StringBuilder();
        foreach (var typeName in _commandRegistry.GetOrderedTypeNames()
                     .Where(typeName => !_commandRegistry.IsEndCommand(typeName))
                     .Take(120))
        {
            builder.Append(typeName)
                .Append(" = ")
                .Append(_commandRegistry.GetDisplayName(typeName))
                .Append(" / ")
                .AppendLine(_commandRegistry.GetCategoryName(typeName));
        }

        return builder.ToString();
    }

    private static string FormatCommandItem(ICommandListItem item, bool includeSettings = false)
    {
        var indent = new string(' ', Math.Max(0, item.NestLevel) * 2);
        var enabledText = item.IsEnable ? "有効" : "無効";
        var builder = new StringBuilder();
        builder.Append(indent)
            .Append(item.LineNumber)
            .Append(". ")
            .Append(CommandListItem.GetDisplayNameForType(item.ItemType))
            .Append(" [")
            .Append(enabledText)
            .Append(']');

        if (!string.IsNullOrWhiteSpace(item.Comment))
        {
            builder.Append(" コメント=").Append(SanitizeValue(item.Comment));
        }

        if (includeSettings && item is ICommandSettings settings)
        {
            var settingsText = FormatSettings(settings);
            if (!string.IsNullOrWhiteSpace(settingsText))
            {
                builder.Append(" 設定={").Append(settingsText).Append('}');
            }
        }

        return builder.ToString();
    }

    private static string FormatSettings(ICommandSettings settings)
    {
        var properties = settings.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static x => x.CanRead && x.GetIndexParameters().Length == 0)
            .Select(x => $"{x.Name}={SanitizeValue(x.GetValue(settings))}")
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Take(24);

        return string.Join(", ", properties);
    }

    private static string SanitizeValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string text when string.IsNullOrWhiteSpace(text) => string.Empty,
            string text => TrimText(text.ReplaceLineEndings(" "), 120),
            _ => TrimText(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty, 120)
        };
    }

    private static string TrimText(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..maxLength] + "...";
    }
}



