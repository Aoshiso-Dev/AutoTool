using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AutoTool.ViewModel;
using AutoTool.Model;
using static AutoTool.Model.FileManager;
using AutoTool.Core.Ports;
using INotifier = AutoTool.Commands.Services.INotifier;
using System.ComponentModel;

namespace AutoTool;

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
    private EventHandler? _commandHistoryChangedHandler;
    private bool _lastKnownIsRunning;
    private bool _disposed;

    [ObservableProperty]
    private CommandHistoryManager _commandHistory = new();

    public bool IsRunning => SelectedTabIndex switch
    {
        TabIndexes.Macro => MacroPanelViewModel.IsRunning,
        _ => false,
    };

    private readonly Dictionary<int, FileManager> _fileManagers = [];

    public string AutoToolTitle => TryGetActiveFileManager(out var fileManager) && fileManager.IsFileOpened
        ? $"AutoTool - {CurrentFileName}"
        : "AutoTool";

    [ObservableProperty]
    private int _selectedTabIndex = TabIndexes.Macro;

    public bool IsFileOperationEnable => _fileManagers.ContainsKey(SelectedTabIndex);

    public bool IsFileOpened => TryGetActiveFileManager(out var fileManager) && fileManager.IsFileOpened;

    public string CurrentFileName
    {
        get => TryGetActiveFileManager(out var fileManager) ? fileManager.CurrentFileName : string.Empty;
        set
        {
            if (TryGetActiveFileManager(out var fileManager))
            {
                fileManager.CurrentFileName = value;
                OnPropertyChanged();
            }
        }
    }

    public string CurrentFilePath
    {
        get => TryGetActiveFileManager(out var fileManager) ? fileManager.CurrentFilePath : string.Empty;
        set
        {
            if (TryGetActiveFileManager(out var fileManager))
            {
                fileManager.CurrentFilePath = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<RecentFile>? RecentFiles =>
        TryGetActiveFileManager(out var fileManager) ? fileManager.RecentFiles : null;

    public string MenuItemHeader_SaveFile => TryGetActiveFileManager(out var fileManager) && fileManager.IsFileOpened
        ? $"{CurrentFileName} を保存"
        : "保存";

    public string MenuItemHeader_SaveFileAs => TryGetActiveFileManager(out var fileManager) && fileManager.IsFileOpened
        ? $"{CurrentFileName} に名前を付けて保存"
        : "名前を付けて保存";

    [ObservableProperty]
    private MacroPanelViewModel _macroPanelViewModel;

    [ObservableProperty]
    private string _statusMessage = "準備完了";

    public MainWindowViewModel(
        INotifier notifier,
        IStatusMessageScheduler statusMessageScheduler,
        IFilePicker filePicker,
        IRecentFileStore recentFileStore,
        MacroPanelViewModel macroPanelViewModel)
    {
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(statusMessageScheduler);
        ArgumentNullException.ThrowIfNull(filePicker);
        ArgumentNullException.ThrowIfNull(recentFileStore);
        ArgumentNullException.ThrowIfNull(macroPanelViewModel);

        _notifier = notifier;
        _statusMessageScheduler = statusMessageScheduler;
        _filePicker = filePicker;
        _recentFileStore = recentFileStore;
        MacroPanelViewModel = macroPanelViewModel;

        InitializeFileManager();
        InitializeCommandHistory();
        _lastKnownIsRunning = IsRunning;
        MacroPanelViewModel.PropertyChanged += OnMacroPanelPropertyChanged;
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
        UpdateProperties();
    }

    private void InitializeCommandHistory()
    {
        MacroPanelViewModel.SetCommandHistory(CommandHistory);
        _commandHistoryChangedHandler = (_, _) => UpdateCommandStates();
        CommandHistory.HistoryChanged += _commandHistoryChangedHandler;
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
        _fileManagers.Add(
            TabIndexes.Macro,
            new FileManager(
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
                _recentFileStore
            )
        );
    }

    private void UpdateProperties()
    {
        OnPropertyChanged(nameof(IsFileOperationEnable));
        OnPropertyChanged(nameof(IsFileOpened));
        OnPropertyChanged(nameof(CurrentFilePath));
        OnPropertyChanged(nameof(CurrentFileName));
        OnPropertyChanged(nameof(RecentFiles));
        OnPropertyChanged(nameof(MenuItemHeader_SaveFile));
        OnPropertyChanged(nameof(MenuItemHeader_SaveFileAs));
        OnPropertyChanged(nameof(AutoToolTitle));

        UpdateCommandStates();
    }

    [RelayCommand]
    private void VersionInfo()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        const string githubUrl = "https://github.com/Aoshiso-Dev/AutoTool";
        var versionString = $"{version?.Major}.{version?.Minor}.{version?.Build}";

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
                UpdateProperties();
                return;
            }

            CommandHistory.Clear();
            StatusMessage = $"ファイルを開きました: {CurrentFileName}";
            UpdateProperties();
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
            UpdateProperties();

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

            UpdateProperties();

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

    public void Dispose()
    {
        if (_disposed) return;

        if (_commandHistoryChangedHandler is not null)
        {
            CommandHistory.HistoryChanged -= _commandHistoryChangedHandler;
            _commandHistoryChangedHandler = null;
        }

        MacroPanelViewModel.PropertyChanged -= OnMacroPanelPropertyChanged;
        MacroPanelViewModel.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
