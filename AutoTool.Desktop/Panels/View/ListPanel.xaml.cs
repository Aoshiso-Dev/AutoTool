using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AutoTool.Desktop.Panels.ViewModel;

namespace AutoTool.Desktop.Panels.View;

/// <summary>
/// ListPanel.xaml の相互作用ロジック
/// </summary>
public partial class ListPanel : UserControl
{
    private enum ListPanelLayoutMode
    {
        Compact,
        Standard,
        Large
    }

    private double _savedVerticalOffset;
    private double _savedHorizontalOffset;
    private ScrollViewer? _scrollViewer;
    private Window? _ownerWindow;
    private ListPanelLayoutMode? _currentLayoutMode;

    public ListPanel()
    {
        InitializeComponent();
        Loaded += ListPanel_Loaded;
        Unloaded += ListPanel_Unloaded;
    }

    private void ListPanel_Loaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer ??= FindVisualChild<ScrollViewer>(CommandDataGrid);
        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
            _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
        }

        CommandDataGrid.SelectionChanged -= DataGrid_SelectionChanged;
        CommandDataGrid.SelectionChanged += DataGrid_SelectionChanged;
        CommandDataGrid.MouseDoubleClick -= DataGrid_MouseDoubleClick;
        CommandDataGrid.MouseDoubleClick += DataGrid_MouseDoubleClick;

        _ownerWindow ??= Window.GetWindow(this);
        if (_ownerWindow is not null)
        {
            _ownerWindow.SizeChanged -= OwnerWindow_SizeChanged;
            _ownerWindow.SizeChanged += OwnerWindow_SizeChanged;
            ApplyLayoutMode(_ownerWindow.ActualWidth);
            return;
        }

        ApplyLayoutMode(ActualWidth);
    }

    private void ListPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
        }

        CommandDataGrid.SelectionChanged -= DataGrid_SelectionChanged;
        CommandDataGrid.MouseDoubleClick -= DataGrid_MouseDoubleClick;

        if (_ownerWindow is not null)
        {
            _ownerWindow.SizeChanged -= OwnerWindow_SizeChanged;
            _ownerWindow = null;
        }
    }

    private void OwnerWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyLayoutMode(e.NewSize.Width);
    }

    private void ApplyLayoutMode(double windowWidth)
    {
        var mode = windowWidth switch
        {
            < 900 => ListPanelLayoutMode.Compact,
            < 1450 => ListPanelLayoutMode.Standard,
            _ => ListPanelLayoutMode.Large
        };

        if (_currentLayoutMode == mode)
        {
            return;
        }

        _currentLayoutMode = mode;
        switch (mode)
        {
            case ListPanelLayoutMode.Compact:
                CommandDataGrid.RowHeight = 32;
                CommandDataGrid.ColumnHeaderHeight = 28;
                CommandDataGrid.FontSize = 11;
                SetColumnWidth(EnableColumn, 50);
                SetColumnWidth(LineNumberColumn, 30);
                SetColumnWidth(ProgressColumn, 58);
                SetColumnWidth(CommandColumn, 130);
                ApplyActionColumnWidths(CommandDataGrid.RowHeight);
                DescriptionColumn.Visibility = Visibility.Collapsed;
                break;
            case ListPanelLayoutMode.Standard:
                CommandDataGrid.RowHeight = 40;
                CommandDataGrid.ColumnHeaderHeight = 34;
                CommandDataGrid.FontSize = 12;
                SetColumnWidth(EnableColumn, 56);
                SetColumnWidth(LineNumberColumn, 34);
                SetColumnWidth(ProgressColumn, 72);
                SetColumnWidth(CommandColumn, 170);
                ApplyActionColumnWidths(CommandDataGrid.RowHeight);
                DescriptionColumn.Visibility = Visibility.Visible;
                DescriptionColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                break;
            default:
                CommandDataGrid.RowHeight = 52;
                CommandDataGrid.ColumnHeaderHeight = 40;
                CommandDataGrid.FontSize = 14;
                SetColumnWidth(EnableColumn, 68);
                SetColumnWidth(LineNumberColumn, 40);
                SetColumnWidth(ProgressColumn, 96);
                SetColumnWidth(CommandColumn, 260);
                ApplyActionColumnWidths(CommandDataGrid.RowHeight);
                DescriptionColumn.Visibility = Visibility.Visible;
                DescriptionColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                break;
        }
    }

    private void ApplyActionColumnWidths(double rowHeight)
    {
        SetColumnWidth(DeleteColumn, rowHeight);
        SetColumnWidth(EditColumn, rowHeight);
        // 上下ボタンは2個を横並びにするため、1つ分の隙間(2px)を加える
        SetColumnWidth(MoveColumn, rowHeight * 2 + 2);
    }

    private static void SetColumnWidth(DataGridColumn column, double width)
    {
        ArgumentNullException.ThrowIfNull(column);
        column.Width = new DataGridLength(width);
    }

    private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        _savedVerticalOffset = scrollViewer.VerticalOffset;
        _savedHorizontalOffset = scrollViewer.HorizontalOffset;
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (_scrollViewer is null)
            {
                _scrollViewer = FindVisualChild<ScrollViewer>(CommandDataGrid);
                if (_scrollViewer is null)
                {
                    return;
                }
            }

            _scrollViewer.ScrollToVerticalOffset(_savedVerticalOffset);
            _scrollViewer.ScrollToHorizontalOffset(_savedHorizontalOffset);
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var element = e.OriginalSource as DependencyObject;
        while (element is not null)
        {
            if (element is DataGridColumnHeader)
            {
                return;
            }

            if (element is DataGridRow)
            {
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
        if (sender is not FrameworkElement { DataContext: AutoTool.Automation.Contracts.Lists.ICommandListItem item })
        {
            return;
        }

        if (DataContext is not ListPanelViewModel viewModel)
        {
            return;
        }

        viewModel.SelectedLineNumber = item.LineNumber - 1;
        viewModel.OnItemDoubleClick();
    }

    /// <summary>
    /// コマンド行の削除ボタンクリック時の処理
    /// </summary>
    private void DeleteCommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: AutoTool.Automation.Contracts.Lists.ICommandListItem item })
        {
            return;
        }

        if (DataContext is not ListPanelViewModel viewModel)
        {
            return;
        }

        viewModel.SelectedLineNumber = item.LineNumber - 1;
        viewModel.Delete();
    }

    /// <summary>
    /// コマンド行の上移動ボタンクリック時の処理
    /// </summary>
    private void MoveUpCommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: AutoTool.Automation.Contracts.Lists.ICommandListItem item })
        {
            return;
        }

        if (DataContext is not ListPanelViewModel viewModel)
        {
            return;
        }

        viewModel.SelectedLineNumber = item.LineNumber - 1;
        viewModel.Up();
    }

    /// <summary>
    /// コマンド行の下移動ボタンクリック時の処理
    /// </summary>
    private void MoveDownCommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: AutoTool.Automation.Contracts.Lists.ICommandListItem item })
        {
            return;
        }

        if (DataContext is not ListPanelViewModel viewModel)
        {
            return;
        }

        viewModel.SelectedLineNumber = item.LineNumber - 1;
        viewModel.Down();
    }

    /// <summary>
    /// ビジュアルツリーから指定した型の子要素を検索
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
            {
                return result;
            }

            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild is not null)
            {
                return childOfChild;
            }
        }

        return default;
    }
}
