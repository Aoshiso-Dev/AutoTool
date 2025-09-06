using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using AutoTool.Services.UI;
using AutoTool.Services.Configuration;
using AutoTool.Message;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// メインウィンドウのメニュー機能を管理するサービス
    /// </summary>
    public interface IMainWindowMenuService
    {
        // ファイル操作
        IRelayCommand OpenFileCommand { get; }
        IRelayCommand SaveFileCommand { get; }
        IRelayCommand SaveFileAsCommand { get; }
        IRelayCommand ExitCommand { get; }
        IRelayCommand OpenRecentFileCommand { get; }
        
        // プラグイン操作
        IRelayCommand LoadPluginFileCommand { get; }
        IRelayCommand RefreshPluginsCommand { get; }
        IRelayCommand ShowPluginInfoCommand { get; }
        
        // ツール操作
        IRelayCommand OpenAppDirCommand { get; }
        IRelayCommand RefreshPerformanceCommand { get; }
        
        // ヘルプ操作
        IRelayCommand ShowAboutCommand { get; }
        
        // ログ操作
        IRelayCommand ClearLogCommand { get; }
        
        // プロパティ
        string CurrentFilePath { get; set; }
        ObservableCollection<RecentFileItem> RecentFiles { get; }
        
        // イベント
        event EventHandler<string> FileOpened;
        event EventHandler<string> FileSaved;
    }

    /// <summary>
    /// 最近開いたファイルのアイテム
    /// </summary>
    public partial class RecentFileItem : ObservableObject
    {
        [ObservableProperty]
        private string _fileName = string.Empty;
        
        [ObservableProperty]
        private string _filePath = string.Empty;
        
        [ObservableProperty]
        private DateTime _lastAccessed = DateTime.Now;
    }

    /// <summary>
    /// メインウィンドウのメニュー機能サービス実装
    /// </summary>
    public partial class MainWindowMenuService : ObservableObject, IMainWindowMenuService
    {
        private readonly ILogger<MainWindowMenuService> _logger;
        private readonly IMessenger _messenger;
        private readonly IEnhancedConfigurationService _configService;
        
        [ObservableProperty]
        private string _currentFilePath = string.Empty;

        public ObservableCollection<RecentFileItem> RecentFiles { get; } = new();

        // Command properties
        public IRelayCommand OpenFileCommand { get; }
        public IRelayCommand SaveFileCommand { get; }
        public IRelayCommand SaveFileAsCommand { get; }
        public IRelayCommand ExitCommand { get; }
        public IRelayCommand OpenRecentFileCommand { get; }
        public IRelayCommand LoadPluginFileCommand { get; }
        public IRelayCommand RefreshPluginsCommand { get; }
        public IRelayCommand ShowPluginInfoCommand { get; }
        public IRelayCommand OpenAppDirCommand { get; }
        public IRelayCommand RefreshPerformanceCommand { get; }
        public IRelayCommand ShowAboutCommand { get; }
        public IRelayCommand ClearLogCommand { get; }

        public event EventHandler<string>? FileOpened;
        public event EventHandler<string>? FileSaved;

        public MainWindowMenuService(
            ILogger<MainWindowMenuService> logger,
            IMessenger messenger,
            IEnhancedConfigurationService configService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));

            // Initialize commands
            OpenFileCommand = new RelayCommand(ExecuteOpenFile);
            SaveFileCommand = new RelayCommand(ExecuteSaveFile);
            SaveFileAsCommand = new RelayCommand(ExecuteSaveFileAs);
            ExitCommand = new RelayCommand(ExecuteExit);
            OpenRecentFileCommand = new RelayCommand<string>(ExecuteOpenRecentFile);
            LoadPluginFileCommand = new RelayCommand(ExecuteLoadPluginFile);
            RefreshPluginsCommand = new RelayCommand(ExecuteRefreshPlugins);
            ShowPluginInfoCommand = new RelayCommand(ExecuteShowPluginInfo);
            OpenAppDirCommand = new RelayCommand(ExecuteOpenAppDir);
            RefreshPerformanceCommand = new RelayCommand(ExecuteRefreshPerformance);
            ShowAboutCommand = new RelayCommand(ExecuteShowAbout);
            ClearLogCommand = new RelayCommand(ExecuteClearLog);

            LoadRecentFiles();
        }

        private void ExecuteOpenFile()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "マクロファイルを開く",
                    Filter = "AutoTool マクロファイル (*.atm)|*.atm|JSONファイル (*.json)|*.json|すべてのファイル (*.*)|*.*",
                    DefaultExt = ".atm",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var filePath = openFileDialog.FileName;
                    _messenger.Send(new LoadMessage(filePath));
                    
                    CurrentFilePath = filePath;
                    AddToRecentFiles(filePath);
                    FileOpened?.Invoke(this, filePath);
                    
                    _logger.LogInformation("ファイルを開きました: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイルオープン中にエラーが発生");
                System.Windows.MessageBox.Show($"ファイルを開く際にエラーが発生しました。\n\n{ex.Message}",
                    "エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteSaveFile()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentFilePath))
                {
                    ExecuteSaveFileAs();
                    return;
                }

                _messenger.Send(new SaveMessage(CurrentFilePath));
                FileSaved?.Invoke(this, CurrentFilePath);
                
                _logger.LogInformation("ファイルを保存しました: {FilePath}", CurrentFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル保存中にエラーが発生");
                System.Windows.MessageBox.Show($"ファイルを保存する際にエラーが発生しました。\n\n{ex.Message}",
                    "エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteSaveFileAs()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "マクロファイルを名前を付けて保存",
                    Filter = "AutoTool マクロファイル (*.atm)|*.atm|JSONファイル (*.json)|*.json|すべてのファイル (*.*)|*.*",
                    DefaultExt = ".atm",
                    FileName = Path.GetFileNameWithoutExtension(CurrentFilePath) + ".atm"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var filePath = saveFileDialog.FileName;
                    _messenger.Send(new SaveMessage(filePath));
                    
                    CurrentFilePath = filePath;
                    AddToRecentFiles(filePath);
                    FileSaved?.Invoke(this, filePath);
                    
                    _logger.LogInformation("ファイルを名前を付けて保存しました: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "名前を付けて保存中にエラーが発生");
                System.Windows.MessageBox.Show($"ファイルを保存する際にエラーが発生しました。\n\n{ex.Message}",
                    "エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteExit()
        {
            try
            {
                _logger.LogInformation("アプリケーション終了要求");
                System.Windows.Application.Current?.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アプリケーション終了中にエラーが発生");
            }
        }

        private void ExecuteOpenRecentFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                if (File.Exists(filePath))
                {
                    _messenger.Send(new LoadMessage(filePath));
                    CurrentFilePath = filePath;
                    UpdateRecentFileAccess(filePath);
                    FileOpened?.Invoke(this, filePath);
                    
                    _logger.LogInformation("最近のファイルを開きました: {FilePath}", filePath);
                }
                else
                {
                    System.Windows.MessageBox.Show($"ファイルが見つかりません。\n{filePath}",
                        "ファイルが見つかりません", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    RemoveFromRecentFiles(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近のファイルオープン中にエラーが発生: {FilePath}", filePath);
                System.Windows.MessageBox.Show($"ファイルを開く際にエラーが発生しました。\n\n{ex.Message}",
                    "エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteLoadPluginFile()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "プラグインファイルを選択",
                    Filter = "プラグインファイル (*.dll)|*.dll|すべてのファイル (*.*)|*.*",
                    DefaultExt = ".dll",
                    Multiselect = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    foreach (var filePath in openFileDialog.FileNames)
                    {
                        // プラグイン読み込み処理をメッセージで通知
                        _messenger.Send(new LoadPluginMessage(filePath));
                    }
                    
                    _logger.LogInformation("プラグインファイルの読み込み要求: {Count}個", openFileDialog.FileNames.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プラグインファイル読み込み中にエラーが発生");
                System.Windows.MessageBox.Show($"プラグインを読み込む際にエラーが発生しました。\n\n{ex.Message}",
                    "エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteRefreshPlugins()
        {
            try
            {
                _messenger.Send(new RefreshPluginsMessage());
                _logger.LogInformation("プラグイン再読み込み要求");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プラグイン再読み込み中にエラーが発生");
            }
        }

        private void ExecuteShowPluginInfo()
        {
            try
            {
                _messenger.Send(new ShowPluginInfoMessage());
                _logger.LogInformation("プラグイン情報表示要求");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プラグイン情報表示中にエラーが発生");
            }
        }

        private void ExecuteOpenAppDir()
        {
            try
            {
                var appDir = AppContext.BaseDirectory;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = appDir,
                    UseShellExecute = true
                });
                
                _logger.LogInformation("アプリケーションフォルダを開きました: {AppDir}", appDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アプリケーションフォルダオープン中にエラーが発生");
                System.Windows.MessageBox.Show($"フォルダを開く際にエラーが発生しました。\n\n{ex.Message}",
                    "エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteRefreshPerformance()
        {
            try
            {
                _messenger.Send(new RefreshPerformanceMessage());
                _logger.LogInformation("パフォーマンス情報更新要求");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "パフォーマンス情報更新中にエラーが発生");
            }
        }

        private void ExecuteShowAbout()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "不明";
                var message = $"AutoTool - 統合マクロ自動化ツール\n\nバージョン: {version}\n\n" +
                             "? 2024 AutoTool Development Team\n\n" +
                             "このソフトウェアは、繰り返し作業の自動化を支援するツールです。";
                
                System.Windows.MessageBox.Show(message, "AutoTool について", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                _logger.LogInformation("バージョン情報を表示しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "バージョン情報表示中にエラーが発生");
            }
        }

        private void ExecuteClearLog()
        {
            try
            {
                _messenger.Send(new ClearLogMessage());
                _logger.LogInformation("ログクリア要求");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログクリア中にエラーが発生");
            }
        }

        private void AddToRecentFiles(string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var existingItem = RecentFiles.FirstOrDefault(f => f.FilePath == filePath);
                
                if (existingItem != null)
                {
                    existingItem.LastAccessed = DateTime.Now;
                    // 既存アイテムを先頭に移動
                    RecentFiles.Remove(existingItem);
                    RecentFiles.Insert(0, existingItem);
                }
                else
                {
                    // 新しいアイテムを先頭に追加
                    RecentFiles.Insert(0, new RecentFileItem
                    {
                        FileName = fileName,
                        FilePath = filePath,
                        LastAccessed = DateTime.Now
                    });
                }

                // 最大10件まで保持
                while (RecentFiles.Count > 10)
                {
                    RecentFiles.RemoveAt(RecentFiles.Count - 1);
                }

                SaveRecentFiles();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近のファイルリスト更新中にエラーが発生");
            }
        }

        private void UpdateRecentFileAccess(string filePath)
        {
            var item = RecentFiles.FirstOrDefault(f => f.FilePath == filePath);
            if (item != null)
            {
                item.LastAccessed = DateTime.Now;
                RecentFiles.Remove(item);
                RecentFiles.Insert(0, item);
                SaveRecentFiles();
            }
        }

        private void RemoveFromRecentFiles(string filePath)
        {
            var item = RecentFiles.FirstOrDefault(f => f.FilePath == filePath);
            if (item != null)
            {
                RecentFiles.Remove(item);
                SaveRecentFiles();
            }
        }

        private void LoadRecentFiles()
        {
            try
            {
                var recentFilesData = _configService.GetValue("UI:RecentFiles", new List<object>());
                // 最近のファイルの復元処理を実装
                _logger.LogDebug("最近のファイルを読み込みました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近のファイル読み込み中にエラーが発生");
            }
        }

        private void SaveRecentFiles()
        {
            try
            {
                var recentFilesData = RecentFiles.Take(10).Select(f => new
                {
                    FileName = f.FileName,
                    FilePath = f.FilePath,
                    LastAccessed = f.LastAccessed
                }).ToList();
                
                _configService.SetValue("UI:RecentFiles", recentFilesData);
                _configService.Save();
                
                _logger.LogDebug("最近のファイルを保存しました: {Count}件", recentFilesData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "最近のファイル保存中にエラーが発生");
            }
        }
    }
}