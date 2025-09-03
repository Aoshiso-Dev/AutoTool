using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel;

namespace AutoTool
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
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
                    DataContext = app._host.Services.GetRequiredService<MainWindowViewModel>();
                }
                else
                {
                    throw new InvalidOperationException("アプリケーションまたはホストサービスが利用できません。");
                }
            }
            catch (Exception ex)
            {
                // フォールバックとして最小限のViewModelを作成
                // Note: この場合は機能制限があることをユーザーに通知すべき
                MessageBox.Show(
                    $"初期化エラーが発生しました。一部の機能が制限される可能性があります。\n\nエラー詳細:\n{ex.Message}",
                    "警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                
                // 最小限の代替ViewModelを設定（サービスが利用できない場合）
                DataContext = null; // エラー状態として null を設定
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
    }
}