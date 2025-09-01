using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow>? _logger;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                // ロガーの取得
                var app = Application.Current as App;
                if (app?._host != null)
                {
                    _logger = app._host.Services.GetRequiredService<ILogger<MainWindow>>();

                    // ビューモデルを取得してDataContextに設定
                    DataContext = app._host.Services.GetRequiredService<MainWindowViewModel>();

                    _logger?.LogDebug("MainWindow 初期化完了");
                }
                else
                {
                    // DIコンテナが利用できない場合のフォールバック
                    System.Diagnostics.Debug.WriteLine("DIコンテナが利用できません。レガシーモードで初期化します。");
                    
                    // レガシーモードで MainWindowViewModel を作成
                    #pragma warning disable CS0618 // Obsolete 警告を無視
                    DataContext = new MainWindowViewModel();
                    #pragma warning restore CS0618
                    
                    System.Diagnostics.Debug.WriteLine("レガシーモードで MainWindow が初期化されました。");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MainWindow 初期化中にエラーが発生しました");
                
                // エラーが発生した場合でもウィンドウを表示するため、最小限の初期化を試行
                try
                {
                    System.Diagnostics.Debug.WriteLine($"MainWindow 初期化エラー: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine("緊急フォールバックモードで初期化を試行します。");
                    
                    #pragma warning disable CS0618 // Obsolete 警告を無視
                    DataContext = new MainWindowViewModel();
                    #pragma warning restore CS0618
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"緊急フォールバックも失敗: {fallbackEx.Message}");
                    MessageBox.Show($"MainWindow の初期化に失敗しました:\n{ex.Message}\n\nフォールバックも失敗:\n{fallbackEx.Message}", 
                                    "初期化エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

                _logger?.LogDebug("MainWindow 終了処理完了");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MainWindow 終了処理中にエラーが発生しました");
            }
        }
    }
}