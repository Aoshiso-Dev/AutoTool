using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;

namespace AutoTool
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック（DI + Messaging対応版）
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow>? _logger;

        /// <summary>
        /// MainWindowのコンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                // App.xamlからサービスを取得してViewModelを設定
                if (Application.Current is App app && app._host != null)
                {
                    // DIからMainWindowViewModelを取得
                    var mainViewModel = app._host.Services.GetRequiredService<MainWindowViewModel>();
                    DataContext = mainViewModel;

                    // DIからListPanelViewModelを取得してListPanelViewに設定
                    var listPanelViewModel = app._host.Services.GetRequiredService<ListPanelViewModel>();
                    CommandListPanel.DataContext = listPanelViewModel;

                    // ロガーも取得
                    _logger = app._host.Services.GetService<ILogger<MainWindow>>();
                    
                    _logger?.LogInformation("MainWindow DI + Messaging初期化完了");
                }
                else
                {
                    throw new InvalidOperationException("アプリケーションまたはホストサービスが利用できません。");
                }
            }
            catch (Exception ex)
            {
                // フォールバックとして最小限のViewModelを作成
                MessageBox.Show(
                    $"初期化エラーが発生しました。一部の機能が制限される可能性があります。\n\nエラー詳細:\n{ex.Message}",
                    "警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                
                // エラー状態として null を設定
                DataContext = null;
            }
        }

        /// <summary>
        /// ウィンドウを閉じる時の処理
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.SaveWindowSettings();
                    viewModel.Cleanup();
                }

                _logger?.LogDebug("MainWindow終了処理完了");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"終了処理でエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// テストボタンのクリックイベント（デバッグ用）
        /// </summary>
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    // 直接ViewModelのメソッドを呼び出し
                    viewModel.AddTestCommand();
                }
                else
                {
                    MessageBox.Show("ViewModelが見つかりません", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"テストボタンクリックでエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 最もシンプルなテスト
        /// </summary>
        private void SimpleTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("シンプルテストボタンが動作しています！", "テスト", MessageBoxButton.OK, MessageBoxImage.Information);
                
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] シンプルテストボタンクリック");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"シンプルテストでエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// デバッグ用：状態確認ボタンのクリックイベント
        /// </summary>
        private void DebugStateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Application.Current is App app && app._host != null)
                {
                    var listPanelViewModel = app._host.Services.GetService<ListPanelViewModel>();
                    if (listPanelViewModel != null)
                    {
                        // listPanelViewModel.DebugItemStates(); // 一時的にコメントアウト
                        // listPanelViewModel.TestExecutionStateDisplay(); // 一時的にコメントアウト
                        MessageBox.Show("状態確認機能は現在無効です。", "デバッグ", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("ListPanelViewModelが見つかりません", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"状態確認中にエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}