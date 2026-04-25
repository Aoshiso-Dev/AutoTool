using AutoTool.Commands.Model.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Commands;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Attributes;
using CommandDef = AutoTool.Automation.Runtime.Definitions;

namespace AutoTool.Automation.Runtime.Lists;

internal static class ImageCommandExecutionHelper
{
    public static Task<FindImageResult> FindImageAsync(
        ICommandExecutionContext context,
        string imagePath,
        double threshold,
        CommandColor? searchColor,
        int timeout,
        int interval,
        string windowTitle,
        string windowClassName,
        CancellationToken cancellationToken)
    {
        var absolutePath = context.ToAbsolutePath(imagePath);
        return FindImageExecutor.ExecuteAsync(
            new FindImageOptions
            {
                ImagePath = absolutePath,
                Threshold = threshold,
                SearchColor = searchColor,
                Timeout = timeout,
                Interval = interval,
                WindowTitle = windowTitle,
                WindowClassName = windowClassName
            },
            context.SearchImageAsync,
            context.ReportProgress,
            cancellationToken);
    }
}

[CommandDef.CommandDefinition(CommandDef.CommandTypeNames.FindImage, typeof(SimpleCommand), typeof(IFindImageCommandSettings), CommandDef.CommandCategory.Variable, displayPriority: 6, displaySubPriority: 3, displayNameJa: "画像検索", displayNameEn: "Image Search")]
/// <summary>
/// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
/// </summary>
public partial class FindImageItem : CommandListItem, IFindImageItem, IFindImageCommandSettings
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("検索画像", EditorType.ImagePicker, Group = "画像設定", Order = 1,
                     Description = "検索する画像ファイル", FileFilter = "画像ファイル|*.png;*.jpg;*.bmp")]
    private string _imagePath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("一致しきい値", EditorType.Slider, Group = "画像設定", Order = 2,
                     Description = "画像一致度の最小値", Min = 0.01, Max = 1.0, Step = 0.01)]
    private double _threshold = 0.8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("強調検索色", EditorType.ColorPicker, Group = "画像設定", Order = 3,
                     Description = "特定の色を強調して検索")]
    private CommandColor? _searchColor = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("タイムアウト", EditorType.NumberBox, Group = "タイミング", Order = 1,
                     Description = "検索を諦めるまでの時間", Unit = "ミリ秒", Min = 0)]
    private int _timeout = 5000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("検索間隔", EditorType.NumberBox, Group = "タイミング", Order = 2,
                     Description = "画像検索の間隔", Unit = "ミリ秒", Min = 0)]
    private int _interval = 500;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("未検出で失敗", EditorType.CheckBox, Group = "実行設定", Order = 1,
                     Description = "オフの場合は見つからなくても次へ進む")]
    private bool _strict = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("結果変数(found)", EditorType.TextBox, Group = "変数出力", Order = 1,
                     Description = "true/false を保存する変数名（空欄で保存しない）")]
    private string _foundVariableName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("結果変数(x)", EditorType.TextBox, Group = "変数出力", Order = 2,
                     Description = "X座標を保存する変数名（空欄で保存しない）")]
    private string _xVariableName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("結果変数(y)", EditorType.TextBox, Group = "変数出力", Order = 3,
                     Description = "Y座標を保存する変数名（空欄で保存しない）")]
    private string _yVariableName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1,
                     Description = "操作対象のウィンドウタイトル（空欄で全画面）")]
    private string _windowTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2,
                     Description = "ウィンドウのクラス名")]
    private string _windowClassName = string.Empty;

    new public string Description => $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms / 厳密失敗:{Strict}";

    public FindImageItem() { }

    public FindImageItem(FindImageItem? item = null) : base(item)
    {
        if (item is null)
        {
            return;
        }

        ImagePath = item.ImagePath;
        Threshold = item.Threshold;
        SearchColor = item.SearchColor;
        Timeout = item.Timeout;
        Interval = item.Interval;
        Strict = item.Strict;
        FoundVariableName = item.FoundVariableName;
        XVariableName = item.XVariableName;
        YVariableName = item.YVariableName;
        WindowTitle = item.WindowTitle;
        WindowClassName = item.WindowClassName;
    }

    public new ICommandListItem Clone()
    {
        return new FindImageItem(this);
    }

    public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await ImageCommandExecutionHelper.FindImageAsync(
            context,
            ImagePath,
            Threshold,
            SearchColor,
            Timeout,
            Interval,
            WindowTitle,
            WindowClassName,
            cancellationToken).ConfigureAwait(false);

        SetResultVariables(context, result);

        if (result.Found)
        {
            context.Log($"画像が見つかりました。({result.Point!.Value.X}, {result.Point!.Value.Y}) / 経過時間={result.ElapsedMilliseconds}ms");
            return true;
        }

        context.Log($"画像が見つかりませんでした。経過時間={result.ElapsedMilliseconds}ms");
        return !Strict;
    }

    private void SetResultVariables(ICommandExecutionContext context, FindImageResult result)
    {
        if (!string.IsNullOrWhiteSpace(FoundVariableName))
        {
            context.SetVariable(FoundVariableName, result.Found ? "true" : "false");
        }

        if (result.Found && result.Point is not null)
        {
            if (!string.IsNullOrWhiteSpace(XVariableName))
            {
                context.SetVariable(XVariableName, result.Point.Value.X.ToString());
            }

            if (!string.IsNullOrWhiteSpace(YVariableName))
            {
                context.SetVariable(YVariableName, result.Point.Value.Y.ToString());
            }
        }
    }
}

[CommandDef.CommandDefinition(CommandDef.CommandTypeNames.WaitImage, typeof(SimpleCommand), typeof(IWaitImageCommandSettings), CommandDef.CommandCategory.Wait, displayPriority: 3, displaySubPriority: 2, displayNameJa: "画像待機", displayNameEn: "Wait for Image")]
/// <summary>
/// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
/// </summary>
public partial class WaitImageItem : CommandListItem, IWaitImageItem, IWaitImageCommandSettings
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("検索画像", EditorType.ImagePicker, Group = "画像設定", Order = 1,
                     Description = "検索する画像ファイル", FileFilter = "画像ファイル|*.png;*.jpg;*.bmp")]
    private string _imagePath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("一致しきい値", EditorType.Slider, Group = "画像設定", Order = 2,
                     Description = "画像一致度の最小値", Min = 0.01, Max = 1.0, Step = 0.01)]
    private double _threshold = 0.8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("強調検索色", EditorType.ColorPicker, Group = "画像設定", Order = 3,
                     Description = "特定の色を強調して検索")]
    private CommandColor? _searchColor = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("タイムアウト", EditorType.NumberBox, Group = "タイミング", Order = 1,
                     Description = "検索を諦めるまでの時間", Unit = "ミリ秒", Min = 0)]
    private int _timeout = 5000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("検索間隔", EditorType.NumberBox, Group = "タイミング", Order = 2,
                     Description = "画像検索の間隔", Unit = "ミリ秒", Min = 0)]
    private int _interval = 500;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1,
                     Description = "操作対象のウィンドウタイトル（空欄で全画面）")]
    private string _windowTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2,
                     Description = "ウィンドウのクラス名")]
    private string _windowClassName = string.Empty;

    new public string Description => $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";

    public WaitImageItem() { }

    public WaitImageItem(WaitImageItem? item = null) : base(item)
    {
        if (item is null)
        {
            return;
        }

        ImagePath = item.ImagePath;
        Threshold = item.Threshold;
        SearchColor = item.SearchColor;
        Timeout = item.Timeout;
        Interval = item.Interval;
        WindowTitle = item.WindowTitle;
        WindowClassName = item.WindowClassName;
    }

    public new ICommandListItem Clone()
    {
        return new WaitImageItem(this);
    }

    public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
    {
        var result = await ImageCommandExecutionHelper.FindImageAsync(
            context,
            ImagePath,
            Threshold,
            SearchColor,
            Timeout,
            Interval,
            WindowTitle,
            WindowClassName,
            cancellationToken).ConfigureAwait(false);

        if (result.Found)
        {
            context.Log($"画像が見つかりました。({result.Point!.Value.X}, {result.Point!.Value.Y})");
            return true;
        }

        context.Log("画像が見つかりませんでした。");
        return false;
    }
}

[CommandDef.CommandDefinition(CommandDef.CommandTypeNames.WaitImageDisappear, typeof(SimpleCommand), typeof(IWaitImageCommandSettings), CommandDef.CommandCategory.Wait, displayPriority: 3, displaySubPriority: 3, displayNameJa: "画像消失待機", displayNameEn: "Wait for Image Disappear")]
/// <summary>
/// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
/// </summary>
public partial class WaitImageDisappearItem : CommandListItem, IWaitImageItem, IWaitImageCommandSettings
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("検索画像", EditorType.ImagePicker, Group = "画像設定", Order = 1,
                     Description = "消失を待つ画像ファイル", FileFilter = "画像ファイル|*.png;*.jpg;*.bmp")]
    private string _imagePath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("一致しきい値", EditorType.Slider, Group = "画像設定", Order = 2,
                     Description = "画像一致度の最小値", Min = 0.01, Max = 1.0, Step = 0.01)]
    private double _threshold = 0.8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("強調検索色", EditorType.ColorPicker, Group = "画像設定", Order = 3,
                     Description = "特定の色を強調して検索")]
    private CommandColor? _searchColor = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("タイムアウト", EditorType.NumberBox, Group = "タイミング", Order = 1,
                     Description = "消失待機を諦めるまでの時間", Unit = "ミリ秒", Min = 0)]
    private int _timeout = 5000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("検索間隔", EditorType.NumberBox, Group = "タイミング", Order = 2,
                     Description = "画像検索の間隔", Unit = "ミリ秒", Min = 0)]
    private int _interval = 500;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1,
                     Description = "操作対象のウィンドウタイトル（空欄で全画面）")]
    private string _windowTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2,
                     Description = "ウィンドウのクラス名")]
    private string _windowClassName = string.Empty;

    new public string Description => $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";

    public WaitImageDisappearItem() { }

    public WaitImageDisappearItem(WaitImageDisappearItem? item = null) : base(item)
    {
        if (item is null)
        {
            return;
        }

        ImagePath = item.ImagePath;
        Threshold = item.Threshold;
        SearchColor = item.SearchColor;
        Timeout = item.Timeout;
        Interval = item.Interval;
        WindowTitle = item.WindowTitle;
        WindowClassName = item.WindowClassName;
    }

    public new ICommandListItem Clone()
    {
        return new WaitImageDisappearItem(this);
    }

    public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
    {
        var absolutePath = context.ToAbsolutePath(ImagePath);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var point = await context.SearchImageAsync(
                absolutePath,
                Threshold,
                SearchColor,
                WindowTitle,
                WindowClassName,
                cancellationToken).ConfigureAwait(false);

            if (point is null)
            {
                context.Log($"画像が消失しました。経過時間={stopwatch.ElapsedMilliseconds}ms");
                return true;
            }

            if (Timeout == 0 || stopwatch.ElapsedMilliseconds >= Timeout)
            {
                context.Log($"タイムアウト: 画像が消失しませんでした。経過時間={stopwatch.ElapsedMilliseconds}ms");
                return false;
            }

            if (Timeout > 0)
            {
                var progress = Math.Clamp((int)Math.Round((double)stopwatch.ElapsedMilliseconds / Timeout * 100), 0, 100);
                context.ReportProgress(progress);
            }

            if (Interval > 0)
            {
                await Task.Delay(Interval, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

[CommandDef.CommandDefinition(CommandDef.CommandTypeNames.ClickImage, typeof(SimpleCommand), typeof(IClickImageCommandSettings), CommandDef.CommandCategory.Click, displayPriority: 1, displaySubPriority: 2, displayNameJa: "画像クリック", displayNameEn: "Image Click")]
/// <summary>
/// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
/// </summary>
public partial class ClickImageItem : CommandListItem, IClickImageItem, IClickImageCommandSettings
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("検索画像", EditorType.ImagePicker, Group = "画像設定", Order = 1,
                     Description = "検索する画像ファイル", FileFilter = "画像ファイル|*.png;*.jpg;*.bmp")]
    private string _imagePath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("一致しきい値", EditorType.Slider, Group = "画像設定", Order = 2,
                     Description = "画像一致度の最小値", Min = 0.01, Max = 1.0, Step = 0.01)]
    private double _threshold = 0.8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("強調検索色", EditorType.ColorPicker, Group = "画像設定", Order = 3,
                     Description = "特定の色を強調して検索")]
    private CommandColor? _searchColor = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("タイムアウト", EditorType.NumberBox, Group = "タイミング", Order = 1,
                     Description = "検索を諦めるまでの時間", Unit = "ミリ秒", Min = 0)]
    private int _timeout = 5000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("検索間隔", EditorType.NumberBox, Group = "タイミング", Order = 2,
                     Description = "画像検索の間隔", Unit = "ミリ秒", Min = 0)]
    private int _interval = 500;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("マウスボタン", EditorType.MouseButtonPicker, Group = "クリック設定", Order = 1,
                     Description = "クリックに使用するボタン")]
    private CommandMouseButton _button = CommandMouseButton.Left;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("押下維持時間", EditorType.NumberBox, Group = "クリック設定", Order = 2,
                     Description = "マウス押下から離すまでの待機時間", Unit = "ミリ秒", Min = 0)]
    private int _holdDurationMs = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("注入方式", EditorType.ComboBox, Group = "クリック設定", Order = 3,
                     Description = "クリック入力の送信方式", Options = "MouseEvent,SendInput")]
    private string _clickInjectionMode = "MouseEvent";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("移動シミュレート", EditorType.CheckBox, Group = "クリック設定", Order = 4,
                     Description = "クリック前にマウス移動を段階的にシミュレートする")]
    private bool _simulateMouseMove = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1,
                     Description = "操作対象のウィンドウタイトル（空欄で全画面）")]
    private string _windowTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2,
                     Description = "ウィンドウのクラス名")]
    private string _windowClassName = string.Empty;

    new public string Description => $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms / ボタン:{Button} / 押下維持:{HoldDurationMs}ms / 方式:{ClickInjectionMode} / 移動シミュレート:{(SimulateMouseMove ? "ON" : "OFF")}";

    public ClickImageItem() { }

    public ClickImageItem(ClickImageItem? item = null) : base(item)
    {
        if (item is null)
        {
            return;
        }

        ImagePath = item.ImagePath;
        Threshold = item.Threshold;
        SearchColor = item.SearchColor;
        Timeout = item.Timeout;
        Interval = item.Interval;
        Button = item.Button;
        HoldDurationMs = item.HoldDurationMs;
        ClickInjectionMode = item.ClickInjectionMode;
        SimulateMouseMove = item.SimulateMouseMove;
        WindowTitle = item.WindowTitle;
        WindowClassName = item.WindowClassName;
    }

    public new ICommandListItem Clone()
    {
        return new ClickImageItem(this);
    }

    public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
    {
        var result = await ImageCommandExecutionHelper.FindImageAsync(
            context,
            ImagePath,
            Threshold,
            SearchColor,
            Timeout,
            Interval,
            WindowTitle,
            WindowClassName,
            cancellationToken).ConfigureAwait(false);

        if (!result.Found || result.Point is null)
        {
            context.Log("画像が見つかりませんでした。");
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await context.ClickAsync(result.Point.Value.X, result.Point.Value.Y, Button, WindowTitle, WindowClassName, HoldDurationMs, ClickInjectionMode, SimulateMouseMove).ConfigureAwait(false);
        context.Log($"画像をクリックしました。({result.Point.Value.X}, {result.Point.Value.Y})");
        return true;
    }
}
