using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AutoTool.Desktop.ViewModel;
using AutoTool.Application.Files;
using AutoTool.Application.History;
using static AutoTool.Application.Files.FileManager;
using AutoTool.Application.Ports;
using INotifier = AutoTool.Commands.Services.INotifier;
using System.ComponentModel;

namespace AutoTool.Desktop;

public static class TabIndexes
{
    public const int Macro = 0;
    public const int Monitor = 1;
}

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly INotifier _notifier;
    private readonly IStatusMessageScheduler _statusMessageScheduler;
    private readonly IFilePicker _filePicker;
    private readonly IRecentFileStore _recentFileStore;
    private readonly IFileSystemPathService _fileSystemPathService;
    private EventHandler? _commandHistoryChangedHandler;
    private bool _lastKnownIsRunning;
    private bool _isDirtyTrackingSuspended;
    private bool _disposed;

    [ObservableProperty]
    private CommandHistoryManager _commandHistory = new();

    public bool IsRunning => SelectedTabIndex switch
    {
        TabIndexes.Macro => MacroPanelViewModel.IsRunning,
        _ => false,
    };

    private readonly Dictionary<int, FileManager> _fileManagers = [];

    [ObservableProperty]
    private int _selectedTabIndex = TabIndexes.Macro;

    [ObservableProperty]
    private string _autoToolTitle = "AutoTool";

    [ObservableProperty]
    private bool _isFileOperationEnable;

    [ObservableProperty]
    private bool _isFileOpened;

    [ObservableProperty]
    private string _currentFileName = string.Empty;

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<RecentFileEntry>? _recentFiles;

    [ObservableProperty]
    private string _menuItemHeader_SaveFile = "保存";

    [ObservableProperty]
    private string _menuItemHeader_SaveFileAs = "名前を付けて保存";

    [ObservableProperty]
    private MacroPanelViewModel _macroPanelViewModel;

    [ObservableProperty]
    private string _statusMessage = "準備完了";

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    public MainWindowViewModel(
        INotifier notifier,
        IStatusMessageScheduler statusMessageScheduler,
        IFilePicker filePicker,
        IRecentFileStore recentFileStore,
        IFileSystemPathService fileSystemPathService,
        MacroPanelViewModel macroPanelViewModel)
    {
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(statusMessageScheduler);
        ArgumentNullException.ThrowIfNull(filePicker);
        ArgumentNullException.ThrowIfNull(recentFileStore);
        ArgumentNullException.ThrowIfNull(fileSystemPathService);
        ArgumentNullException.ThrowIfNull(macroPanelViewModel);

        _notifier = notifier;
        _statusMessageScheduler = statusMessageScheduler;
        _filePicker = filePicker;
        _recentFileStore = recentFileStore;
        _fileSystemPathService = fileSystemPathService;
        MacroPanelViewModel = macroPanelViewModel;

        InitializeFileManager();
        InitializeCommandHistory();
        _lastKnownIsRunning = IsRunning;
        MacroPanelViewModel.PropertyChanged += OnMacroPanelPropertyChanged;
        MacroPanelViewModel.StatusMessageRequested += OnMacroStatusMessageRequested;
        MacroPanelViewModel.NewFileStateRequested += OnMacroNewFileStateRequested;
    }

    private void OnMacroPanelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MacroPanelViewModel.IsRunning))
        {
            return;
        }

        OnPropertyChanged(nameof(IsRunning));
        if (_lastKnownIsRunning != IsRunning)
        {
            _lastKnownIsRunning = IsRunning;
            UpdateCommandStates();
        }
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        RefreshFileUiState();
    }

    private void InitializeCommandHistory()
    {
        MacroPanelViewModel.SetCommandHistory(CommandHistory);
        _commandHistoryChangedHandler = (_, _) =>
        {
            if (!_isDirtyTrackingSuspended)
            {
                HasUnsavedChanges = true;
            }

            UpdateCommandStates();
        };
        CommandHistory.HistoryChanged += _commandHistoryChangedHandler;
    }

    private void ClearDirtyState()
    {
        _isDirtyTrackingSuspended = true;
        try
        {
            CommandHistory.Clear();
            HasUnsavedChanges = false;
        }
        finally
        {
            _isDirtyTrackingSuspended = false;
        }
    }

    private void UpdateCommandStates()
    {
        OpenFileCommand.NotifyCanExecuteChanged();
        SaveFileCommand.NotifyCanExecuteChanged();
        SaveFileAsCommand.NotifyCanExecuteChanged();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void InitializeFileManager()
    {
        var macroFileManager = new FileManager(
            new FileTypeInfo
            {
                Filter = "AutoTool マクロファイル (*.macro)|*.macro",
                FilterIndex = 1,
                RestoreDirectory = true,
                DefaultExt = "macro",
                Title = "マクロファイルを開く",
            },
            filePath => MacroPanelViewModel.SaveMacroFile(filePath),
            filePath => MacroPanelViewModel.LoadMacroFile(filePath),
            _filePicker,
            _recentFileStore,
            _fileSystemPathService);

        macroFileManager.PropertyChanged += OnFileManagerPropertyChanged;
        _fileManagers.Add(TabIndexes.Macro, macroFileManager);
        RefreshFileUiState();
    }

    private void RefreshFileUiState()
    {
        IsFileOperationEnable = _fileManagers.ContainsKey(SelectedTabIndex);

        if (TryGetActiveFileManager(out var fileManager))
        {
            IsFileOpened = fileManager.IsFileOpened;
            CurrentFilePath = fileManager.CurrentFilePath;
            CurrentFileName = fileManager.CurrentFileName;
            RecentFiles = fileManager.RecentFiles;
        }
        else
        {
            IsFileOpened = false;
            CurrentFilePath = string.Empty;
            CurrentFileName = string.Empty;
            RecentFiles = null;
        }

        MenuItemHeader_SaveFile = IsFileOpened
            ? $"{CurrentFileName} を保存"
            : "保存";
        MenuItemHeader_SaveFileAs = IsFileOpened
            ? $"{CurrentFileName} に名前を付けて保存"
            : "名前を付けて保存";
        AutoToolTitle = IsFileOpened
            ? $"AutoTool - {CurrentFileName}"
            : "AutoTool";

        UpdateCommandStates();
    }

    private void OnFileManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not FileManager changedManager)
        {
            return;
        }

        if (!TryGetActiveFileManager(out var activeManager) || !ReferenceEquals(changedManager, activeManager))
        {
            return;
        }

        RefreshFileUiState();
    }

    [RelayCommand]
    private void VersionInfo()
    {
        var assembly = System.Reflection.Assembly.GetEntryAssembly() ?? System.Reflection.Assembly.GetExecutingAssembly();
        var appName = assembly.GetName().Name;
        var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
        var version = assembly.GetName().Version;
        const string githubUrl = "https://github.com/Aoshiso-Dev/AutoTool";
        var versionString = string.IsNullOrWhiteSpace(fileVersionInfo.FileVersion)
            ? $"{version?.Major}.{version?.Minor}.{version?.Build}"
            : fileVersionInfo.FileVersion;

        _notifier.ShowInfo($"{appName}\nVer.{versionString}\n{githubUrl}", "バージョン情報");
    }

    [RelayCommand]
    private void OpenAppDir()
    {
        var appDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        if (appDir is not null)
        {
            System.Diagnostics.Process.Start("EXPLORER.EXE", appDir);
        }
        else
        {
            _notifier.ShowError("アプリケーションのディレクトリを取得できませんでした。", "エラー");
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private void OpenFile(string filePath)
    {
        try
        {
            if (!TryGetActiveFileManager(out var fileManager) || !fileManager.OpenFile(filePath))
            {
                StatusMessage = "ファイルを開けませんでした。";
                RefreshFileUiState();
                return;
            }

            ClearDirtyState();
            RefreshFileUiState();
            StatusMessage = $"ファイルを開きました: {CurrentFileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = "ファイルを開けませんでした。";
            _notifier.ShowError($"ファイルを開く際にエラーが発生しました。\n{ex.Message}", "エラー");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveFile))]
    private void SaveFile()
    {
        try
        {
            if (!TryGetActiveFileManager(out var fileManager))
            {
                return;
            }

            if (!IsFileOpened || string.IsNullOrWhiteSpace(CurrentFilePath))
            {
                SaveFileAs();
                return;
            }

            StatusMessage = "保存中...";
            fileManager.SaveFile();
            HasUnsavedChanges = false;
            RefreshFileUiState();

            StatusMessage = $"保存しました: {CurrentFileName}";
            _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(3), () => StatusMessage = "準備完了");
        }
        catch (Exception ex)
        {
            StatusMessage = "保存に失敗しました。";
            _notifier.ShowError($"保存時にエラーが発生しました。\n{ex.Message}", "保存エラー");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveFileAs))]
    private void SaveFileAs()
    {
        try
        {
            if (!TryGetActiveFileManager(out var fileManager))
            {
                return;
            }

            StatusMessage = "名前を付けて保存中...";
            if (!fileManager.SaveFileAs())
            {
                StatusMessage = "保存がキャンセルされました。";
                _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(2), () => StatusMessage = "準備完了");
                return;
            }

            HasUnsavedChanges = false;
            RefreshFileUiState();

            StatusMessage = $"保存しました: {CurrentFileName}";
            _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(3), () => StatusMessage = "準備完了");
        }
        catch (Exception ex)
        {
            StatusMessage = "保存に失敗しました。";
            _notifier.ShowError($"保存時にエラーが発生しました。\n{ex.Message}", "保存エラー");
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        CommandHistory.Undo();
        StatusMessage = $"元に戻す: {CommandHistory.RedoDescription}";
        _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(2), () => StatusMessage = "準備完了");
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        CommandHistory.Redo();
        StatusMessage = $"やり直し: {CommandHistory.UndoDescription}";
        _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(2), () => StatusMessage = "準備完了");
    }

    private bool CanOpenFile() => IsFileOperationEnable && !IsRunning;
    private bool CanSaveFile() => IsFileOperationEnable && !IsRunning;
    private bool CanSaveFileAs() => IsFileOperationEnable && !IsRunning;
    private bool CanUndo() => !IsRunning && CommandHistory.CanUndo;
    private bool CanRedo() => !IsRunning && CommandHistory.CanRedo;

    private bool TryGetActiveFileManager(out FileManager fileManager)
    {
        return _fileManagers.TryGetValue(SelectedTabIndex, out fileManager!);
    }

    private void OnMacroStatusMessageRequested(string message)
    {
        StatusMessage = message;
        _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(2), () => StatusMessage = "準備完了");
    }

    private void OnMacroNewFileStateRequested()
    {
        if (!TryGetMacroFileManager(out var macroFileManager))
        {
            return;
        }

        macroFileManager.ResetToNewFile();
        ClearDirtyState();
        StatusMessage = "全削除したため、新規保存モードに切り替えました。";
        _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(2), () => StatusMessage = "準備完了");
        RefreshFileUiState();
    }

    public void RestoreSessionState(
        int selectedTabIndex,
        bool isFavoritePanelOpen,
        bool isLogPanelOpen,
        int selectedMacroListTabIndex,
        double favoritePanelWidth,
        string? lastOpenedMacroFilePath)
    {
        SelectedTabIndex = selectedTabIndex is TabIndexes.Macro or TabIndexes.Monitor
            ? selectedTabIndex
            : TabIndexes.Macro;

        MacroPanelViewModel.FavoritePanelWidth = Math.Clamp(favoritePanelWidth, 240, 700);
        MacroPanelViewModel.IsFavoritePanelOpen = isFavoritePanelOpen;
        MacroPanelViewModel.IsLogPanelOpen = isLogPanelOpen;
        MacroPanelViewModel.SelectedListTabIndex = Math.Max(0, selectedMacroListTabIndex);

        if (string.IsNullOrWhiteSpace(lastOpenedMacroFilePath))
        {
            RefreshFileUiState();
            return;
        }

        if (!TryGetMacroFileManager(out var macroFileManager) || !macroFileManager.OpenFile(lastOpenedMacroFilePath))
        {
            StatusMessage = "前回のマクロファイルを復元できませんでした。";
            _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(3), () => StatusMessage = "準備完了");
            RefreshFileUiState();
            return;
        }

        ClearDirtyState();
        RefreshFileUiState();
        StatusMessage = $"前回の状態を復元しました: {macroFileManager.CurrentFileName}";
        _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(3), () => StatusMessage = "準備完了");
    }

    public string? GetLastOpenedMacroFilePath()
    {
        return TryGetMacroFileManager(out var macroFileManager) && macroFileManager.IsFileOpened
            ? macroFileManager.CurrentFilePath
            : null;
    }

    private bool TryGetMacroFileManager(out FileManager fileManager)
    {
        return _fileManagers.TryGetValue(TabIndexes.Macro, out fileManager!);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_commandHistoryChangedHandler is not null)
        {
            CommandHistory.HistoryChanged -= _commandHistoryChangedHandler;
            _commandHistoryChangedHandler = null;
        }

        foreach (var fileManager in _fileManagers.Values)
        {
            fileManager.PropertyChanged -= OnFileManagerPropertyChanged;
        }

        MacroPanelViewModel.PropertyChanged -= OnMacroPanelPropertyChanged;
        MacroPanelViewModel.StatusMessageRequested -= OnMacroStatusMessageRequested;
        MacroPanelViewModel.NewFileStateRequested -= OnMacroNewFileStateRequested;
        MacroPanelViewModel.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
