using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AutoTool.ViewModel;
using AutoTool.Model;
using static AutoTool.Model.FileManager;
using AutoTool.Services.Interfaces;
using System.Windows.Threading;

namespace AutoTool
{
    public static class TabIndexes
    {
        public const int Macro = 0;
        public const int Monitor = 1;
    }

    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly INotificationService _notificationService;
        private readonly IStatusMessageScheduler _statusMessageScheduler;
        private readonly IFileDialogService _fileDialogService;
        private readonly IRecentFileStore _recentFileStore;
        private readonly ILogService _logService;

        // Undo/Redo管理
        [ObservableProperty]
        private CommandHistoryManager _commandHistory = new();

        public bool IsRunning
        {
            get
            {
                return SelectedTabIndex switch
                {
                    TabIndexes.Macro => MacroPanelViewModel.IsRunning,
                    _ => false,
                };
            }
        }
             
        private Dictionary<int, FileManager> _fileManagers = [];

        public string AutoToolTitle
        {
            get { return IsFileOperationEnable && _fileManagers[SelectedTabIndex].IsFileOpened ? $"AutoTool - {CurrentFileName}" : "AutoTool"; }
        }

        private int _selectedTabIndex = TabIndexes.Macro;
        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                SetProperty(ref _selectedTabIndex, value);
                UpdateProperties();
            }
        }

        public bool IsFileOperationEnable
        {
            get { return _fileManagers.ContainsKey(SelectedTabIndex); }
        }

        public bool IsFileOpened
        {
            get { return IsFileOperationEnable && _fileManagers[SelectedTabIndex].IsFileOpened; }
        }

        public string CurrentFileName
        {
            get { return _fileManagers[SelectedTabIndex].CurrentFileName; }
            set
            {
                _fileManagers[SelectedTabIndex].CurrentFileName = value;
                OnPropertyChanged(nameof(CurrentFileName));
            }
        }

        public string CurrentFilePath
        {
            get { return _fileManagers[SelectedTabIndex].CurrentFilePath; }
            set
            {
                _fileManagers[SelectedTabIndex].CurrentFilePath = value;
                OnPropertyChanged(nameof(CurrentFilePath));
            }
        }

        public ObservableCollection<RecentFile>? RecentFiles
        {
            get { return _fileManagers[SelectedTabIndex].RecentFiles; }
        }

        public string MenuItemHeader_SaveFile
        {
            get { return IsFileOperationEnable && _fileManagers[SelectedTabIndex].IsFileOpened ? $"{CurrentFileName} を保存" : "保存"; }
        }

        public string MenuItemHeader_SaveFileAs
        {
            get { return IsFileOperationEnable && _fileManagers[SelectedTabIndex].IsFileOpened ? $"{CurrentFileName} を名前を付けて保存" : "名前を付けて保存"; }
        }

        [ObservableProperty]
        private MacroPanelViewModel _macroPanelViewModel;

        [ObservableProperty]
        private string _statusMessage = "準備完了";

        public MainWindowViewModel(
            INotificationService notificationService,
            IStatusMessageScheduler statusMessageScheduler,
            IFileDialogService fileDialogService,
            IRecentFileStore recentFileStore,
            ILogService logService)
        {
            _notificationService = notificationService;
            _statusMessageScheduler = statusMessageScheduler;
            _fileDialogService = fileDialogService;
            _recentFileStore = recentFileStore;
            _logService = logService;

            MacroPanelViewModel = new MacroPanelViewModel(_notificationService, _logService);

            InitializeFileManager();
            InitializeCommandHistory();

            // IsEnabledを定期的に更新する
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += (sender, e) => 
            {
                var wasRunning = IsRunning;
                OnPropertyChanged(nameof(IsRunning));
                
                // 実行状態が変わった場合はコマンドの実行可能状態も更新
                if (wasRunning != IsRunning)
                {
                    UpdateCommandStates();
                }
            };
            timer.Start();
        }

        private void InitializeCommandHistory()
        {
            // MacroPanelViewModelにCommandHistoryを渡す
            MacroPanelViewModel.SetCommandHistory(CommandHistory);
            
            // 履歴変更時にコマンド状態を更新
            CommandHistory.HistoryChanged += (s, e) => UpdateCommandStates();
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
                    new FileManager.FileTypeInfo()
                    {
                        Filter = "AutoTool マクロファイル(*.macro)|*.macro",
                        FilterIndex = 1,
                        RestoreDirectory = true,
                        DefaultExt = "macro",
                        Title = "マクロファイルを開く",
                    },
                    SaveFile,
                    LoadFile,
                    _fileDialogService,
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
            string githubUrl = "https://github.com/Aoshiso-Dev/AutoTool";
            var versionString = $"{version.Major}.{version.Minor}.{version.Build}";

            _notificationService.ShowInfo($"{appName}\nVer.{versionString}\n{githubUrl}", "バージョン情報");
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
                _notificationService.ShowError("アプリケーションのディレクトリが見つかりませんでした。", "エラー");
            }
        }

        [RelayCommand(CanExecute = nameof(CanOpenFile))]
        private void OpenFile(string filePath)
        {
            _fileManagers[SelectedTabIndex].OpenFile(filePath);
            
            // ファイル読み込み後は履歴をクリア
            CommandHistory.Clear();
            StatusMessage = $"ファイルを開きました: {CurrentFileName}";
            
            UpdateProperties();
        }

        [RelayCommand(CanExecute = nameof(CanSaveFile))]
        private void SaveFile()
        {
            try
            {
                StatusMessage = "保存中...";
                _fileManagers[SelectedTabIndex].SaveFile();
                UpdateProperties();
                
                StatusMessage = $"保存完了: {CurrentFileName}";
                
                // 3秒後にステータスメッセージをクリア
                _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(3), () => StatusMessage = "準備完了");
            }
            catch (Exception ex)
            {
                StatusMessage = "保存に失敗しました";
                _notificationService.ShowError($"ファイルの保存に失敗しました。\n{ex.Message}", "保存エラー");
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
                
                // 3秒後にステータスメッセージをクリア
                _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(3), () => StatusMessage = "準備完了");
            }
            catch (Exception ex)
            {
                StatusMessage = "保存に失敗しました";
                _notificationService.ShowError($"ファイルの保存に失敗しました。\n{ex.Message}", "保存エラー");
            }
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            CommandHistory.Undo();
            StatusMessage = $"元に戻しました: {CommandHistory.RedoDescription}";
            
            // 2秒後にステータスメッセージをクリア
            _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(2), () => StatusMessage = "準備完了");
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            CommandHistory.Redo();
            StatusMessage = $"やり直しました: {CommandHistory.UndoDescription}";
            
            // 2秒後にステータスメッセージをクリア
            _statusMessageScheduler.Schedule(TimeSpan.FromSeconds(2), () => StatusMessage = "準備完了");
        }

        // CanExecute メソッドを追加
        private bool CanOpenFile()
        {
            return IsFileOperationEnable && !IsRunning;
        }

        private bool CanSaveFile()
        {
            return IsFileOpened && !IsRunning;
        }

        private bool CanSaveFileAs()
        {
            return IsFileOperationEnable && !IsRunning;
        }

        private bool CanUndo()
        {
            return !IsRunning && CommandHistory.CanUndo;
        }

        private bool CanRedo()
        {
            return !IsRunning && CommandHistory.CanRedo;
        }

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
    }
}
