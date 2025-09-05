using System;
using System.Windows;
using System.Windows.Controls;
using AutoTool.ViewModel.Panels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.View.Panels
{
    /// <summary>
    /// ButtonPanelView.xaml の相互作用ロジック
    /// </summary>
    public partial class ButtonPanelView : System.Windows.Controls.UserControl
    {
        private ILogger<ButtonPanelView>? _logger;
        private ButtonPanelViewModel? ViewModel => DataContext as ButtonPanelViewModel;

        public ButtonPanelView()
        {
            InitializeComponent();
            
            // DataContext が設定されたときのイベント
            DataContextChanged += ButtonPanelView_DataContextChanged;
            Loaded += ButtonPanelView_Loaded;
        }

        private void ButtonPanelView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (_logger == null)
                {
                    // ロガーを取得
                    if (System.Windows.Application.Current is App app && app.Services != null)
                    {
                        _logger = app.Services.GetService<ILogger<ButtonPanelView>>();
                    }
                }

                _logger?.LogDebug("ButtonPanelView DataContextChanged: {OldValue} -> {NewValue}",
                    e.OldValue?.GetType().FullName ?? "null",
                    e.NewValue?.GetType().FullName ?? "null");

                if (e.NewValue is ButtonPanelViewModel buttonVM)
                {
                    _logger?.LogInformation("ButtonPanelViewModelがDataContextに設定されました: {TypeName}", buttonVM.GetType().FullName);
                }
                else if (e.NewValue != null)
                {
                    _logger?.LogWarning("期待したButtonPanelViewModel以外の型がDataContextに設定されました: {TypeName}", e.NewValue.GetType().FullName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DataContext変更処理中にエラー");
            }
        }

        private void ButtonPanelView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_logger == null)
                {
                    // ロガーを再取得
                    if (System.Windows.Application.Current is App app && app.Services != null)
                    {
                        _logger = app.Services.GetService<ILogger<ButtonPanelView>>();
                    }
                }

                _logger?.LogDebug("ButtonPanelView Loaded: DataContext = {DataContext}",
                    DataContext?.GetType().FullName ?? "null");

                if (ViewModel != null)
                {
                    _logger?.LogInformation("ButtonPanelView読み込み完了: ViewModelが利用可能");
                    
                    // 初期化処理があれば実行
                    ViewModel.Prepare();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Loaded処理中にエラー");
            }
        }

        /// <summary>
        /// 統計表示ボタンクリックイベント
        /// </summary>
        private void ShowStatsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel != null)
                {
                    var stats = ViewModel.GetCommandTypeStats();
                    
                    var statsMessage = $"コマンドタイプ統計:\n\n" +
                        $"総タイプ数: {stats.TotalTypes}\n" +
                        $"最近使用: {stats.RecentCount}\n" +
                        $"お気に入り: {stats.FavoriteCount}\n\n" +
                        "カテゴリ別:\n";
                    
                    foreach (var categoryStats in stats.CategoryStats)
                    {
                        statsMessage += $"  {categoryStats.Key}: {categoryStats.Value}個\n";
                    }
                    
                    System.Windows.MessageBox.Show(statsMessage, "コマンド統計", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    _logger?.LogInformation("コマンド統計を表示しました");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "統計表示中にエラー");
                System.Windows.MessageBox.Show($"統計表示中にエラーが発生しました: {ex.Message}", 
                    "統計表示エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}