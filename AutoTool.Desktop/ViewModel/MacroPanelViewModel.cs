using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoTool.Commands.Services;
using AutoTool.Panels.ViewModel;
using AutoTool.Panels.Model.MacroFactory;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Model;
using AutoTool.Core.Ports;
using AutoTool.Panels.Serialization;

namespace AutoTool.ViewModel;

public partial class MacroPanelViewModel : ObservableObject, IDisposable
{
    private readonly INotifier _notifier;
    private readonly ILogWriter _logWriter;
    private readonly ICommandEventBus _commandEventBus;
    private readonly IMacroFactory _macroFactory;
    private readonly IMacroFileSerializer _macroFileSerializer;
    private readonly ICommandRegistry _commandRegistry;
    private readonly IListPanelViewModel _listPanel;
    private readonly IEditPanelViewModel _editPanel;
    private readonly IButtonPanelViewModel _buttonPanel;
    private readonly ILogPanelViewModel _logPanel;
    private readonly IFavoritePanelViewModel _favoritePanel;
    private CancellationTokenSource? _cts;
    private CommandHistoryManager? _commandHistory;
    private bool _disposed;
    private bool _isEditDialogOpen;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _selectedListTabIndex;

    [ObservableProperty]
    private bool _isFavoritePanelOpen;

    [ObservableProperty]
    private bool _isLogPanelOpen;

    // Exposed for view binding without concrete casts.
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
        IListPanelViewModel listPanelViewModel,
        IEditPanelViewModel editPanelViewModel,
        IButtonPanelViewModel buttonPanelViewModel,
        ILogPanelViewModel logPanelViewModel,
        IFavoritePanelViewModel favoritePanelViewModel)
    {
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
        _commandEventBus = commandEventBus ?? throw new ArgumentNullException(nameof(commandEventBus));
        _macroFactory = macroFactory ?? throw new ArgumentNullException(nameof(macroFactory));
        _macroFileSerializer = macroFileSerializer ?? throw new ArgumentNullException(nameof(macroFileSerializer));
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _listPanel = listPanelViewModel ?? throw new ArgumentNullException(nameof(listPanelViewModel));
        _editPanel = editPanelViewModel ?? throw new ArgumentNullException(nameof(editPanelViewModel));
        _buttonPanel = buttonPanelViewModel ?? throw new ArgumentNullException(nameof(buttonPanelViewModel));
        _logPanel = logPanelViewModel ?? throw new ArgumentNullException(nameof(logPanelViewModel));
        _favoritePanel = favoritePanelViewModel ?? throw new ArgumentNullException(nameof(favoritePanelViewModel));

        SubscribeToChildViewModelEvents();
        RegisterCommandEventHandlers();
    }

    [RelayCommand]
    private void ToggleFavoritePanel()
    {
        IsFavoritePanelOpen = !IsFavoritePanelOpen;
    }

    [RelayCommand]
    private void ToggleLogPanel()
    {
        IsLogPanelOpen = !IsLogPanelOpen;
    }

    public void SetCommandHistory(CommandHistoryManager commandHistory)
    {
        _commandHistory = commandHistory;
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
        _cts?.Cancel();
        _cts?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
