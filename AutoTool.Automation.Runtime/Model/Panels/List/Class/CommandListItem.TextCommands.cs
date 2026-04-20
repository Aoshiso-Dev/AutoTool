using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Commands;
using AutoTool.Commands.Services;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Attributes;
using CommandDef = AutoTool.Automation.Runtime.Definitions;

namespace AutoTool.Automation.Runtime.Lists;

[CommandDef.CommandDefinition(CommandDef.CommandTypeNames.FindText, typeof(SimpleCommand), typeof(IFindTextCommandSettings), CommandDef.CommandCategory.Variable, displayPriority: 5, displaySubPriority: 5, displayNameJa: "変数設定 - 文字検索", displayNameEn: "Set Variable - Text Search")]
/// <summary>
/// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
/// </summary>
public partial class FindTextItem : CommandListItem, IFindTextItem, IFindTextCommandSettings
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("検索文字列", EditorType.TextBox, Group = "検索条件", Order = 1,
                     Description = "存在判定する文字列")]
    private string _targetText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("マッチ方式", EditorType.ComboBox, Group = "検索条件", Order = 2,
                     Description = "Contains: 部分一致 / Equals: 完全一致", Options = "Contains,Equals")]
    private string _matchMode = "Contains";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("大文字小文字を区別", EditorType.CheckBox, Group = "検索条件", Order = 3,
                     Description = "オフの場合は大文字小文字を無視")]
    private bool _caseSensitive = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("領域", EditorType.PointPicker, Group = "OCR領域", Order = 1,
                     Description = "PickでOCR領域をドラッグ選択")]
    private int _x = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("Y", EditorType.NumberBox, Group = "OCR領域", Order = 2,
                     Description = "OCR領域の左上Y座標", Min = 0)]
    private int _y = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("幅", EditorType.NumberBox, Group = "OCR領域", Order = 3,
                     Description = "OCR領域の幅", Min = 1)]
    private int _width = 300;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("高さ", EditorType.NumberBox, Group = "OCR領域", Order = 4,
                     Description = "OCR領域の高さ", Min = 1)]
    private int _height = 100;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("タイムアウト", EditorType.NumberBox, Group = "タイミング", Order = 1,
                     Description = "検索を諦めるまでの時間", Unit = "ミリ秒", Min = 0)]
    private int _timeout = 3000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("検索間隔", EditorType.NumberBox, Group = "タイミング", Order = 2,
                     Description = "OCR再試行の間隔", Unit = "ミリ秒", Min = 0)]
    private int _interval = 500;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("未検出で失敗", EditorType.CheckBox, Group = "実行設定", Order = 1,
                     Description = "オフの場合は見つからなくても次へ進む")]
    private bool _strict = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("最小信頼度", EditorType.Slider, Group = "OCR設定", Order = 1,
                     Description = "この値未満のOCR結果は不一致扱い", Min = 0.0, Max = 100.0, Step = 1.0)]
    private double _minConfidence = 50.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("言語", EditorType.ComboBox, Group = "OCR設定", Order = 2,
                     Description = "Tesseract OCRの言語", Options = "jpn,jpn+eng,eng")]
    private string _language = "jpn";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("PSM", EditorType.ComboBox, Group = "OCR設定", Order = 3,
                     Description = "ページ分割モード", Options = "6,7,11,12,13")]
    private string _pageSegmentationMode = "6";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("文字種制限", EditorType.TextBox, Group = "OCR設定", Order = 4,
                     Description = "空欄で無効。例: 0123456789")]
    private string _whitelist = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("前処理", EditorType.ComboBox, Group = "OCR設定", Order = 5,
                     Description = "OCR前の画像前処理", Options = "Gray,Binarize,AdaptiveThreshold,None")]
    private string _preprocessMode = "Gray";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("tessdataディレクトリ", EditorType.DirectoryPicker, Group = "詳細設定", Order = 1,
                     Description = "必要な場合のみ指定")]
    private string _tessdataPath = string.Empty;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("結果変数(found)", EditorType.TextBox, Group = "変数出力", Order = 1,
                     Description = "true/false を保存する変数名（空欄で保存しない）")]
    private string _foundVariableName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("結果変数(text)", EditorType.TextBox, Group = "変数出力", Order = 2,
                     Description = "OCR抽出文字列を保存する変数名（空欄で保存しない）")]
    private string _textVariableName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("結果変数(confidence)", EditorType.TextBox, Group = "変数出力", Order = 3,
                     Description = "OCR信頼度を保存する変数名（空欄で保存しない）")]
    private string _confidenceVariableName = string.Empty;

    new public string Description =>
        $"文字:\"{TargetText}\" / マッチ:{MatchMode} / 領域:({X},{Y},{Width},{Height}) / タイムアウト:{Timeout}ms / Strict:{Strict}";

    public FindTextItem() { }

    public FindTextItem(FindTextItem? item = null) : base(item)
    {
        if (item is null)
        {
            return;
        }

        TargetText = item.TargetText;
        MatchMode = item.MatchMode;
        CaseSensitive = item.CaseSensitive;
        X = item.X;
        Y = item.Y;
        Width = item.Width;
        Height = item.Height;
        Timeout = item.Timeout;
        Interval = item.Interval;
        Strict = item.Strict;
        MinConfidence = item.MinConfidence;
        Language = item.Language;
        PageSegmentationMode = item.PageSegmentationMode;
        Whitelist = item.Whitelist;
        PreprocessMode = item.PreprocessMode;
        TessdataPath = item.TessdataPath;
        WindowTitle = item.WindowTitle;
        WindowClassName = item.WindowClassName;
        FoundVariableName = item.FoundVariableName;
        TextVariableName = item.TextVariableName;
        ConfidenceVariableName = item.ConfidenceVariableName;
    }

    public new ICommandListItem Clone()
    {
        return new FindTextItem(this);
    }

    public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var timeout = Math.Max(0, Timeout);
        var interval = Math.Max(0, Interval);
        var runOnce = timeout == 0;

        while (runOnce || stopwatch.ElapsedMilliseconds < timeout)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var result = await context.ExtractTextAsync(new OcrRequest
            {
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                WindowTitle = WindowTitle,
                WindowClassName = WindowClassName,
                Language = Language,
                PageSegmentationMode = PageSegmentationMode,
                Whitelist = Whitelist,
                PreprocessMode = PreprocessMode,
                TessdataPath = string.IsNullOrWhiteSpace(TessdataPath)
                    ? TessdataPath
                    : context.ToAbsolutePath(TessdataPath)
            }, cancellationToken).ConfigureAwait(false);

            var matched = IsMatched(result.Text, result.Confidence);
            SaveResultVariables(context, matched, result.Text, result.Confidence);

            if (matched)
            {
                context.ReportProgress(100);
                context.Log($"文字が見つかりました。抽出文字列=\"{result.Text}\" / 信頼度={result.Confidence:F1}");
                return true;
            }

            if (runOnce)
            {
                break;
            }

            var progress = timeout > 0 ? (int)((stopwatch.ElapsedMilliseconds * 100) / timeout) : 100;
            context.ReportProgress(Math.Clamp(progress, 0, 100));
            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }

        context.Log("文字が見つかりませんでした。");
        return !Strict;
    }

    private bool IsMatched(string extractedText, double confidence)
    {
        if (confidence < MinConfidence)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(TargetText))
        {
            return !string.IsNullOrWhiteSpace(extractedText);
        }

        var comparison = CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return MatchMode switch
        {
            "Equals" => string.Equals(extractedText.Trim(), TargetText.Trim(), comparison),
            _ => extractedText.Contains(TargetText, comparison)
        };
    }

    private void SaveResultVariables(ICommandExecutionContext context, bool found, string extractedText, double confidence)
    {
        if (!string.IsNullOrWhiteSpace(FoundVariableName))
        {
            context.SetVariable(FoundVariableName, found ? "true" : "false");
        }

        if (!string.IsNullOrWhiteSpace(TextVariableName))
        {
            context.SetVariable(TextVariableName, extractedText);
        }

        if (!string.IsNullOrWhiteSpace(ConfidenceVariableName))
        {
            context.SetVariable(ConfidenceVariableName, confidence.ToString("F1"));
        }
    }
}
