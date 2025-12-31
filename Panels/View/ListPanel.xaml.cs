using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MacroPanels.ViewModel;

namespace MacroPanels.View
{
    /// <summary>
    /// ListPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class ListPanel : UserControl
    {
        private double _savedVerticalOffset = 0;
        private double _savedHorizontalOffset = 0;

        public ListPanel()
        {
            InitializeComponent();
            
            // DataGridのLoadedイベントでスクロール位置保持を設定
            this.Loaded += ListPanel_Loaded;
        }

        private void ListPanel_Loaded(object sender, RoutedEventArgs e)
        {
            // DataGridを取得
            var dataGrid = FindVisualChild<DataGrid>(this);
            if (dataGrid != null)
            {
                // ScrollViewerを取得
                var scrollViewer = FindVisualChild<ScrollViewer>(dataGrid);
                if (scrollViewer != null)
                {
                    // スクロール位置変更イベントを監視
                    scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
                }

                // SelectionChangedイベントでスクロール位置を復元
                dataGrid.SelectionChanged += DataGrid_SelectionChanged;
                
                // ダブルクリックイベントを追加
                dataGrid.MouseDoubleClick += DataGrid_MouseDoubleClick;
            }
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // スクロール位置を保存
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                _savedVerticalOffset = scrollViewer.VerticalOffset;
                _savedHorizontalOffset = scrollViewer.HorizontalOffset;
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 少し遅延してからスクロール位置を復元
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid != null)
                {
                    var scrollViewer = FindVisualChild<ScrollViewer>(dataGrid);
                    if (scrollViewer != null)
                    {
                        // 保存されたスクロール位置に復元
                        scrollViewer.ScrollToVerticalOffset(_savedVerticalOffset);
                        scrollViewer.ScrollToHorizontalOffset(_savedHorizontalOffset);
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // ヘッダー部分のダブルクリックは無視
            var dataGrid = sender as DataGrid;
            if (dataGrid == null) return;

            // クリック位置の要素を確認
            var element = e.OriginalSource as DependencyObject;
            while (element != null)
            {
                if (element is DataGridColumnHeader)
                {
                    // ヘッダーのダブルクリックは無視
                    return;
                }
                if (element is DataGridRow)
                {
                    // 行のダブルクリック - ViewModelに通知
                    if (DataContext is ListPanelViewModel viewModel)
                    {
                        viewModel.OnItemDoubleClick();
                    }
                    return;
                }
                element = VisualTreeHelper.GetParent(element);
            }
        }

        /// <summary>
        /// 設定編集ボタンクリック時の処理
        /// </summary>
        private void EditSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // クリックされたボタンの行を選択状態にする
            var button = sender as FrameworkElement;
            if (button?.DataContext is MacroPanels.Model.List.Interface.ICommandListItem item)
            {
                if (DataContext is ListPanelViewModel viewModel)
                {
                    // 行を選択
                    viewModel.SelectedLineNumber = item.LineNumber - 1;
                    // 編集ウィンドウを開く
                    viewModel.OnItemDoubleClick();
                }
            }
        }

        /// <summary>
        /// ビジュアルツリーから指定した型の子要素を検索
        /// </summary>
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
    }
}
