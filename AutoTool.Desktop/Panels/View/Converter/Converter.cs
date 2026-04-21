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
using Wpf.Ui.Controls;
using AutoTool.Desktop.Panels.ViewModel;

namespace AutoTool.Desktop.Panels.View.Converter;

internal static class BlockCommandToggleRule
{
    public static bool IsToggleTarget(ICommandListItem item)
    {
        return IsBlockStart(item) || IsBlockEnd(item);
    }

    public static bool IsBlockStart(ICommandListItem item)
    {
        return item is ILoopItem or IIfItem
            || (CommandMetadataCatalog.TryGetByTypeName(item.ItemType, out var metadata)
                && (metadata.IsIfCommand || metadata.IsLoopCommand));
    }

    public static bool IsBlockEnd(ICommandListItem item)
    {
        return item is ILoopEndItem or IIfEndItem or IRetryEndItem
            || (CommandMetadataCatalog.TryGetByTypeName(item.ItemType, out var metadata) && metadata.IsEndCommand);
    }
}

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && boolValue ? Visibility.Collapsed : Visibility.Visible;
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
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
public class ValidationErrorContainsItemConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not ICommandListItem item)
        {
            return false;
        }

        if (values[1] is not IEnumerable<ICommandListItem> items)
        {
            return false;
        }

        return items.Contains(item);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 有効/無効フラグを表示用の不透明度に変換します。
/// </summary>
public class EnabledToOpacityConverter : IValueConverter
{
    public double EnabledOpacity { get; set; } = 1.0;
    public double DisabledOpacity { get; set; } = 0.45;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isEnabled && isEnabled ? EnabledOpacity : DisabledOpacity;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// 親ブロックの無効化を含めた実効有効状態を不透明度に変換します。
/// </summary>
public class CommandEffectiveEnabledOpacityConverter : IMultiValueConverter
{
    public double EnabledOpacity { get; set; } = 1.0;
    public double DisabledOpacity { get; set; } = 0.45;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not ICommandListItem target || values[1] is not IEnumerable<ICommandListItem> items)
        {
            return DisabledOpacity;
        }

        return IsEffectivelyEnabled(target, items) ? EnabledOpacity : DisabledOpacity;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }

    private static bool IsEffectivelyEnabled(ICommandListItem target, IEnumerable<ICommandListItem> items)
    {
        var sortedItems = items.OrderBy(static x => x.LineNumber).ToList();
        Stack<int> disabledBlockEndLines = [];

        foreach (var item in sortedItems)
        {
            while (disabledBlockEndLines.Count > 0 && item.LineNumber > disabledBlockEndLines.Peek())
            {
                _ = disabledBlockEndLines.Pop();
            }

            var isInsideDisabledBlock = disabledBlockEndLines.Count > 0;
            var isTarget = ReferenceEquals(item, target) || item.LineNumber == target.LineNumber;
            if (isTarget)
            {
                return !isInsideDisabledBlock && item.IsEnable;
            }

            if (!isInsideDisabledBlock && !item.IsEnable && TryGetBlockEndLine(item, out var blockEndLine))
            {
                disabledBlockEndLines.Push(blockEndLine);
            }
        }

        return target.IsEnable;
    }

    private static bool TryGetBlockEndLine(ICommandListItem item, out int endLine)
    {
        endLine = item switch
        {
            IIfItem { Pair.LineNumber: > 0 } x when x.Pair!.LineNumber > x.LineNumber => x.Pair.LineNumber,
            ILoopItem { Pair.LineNumber: > 0 } x when x.Pair!.LineNumber > x.LineNumber => x.Pair.LineNumber,
            IRetryItem { Pair.LineNumber: > 0 } x when x.Pair!.LineNumber > x.LineNumber => x.Pair.LineNumber,
            _ => -1
        };

        return endLine > 0;
    }
}

/// <summary>
/// 実行中ハイライト対象行かどうかを判定します。
/// </summary>
public class CommandRunningRowHighlightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not bool isRunning || values[1] is not string itemType)
        {
            return false;
        }

        if (!isRunning)
        {
            return false;
        }

        return !IsBlockStructure(itemType);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }

    private static bool IsBlockStructure(string itemType)
    {
        if (CommandMetadataCatalog.TryGetByTypeName(itemType, out var metadata))
        {
            return metadata.IsIfCommand || metadata.IsLoopCommand || metadata.IsEndCommand;
        }

        return false;
    }
}

/// <summary>
/// 実行中バッジの表示テキストを生成します。
/// </summary>
public class CommandRunningBadgeTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3 || values[0] is not ICommandListItem item || values[1] is not bool isRunning)
        {
            return string.Empty;
        }

        // values[2] (Progress) を受け取り、進捗更新時に再評価されるようにします。
        _ = values[2];

        if (!isRunning)
        {
            return string.Empty;
        }

        return item switch
        {
            ILoopItem => "LOOP 実行中",
            IIfItem => "IF 実行中",
            IRetryItem => "RETRY 実行中",
            _ => string.Empty
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
public class NestLevelToMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is int nestLevel ? new Thickness(Math.Max(nestLevel, 0) * 16, 0, 0, 0) : new Thickness(0);
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
/// コマンド行の種類も考慮してインデントを算出します。
/// </summary>
public class CommandIndentMarginConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 1 || values[0] is not ICommandListItem item)
        {
            return new Thickness(0);
        }

        var commandItems = values.Length > 1 && values[1] is IEnumerable<ICommandListItem> items
            ? items
            : values.Length > 2 && values[2] is IEnumerable<ICommandListItem> moreItems
                ? moreItems
                : null;

        var nestLevel = commandItems is null ? item.NestLevel : CalculateDisplayNestLevel(item, commandItems);

        var indent = Math.Max(nestLevel, 0) * 20;

        return new Thickness(indent, 0, 0, 0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }

    private static int CalculateDisplayNestLevel(ICommandListItem target, IEnumerable<ICommandListItem> items)
    {
        var depth = 0;

        foreach (var command in items)
        {
            var isEnd = IsBlockEndCommandType(command.ItemType);
            if (isEnd)
            {
                depth = Math.Max(0, depth - 1);

                if (ReferenceEquals(command, target) || command.LineNumber == target.LineNumber)
                {
                    return depth;
                }

                continue;
            }

            if (ReferenceEquals(command, target) || command.LineNumber == target.LineNumber)
            {
                return depth;
            }

            if (IsBlockStartCommandType(command.ItemType))
            {
                depth++;
            }
        }

        var fallback = items.FirstOrDefault(x => x is not null && x.LineNumber == target.LineNumber);
        if (fallback is not null)
        {
            return Math.Max(fallback.NestLevel, 0);
        }

        return Math.Max(target.NestLevel, 0);
    }

    private static bool IsBlockStartCommandType(string itemType)
    {
        return itemType is
            CommandTypeNames.Loop or
            CommandTypeNames.IfImageExist or
            CommandTypeNames.IfImageNotExist or
            CommandTypeNames.IfTextExist or
            CommandTypeNames.IfTextNotExist or
            CommandTypeNames.IfImageExistAI or
            CommandTypeNames.IfImageNotExistAI or
            CommandTypeNames.IfVariable or
            CommandTypeNames.Retry;
    }

    private static bool IsBlockEndCommandType(string itemType)
    {
        return itemType is
            CommandTypeNames.LoopEnd or
            CommandTypeNames.IfEnd or
            CommandTypeNames.RetryEnd;
    }

}

/// <summary>
/// 開始行のトグルボタン分の左マージンを返します。
/// </summary>
public class CommandToggleLeadingMarginConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var item = values.Length > 0 ? values[0] as ICommandListItem : null;
        if (item is null)
        {
            return new Thickness(0);
        }

        if (BlockCommandToggleRule.IsToggleTarget(item))
        {
            return new Thickness(24, 0, 0, 0);
        }

        return new Thickness(0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
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
/// 設定内容の説明文を改行表示向けに変換します。
/// </summary>
public class CommandDescriptionToMultilineConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text.Replace(" / ", Environment.NewLine);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// コメント優先で説明テキストを返し、コメント未設定時は代表的な設定例を返します。
/// </summary>
public class CommandCommentOrExampleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ICommandListItem item)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(item.Comment))
        {
            return item.Comment.Trim();
        }

        return BuildExampleText(item);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }

    private static string BuildExampleText(ICommandListItem item)
    {
        return item.ItemType switch
        {
            CommandTypeNames.Click when item is IClickItem clickItem =>
                $"({clickItem.X}, {clickItem.Y}) を{ToMouseButtonText(clickItem.Button)}",

            CommandTypeNames.ClickImage when item is IClickImageItem clickImageItem =>
                $"{GetFileNameOrPlaceholder(clickImageItem.ImagePath, "target.png")} が見つかった位置を{ToMouseButtonText(clickImageItem.Button)} (閾値 {clickImageItem.Threshold:0.00})",

            CommandTypeNames.ClickImageAI when item is IClickImageAIItem clickImageAiItem =>
                $"ClassID={clickImageAiItem.ClassID} を検出して{ToMouseButtonText(clickImageAiItem.Button)} (信頼度 {clickImageAiItem.ConfThreshold:0.00})",

            CommandTypeNames.Hotkey when item is IHotkeyItem hotkeyItem =>
                $"{FormatHotkey(hotkeyItem)} を送信",

            CommandTypeNames.Wait when item is IWaitItem waitItem =>
                $"{FormatDuration(waitItem.Wait)} 待機",

            CommandTypeNames.WaitImage when item is IWaitImageItem waitImageItem =>
                $"{GetFileNameOrPlaceholder(waitImageItem.ImagePath, "target.png")} が表示されるまで待機 (最大{FormatDuration(waitImageItem.Timeout)})",

            CommandTypeNames.WaitImageDisappear when item is IWaitImageItem waitImageDisappearItem =>
                $"{GetFileNameOrPlaceholder(waitImageDisappearItem.ImagePath, "loading.png")} が消えるまで待機 (最大{FormatDuration(waitImageDisappearItem.Timeout)})",

            CommandTypeNames.Execute when item is IExecuteItem executeItem =>
                $"{GetFileNameOrPlaceholder(executeItem.ProgramPath, "app.exe")} を起動",

            CommandTypeNames.Screenshot when item is IScreenshotItem screenshotItem =>
                $"アクティブ画面を {GetDirectoryOrPlaceholder(screenshotItem.SaveDirectory, @"C:\AutoTool\Screenshots")} に保存",

            CommandTypeNames.SetVariable when item is ISetVariableItem setVariableItem =>
                $"変数 {GetTextOrPlaceholder(setVariableItem.Name, "name")} に \"{GetTextOrPlaceholder(setVariableItem.Value, "value")}\" を設定",

            CommandTypeNames.SetVariableAI when item is ISetVariableAIItem setVariableAiItem =>
                $"検出結果の座標を変数 {GetTextOrPlaceholder(setVariableAiItem.Name, "resultVar")} に設定",

            CommandTypeNames.SetVariableOCR when item is ISetVariableOCRItem setVariableOcrItem =>
                $"OCR結果を変数 {GetTextOrPlaceholder(setVariableOcrItem.Name, "textVar")} に設定",

            CommandTypeNames.FindImage when item is IFindImageItem findImageItem =>
                $"検索結果を {GetTextOrPlaceholder(findImageItem.FoundVariableName, "found")} / {GetTextOrPlaceholder(findImageItem.XVariableName, "posX")} / {GetTextOrPlaceholder(findImageItem.YVariableName, "posY")} に設定",

            CommandTypeNames.FindText when item is IFindTextItem findTextItem =>
                $"検索結果を {GetTextOrPlaceholder(findTextItem.FoundVariableName, "textFound")} / {GetTextOrPlaceholder(findTextItem.TextVariableName, "textValue")} / {GetTextOrPlaceholder(findTextItem.ConfidenceVariableName, "confidence")} に設定",

            CommandTypeNames.IfImageExist when item is IIfImageExistItem ifImageExistItem =>
                $"if {GetFileNameOrPlaceholder(ifImageExistItem.ImagePath, "target.png")} が存在するなら実行",

            CommandTypeNames.IfImageNotExist when item is IIfImageNotExistItem ifImageNotExistItem =>
                $"if {GetFileNameOrPlaceholder(ifImageNotExistItem.ImagePath, "target.png")} が存在しないなら実行",

            CommandTypeNames.IfTextExist when item is IIfTextExistItem ifTextExistItem =>
                $"if \"{GetTextOrPlaceholder(ifTextExistItem.TargetText, "キーワード")}\" が含まれるなら実行",

            CommandTypeNames.IfTextNotExist when item is IIfTextNotExistItem ifTextNotExistItem =>
                $"if \"{GetTextOrPlaceholder(ifTextNotExistItem.TargetText, "キーワード")}\" が見つからないなら実行",

            CommandTypeNames.IfImageExistAI when item is IIfImageExistAIItem ifImageExistAiItem =>
                $"if ClassID={ifImageExistAiItem.ClassID} が検出されたら実行",

            CommandTypeNames.IfImageNotExistAI when item is IIfImageNotExistAIItem ifImageNotExistAiItem =>
                $"if ClassID={ifImageNotExistAiItem.ClassID} が未検出なら実行",

            CommandTypeNames.IfVariable when item is IIfVariableItem ifVariableItem =>
                $"if {GetTextOrPlaceholder(ifVariableItem.Name, "value")} {GetTextOrPlaceholder(ifVariableItem.Operator, "==")} \"{GetTextOrPlaceholder(ifVariableItem.Value, "expected")}\" なら実行",

            CommandTypeNames.IfEnd =>
                "if ブロック終了",

            CommandTypeNames.Loop when item is ILoopItem loopItem =>
                $"{loopItem.LoopCount}回 ループ",

            CommandTypeNames.LoopBreak =>
                "ループを中断",

            CommandTypeNames.LoopEnd =>
                "ループ終了",

            CommandTypeNames.Retry when item is IRetryItem retryItem =>
                $"最大{retryItem.RetryCount}回 リトライ (間隔{retryItem.RetryInterval}ms)",

            CommandTypeNames.RetryEnd =>
                "リトライブロック終了",

            _ => string.IsNullOrWhiteSpace(item.Description)
                ? $"{ResolveDisplayName(item.ItemType)} の設定例"
                : item.Description
        };
    }

    private static string FormatDuration(int milliseconds)
    {
        if (milliseconds <= 0)
        {
            return "0秒";
        }

        if (milliseconds % 60000 == 0)
        {
            return $"{milliseconds / 60000}分";
        }

        if (milliseconds % 1000 == 0)
        {
            return $"{milliseconds / 1000}秒";
        }

        return $"{milliseconds}ms";
    }

    private static string ToMouseButtonText(CommandMouseButton button)
    {
        return button switch
        {
            CommandMouseButton.Left => "左クリック",
            CommandMouseButton.Right => "右クリック",
            CommandMouseButton.Middle => "中クリック",
            _ => "クリック"
        };
    }

    private static string FormatHotkey(IHotkeyItem hotkeyItem)
    {
        List<string> keys = [];
        if (hotkeyItem.Ctrl) keys.Add("Ctrl");
        if (hotkeyItem.Alt) keys.Add("Alt");
        if (hotkeyItem.Shift) keys.Add("Shift");
        keys.Add(hotkeyItem.Key.ToString());
        return string.Join(" + ", keys);
    }

    private static string GetTextOrPlaceholder(string value, string placeholder)
    {
        return string.IsNullOrWhiteSpace(value) ? placeholder : value.Trim();
    }

    private static string GetFileNameOrPlaceholder(string path, string placeholder)
    {
        return string.IsNullOrWhiteSpace(path) ? placeholder : System.IO.Path.GetFileName(path);
    }

    private static string GetDirectoryOrPlaceholder(string path, string placeholder)
    {
        return string.IsNullOrWhiteSpace(path) ? placeholder : path.Trim();
    }

    private static string ResolveDisplayName(string itemType)
    {
        return CommandMetadataCatalog.TryGetByTypeName(itemType, out var metadata)
            ? metadata.DisplayNameJa
            : itemType;
    }
}

/// <summary>
/// コマンドタイプを短縮表示名に変換します。
/// </summary>
public class CommandTypeToCompactNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string commandType || string.IsNullOrWhiteSpace(commandType))
        {
            return string.Empty;
        }

        return commandType switch
        {
            CommandTypeNames.Click => "クリック",
            CommandTypeNames.ClickImage => "画像クリック",
            CommandTypeNames.FindImage => "画像検索",
            CommandTypeNames.FindText => "文字検索",
            CommandTypeNames.ClickImageAI => "画像クリックAI",
            CommandTypeNames.Hotkey => "ホットキー",
            CommandTypeNames.Wait => "待機",
            CommandTypeNames.WaitImage => "画像待機",
            CommandTypeNames.WaitImageDisappear => "画像消失待機",
            CommandTypeNames.Execute => "実行",
            CommandTypeNames.Screenshot => "スクショ",
            CommandTypeNames.Loop => "ループ開始",
            CommandTypeNames.LoopEnd => "ループ終了",
            CommandTypeNames.LoopBreak => "ループ中断",
            CommandTypeNames.Retry => "リトライ",
            CommandTypeNames.RetryEnd => "リトライ終了",
            CommandTypeNames.IfImageExist => "画像ありなら実行",
            CommandTypeNames.IfImageNotExist => "画像なしなら実行",
            CommandTypeNames.IfTextExist => "文字ありなら実行",
            CommandTypeNames.IfTextNotExist => "文字なしなら実行",
            CommandTypeNames.IfImageExistAI => "AI画像ありなら実行",
            CommandTypeNames.IfImageNotExistAI => "AI画像なしなら実行",
            CommandTypeNames.IfVariable => "変数条件",
            CommandTypeNames.IfEnd => "条件終了",
            CommandTypeNames.SetVariable => "変数設定",
            CommandTypeNames.SetVariableAI => "変数AI",
            CommandTypeNames.SetVariableOCR => "変数OCR",
            _ => CommandMetadataCatalog.TryGetByTypeName(commandType, out var metadata)
                ? metadata.DisplayNameJa
                : commandType
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// コマンドタイプを Fluent System Icons に対応するアイコンに変換します。
/// </summary>
public class CommandTypeToFluentIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string commandType || string.IsNullOrWhiteSpace(commandType))
        {
            return SymbolRegular.DocumentAdd24;
        }

        return commandType switch
        {
            CommandTypeNames.Hotkey => SymbolRegular.Keyboard24,
            CommandTypeNames.Screenshot => SymbolRegular.Screenshot24,
            CommandTypeNames.Loop => SymbolRegular.ArrowRepeatAll24,
            CommandTypeNames.LoopEnd => SymbolRegular.ArrowRepeatAll24,
            CommandTypeNames.LoopBreak => SymbolRegular.Dismiss24,
            CommandTypeNames.Retry => SymbolRegular.ArrowUp24,
            CommandTypeNames.RetryEnd => SymbolRegular.ArrowDown24,
            CommandTypeNames.IfImageExist => SymbolRegular.Merge24,
            CommandTypeNames.IfImageNotExist => SymbolRegular.Merge24,
            CommandTypeNames.IfTextExist => SymbolRegular.Merge24,
            CommandTypeNames.IfTextNotExist => SymbolRegular.Merge24,
            CommandTypeNames.IfImageExistAI => SymbolRegular.Merge24,
            CommandTypeNames.IfImageNotExistAI => SymbolRegular.Merge24,
            CommandTypeNames.IfVariable => SymbolRegular.Merge24,
            CommandTypeNames.IfEnd => SymbolRegular.Merge24,
            CommandTypeNames.SetVariable => SymbolRegular.DocumentAdd24,
            CommandTypeNames.SetVariableAI => SymbolRegular.DocumentAdd24,
            CommandTypeNames.SetVariableOCR => SymbolRegular.DocumentAdd24,
            _ => CommandMetadataCatalog.TryGetByTypeName(commandType, out var metadata)
                ? metadata.DisplayPriority switch
                {
                    1 => SymbolRegular.CursorClick24,
                    2 => SymbolRegular.CursorClick24,
                    3 => SymbolRegular.ArrowUp24,
                    4 => SymbolRegular.ArrowDown24,
                    5 => SymbolRegular.DocumentAdd24,
                    6 => SymbolRegular.Settings24,
                    _ => SymbolRegular.CursorClick24
                }
                : SymbolRegular.CursorClick24
        };
    }

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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// 値を画面表示やバインディング向けの形式へ変換します。
/// </summary>
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

/// <summary>
/// コマンドが折りたたみ中に非表示対象かどうかを判定します。
/// </summary>
public class CommandRowCollapsedConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3 || values[0] is not ICommandListItem item || values[1] is not IListPanelViewModel viewModel)
        {
            return false;
        }

        return viewModel.ShouldHideCommandInCollapsedScope(item);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

/// <summary>
/// 開始/終了コマンド行のトグルボタン可視性を判定します。
/// </summary>
public class CommandBlockToggleVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var item = values.Length > 0 ? values[0] as ICommandListItem : null;
        if (item is null || values.Length < 3 || values[1] is not IListPanelViewModel viewModel)
        {
            return item is null ? Visibility.Collapsed : BlockCommandToggleRule.IsToggleTarget(item) ? Visibility.Visible : Visibility.Collapsed;
        }

        return viewModel is not null && (BlockCommandToggleRule.IsBlockStart(item) || BlockCommandToggleRule.IsBlockEnd(item))
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

/// <summary>
/// 開始/終了コマンド行の折りたたみ状態をアイコン文字列に変換します。
/// </summary>
public class CommandBlockToggleGlyphConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var item = values.Length > 0 ? values[0] as ICommandListItem : null;
        if (item is null || values.Length < 3 || values[1] is not IListPanelViewModel viewModel)
        {
            if (item is null)
            {
                return string.Empty;
            }

            return BlockCommandToggleRule.IsBlockEnd(item) ? "▴" : "▾";
        }

        if (BlockCommandToggleRule.IsBlockStart(item) || BlockCommandToggleRule.IsBlockEnd(item))
        {
            return BlockCommandToggleRule.IsBlockEnd(item) ? "▴" : viewModel.IsBlockCollapsed(item) ? "▸" : "▾";
        }

        return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [];
    }
}

/// <summary>
/// 型情報や依存関係を解決し、実行に必要なインスタンスを返します。
/// </summary>
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




