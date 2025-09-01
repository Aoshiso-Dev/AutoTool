using System;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace AutoTool
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                // ロガーの取得
                var app = Application.Current as App;
                if (app?._host != null)
                {
                    _logger = app._host.Services.GetService<ILogger<MainWindow>>();

                    // ビューモデルを取得してDataContextに設定
                    DataContext = app._host.Services.GetService<MainWindowViewModel>();

                    _logger.LogDebug("MainWindow 初期化完了");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MainWindow 初期化中にエラーが発生しました");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // ビューモデルのクリーンアップ
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.SaveWindowSettings();
                    viewModel.Cleanup();
                }

                _logger.LogDebug("MainWindow 終了処理完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MainWindow 終了処理中にエラーが発生しました");
            }
        }
    }
}