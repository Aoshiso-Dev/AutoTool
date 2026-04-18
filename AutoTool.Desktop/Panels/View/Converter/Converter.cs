using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Commands.Model.Input;

namespace AutoTool.Desktop.Panels.View.Converter;

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class BoolToVisibilityMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.OfType<bool>().Any(b => b) ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

public class NumberToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is int intValue && intValue > 0;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class BooleanToTextConverter : IValueConverter
{
    public string TrueText { get; set; } = "有効";
    public string FalseText { get; set; } = "無効";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && boolValue ? TrueText : FalseText;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string str && !string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class KeyToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is CommandKey key ? key.ToString() : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && Enum.TryParse<CommandKey>(str, out var key))
            return key;
        return CommandKey.None;
    }
}

public class NestLevelToMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is int nestLevel ? new Thickness(nestLevel * 20, 0, 0, 0) : new Thickness(0);
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// コマンドタイプを日本語表示名に変換するコンバーター
/// </summary>
public class CommandTypeToDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string commandType && !string.IsNullOrEmpty(commandType))
        {
            return CommandMetadataCatalog.TryGetByTypeName(commandType, out var metadata)
                ? metadata.DisplayNameJa
                : commandType;
        }
        return value ?? string.Empty;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// コマンドタイプからカテゴリ色を取得するコンバーター
/// </summary>
public class CommandTypeToCategoryColorConverter : IValueConverter
{
    private static readonly SolidColorBrush SkyBlue = new(Color.FromRgb(135, 206, 235));
    private static readonly SolidColorBrush LightGreen = new(Color.FromRgb(152, 251, 152));
    private static readonly SolidColorBrush PeachPuff = new(Color.FromRgb(255, 218, 185));
    private static readonly SolidColorBrush Plum = new(Color.FromRgb(221, 160, 221));
    private static readonly SolidColorBrush Bisque = new(Color.FromRgb(255, 228, 196));
    private static readonly SolidColorBrush Silver = new(Color.FromRgb(192, 192, 192));
    private static readonly SolidColorBrush White = new(Color.FromRgb(255, 255, 255));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string commandType && !string.IsNullOrEmpty(commandType))
        {
            var priority = CommandMetadataCatalog.TryGetByTypeName(commandType, out var metadata)
                ? metadata.DisplayPriority
                : 9;
            return priority switch
            {
                1 => SkyBlue,      // クリック操作
                2 => LightGreen,   // 基本操作
                3 => PeachPuff,    // ループ制御
                4 => Plum,         // 条件分岐
                5 => Bisque,       // 変数操作
                6 => Silver,       // システム操作
                _ => White         // その他
            };
        }
        return White;
    }

    /// <summary>
    /// 一方向バインディング用のため未実装
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// プログレス値(0-100)を指定された最大幅にスケーリングするコンバーター
/// </summary>
public class ProgressToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int progress && parameter is double maxWidth)
        {
            return maxWidth * progress / 100.0;
        }
        if (value is int progressInt)
        {
            // パラメータがない場合はデフォルト50px
            var max = 50.0;
            if (parameter is string paramStr && double.TryParse(paramStr, out var parsed))
            {
                max = parsed;
            }
            return max * progressInt / 100.0;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class OcrRegionNumberEditorVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not AutoTool.Automation.Runtime.Attributes.PropertyMetadata metadata)
        {
            return Visibility.Visible;
        }

        if (metadata.Target.GetType().Name != "SetVariableOCRItem")
        {
            return Visibility.Visible;
        }

        return metadata.PropertyInfo.Name is "Y" or "Width" or "Height"
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class OcrRegionNumberEditorVisibilityMultiConverter : IMultiValueConverter
{
    private static readonly HashSet<string> OcrPointPickerItemTypes = new(StringComparer.Ordinal)
    {
        "SetVariable_OCR",
        "Find_Text",
        "IF_TextExist",
        "IF_TextNotExist"
    };

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var itemType = values.Length > 0 ? values[0]?.ToString() : string.Empty;
        var propertyName = values.Length > 1 ? values[1]?.ToString() : string.Empty;

        if (itemType is not null &&
            OcrPointPickerItemTypes.Contains(itemType) &&
            (string.Equals(propertyName, "Y", StringComparison.Ordinal) ||
             string.Equals(propertyName, "Width", StringComparison.Ordinal) ||
             string.Equals(propertyName, "Height", StringComparison.Ordinal)))
        {
            return Visibility.Collapsed;
        }

        return Visibility.Visible;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class CommandPairBadgeVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return GetPairLineNumber(value) > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }

    private static int GetPairLineNumber(object? value) => value switch
    {
        IIfItem { Pair.LineNumber: > 0 } x => x.Pair!.LineNumber,
        IIfEndItem { Pair.LineNumber: > 0 } x => x.Pair!.LineNumber,
        ILoopItem { Pair.LineNumber: > 0 } x => x.Pair!.LineNumber,
        ILoopEndItem { Pair.LineNumber: > 0 } x => x.Pair!.LineNumber,
        _ => 0
    };
}

public class CommandPairBadgeTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var (label, lineNumber) = value switch
        {
            ILoopItem { Pair.LineNumber: > 0 } x => ("LOOP", x.Pair!.LineNumber),
            ILoopEndItem { Pair.LineNumber: > 0 } x => ("LOOP", x.Pair!.LineNumber),
            IIfItem { Pair.LineNumber: > 0 } x => ("IF", x.Pair!.LineNumber),
            IIfEndItem { Pair.LineNumber: > 0 } x => ("IF", x.Pair!.LineNumber),
            _ => (string.Empty, 0)
        };

        return lineNumber > 0 ? $"{label} #{lineNumber}" : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class CommandPairGuideBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush LoopBrush = new(Color.FromRgb(37, 99, 235));
    private static readonly SolidColorBrush IfBrush = new(Color.FromRgb(22, 163, 74));
    private static readonly SolidColorBrush UnknownBrush = new(Color.FromRgb(156, 163, 175));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            ILoopItem or ILoopEndItem => LoopBrush,
            IIfItem or IIfEndItem => IfBrush,
            _ => UnknownBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class CommandPairGuideThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            ILoopItem or ILoopEndItem => 3.0,
            IIfItem or IIfEndItem => 2.0,
            _ => 1.0
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class CommandPairGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            ILoopItem or IIfItem => "┌",
            ILoopEndItem or IIfEndItem => "└",
            _ => "│"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class CommandPairOrderBadgeMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var item = values.Length > 0 ? values[0] as ICommandListItem : null;
        var items = values.Length > 1 ? values[1] as IEnumerable<ICommandListItem> : null;
        if (item is null || items is null)
        {
            return string.Empty;
        }

        if (!TryGetPairKindAndStartLine(item, out var kind, out var startLine))
        {
            return string.Empty;
        }

        var starts = kind switch
        {
            "LOOP" => items
                .OfType<ILoopItem>()
                .Where(x => x.Pair?.LineNumber > x.LineNumber)
                .OrderBy(x => x.LineNumber)
                .ToList<ICommandListItem>(),
            "IF" => items
                .OfType<IIfItem>()
                .Where(x => x.Pair?.LineNumber > x.LineNumber)
                .OrderBy(x => x.LineNumber)
                .ToList<ICommandListItem>(),
            _ => []
        };

        if (starts.Count == 0)
        {
            return string.Empty;
        }

        var index = starts.FindIndex(x => x.LineNumber == startLine);
        if (index < 0)
        {
            return string.Empty;
        }

        var shortPrefix = kind switch
        {
            "LOOP" => "L",
            "IF" => "I",
            _ => string.Empty
        };

        return string.IsNullOrEmpty(shortPrefix) ? string.Empty : $"{shortPrefix}{index + 1}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }

    private static bool TryGetPairKindAndStartLine(ICommandListItem item, out string kind, out int startLine)
    {
        switch (item)
        {
            case ILoopItem { Pair.LineNumber: > 0 } loopStart:
                kind = "LOOP";
                startLine = loopStart.LineNumber;
                return true;
            case ILoopEndItem { Pair.LineNumber: > 0 } loopEnd:
                kind = "LOOP";
                startLine = loopEnd.Pair!.LineNumber;
                return true;
            case IIfItem { Pair.LineNumber: > 0 } ifStart:
                kind = "IF";
                startLine = ifStart.LineNumber;
                return true;
            case IIfEndItem { Pair.LineNumber: > 0 } ifEnd:
                kind = "IF";
                startLine = ifEnd.Pair!.LineNumber;
                return true;
            default:
                kind = string.Empty;
                startLine = 0;
                return false;
        }
    }
}

public class LoopGuideVisibilityMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var item = values.Length > 0 ? values[0] as ICommandListItem : null;
        var items = values.Length > 1 ? values[1] as IEnumerable<ICommandListItem> : null;
        if (item is null || items is null)
        {
            return Visibility.Collapsed;
        }

        return CommandGuideSpanResolver.TryGetActiveLoopSpan(item.LineNumber, items, out _, out _)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

public class IfGuideVisibilityMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var item = values.Length > 0 ? values[0] as ICommandListItem : null;
        var items = values.Length > 1 ? values[1] as IEnumerable<ICommandListItem> : null;
        if (item is null || items is null)
        {
            return Visibility.Collapsed;
        }

        return CommandGuideSpanResolver.TryGetActiveIfSpan(item.LineNumber, items, out _, out _)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

public class LoopGuideMarginMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var item = values.Length > 0 ? values[0] as ICommandListItem : null;
        var items = values.Length > 1 ? values[1] as IEnumerable<ICommandListItem> : null;
        if (item is null || items is null)
        {
            return new Thickness(0);
        }

        return CommandGuideSpanResolver.TryGetActiveLoopSpan(item.LineNumber, items, out var nestLevel, out _)
            ? new Thickness(6 + (nestLevel * 16), 0, 0, 0)
            : new Thickness(0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

public class IfGuideMarginMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var item = values.Length > 0 ? values[0] as ICommandListItem : null;
        var items = values.Length > 1 ? values[1] as IEnumerable<ICommandListItem> : null;
        if (item is null || items is null)
        {
            return new Thickness(0);
        }

        return CommandGuideSpanResolver.TryGetActiveIfSpan(item.LineNumber, items, out var nestLevel, out _)
            ? new Thickness(12 + (nestLevel * 16), 0, 0, 0)
            : new Thickness(0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

public class SelectedAndNonEmptyToVisibilityMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var isSelected = values.Length > 0 && values[0] is bool b && b;
        var text = values.Length > 1 ? values[1]?.ToString() : string.Empty;
        return isSelected && !string.IsNullOrWhiteSpace(text) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

public class LoopGuideGlyphMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var item = values.Length > 0 ? values[0] as ICommandListItem : null;
        var items = values.Length > 1 ? values[1] as IEnumerable<ICommandListItem> : null;
        if (item is null || items is null)
        {
            return string.Empty;
        }

        if (!CommandGuideSpanResolver.TryGetActiveLoopSpan(item.LineNumber, items, out _, out var span))
        {
            return string.Empty;
        }

        return item.LineNumber == span.startLine
            ? "┌"
            : item.LineNumber == span.endLine
                ? "└"
                : "│";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

public class IfGuideGlyphMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var item = values.Length > 0 ? values[0] as ICommandListItem : null;
        var items = values.Length > 1 ? values[1] as IEnumerable<ICommandListItem> : null;
        if (item is null || items is null)
        {
            return string.Empty;
        }

        if (!CommandGuideSpanResolver.TryGetActiveIfSpan(item.LineNumber, items, out _, out var span))
        {
            return string.Empty;
        }

        return item.LineNumber == span.startLine
            ? "┌"
            : item.LineNumber == span.endLine
                ? "└"
                : "│";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

file static class CommandGuideSpanResolver
{
    public static bool TryGetActiveLoopSpan(int lineNumber, IEnumerable<ICommandListItem> items, out int nestLevel, out (int startLine, int endLine) span)
    {
        var candidate = items
            .OfType<ILoopItem>()
            .Where(x => x.Pair?.LineNumber > x.LineNumber)
            .Select(x => new { x.NestLevel, Start = x.LineNumber, End = x.Pair!.LineNumber })
            .Where(x => lineNumber >= x.Start && lineNumber <= x.End)
            .OrderByDescending(x => x.NestLevel)
            .ThenByDescending(x => x.Start)
            .FirstOrDefault();

        if (candidate is null)
        {
            nestLevel = 0;
            span = default;
            return false;
        }

        nestLevel = candidate.NestLevel;
        span = (candidate.Start, candidate.End);
        return true;
    }

    public static bool TryGetActiveIfSpan(int lineNumber, IEnumerable<ICommandListItem> items, out int nestLevel, out (int startLine, int endLine) span)
    {
        var candidate = items
            .OfType<IIfItem>()
            .Where(x => x.Pair?.LineNumber > x.LineNumber)
            .Select(x => new { x.NestLevel, Start = x.LineNumber, End = x.Pair!.LineNumber })
            .Where(x => lineNumber >= x.Start && lineNumber <= x.End)
            .OrderByDescending(x => x.NestLevel)
            .ThenByDescending(x => x.Start)
            .FirstOrDefault();

        if (candidate is null)
        {
            nestLevel = 0;
            span = default;
            return false;
        }

        nestLevel = candidate.NestLevel;
        span = (candidate.Start, candidate.End);
        return true;
    }
}




