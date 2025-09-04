using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;

namespace AutoTool
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック(DI + Messaging対応)
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

                    // DIからEditPanelViewModelを取得してEditPanelViewに設定
                    var editPanelViewModel = app._host.Services.GetRequiredService<EditPanelViewModel>();
                    EditPanelViewControl.DataContext = mainViewModel; // 継続: MainWindowVM経由でバインド
                    // または直接VMを割り当てる場合は以下
                    // EditPanelViewControl.DataContext = editPanelViewModel;

                    // ロガー取得
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
                    $"初期化中にエラーが発生しました。必要なサービスが利用できない可能性があります。\n\nエラー詳細:\n{ex.Message}",
                    "警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                
                // エラー時として null を設定
                DataContext = null;
            }
        }

        /// <summary>
        /// ウィンドウを閉じる際の処理
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
            catch (Exception)
            {
                // ignore
            }
        }

        private void DebugStateButton_Click(object sender, RoutedEventArgs e)
        {
            // 既存のデバッグハンドラがある想定。必要であれば実装。
        }
    }
}