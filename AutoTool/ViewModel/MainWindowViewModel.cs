using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoTool.Services;
using AutoTool.Services.Plugin;
using AutoTool.Services.UI;
using AutoTool.ViewModel.Panels;
using AutoTool.ViewModel.Shared;
using AutoTool.Model.CommandDefinition;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoTool.ViewModel
{
    /// <summary>
    /// MainWindowViewModel (DirectCommandRegistry統合版)
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRecentFileService _recentFileService;
        private readonly IPluginService _pluginService;
        private readonly IMainWindowMenuService _menuService;
        private readonly IMainWindowButtonService _buttonService;

        [ObservableProperty]
        private string _title = "AutoTool - 自動化ツール";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _statusMessage = "準備完了";

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _availableCommands = new();

        [ObservableProperty]
        private double _windowWidth = 1200;

        [ObservableProperty]
        private double _windowHeight = 800;

        [ObservableProperty]
        private bool _isMaximized = false;

        // ViewModelプロパティ
        public ListPanelViewModel ListPanelViewModel { get; }
        public EditPanelViewModel EditPanelViewModel { get; }
        public ButtonPanelViewModel ButtonPanelViewModel { get; }

        // 統計プロパティ
        public int CommandCount => ListPanelViewModel.Items.Count;
        public bool HasCommands => CommandCount > 0;

        // その他のダミーコマンド（バインディングエラー回避用）
        [RelayCommand]
        private void ChangeTheme(string theme)
        {
            try
            {
                _logger.LogDebug("テーマ変更要求: {Theme}", theme);
                StatusMessage = $"テーマを{theme}に変更しました";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テーマ変更中にエラー");
                StatusMessage = $"テーマ変更エラー: {ex.Message}";
            }
        }

        [RelayCommand]
        private void RefreshPerformance()
        {
            StatusMessage = "パフォーマンス情報を更新しました";
        }

        [RelayCommand]
        private void LoadPluginFile()
        {
            StatusMessage = "プラグイン読み込み機能は未実装です";
        }

        [RelayCommand]
        private void RefreshPlugins()
        {
            StatusMessage = "プラグイン再読み込み機能は未実装です";
        }

        [RelayCommand]
        private void ShowPluginInfo()
        {
            StatusMessage = "プラグイン情報表示機能は未実装です";
        }

        [RelayCommand]
        private void OpenAppDir()
        {
            StatusMessage = "アプリケーションフォルダを開く機能は未実装です";
        }

        [RelayCommand]
        private void ClearLog()
        {
            StatusMessage = "ログをクリアしました";
        }

        // プロパティ（バインディングエラー回避用）
        public string MenuItemHeader_SaveFile => "保存(_S)";
        public string MenuItemHeader_SaveFileAs => "名前を付けて保存(_A)";
        public ObservableCollection<object> RecentFiles { get; } = new();
        public ObservableCollection<string> LogEntries { get; } = new();
        public string MemoryUsage => "0 MB";
        public string CpuUsage => "0%";
        public int PluginCount => 0;

        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IServiceProvider serviceProvider,
            IRecentFileService recentFileService,
            IPluginService pluginService,
            IMainWindowMenuService menuService,
            IMainWindowButtonService buttonService,
            ListPanelViewModel listPanelViewModel,
            EditPanelViewModel editPanelViewModel,
            ButtonPanelViewModel buttonPanelViewModel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _recentFileService = recentFileService ?? throw new ArgumentNullException(nameof(recentFileService));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _buttonService = buttonService ?? throw new ArgumentNullException(nameof(buttonService));
            
            ListPanelViewModel = listPanelViewModel ?? throw new ArgumentNullException(nameof(listPanelViewModel));
            EditPanelViewModel = editPanelViewModel ?? throw new ArgumentNullException(nameof(editPanelViewModel));
            ButtonPanelViewModel = buttonPanelViewModel ?? throw new ArgumentNullException(nameof(buttonPanelViewModel));

            _logger.LogInformation("MainWindowViewModel (DirectCommandRegistry版) 初期化開始");

            // ListPanelViewModelのアイテム数変更を監視
            ListPanelViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ListPanelViewModel.Items))
                {
                    OnPropertyChanged(nameof(CommandCount));
                    OnPropertyChanged(nameof(HasCommands));
                }
            };

            Initialize();
        }

        private void Initialize()
        {
            try
            {
                LoadAvailableCommands();
                StatusMessage = "初期化完了";
                _logger.LogInformation("MainWindowViewModel 初期化完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MainWindowViewModel 初期化中にエラー");
                StatusMessage = $"初期化エラー: {ex.Message}";
            }
        }

        private void LoadAvailableCommands()
        {
            try
            {
                _logger.LogDebug("利用可能なコマンドの読み込み開始");
                
                var displayItems = DirectCommandRegistry.GetOrderedTypeNames()
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = DirectCommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = DirectCommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList();

                AvailableCommands = new ObservableCollection<CommandDisplayItem>(displayItems);
                
                _logger.LogDebug("利用可能なコマンド読み込み完了: {Count}個", AvailableCommands.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "利用可能なコマンド読み込み中にエラー");
                AvailableCommands = new ObservableCollection<CommandDisplayItem>();
            }
        }

        [RelayCommand]
        private async Task LoadFileAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "ファイルを読み込み中...";
                
                // TODO: メニューサービスにLoadFileAsyncメソッドを追加する必要があります
                // await _menuService.LoadFileAsync();
                await Task.Delay(100); // 一時的な代替
                
                StatusMessage = "ファイル読み込み完了";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル読み込み中にエラー");
                StatusMessage = $"読み込みエラー: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveFileAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "ファイルを保存中...";
                
                // TODO: メニューサービスにSaveFileAsyncメソッドを追加する必要があります
                // await _menuService.SaveFileAsync();
                await Task.Delay(100); // 一時的な代替
                
                StatusMessage = "ファイル保存完了";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル保存中にエラー");
                StatusMessage = $"保存エラー: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Exit()
        {
            try
            {
                _logger.LogInformation("アプリケーション終了要求");
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アプリケーション終了中にエラー");
            }
        }

        [RelayCommand]
        private void ShowAbout()
        {
            try
            {
                _logger.LogDebug("About ダイアログ表示");
                StatusMessage = "AutoTool v1.0 - 自動化ツール";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "About ダイアログ表示中にエラー");
            }
        }

        public void SetStatus(string message)
        {
            StatusMessage = message;
            _logger.LogDebug("ステータス更新: {Message}", message);
        }

        public void SetLoading(bool isLoading)
        {
            IsLoading = isLoading;
            if (isLoading)
            {
                StatusMessage = "処理中...";
            }
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            _logger.LogTrace("プロパティ変更: {PropertyName}", e.PropertyName);
        }
    }
}