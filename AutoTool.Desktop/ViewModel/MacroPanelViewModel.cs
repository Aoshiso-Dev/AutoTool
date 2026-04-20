using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoTool.Commands.Services;
using AutoTool.Desktop.Panels.ViewModel;
using AutoTool.Automation.Runtime.MacroFactory;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Application.Files;
using AutoTool.Application.History;
using AutoTool.Application.Ports;
using AutoTool.Automation.Runtime.Serialization;
using System.Collections.ObjectModel;

namespace AutoTool.Desktop.ViewModel;

public partial class MacroPanelViewModel : ObservableObject, IDisposable
{
    private readonly INotifier _notifier;
    private readonly ILogWriter _logWriter;
    private readonly ICommandEventBus _commandEventBus;
    private readonly IMacroFactory _macroFactory;
    private readonly IMacroFileSerializer _macroFileSerializer;
    private readonly ICommandRegistry _commandRegistry;
    private readonly IPathResolver _pathResolver;
    private readonly TimeProvider _timeProvider;
    private readonly IListPanelViewModel _listPanel;
    private readonly IEditPanelViewModel _editPanel;
    private readonly IButtonPanelViewModel _buttonPanel;
    private readonly ILogPanelViewModel _logPanel;
    private readonly IFavoritePanelViewModel _favoritePanel;
    private CancellationTokenSource? _commandEventSubscriptionCts;
    private Task? _commandEventSubscriptionTask;
    private CancellationTokenSource? _cts;
    private CommandHistoryManager? _commandHistory;
    private long _lastObservedDroppedCommandEvents;
    private bool _disposed;
    private bool _isEditDialogOpen;
    public event Action<string>? StatusMessageRequested;
    public event Action? NewFileStateRequested;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _selectedListTabIndex;

    [ObservableProperty]
    private bool _isFavoritePanelOpen;

    [ObservableProperty]
    private bool _isLogPanelOpen;

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
    public IFavoritePanelViewModel FavoritePanelViewModel => _favoritePanel;

    public MacroPanelViewModel(
        INotifier notifier,
        ILogWriter logWriter,
        ICommandEventBus commandEventBus,
        IMacroFactory macroFactory,
        IMacroFileSerializer macroFileSerializer,
        ICommandRegistry commandRegistry,
        IPathResolver pathResolver,
        TimeProvider timeProvider,
        IListPanelViewModel listPanelViewModel,
        IEditPanelViewModel editPanelViewModel,
        IButtonPanelViewModel buttonPanelViewModel,
        ILogPanelViewModel logPanelViewModel,
        IFavoritePanelViewModel favoritePanelViewModel)
    {
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(logWriter);
        ArgumentNullException.ThrowIfNull(commandEventBus);
        ArgumentNullException.ThrowIfNull(macroFactory);
        ArgumentNullException.ThrowIfNull(macroFileSerializer);
        ArgumentNullException.ThrowIfNull(commandRegistry);
        ArgumentNullException.ThrowIfNull(pathResolver);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(listPanelViewModel);
        ArgumentNullException.ThrowIfNull(editPanelViewModel);
        ArgumentNullException.ThrowIfNull(buttonPanelViewModel);
        ArgumentNullException.ThrowIfNull(logPanelViewModel);
        ArgumentNullException.ThrowIfNull(favoritePanelViewModel);

        _notifier = notifier;
        _logWriter = logWriter;
        _commandEventBus = commandEventBus;
        _macroFactory = macroFactory;
        _macroFileSerializer = macroFileSerializer;
        _commandRegistry = commandRegistry;
        _pathResolver = pathResolver;
        _timeProvider = timeProvider;
        _listPanel = listPanelViewModel;
        _editPanel = editPanelViewModel;
        _buttonPanel = buttonPanelViewModel;
        _logPanel = logPanelViewModel;
        _favoritePanel = favoritePanelViewModel;

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
        PublishStatusMessage(IsLogPanelOpen ? "ログパネルを表示しました。" : "ログパネルを閉じました。");
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
    }

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
}
