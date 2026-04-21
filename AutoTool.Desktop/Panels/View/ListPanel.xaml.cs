using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Desktop.Panels.ViewModel;

namespace AutoTool.Desktop.Panels.View;

/// <summary>
/// ListPanel.xaml の相互作用ロジック
/// </summary>
public partial class ListPanel : UserControl
{
    private const string DragItemDataFormat = "AutoTool.CommandListItem";
    /// <summary>
    /// この機能で扱う状態や種別の選択肢を列挙し、分岐条件を明確にします。
    /// </summary>

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
    private Point? _dragStartPoint;
    private ICommandListItem? _dragSourceItem;

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
        CommandDataGrid.PreviewMouseDown -= DataGrid_PreviewInteraction;
        CommandDataGrid.PreviewMouseDown += DataGrid_PreviewInteraction;
        CommandDataGrid.PreviewMouseWheel -= DataGrid_PreviewInteraction;
        CommandDataGrid.PreviewMouseWheel += DataGrid_PreviewInteraction;
        CommandDataGrid.PreviewKeyDown -= DataGrid_PreviewInteraction;
        CommandDataGrid.PreviewKeyDown += DataGrid_PreviewInteraction;

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
        CommandDataGrid.PreviewMouseDown -= DataGrid_PreviewInteraction;
        CommandDataGrid.PreviewMouseWheel -= DataGrid_PreviewInteraction;
        CommandDataGrid.PreviewKeyDown -= DataGrid_PreviewInteraction;

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
                SetColumnWidth(LineNumberColumn, 44);
                SetColumnWidth(CommandColumn, 130);
                ApplyActionColumnWidths(CommandDataGrid.RowHeight);
                DescriptionColumn.Visibility = Visibility.Collapsed;
                break;
            case ListPanelLayoutMode.Standard:
                CommandDataGrid.RowHeight = 40;
                CommandDataGrid.ColumnHeaderHeight = 34;
                CommandDataGrid.FontSize = 12;
                SetColumnWidth(EnableColumn, 56);
                SetColumnWidth(LineNumberColumn, 48);
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
                SetColumnWidth(LineNumberColumn, 56);
                SetColumnWidth(CommandColumn, 260);
                ApplyActionColumnWidths(CommandDataGrid.RowHeight);
                DescriptionColumn.Visibility = Visibility.Visible;
                DescriptionColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                break;
        }
    }

    private void ApplyActionColumnWidths(double rowHeight)
    {
        // 操作ボタン列（設定/上移動/下移動/削除）を1セルで表示
        SetColumnWidth(ActionColumn, rowHeight * 4 + 6);
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
        if (DataContext is IListPanelViewModel viewModel)
        {
            var selectedItems = CommandDataGrid.SelectedItems
                .OfType<AutoTool.Automation.Contracts.Lists.ICommandListItem>()
                .ToList();
            viewModel.SetSelectedItems(selectedItems);
        }

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
                if (DataContext is IListPanelViewModel viewModel)
                {
                    viewModel.OnItemDoubleClick();
                }
                return;
            }

            element = VisualTreeHelper.GetParent(element);
        }
    }

    private void DataGrid_PreviewInteraction(object sender, RoutedEventArgs e)
    {
        if (DataContext is IListPanelViewModel viewModel)
        {
            viewModel.NotifyInteraction();
        }
    }

    private void CommandDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not IListPanelViewModel { IsRunning: false } viewModel)
        {
            _dragStartPoint = null;
            _dragSourceItem = null;
            return;
        }

        var sourceElement = e.OriginalSource as DependencyObject;
        if (sourceElement is null || IsInteractiveChildElement(sourceElement))
        {
            _dragStartPoint = null;
            _dragSourceItem = null;
            return;
        }

        var row = FindVisualParent<DataGridRow>(sourceElement);
        if (row?.Item is not ICommandListItem rowItem)
        {
            _dragStartPoint = null;
            _dragSourceItem = null;
            return;
        }

        _dragStartPoint = e.GetPosition(CommandDataGrid);
        _dragSourceItem = rowItem;

        if (!CommandDataGrid.SelectedItems.Contains(rowItem))
        {
            CommandDataGrid.SelectedItem = rowItem;
        }

        viewModel.SetSelectedItems(
        [
            .. CommandDataGrid.SelectedItems.OfType<ICommandListItem>()
        ]);
    }

    private void CommandDataGrid_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed ||
            _dragStartPoint is not Point dragStartPoint ||
            _dragSourceItem is null ||
            DataContext is not IListPanelViewModel { IsRunning: false })
        {
            return;
        }

        var currentPosition = e.GetPosition(CommandDataGrid);
        var horizontalDistance = Math.Abs(currentPosition.X - dragStartPoint.X);
        var verticalDistance = Math.Abs(currentPosition.Y - dragStartPoint.Y);
        if (horizontalDistance < SystemParameters.MinimumHorizontalDragDistance &&
            verticalDistance < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var dragData = new DataObject(DragItemDataFormat, _dragSourceItem);
        DragDrop.DoDragDrop(CommandDataGrid, dragData, DragDropEffects.Move);
    }

    private void CommandDataGrid_DragOver(object sender, DragEventArgs e)
    {
        if (DataContext is not IListPanelViewModel { IsRunning: false } ||
            e.Data.GetData(DragItemDataFormat) is not ICommandListItem sourceItem ||
            CommandDataGrid.Items.IndexOf(sourceItem) < 0)
        {
            HideDropInsertIndicator();
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var fromIndex = CommandDataGrid.Items.IndexOf(sourceItem);
        var toIndex = GetDropTargetIndex(fromIndex, e.GetPosition(CommandDataGrid), out var indicatorY);
        e.Effects = toIndex >= 0 && toIndex != fromIndex ? DragDropEffects.Move : DragDropEffects.None;
        if (e.Effects == DragDropEffects.Move)
        {
            ShowDropInsertIndicator(indicatorY);
        }
        else
        {
            HideDropInsertIndicator();
        }
        e.Handled = true;
    }

    private void CommandDataGrid_DragLeave(object sender, DragEventArgs e)
    {
        var position = e.GetPosition(CommandDataGrid);
        var isOutside =
            position.X < 0 || position.Y < 0 ||
            position.X > CommandDataGrid.ActualWidth ||
            position.Y > CommandDataGrid.ActualHeight;

        if (isOutside)
        {
            HideDropInsertIndicator();
        }
    }

    private void CommandDataGrid_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not IListPanelViewModel { IsRunning: false } viewModel ||
            e.Data.GetData(DragItemDataFormat) is not ICommandListItem sourceItem)
        {
            HideDropInsertIndicator();
            e.Handled = true;
            return;
        }

        var fromIndex = CommandDataGrid.Items.IndexOf(sourceItem);
        var toIndex = GetDropTargetIndex(fromIndex, e.GetPosition(CommandDataGrid), out _);
        HideDropInsertIndicator();
        if (fromIndex < 0 || toIndex < 0 || fromIndex == toIndex)
        {
            e.Handled = true;
            return;
        }

        viewModel.RequestMoveItem(fromIndex, toIndex);
        viewModel.SetSelectedItems([sourceItem]);
        viewModel.NotifyInteraction();
        e.Handled = true;
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

        if (DataContext is not IListPanelViewModel viewModel)
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

        if (DataContext is not IListPanelViewModel viewModel)
        {
            return;
        }

        viewModel.SelectedLineNumber = item.LineNumber - 1;
        viewModel.RequestDelete();
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

        if (DataContext is not IListPanelViewModel viewModel)
        {
            return;
        }

        var fromIndex = item.LineNumber - 1;
        viewModel.SelectedLineNumber = fromIndex;
        viewModel.RequestMoveItem(fromIndex, fromIndex - 1);
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

        if (DataContext is not IListPanelViewModel viewModel)
        {
            return;
        }

        var fromIndex = item.LineNumber - 1;
        viewModel.SelectedLineNumber = fromIndex;
        viewModel.RequestMoveItem(fromIndex, fromIndex + 1);
    }

    private void ToggleBlockCollapseButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: AutoTool.Automation.Contracts.Lists.ICommandListItem item })
        {
            return;
        }

        if (DataContext is not IListPanelViewModel viewModel)
        {
            return;
        }

        viewModel.ToggleBlockCollapse(item);
        e.Handled = true;
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

    private int GetDropTargetIndex(int fromIndex, Point position, out double indicatorY)
    {
        indicatorY = 0;
        var itemCount = CommandDataGrid.Items.Count;
        if (itemCount == 0 || fromIndex < 0 || fromIndex >= itemCount)
        {
            return -1;
        }

        var hitResult = VisualTreeHelper.HitTest(CommandDataGrid, position);
        var row = FindVisualParent<DataGridRow>(hitResult?.VisualHit);
        int insertionIndex;
        if (row is null)
        {
            insertionIndex = itemCount;
            indicatorY = GetBottomIndicatorY();
            return GetAdjustedDropIndex(fromIndex, insertionIndex, itemCount);
        }

        var rowIndex = CommandDataGrid.ItemContainerGenerator.IndexFromContainer(row);
        if (rowIndex < 0)
        {
            insertionIndex = itemCount;
            indicatorY = GetBottomIndicatorY();
            return GetAdjustedDropIndex(fromIndex, insertionIndex, itemCount);
        }

        var rowPosition = position.Y - row.TranslatePoint(new Point(0, 0), CommandDataGrid).Y;
        var moveAfterRow = rowPosition > row.ActualHeight / 2.0;
        insertionIndex = moveAfterRow ? rowIndex + 1 : rowIndex;
        var rowTop = row.TranslatePoint(new Point(0, 0), CommandDataGrid).Y;
        indicatorY = moveAfterRow ? rowTop + row.ActualHeight : rowTop;
        return GetAdjustedDropIndex(fromIndex, insertionIndex, itemCount);
    }

    private static int GetAdjustedDropIndex(int fromIndex, int insertionIndex, int itemCount)
    {
        var clampedInsertion = Math.Clamp(insertionIndex, 0, itemCount);
        var targetIndex = fromIndex < clampedInsertion ? clampedInsertion - 1 : clampedInsertion;
        return Math.Clamp(targetIndex, 0, itemCount - 1);
    }

    private static bool IsInteractiveChildElement(DependencyObject element)
    {
        return FindVisualParent<ButtonBase>(element) is not null ||
               FindVisualParent<ToggleButton>(element) is not null;
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T result)
            {
                return result;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return default;
    }

    private void ShowDropInsertIndicator(double indicatorY)
    {
        if (double.IsNaN(indicatorY) || double.IsInfinity(indicatorY))
        {
            HideDropInsertIndicator();
            return;
        }

        var top = Math.Clamp(indicatorY - (DropInsertIndicator.Height / 2.0), 0, Math.Max(0, CommandDataGrid.ActualHeight - DropInsertIndicator.Height));
        DropInsertIndicator.Margin = new Thickness(0, top, 0, 0);
        DropInsertIndicator.Visibility = Visibility.Visible;
    }

    private void HideDropInsertIndicator()
    {
        DropInsertIndicator.Visibility = Visibility.Collapsed;
    }

    private double GetBottomIndicatorY()
    {
        if (CommandDataGrid.Items.Count == 0)
        {
            return 0;
        }

        if (CommandDataGrid.ItemContainerGenerator.ContainerFromIndex(CommandDataGrid.Items.Count - 1) is DataGridRow row)
        {
            var rowTop = row.TranslatePoint(new Point(0, 0), CommandDataGrid).Y;
            return rowTop + row.ActualHeight;
        }

        return Math.Max(0, CommandDataGrid.ActualHeight);
    }
}
