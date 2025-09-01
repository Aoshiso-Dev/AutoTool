using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Collections.ObjectModel;
using AutoTool.ViewModel;
using AutoTool.Model;
using static AutoTool.Model.FileManager;
using System.Windows.Threading;
using System.IO;

namespace AutoTool
{
    public static class TabIndexes
    {
        public const int Macro = 0;
        public const int Monitor = 1;
    }

    public partial class MainWindowViewModel : ObservableObject
    {
        // Undo/Redo管理
        [ObservableProperty]
        private CommandHistoryManager _commandHistory = new();

        // ウィンドウ設定
        private WindowSettings _windowSettings;

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
                var oldValue = _fileManagers[SelectedTabIndex].CurrentFilePath;
                _fileManagers[SelectedTabIndex].CurrentFilePath = value;
                OnPropertyChanged(nameof(CurrentFilePath));
                
                // ファイルパスが変更されたときに設定を更新
                if (!string.IsNullOrEmpty(value) && oldValue != value)
                {
                    _windowSettings.UpdateLastOpenedFile(value);
                    _windowSettings.Save();
                    System.Diagnostics.Debug.WriteLine($"最後に開いたファイルを設定に保存: {value}");
                }
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

        public MainWindowViewModel()
        {
            // ウィンドウ設定を読み込み
            _windowSettings = WindowSettings.Load();
            
            MacroPanelViewModel = new MacroPanelViewModel();

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

        /// <summary>
        /// ウィンドウ設定を取得
        /// </summary>
        public WindowSettings GetWindowSettings() => _windowSettings;

        /// <summary>
        /// デバッグ用：現在の設定状況を出力
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public void PrintDebugSettings()
        {
            System.Diagnostics.Debug.WriteLine("=== WindowSettings Debug Info ===");
            System.Diagnostics.Debug.WriteLine($"OpenLastFileOnStartup: {_windowSettings.OpenLastFileOnStartup}");
            System.Diagnostics.Debug.WriteLine($"LastOpenedFilePath: '{_windowSettings.LastOpenedFilePath}'");
            System.Diagnostics.Debug.WriteLine($"IsFileOperationEnable: {IsFileOperationEnable}");
            System.Diagnostics.Debug.WriteLine($"IsFileOpened: {IsFileOpened}");
            if (IsFileOperationEnable)
            {
                System.Diagnostics.Debug.WriteLine($"CurrentFilePath: '{CurrentFilePath}'");
                System.Diagnostics.Debug.WriteLine($"CurrentFileName: '{CurrentFileName}'");
            }
            System.Diagnostics.Debug.WriteLine("=== End Debug Info ===");
        }

        /// <summary>
        /// 起動時に前回のファイルを開く
        /// </summary>
        public void LoadLastOpenedFileOnStartup()
        {
            System.Diagnostics.Debug.WriteLine("=== LoadLastOpenedFileOnStartup 開始 ===");
            System.Diagnostics.Debug.WriteLine($"OpenLastFileOnStartup: {_windowSettings.OpenLastFileOnStartup}");
            System.Diagnostics.Debug.WriteLine($"LastOpenedFilePath: '{_windowSettings.LastOpenedFilePath}'");
            System.Diagnostics.Debug.WriteLine($"IsLastOpenedFileValid: {_windowSettings.IsLastOpenedFileValid()}");
            
            if (_windowSettings.OpenLastFileOnStartup && _windowSettings.IsLastOpenedFileValid())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"前回のファイルを開いています: {_windowSettings.LastOpenedFilePath}");
                    
                    // ファイルの存在を再確認
                    if (!File.Exists(_windowSettings.LastOpenedFilePath))
                    {
                        System.Diagnostics.Debug.WriteLine("ファイルが存在しません");
                        StatusMessage = $"前回のファイルが見つかりません: {Path.GetFileName(_windowSettings.LastOpenedFilePath)}";
                        _windowSettings.LastOpenedFilePath = string.Empty;
                        _windowSettings.Save();
                        return;
                    }
                    
                    OpenFile(_windowSettings.LastOpenedFilePath);
                    StatusMessage = $"前回のファイルを開きました: {CurrentFileName}";
                    
                    // 3秒後にステータスメッセージをクリア
                    var timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(3);
                    timer.Tick += (s, e) => 
                    {
                        StatusMessage = "準備完了";
                        timer.Stop();
                    };
                    timer.Start();
                    
                    System.Diagnostics.Debug.WriteLine("前回のファイルを正常に開きました");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"前回のファイルを開くのに失敗しました: {ex.Message}");
                    StatusMessage = $"前回のファイルを開くのに失敗しました: {Path.GetFileName(_windowSettings.LastOpenedFilePath)}";
                    
                    // 失敗した場合は設定をクリア
                    _windowSettings.LastOpenedFilePath = string.Empty;
                    _windowSettings.Save();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("前回のファイル復元は実行されませんでした");
                if (!_windowSettings.OpenLastFileOnStartup)
                {
                    System.Diagnostics.Debug.WriteLine("理由: OpenLastFileOnStartup が false");
                }
                if (string.IsNullOrEmpty(_windowSettings.LastOpenedFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("理由: LastOpenedFilePath が空");
                }
                if (!string.IsNullOrEmpty(_windowSettings.LastOpenedFilePath) && !File.Exists(_windowSettings.LastOpenedFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("理由: ファイルが存在しない");
                }
            }
            
            System.Diagnostics.Debug.WriteLine("=== LoadLastOpenedFileOnStartup 終了 ===");
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
                    LoadFile
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

            MessageBox.Show($"{appName}\nVer.{versionString}\n{githubUrl}", "バージョン情報", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("アプリケーションのディレクトリが見つかりませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanOpenFile))]
        private void OpenFile(string filePath)
        {
            try
            {
                _fileManagers[SelectedTabIndex].OpenFile(filePath);
                
                // ファイル読み込み後は履歴をクリア
                CommandHistory.Clear();
                StatusMessage = $"ファイルを開きました: {CurrentFileName}";
                
                // ファイルパスを明示的に更新して設定保存をトリガー
                UpdateProperties();
                
                // 確実にファイルパスを保存
                if (!string.IsNullOrEmpty(CurrentFilePath))
                {
                    _windowSettings.UpdateLastOpenedFile(CurrentFilePath);
                    _windowSettings.Save();
                    System.Diagnostics.Debug.WriteLine($"ファイル開いた後に設定保存: {CurrentFilePath}");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"ファイルの読み込みに失敗しました: {Path.GetFileName(filePath)}";
                System.Diagnostics.Debug.WriteLine($"ファイル読み込みエラー: {ex.Message}");
                MessageBox.Show($"ファイルの読み込みに失敗しました。\n{ex.Message}", "読み込みエラー", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanSaveFile))]
        private void SaveFile()
        {
            try
            {
                StatusMessage = "保存中...";
                _fileManagers[SelectedTabIndex].SaveFile();
                UpdateProperties();
                
                // 確実にファイルパスを保存
                if (!string.IsNullOrEmpty(CurrentFilePath))
                {
                    _windowSettings.UpdateLastOpenedFile(CurrentFilePath);
                    _windowSettings.Save();
                    System.Diagnostics.Debug.WriteLine($"ファイル保存後に設定保存: {CurrentFilePath}");
                }
                
                StatusMessage = $"保存完了: {CurrentFileName}";
                
                // 3秒後にステータスメッセージをクリア
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, e) => 
                {
                    StatusMessage = "準備完了";
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                StatusMessage = "保存に失敗しました";
                MessageBox.Show($"ファイルの保存に失敗しました。\n{ex.Message}", "保存エラー", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
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
                
                // 確実にファイルパスを保存
                if (!string.IsNullOrEmpty(CurrentFilePath))
                {
                    _windowSettings.UpdateLastOpenedFile(CurrentFilePath);
                    _windowSettings.Save();
                    System.Diagnostics.Debug.WriteLine($"名前を付けて保存後に設定保存: {CurrentFilePath}");
                }
                
                StatusMessage = $"保存完了: {CurrentFileName}";
                
                // 3秒後にステータスメッセージをクリア
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, e) => 
                {
                    StatusMessage = "準備完了";
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                StatusMessage = "保存に失敗しました";
                MessageBox.Show($"ファイルの保存に失敗しました。\n{ex.Message}", "保存エラー", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            CommandHistory.Undo();
            StatusMessage = $"元に戻しました: {CommandHistory.RedoDescription}";
            
            // 2秒後にステータスメッセージをクリア
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, e) => 
            {
                StatusMessage = "準備完了";
                timer.Stop();
            };
            timer.Start();
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            CommandHistory.Redo();
            StatusMessage = $"やり直しました: {CommandHistory.UndoDescription}";
            
            // 2秒後にステータスメッセージをクリア
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, e) => 
            {
                StatusMessage = "準備完了";
                timer.Stop();
            };
            timer.Start();
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
