using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AutoTool.ViewModel.Panels;
using AutoTool.Command.Definition;
using AutoTool.ViewModel.Shared;

namespace AutoTool.View.Converters
{
    /// <summary>
    /// ネストレベルに基づく表示テキストコンバーター
    /// </summary>
    public class NestLevelToDisplayTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UniversalCommandItem item)
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
                
                var displayName = AutoToolCommandRegistry.DisplayOrder.GetDisplayName(item.ItemType) ?? item.ItemType;
                
                // 終了コマンドは特別な表記
                if (item.ItemType == "Loop_End" || item.ItemType == "IfEnd")
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
                if (values.Length >= 2 && values[0] is UniversalCommandItem item)
                {
                    var currentExecutingItem = values[1] as UniversalCommandItem;
                    
                    // 現在実行中のアイテムをチェック
                    var isCurrentExecuting = IsCurrentExecutingItem(item, currentExecutingItem);
                    
                    if (item.IsRunning || isCurrentExecuting)
                    {
                        return new SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 255, 255, 0)); // 半透明黄色
                    }
                    else if (!item.IsEnable)
                    {
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
                    
                    return nestBrush;
                }
                
                return System.Windows.Media.Brushes.Transparent;
            }
            catch (Exception)
            {
                return System.Windows.Media.Brushes.Transparent;
            }
        }

        private bool IsCurrentExecutingItem(UniversalCommandItem item, UniversalCommandItem? currentExecutingItem)
        {
            if (currentExecutingItem == null) return false;
            return item.LineNumber == currentExecutingItem.LineNumber;
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
                if (value is UniversalCommandItem item)
                {
                    if (item.IsRunning)
                    {
                        return "▶"; // 実行中
                    }
                    else if (!item.IsEnable)
                    {
                        return "⏸"; // 無効
                    }
                    else
                    {
                        // コマンドタイプに応じたアイコン
                        var icon = item.ItemType switch
                        {
                            "Loop" => "🔄",
                            "LoopEnd" => "🔚",
                            "LoopBreak" => "⚡",
                            "IfImageExist" => "❓",
                            "IfImageNotExist" => "❗",
                            "IfImageExistAI" => "🔍",
                            "IfImageNotExist_AI" => "🔍",
                            "IfVariable" => "📊",
                            "IfEnd" => "✅",
                            "WaitImage" => "⏱",
                            "ClickImage" => "🖱",
                            "ClickImageAI" => "🤖",
                            "Hotkey" => "⌨",
                            "Click" => "👆",
                            "Wait" => "⏸",
                            "Execute" => "🚀",
                            "SetVariable" => "📝",
                            "SetVariableAI" => "🧠",
                            "Screenshot" => "📸",
                            _ => "📄"
                        };
                        
                        return icon;
                    }
                }
                return "📄";
            }
            catch (Exception)
            {
                return "📄";
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
                if (value is UniversalCommandItem item)
                {
                    var isVisible = item.IsRunning && item.Progress > 0;
                    return isVisible ? Visibility.Visible : Visibility.Collapsed;
                }
                return Visibility.Collapsed;
            }
            catch (Exception)
            {
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
            if (value is UniversalCommandItem item)
            {
                // Pairプロパティを動的に取得
                var pairProperty = item.GetType().GetProperty("Pair");
                if (pairProperty != null)
                {
                    var pairValue = pairProperty.GetValue(item) as UniversalCommandItem;
                    if (pairValue != null)
                    {
                        // 開始コマンドと終了コマンドで表示を変える
                        if (item.ItemType == "Loop")
                            return $"{item.LineNumber}┬{pairValue.LineNumber}"; // Loop開始
                        else if (item.ItemType == "LoopEnd")
                            return $"{pairValue.LineNumber}┴{item.LineNumber}"; // Loop終了
                        else if (IsIfCommand(item.ItemType))
                            return $"{item.LineNumber}┬{pairValue.LineNumber}"; // If開始
                        else if (item.ItemType == "IfEnd")
                            return $"{pairValue.LineNumber}┴{item.LineNumber}"; // If終了
                        else
                            return $"{item.LineNumber}↔{pairValue.LineNumber}"; // その他のペア
                    }
                }
                
                // ペアがない場合の表示
                if (item.ItemType == "Loop" || IsIfCommand(item.ItemType))
                    return $"{item.LineNumber}┬?"; // 開始コマンドで対応する終了がない
                else if (item.ItemType == "LoopEnd" || item.ItemType == "IfEnd")
                    return $"?┴{item.LineNumber}"; // 終了コマンドで対応する開始がない
                else
                    return $"{item.LineNumber}"; // 通常のコマンド
            }
            return "";
        }

        private bool IsIfCommand(string itemType) => itemType switch
        {
            "IfImageExist" => true,
            "IfImageNotExist" => true,
            "IfImageExistAI" => true,
            "IfImageNotExistAI" => true,
            "IfVariable" => true,
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
            if (value is UniversalCommandItem item)
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