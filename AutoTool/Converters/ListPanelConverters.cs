using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AutoTool.Model.List.Interface;
using AutoTool.ViewModel.Panels;

namespace AutoTool.Converters
{
    /// <summary>
    /// ネストレベルに基づくマージンコンバーター
    /// </summary>
    public class NestLevelToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ICommandListItem item)
            {
                var indent = item.NestLevel * 20; // 1レベルにつき20ピクセル
                return new Thickness(indent, 2, 0, 2);
            }
            return new Thickness(0, 2, 0, 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ネストレベルに基づく表示テキストコンバーター
    /// </summary>
    public class NestLevelToDisplayTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ICommandListItem item)
            {
                var prefix = "";
                for (int i = 0; i < item.NestLevel; i++)
                {
                    prefix += "　　"; // 全角スペース2つでインデント
                }
                
                var displayName = AutoTool.Model.CommandDefinition.CommandRegistry.DisplayOrder.GetDisplayName(item.ItemType) ?? item.ItemType;
                return $"{prefix}{displayName}";
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 実行状態に基づく背景色コンバーター
    /// </summary>
    public class ExecutionStateToBackgroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length >= 2 && values[0] is ICommandListItem item)
                {
                    var currentExecutingItem = values[1] as ICommandListItem;
                    
                    // デバッグログ出力
                    System.Diagnostics.Debug.WriteLine($"ExecutionStateToBackgroundConverter: Item={item.ItemType}(行{item.LineNumber}), IsRunning={item.IsRunning}, CurrentExecuting={currentExecutingItem?.ItemType ?? "null"}");
                    
                    // 現在実行中のアイテムかチェック
                    var isCurrentExecuting = IsCurrentExecutingItem(item, currentExecutingItem);
                    
                    if (item.IsRunning || isCurrentExecuting)
                    {
                        System.Diagnostics.Debug.WriteLine($"  -> 黄色ハイライト適用: {item.ItemType}");
                        return new SolidColorBrush(Color.FromArgb(150, 255, 255, 0)); // より濃い黄色
                    }
                    else if (!item.IsEnable)
                    {
                        System.Diagnostics.Debug.WriteLine($"  -> グレーアウト適用: {item.ItemType}");
                        return new SolidColorBrush(Color.FromArgb(80, 128, 128, 128));
                    }
                    
                    // ネストレベルに応じた背景色
                    var nestBrush = item.NestLevel switch
                    {
                        0 => Brushes.Transparent,
                        1 => new SolidColorBrush(Color.FromArgb(25, 0, 100, 255)),
                        2 => new SolidColorBrush(Color.FromArgb(35, 0, 150, 255)),
                        3 => new SolidColorBrush(Color.FromArgb(45, 0, 200, 255)),
                        _ => new SolidColorBrush(Color.FromArgb(55, 0, 255, 255))
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"  -> ネストレベル背景適用: {item.ItemType}, Level={item.NestLevel}");
                    return nestBrush;
                }
                
                System.Diagnostics.Debug.WriteLine("ExecutionStateToBackgroundConverter: 値が不正またはnull");
                return Brushes.Transparent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExecutionStateToBackgroundConverter エラー: {ex.Message}");
                return Brushes.Transparent;
            }
        }

        private bool IsCurrentExecutingItem(ICommandListItem item, ICommandListItem? currentExecutingItem)
        {
            if (currentExecutingItem == null) return false;
            
            var result = item.LineNumber == currentExecutingItem.LineNumber;
            System.Diagnostics.Debug.WriteLine($"    IsCurrentExecutingItem: {item.ItemType}(行{item.LineNumber}) == {currentExecutingItem.ItemType}(行{currentExecutingItem.LineNumber}) -> {result}");
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 実行状態アイコンコンバーター
    /// </summary>
    public class ExecutionStateToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is ICommandListItem item)
                {
                    if (item.IsRunning)
                    {
                        System.Diagnostics.Debug.WriteLine($"ExecutionStateToIconConverter: {item.ItemType} -> ? (実行中)");
                        return "?";
                    }
                    else if (!item.IsEnable)
                    {
                        System.Diagnostics.Debug.WriteLine($"ExecutionStateToIconConverter: {item.ItemType} -> ? (無効)");
                        return "?";
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"ExecutionStateToIconConverter: {item.ItemType} -> ? (停止)");
                        return "?";
                    }
                }
                return "?";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExecutionStateToIconConverter エラー: {ex.Message}");
                return "?";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// プログレス表示可視性コンバーター
    /// </summary>
    public class ProgressVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is ICommandListItem item)
                {
                    var isVisible = item.IsRunning && item.Progress > 0;
                    System.Diagnostics.Debug.WriteLine($"ProgressVisibilityConverter: {item.ItemType} IsRunning={item.IsRunning}, Progress={item.Progress} -> {(isVisible ? "Visible" : "Collapsed")}");
                    return isVisible ? Visibility.Visible : Visibility.Collapsed;
                }
                return Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProgressVisibilityConverter エラー: {ex.Message}");
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ブール値から可視性への変換
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// ペアリング表示用コンバーター
    /// </summary>
    public class PairLineNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ICommandListItem item)
            {
                // Pairプロパティを動的に取得
                var pairProperty = item.GetType().GetProperty("Pair");
                if (pairProperty != null)
                {
                    var pairValue = pairProperty.GetValue(item) as ICommandListItem;
                    if (pairValue != null)
                    {
                        return $"{item.LineNumber}->{pairValue.LineNumber}";
                    }
                }
                return $"{item.LineNumber}-->";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// コメント表示用コンバーター
    /// </summary>
    public class CommentDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ICommandListItem item)
            {
                if (!string.IsNullOrEmpty(item.Comment))
                {
                    return $" / {item.Comment}";
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// プログレス率コンバーター
    /// </summary>
    public class ProgressPercentageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && 
                values[0] is int current && 
                values[1] is int total && 
                total > 0)
            {
                return (double)current / total * 100;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ブール値からフォントウェイトへの変換
    /// </summary>
    public class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? FontWeights.Normal : FontWeights.Light;
            }
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 実行状態から色への変換（修正版）
    /// </summary>
    public class RunningStateToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2)
            {
                var isRunning = values[0] is bool running && running;
                var showProgress = values[1] is bool progress && progress;
                
                if (isRunning && showProgress)
                    return Colors.LimeGreen; // 実行中
                else if (isRunning)
                    return Colors.Orange;    // 準備中
                else
                    return Colors.Gray;      // 停止中
            }
            return Colors.Gray;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 実行状態からテキストへの変換
    /// </summary>
    public class RunningStateToTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2)
            {
                var isRunning = values[0] is bool running && running;
                var showProgress = values[1] is bool progress && progress;
                
                if (isRunning && showProgress)
                    return "実行中";
                else if (isRunning)
                    return "準備中";
                else
                    return "待機中";
            }
            return "待機中";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// プログレス値からスケール値への変換
    /// </summary>
    public class ProgressToScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                return progress / 100.0;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}