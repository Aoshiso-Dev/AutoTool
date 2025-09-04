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
                // ネストレベルに応じたインデント
                // レベル0: インデントなし
                // レベル1: 25px (Loop/If内)
                // レベル2: 45px (Loop内のIf、またはIf内のLoop)
                // レベル3: 65px (複雑な入れ子)
                var baseIndent = item.NestLevel * 20; // 基本インデント
                var extraIndent = item.NestLevel > 0 ? 5 : 0; // 1レベル目以降は少し余分にインデント
                var totalIndent = baseIndent + extraIndent;
                
                return new Thickness(totalIndent, 2, 0, 2);
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
                
                // ネストレベルに応じた視覚的な表現
                for (int i = 0; i < item.NestLevel; i++)
                {
                    if (i == 0)
                        prefix += "├── "; // 最初のレベルは├──
                    else
                        prefix += "│   "; // その後のレベルは│とスペース
                }
                
                var displayName = AutoTool.Model.CommandDefinition.CommandRegistry.DisplayOrder.GetDisplayName(item.ItemType) ?? item.ItemType;
                
                // 終了コマンドは特別な表記
                if (item.ItemType == "Loop_End" || item.ItemType == "IF_End")
                {
                    if (item.NestLevel > 0)
                    {
                        // 終了コマンドは└──で表現
                        prefix = "";
                        for (int i = 0; i < item.NestLevel - 1; i++)
                        {
                            prefix += "│   ";
                        }
                        prefix += "└── ";
                    }
                    else
                    {
                        prefix = "└── ";
                    }
                }
                
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
                    
                    // 現在実行中のアイテムをチェック
                    var isCurrentExecuting = IsCurrentExecutingItem(item, currentExecutingItem);
                    
                    if (item.IsRunning || isCurrentExecuting)
                    {
                        System.Diagnostics.Debug.WriteLine($"  -> 黄色ハイライト適用: {item.ItemType}");
                        return new SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 255, 255, 0)); // 半透明黄色
                    }
                    else if (!item.IsEnable)
                    {
                        System.Diagnostics.Debug.WriteLine($"  -> グレーアウト適用: {item.ItemType}");
                        return new SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 128, 128, 128));
                    }
                    
                    // ネストレベルに応じた背景色（改良版）
                    var nestBrush = item.NestLevel switch
                    {
                        0 => System.Windows.Media.Brushes.Transparent,
                        1 => item.IsInLoop && item.IsInIf 
                             ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 128, 0, 128)) // Loop + If: 紫
                             : item.IsInLoop 
                               ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(25, 0, 150, 255))   // Loop: 青
                               : new SolidColorBrush(System.Windows.Media.Color.FromArgb(25, 0, 200, 100)),  // If: 緑
                        2 => new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, 255, 140, 0)),     // オレンジ
                        3 => new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 200, 0, 200)),     // マゼンタ
                        _ => new SolidColorBrush(System.Windows.Media.Color.FromArgb(55, 100, 100, 100))    // グレー
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"  -> ネストレベル背景適用: {item.ItemType}, Level={item.NestLevel}, InLoop={item.IsInLoop}, InIf={item.IsInIf}");
                    return nestBrush;
                }
                
                System.Diagnostics.Debug.WriteLine("ExecutionStateToBackgroundConverter: 値が不正またはnull");
                return System.Windows.Media.Brushes.Transparent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExecutionStateToBackgroundConverter エラー: {ex.Message}");
                return System.Windows.Media.Brushes.Transparent;
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
                        return "?"; // 実行中
                    }
                    else if (!item.IsEnable)
                    {
                        System.Diagnostics.Debug.WriteLine($"ExecutionStateToIconConverter: {item.ItemType} -> ? (無効)");
                        return "?"; // 無効
                    }
                    else
                    {
                        // コマンドタイプに応じたアイコン
                        var icon = item.ItemType switch
                        {
                            "Loop" => "??",
                            "Loop_End" => "??",
                            "Loop_Break" => "?",
                            "IF_ImageExist" => "??",
                            "IF_ImageNotExist" => "??",
                            "IF_ImageExist_AI" => "??",
                            "IF_ImageNotExist_AI" => "??",
                            "IF_Variable" => "??",
                            "IF_End" => "?",
                            "Wait_Image" => "?",
                            "Click_Image" => "??",
                            "Click_Image_AI" => "??",
                            "Hotkey" => "?",
                            "Click" => "??",
                            "Wait" => "?",
                            "Execute" => "??",
                            "SetVariable" => "??",
                            "SetVariable_AI" => "??",
                            "Screenshot" => "??",
                            _ => "??"
                        };
                        
                        System.Diagnostics.Debug.WriteLine($"ExecutionStateToIconConverter: {item.ItemType} -> {icon} (待機)");
                        return icon;
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
    /// ペアリング表示用コンバーター（改良版）
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
                        // 開始コマンドと終了コマンドで表示を変える
                        if (item.ItemType == "Loop")
                            return $"{item.LineNumber}┬{pairValue.LineNumber}"; // Loop開始
                        else if (item.ItemType == "Loop_End")
                            return $"{pairValue.LineNumber}┴{item.LineNumber}"; // Loop終了
                        else if (IsIfCommand(item.ItemType))
                            return $"{item.LineNumber}┬{pairValue.LineNumber}"; // If開始
                        else if (item.ItemType == "IF_End")
                            return $"{pairValue.LineNumber}┴{item.LineNumber}"; // If終了
                        else
                            return $"{item.LineNumber}?{pairValue.LineNumber}"; // その他のペア
                    }
                }
                
                // ペアがない場合の表示
                if (item.ItemType == "Loop" || IsIfCommand(item.ItemType))
                    return $"{item.LineNumber}┬?"; // 開始コマンドで対応する終了がない
                else if (item.ItemType == "Loop_End" || item.ItemType == "IF_End")
                    return $"?┴{item.LineNumber}"; // 終了コマンドで対応する開始がない
                else
                    return $"{item.LineNumber}"; // 通常のコマンド
            }
            return "";
        }

        private bool IsIfCommand(string itemType) => itemType switch
        {
            "IF_ImageExist" => true,
            "IF_ImageNotExist" => true,
            "IF_ImageExist_AI" => true,
            "IF_ImageNotExist_AI" => true,
            "IF_Variable" => true,
            _ => false
        };

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