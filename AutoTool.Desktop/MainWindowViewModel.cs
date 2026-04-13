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
    private readonly ILogWriter _logWriter;
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

    public string AutoToolTitle => IsFileOperationEnable && _fileManagers[SelectedTabIndex].IsFileOpened 
        ? $"AutoTool - {CurrentFileName}" 
        : "AutoTool";

    private int _selectedTabIndex = TabIndexes.Macro;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            SetProperty(ref _selectedTabIndex, value);
            UpdateProperties();
        }
    }

    public bool IsFileOperationEnable => _fileManagers.ContainsKey(SelectedTabIndex);

    public bool IsFileOpened => IsFileOperationEnable && _fileManagers[SelectedTabIndex].IsFileOpened;

    public string CurrentFileName
    {
        get => _fileManagers[SelectedTabIndex].CurrentFileName;
        set
        {
            _fileManagers[SelectedTabIndex].CurrentFileName = value;
            OnPropertyChanged();
        }
    }

    public string CurrentFilePath
    {
        get => _fileManagers[SelectedTabIndex].CurrentFilePath;
        set
        {
            _fileManagers[SelectedTabIndex].CurrentFilePath = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<RecentFile>? RecentFiles => _fileManagers[SelectedTabIndex].RecentFiles;

    public string MenuItemHeader_SaveFile => IsFileOperationEnable && _fileManagers[SelectedTabIndex].IsFileOpened 
        ? $"{CurrentFileName} を保存" 
        : "保存";

    public string MenuItemHeader_SaveFileAs => IsFileOperationEnable && _fileManagers[SelectedTabIndex].IsFileOpened 
        ? $"{CurrentFileName} を名前を付けて保存" 
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
        ILogWriter logWriter,
        MacroPanelViewModel macroPanelViewModel)
    {
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _statusMessageScheduler = statusMessageScheduler ?? throw new ArgumentNullException(nameof(statusMessageScheduler));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _recentFileStore = recentFileStore ?? throw new ArgumentNullException(nameof(recentFileStore));
        _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
        MacroPanelViewModel = macroPanelViewModel ?? throw new ArgumentNullException(nameof(macroPanelViewModel));

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

    private void InitializeCommandHistory()
    {
        MacroPanelViewModel.SetCommandHistory(CommandHistory);
        CommandHistory.HistoryChanged += (_, _) => UpdateCommandStates();
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
                    Filter = "AutoTool マクロファイル(*.macro)|*.macro",
                    FilterIndex = 1,
                    RestoreDirectory = true,
                    DefaultExt = "macro",
                    Title = "マクロファイルを開く",
                },
                SaveFile,
                LoadFile,
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
        if (appDir != null)
        {
            System.Diagnostics.Process.Start("EXPLORER.EXE", appDir);
        }
        else
        {
            _notifier.ShowError("アプリケーションのディレクトリが見つかりませんでした。", "エラー");
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private void OpenFile(string filePath)
    {
        _fileManagers[SelectedTabIndex].OpenFile(filePath);
        CommandHistory.Clear();
        StatusMessage = $"ファイルを開きました: {CurrentFileName}";
        UpdateProperties();
    }

    [RelayCommand(CanExecute = nameof(CanSaveFile))]
    private void SaveFile()
    {
        try
        {
            // 未保存（新規）状態では「名前を付けて保存」にフォールバック
            if (!IsFileOpened || string.IsNullOrWhiteSpace(CurrentFilePath))
            {
                SaveFileAs();
                return;
            }

            StatusMessage = "保存中...";
            _fileManagers[SelectedTabIndex].SaveFile();
            UpdateProperties();
            
            StatusMessage = $"保存完了: {CurrentFileName}";
            _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(3), () => StatusMessage = "準備完了");
        }
        catch (Exception ex)
        {
            StatusMessage = "保存に失敗しました";
            _notifier.ShowError($"ファイルの保存に失敗しました。\n{ex.Message}", "保存エラー");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveFileAs))]
    private void SaveFileAs()
    {
        try
        {
            StatusMessage = "名前を付けて保存中...";
            _fileManagers[SelectedTabIndex].SaveFileAs();
            UpdateProperties();
            
            StatusMessage = $"保存完了: {CurrentFileName}";
            _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(3), () => StatusMessage = "準備完了");
        }
        catch (Exception ex)
        {
            StatusMessage = "保存に失敗しました";
            _notifier.ShowError($"ファイルの保存に失敗しました。\n{ex.Message}", "保存エラー");
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        CommandHistory.Undo();
        StatusMessage = $"元に戻しました: {CommandHistory.RedoDescription}";
        _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(2), () => StatusMessage = "準備完了");
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        CommandHistory.Redo();
        StatusMessage = $"やり直しました: {CommandHistory.UndoDescription}";
        _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(2), () => StatusMessage = "準備完了");
    }

    private bool CanOpenFile() => IsFileOperationEnable && !IsRunning;
    private bool CanSaveFile() => IsFileOperationEnable && !IsRunning;
    private bool CanSaveFileAs() => IsFileOperationEnable && !IsRunning;
    private bool CanUndo() => !IsRunning && CommandHistory.CanUndo;
    private bool CanRedo() => !IsRunning && CommandHistory.CanRedo;

    private void SaveFile(string filePath)
    {
        if (SelectedTabIndex == TabIndexes.Macro)
        {
            MacroPanelViewModel.SaveMacroFile(filePath);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private void LoadFile(string filePath)
    {
        if (SelectedTabIndex == TabIndexes.Macro)
        {
            MacroPanelViewModel.LoadMacroFile(filePath);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        MacroPanelViewModel.PropertyChanged -= OnMacroPanelPropertyChanged;
        MacroPanelViewModel.Dispose();
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }
}


