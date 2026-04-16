using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Commands;
using AutoTool.Panels.Model.List.Interface;
using AutoTool.Panels.Attributes;
using CommandDef = AutoTool.Panels.Model.CommandDefinition;

namespace AutoTool.Panels.List.Class;

[CommandDef.CommandDefinition(CommandDef.CommandTypeNames.FindImage, typeof(SimpleCommand), typeof(IFindImageCommandSettings), CommandDef.CommandCategory.Variable, displayPriority: 5, displaySubPriority: 4, displayNameJa: "変数設定 - 画像検索", displayNameEn: "Set Variable - Image Search")]
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
    private Color? _searchColor = null;

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

    new public string Description => $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms / Strict:{Strict}";

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

    public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
    {
        var absolutePath = context.ToAbsolutePath(ImagePath);
        var result = await FindImageExecutor.ExecuteAsync(
            new FindImageOptions
            {
                ImagePath = absolutePath,
                Threshold = Threshold,
                SearchColor = SearchColor,
                Timeout = Timeout,
                Interval = Interval,
                WindowTitle = WindowTitle,
                WindowClassName = WindowClassName
            },
            context.SearchImageAsync,
            context.ReportProgress,
            cancellationToken);

        SetResultVariables(context, result);

        if (result.Found)
        {
            context.Log($"画像が見つかりました。({result.Point!.Value.X}, {result.Point!.Value.Y}) / elapsed={result.ElapsedMilliseconds}ms");
            return true;
        }

        context.Log($"画像が見つかりませんでした。elapsed={result.ElapsedMilliseconds}ms");
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

[CommandDef.CommandDefinition(CommandDef.CommandTypeNames.WaitImage, typeof(SimpleCommand), typeof(IWaitImageCommandSettings), CommandDef.CommandCategory.Action, displayPriority: 2, displaySubPriority: 4, displayNameJa: "画像待機", displayNameEn: "Wait for Image")]
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
    private Color? _searchColor = null;

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

    public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
    {
        var absolutePath = context.ToAbsolutePath(ImagePath);
        var result = await FindImageExecutor.ExecuteAsync(
            new FindImageOptions
            {
                ImagePath = absolutePath,
                Threshold = Threshold,
                SearchColor = SearchColor,
                Timeout = Timeout,
                Interval = Interval,
                WindowTitle = WindowTitle,
                WindowClassName = WindowClassName
            },
            context.SearchImageAsync,
            context.ReportProgress,
            cancellationToken);

        if (result.Found)
        {
            context.Log($"画像が見つかりました。({result.Point!.Value.X}, {result.Point!.Value.Y})");
            return true;
        }

        context.Log("画像が見つかりませんでした。");
        return false;
    }
}

[CommandDef.CommandDefinition(CommandDef.CommandTypeNames.ClickImage, typeof(SimpleCommand), typeof(IClickImageCommandSettings), CommandDef.CommandCategory.Action, displayPriority: 1, displaySubPriority: 2, displayNameJa: "画像クリック", displayNameEn: "Image Click")]
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
    private Color? _searchColor = null;

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
    private System.Windows.Input.MouseButton _button = System.Windows.Input.MouseButton.Left;

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

    new public string Description => $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms / ボタン:{Button}";

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
        WindowTitle = item.WindowTitle;
        WindowClassName = item.WindowClassName;
    }

    public new ICommandListItem Clone()
    {
        return new ClickImageItem(this);
    }

    public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
    {
        var absolutePath = context.ToAbsolutePath(ImagePath);
        var result = await FindImageExecutor.ExecuteAsync(
            new FindImageOptions
            {
                ImagePath = absolutePath,
                Threshold = Threshold,
                SearchColor = SearchColor,
                Timeout = Timeout,
                Interval = Interval,
                WindowTitle = WindowTitle,
                WindowClassName = WindowClassName
            },
            context.SearchImageAsync,
            context.ReportProgress,
            cancellationToken);

        if (!result.Found || result.Point is null)
        {
            context.Log("画像が見つかりませんでした。");
            return false;
        }

        await context.ClickAsync(result.Point.Value.X, result.Point.Value.Y, Button, WindowTitle, WindowClassName);
        context.Log($"画像をクリックしました。({result.Point.Value.X}, {result.Point.Value.Y})");
        return true;
    }
}
