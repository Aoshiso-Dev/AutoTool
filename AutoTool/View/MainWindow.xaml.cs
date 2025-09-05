using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;
using AutoTool.Services.UI;

namespace AutoTool
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック(DI + Messaging対応)
    /// </summary>
    public partial class MainWindow : Window
    {
        private ILogger<MainWindow>? _logger;
        private IServiceProvider? _serviceProvider;
        private IDataContextLocator? _dataContextLocator;
        private IMainWindowButtonService? _buttonService;

        /// <summary>
        /// MainWindowのコンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // Loaded event を runtime のみ処理
            Loaded += MainWindow_Loaded;
            
            // キーボードイベントを処理
            KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeDI();
                SetupViewModels();
                TestVariableStore();
                
                _logger?.LogInformation("MainWindow DI初期化完了 - All services resolved successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MainWindow DI初期化にエラー発生");
                
                System.Windows.MessageBox.Show(
                    $"MainWindow初期化にエラーが発生しました。\n\nエラー詳細:\n{ex.Message}",
                    "警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        /// <summary>
        /// DI初期化
        /// </summary>
        private void InitializeDI()
        {
            if (System.Windows.Application.Current is App app && app.Services != null)
            {
                _serviceProvider = app.Services;
                _logger = _serviceProvider.GetService<ILogger<MainWindow>>();
                _dataContextLocator = _serviceProvider.GetService<IDataContextLocator>();
                _buttonService = _serviceProvider.GetService<IMainWindowButtonService>();

                _logger?.LogDebug("MainWindow DI初期化完了");
            }
            else
            {
                throw new InvalidOperationException("DIコンテナが利用できません");
            }
        }

        /// <summary>
        /// ViewModel設定
        /// </summary>
        private void SetupViewModels()
        {
            if (_serviceProvider == null) return;

            try
            {
                // MainWindow用ViewModel設定
                var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
                DataContext = mainViewModel;
                _logger?.LogDebug("MainWindowViewModel設定完了");

                // ButtonPanel用ViewModel設定
                var buttonPanelViewModel = _serviceProvider.GetRequiredService<ButtonPanelViewModel>();
                ButtonPanelViewControl.DataContext = buttonPanelViewModel;
                _logger?.LogDebug("ButtonPanelViewModel設定完了");

                // ListPanel用ViewModel設定
                var listPanelViewModel = _serviceProvider.GetRequiredService<ListPanelViewModel>();
                CommandListPanel.DataContext = listPanelViewModel;
                _logger?.LogDebug("ListPanelViewModel設定完了");

                // EditPanel用ViewModel設定
                var editPanelViewModel = _serviceProvider.GetRequiredService<EditPanelViewModel>();
                EditPanelViewControl.DataContext = editPanelViewModel;
                _logger?.LogDebug("EditPanelViewModel設定完了");

                _logger?.LogInformation("全ViewModelの設定が完了しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ViewModel設定中にエラー");
                throw;
            }
        }

        /// <summary>
        /// VariableStoreのテスト
        /// </summary>
        private void TestVariableStore()
        {
            try
            {
                var variableStore = _serviceProvider?.GetService<AutoTool.Services.IVariableStore>();
                if (variableStore != null)
                {
                    variableStore.Set("TestVariable", "Hello World");
                    var value = variableStore.Get("TestVariable");
                    _logger?.LogDebug("VariableStoreテスト成功: {Value}", value);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "VariableStoreテスト中にエラー");
            }
        }

        /// <summary>
        /// Window closing event handler
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _logger?.LogInformation("MainWindow終了処理開始");
                
                // 実行中の場合は警告
                if (DataContext is MainWindowViewModel viewModel && (_buttonService?.IsRunning ?? false))
                {
                    var result = System.Windows.MessageBox.Show(
                        "マクロが実行中です。終了しますか？",
                        "確認",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                
                _logger?.LogInformation("MainWindow正常終了");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MainWindow終了処理中にエラー");
            }
        }

        /// <summary>
        /// Debug State Button Click Handler
        /// </summary>
        private void DebugStateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger?.LogInformation("=== DEBUG: 状態確認開始 ===");
                
                if (DataContext is MainWindowViewModel mainViewModel)
                {
                    _logger?.LogInformation("MainViewModel状況:");
                    _logger?.LogInformation("  CommandCount: {CommandCount}", mainViewModel.CommandCount);
                    _logger?.LogInformation("  StatusMessage: {StatusMessage}", mainViewModel.StatusMessage);
                }

                // ButtonPanel的状態確認
                if (ButtonPanelViewControl.DataContext is ButtonPanelViewModel buttonViewModel)
                {
                    _logger?.LogInformation("ButtonPanelViewModel状況:");
                    _logger?.LogInformation("  IsRunning: {IsRunning}", buttonViewModel.IsRunning);
                    _logger?.LogInformation("  SelectedItemType: {SelectedItemType}", buttonViewModel.SelectedItemType?.DisplayName ?? "null");
                    _logger?.LogInformation("  ItemTypes.Count: {Count}", buttonViewModel.ItemTypes.Count);
                    _logger?.LogInformation("  StatusText: {StatusText}", buttonViewModel.StatusText);
                    
                    var stats = buttonViewModel.GetCommandTypeStats();
                    _logger?.LogInformation("  統計: 総タイプ{TotalTypes}, 最近{RecentCount}, お気に入り{FavoriteCount}", 
                        stats.TotalTypes, stats.RecentCount, stats.FavoriteCount);
                }

                // EditPanel的状態確認
                if (EditPanelViewControl.DataContext is EditPanelViewModel editViewModel)
                {
                    _logger?.LogInformation("EditPanelViewModel状況:");
                    _logger?.LogInformation("  SelectedItem: {SelectedItem}", editViewModel.SelectedItem?.ItemType ?? "null");
                    _logger?.LogInformation("  IsDynamicItem: {IsDynamicItem}", editViewModel.IsDynamicItem);
                    _logger?.LogInformation("  IsLegacyItem: {IsLegacyItem}", editViewModel.IsLegacyItem);
                    _logger?.LogInformation("  SettingGroups.Count: {Count}", editViewModel.SettingGroups.Count);
                    
                    // 詳細診断実行
                    editViewModel.DiagnosticProperties();
                }

                // ListPanel的状態確認
                if (CommandListPanel.DataContext is ListPanelViewModel listViewModel)
                {
                    _logger?.LogInformation("ListPanelViewModel状況:");
                    _logger?.LogInformation("  Items.Count: {Count}", listViewModel.Items.Count);
                    _logger?.LogInformation("  SelectedItem: {SelectedItem}", listViewModel.SelectedItem?.ItemType ?? "null");
                    _logger?.LogInformation("  IsRunning: {IsRunning}", listViewModel.IsRunning);
                }

                _logger?.LogInformation("=== DEBUG: 状態確認終了 ===");
                
                System.Windows.MessageBox.Show("状態確認完了。詳細はログを確認してください。", "デバッグ情報", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "状態確認中にエラー");
                System.Windows.MessageBox.Show($"状態確認中にエラー: {ex.Message}", "エラー", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// キーボードショートカットを処理してメッセージを送信
        /// </summary>
        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                var key = e.Key;
                var modifiers = System.Windows.Input.Keyboard.Modifiers;

                string shortcutKey = "";

                // Ctrl+S: 保存
                if (modifiers == System.Windows.Input.ModifierKeys.Control && key == System.Windows.Input.Key.S)
                {
                    shortcutKey = "Ctrl+S";
                }
                // Ctrl+Z: 元に戻す
                else if (modifiers == System.Windows.Input.ModifierKeys.Control && key == System.Windows.Input.Key.Z)
                {
                    shortcutKey = "Ctrl+Z";
                }
                // Ctrl+Y: やり直し
                else if (modifiers == System.Windows.Input.ModifierKeys.Control && key == System.Windows.Input.Key.Y)
                {
                    shortcutKey = "Ctrl+Y";
                }

                if (!string.IsNullOrEmpty(shortcutKey))
                {
                    // 一時的にログのみで処理
                    _logger?.LogDebug("キーボードショートカット処理: {ShortcutKey}", shortcutKey);
                    e.Handled = true; // イベントを処理済みにマーク
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "キーボードショートカット処理エラー");
            }
        }
    }
}