using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Commands.Model.Input;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Commands;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Attributes;
using CommandDef = AutoTool.Automation.Runtime.Definitions;

namespace AutoTool.Automation.Runtime.Lists;

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfImageExist, typeof(IfImageExistCommand), typeof(IIfImageCommandSettings), CommandDef.CommandCategory.Condition, isIfCommand: true, displayPriority: 4, displaySubPriority: 1, displayNameJa: "画像存在判定", displayNameEn: "If Image Exists")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class IfImageExistItem : CommandListItem, IIfItem, IIfImageExistItem, IIfImageCommandSettings
    {
        new public bool IsEnable
        {
            get => base.IsEnable;
            set => SetIsEnableWithPair(Pair, value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("検索画像", EditorType.ImagePicker, Group = "画像設定", Order = 1,
                         Description = "検索する画像ファイル")]
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
        private ICommandListItem? _pair = null;


        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / 対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]") } / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold}";

        public IfImageExistItem() { }

        public IfImageExistItem(IfImageExistItem? item = null) : base(item)
        {
            if (item is not null)
            {
                ImagePath = item.ImagePath;
                Threshold = item.Threshold;
                SearchColor = item.SearchColor;
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone()
        {
            return new IfImageExistItem(this);
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfImageNotExist, typeof(IfImageNotExistCommand), typeof(IIfImageCommandSettings), CommandDef.CommandCategory.Condition, isIfCommand: true, displayPriority: 4, displaySubPriority: 2, displayNameJa: "画像非存在判定", displayNameEn: "If Image Not Exists")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class IfImageNotExistItem : CommandListItem, IIfItem, IIfImageNotExistItem, IIfImageCommandSettings
    {
        new public bool IsEnable
        {
            get => base.IsEnable;
            set => SetIsEnableWithPair(Pair, value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("検索画像", EditorType.ImagePicker, Group = "画像設定", Order = 1,
                         Description = "検索する画像ファイル")]
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
        private ICommandListItem? _pair = null;

        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / 対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold}";

        public IfImageNotExistItem() { }

        public IfImageNotExistItem(IfImageNotExistItem? item = null) : base(item)
        {
            if (item is not null)
            {
                ImagePath = item.ImagePath;
                Threshold = item.Threshold;
                SearchColor = item.SearchColor;
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone()
        {
            return new IfImageNotExistItem(this);
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfTextExist, typeof(IfTextExistCommand), typeof(IIfTextCommandSettings), CommandDef.CommandCategory.Condition, isIfCommand: true, displayPriority: 4, displaySubPriority: 3, displayNameJa: "文字存在判定", displayNameEn: "If Text Exists")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class IfTextExistItem : CommandListItem, IIfItem, IIfTextExistItem, IIfTextCommandSettings
    {
        new public bool IsEnable
        {
            get => base.IsEnable;
            set => SetIsEnableWithPair(Pair, value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("検索文字列", EditorType.TextBox, Group = "検索条件", Order = 1, Description = "存在判定する文字列")]
        private string _targetText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("マッチ方式", EditorType.ComboBox, Group = "検索条件", Order = 2, Description = "Contains: 部分一致 / Equals: 完全一致", Options = "Contains,Equals")]
        private string _matchMode = "Contains";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("大文字小文字を区別", EditorType.CheckBox, Group = "検索条件", Order = 3, Description = "オフの場合は大文字小文字を無視")]
        private bool _caseSensitive = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("領域", EditorType.PointPicker, Group = "OCR領域", Order = 1, Description = "PickでOCR領域をドラッグ選択")]
        private int _x = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("Y", EditorType.NumberBox, Group = "OCR領域", Order = 2, Description = "OCR領域の左上Y座標", Min = 0)]
        private int _y = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("幅", EditorType.NumberBox, Group = "OCR領域", Order = 3, Description = "OCR領域の幅", Min = 1)]
        private int _width = 300;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("高さ", EditorType.NumberBox, Group = "OCR領域", Order = 4, Description = "OCR領域の高さ", Min = 1)]
        private int _height = 100;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("最小信頼度", EditorType.Slider, Group = "OCR設定", Order = 1, Description = "この値未満のOCR結果は不一致扱い", Min = 0.0, Max = 100.0, Step = 1.0)]
        private double _minConfidence = 50.0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("言語", EditorType.ComboBox, Group = "OCR設定", Order = 2, Description = "Tesseract OCRの言語", Options = "jpn,jpn+eng,eng")]
        private string _language = "jpn";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("PSM", EditorType.ComboBox, Group = "OCR設定", Order = 3, Description = "ページ分割モード", Options = "6,7,11,12,13")]
        private string _pageSegmentationMode = "6";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("文字種制限", EditorType.TextBox, Group = "OCR設定", Order = 4, Description = "空欄で無効。例: 0123456789")]
        private string _whitelist = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("前処理", EditorType.ComboBox, Group = "OCR設定", Order = 5, Description = "OCR前の画像前処理", Options = "Gray,Binarize,AdaptiveThreshold,None")]
        private string _preprocessMode = "Gray";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("tessdataディレクトリ", EditorType.DirectoryPicker, Group = "詳細設定", Order = 1, Description = "必要な場合のみ指定")]
        private string _tessdataPath = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1, Description = "操作対象のウィンドウタイトル（空欄で全画面）")]
        private string _windowTitle = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2, Description = "ウィンドウのクラス名")]
        private string _windowClassName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;

        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / 文字:\"{TargetText}\" / マッチ:{MatchMode} / 領域:({X},{Y},{Width},{Height})";

        public IfTextExistItem() { }

        public IfTextExistItem(IfTextExistItem? item = null) : base(item)
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
            MinConfidence = item.MinConfidence;
            Language = item.Language;
            PageSegmentationMode = item.PageSegmentationMode;
            Whitelist = item.Whitelist;
            PreprocessMode = item.PreprocessMode;
            TessdataPath = item.TessdataPath;
            WindowTitle = item.WindowTitle;
            WindowClassName = item.WindowClassName;
            Pair = item.Pair;
        }

        public new ICommandListItem Clone()
        {
            return new IfTextExistItem(this);
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfTextNotExist, typeof(IfTextNotExistCommand), typeof(IIfTextCommandSettings), CommandDef.CommandCategory.Condition, isIfCommand: true, displayPriority: 4, displaySubPriority: 4, displayNameJa: "文字非存在判定", displayNameEn: "If Text Not Exists")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class IfTextNotExistItem : CommandListItem, IIfItem, IIfTextNotExistItem, IIfTextCommandSettings
    {
        new public bool IsEnable
        {
            get => base.IsEnable;
            set => SetIsEnableWithPair(Pair, value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("検索文字列", EditorType.TextBox, Group = "検索条件", Order = 1, Description = "存在判定する文字列")]
        private string _targetText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("マッチ方式", EditorType.ComboBox, Group = "検索条件", Order = 2, Description = "Contains: 部分一致 / Equals: 完全一致", Options = "Contains,Equals")]
        private string _matchMode = "Contains";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("大文字小文字を区別", EditorType.CheckBox, Group = "検索条件", Order = 3, Description = "オフの場合は大文字小文字を無視")]
        private bool _caseSensitive = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("領域", EditorType.PointPicker, Group = "OCR領域", Order = 1, Description = "PickでOCR領域をドラッグ選択")]
        private int _x = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("Y", EditorType.NumberBox, Group = "OCR領域", Order = 2, Description = "OCR領域の左上Y座標", Min = 0)]
        private int _y = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("幅", EditorType.NumberBox, Group = "OCR領域", Order = 3, Description = "OCR領域の幅", Min = 1)]
        private int _width = 300;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("高さ", EditorType.NumberBox, Group = "OCR領域", Order = 4, Description = "OCR領域の高さ", Min = 1)]
        private int _height = 100;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("最小信頼度", EditorType.Slider, Group = "OCR設定", Order = 1, Description = "この値未満のOCR結果は不一致扱い", Min = 0.0, Max = 100.0, Step = 1.0)]
        private double _minConfidence = 50.0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("言語", EditorType.ComboBox, Group = "OCR設定", Order = 2, Description = "Tesseract OCRの言語", Options = "jpn,jpn+eng,eng")]
        private string _language = "jpn";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("PSM", EditorType.ComboBox, Group = "OCR設定", Order = 3, Description = "ページ分割モード", Options = "6,7,11,12,13")]
        private string _pageSegmentationMode = "6";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("文字種制限", EditorType.TextBox, Group = "OCR設定", Order = 4, Description = "空欄で無効。例: 0123456789")]
        private string _whitelist = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("前処理", EditorType.ComboBox, Group = "OCR設定", Order = 5, Description = "OCR前の画像前処理", Options = "Gray,Binarize,AdaptiveThreshold,None")]
        private string _preprocessMode = "Gray";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("tessdataディレクトリ", EditorType.DirectoryPicker, Group = "詳細設定", Order = 1, Description = "必要な場合のみ指定")]
        private string _tessdataPath = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1, Description = "操作対象のウィンドウタイトル（空欄で全画面）")]
        private string _windowTitle = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2, Description = "ウィンドウのクラス名")]
        private string _windowClassName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;

        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / 文字:\"{TargetText}\" / マッチ:{MatchMode} / 領域:({X},{Y},{Width},{Height})";

        public IfTextNotExistItem() { }

        public IfTextNotExistItem(IfTextNotExistItem? item = null) : base(item)
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
            MinConfidence = item.MinConfidence;
            Language = item.Language;
            PageSegmentationMode = item.PageSegmentationMode;
            Whitelist = item.Whitelist;
            PreprocessMode = item.PreprocessMode;
            TessdataPath = item.TessdataPath;
            WindowTitle = item.WindowTitle;
            WindowClassName = item.WindowClassName;
            Pair = item.Pair;
        }

        public new ICommandListItem Clone()
        {
            return new IfTextNotExistItem(this);
        }
    }


    [CommandDef.SimpleCommandBinding(typeof(IfEndCommand), typeof(ICommandSettings))]
    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfEnd, typeof(IfEndCommand), typeof(ICommandSettings), CommandDef.CommandCategory.Condition, isEndCommand: true, displayPriority: 4, displaySubPriority: 8, displayNameJa: "条件終了", displayNameEn: "If End")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class IfEndItem : CommandListItem, IIfEndItem, ICommandSettings
    {
        new public bool IsEnable
        {
            get => base.IsEnable;
            set => SetIsEnableWithPair(Pair, value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;


        new public string Description => $"{Pair?.LineNumber}->{LineNumber}";

        public IfEndItem() { }
        public IfEndItem(IfEndItem? item = null) : base(item) { }

        public new ICommandListItem Clone()
        {
            return new IfEndItem(this);
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.Loop, typeof(LoopCommand), typeof(ILoopCommandSettings), CommandDef.CommandCategory.Control, isLoopCommand: true, displayPriority: 5, displaySubPriority: 1, displayNameJa: "ループ開始", displayNameEn: "Loop Start")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class LoopItem : CommandListItem, ILoopItem, ICommandListItem
    {
        new public bool IsEnable
        {
            get => base.IsEnable;
            set => SetIsEnableWithPair(Pair, value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ループ回数", EditorType.NumberBox, Group = "基本設定", Order = 1,
                         Description = "繰り返す回数（0で無限ループ）", Min = 0)]
        private int _loopCount = 2;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;

        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / ループ回数:{LoopCount}";

        public LoopItem() { }

        public LoopItem(LoopItem? item = null) : base(item)
        {
            if (item is not null)
            {
                LoopCount = item.LoopCount;
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone()
        {
            return new LoopItem(this);
        }
    }

    [CommandDef.SimpleCommandBinding(typeof(LoopEndCommand), typeof(ICommandSettings))]
    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.LoopEnd, typeof(LoopEndCommand), typeof(ICommandSettings), CommandDef.CommandCategory.Control, isEndCommand: true, displayPriority: 5, displaySubPriority: 3, displayNameJa: "ループ終了", displayNameEn: "Loop End")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class LoopEndItem : CommandListItem, ILoopEndItem, ICommandSettings
    {
        new public bool IsEnable
        {
            get => base.IsEnable;
            set => SetIsEnableWithPair(Pair, value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;

        new public string Description => $"{Pair?.LineNumber}->{LineNumber}";

        public LoopEndItem() { }

        public LoopEndItem(LoopEndItem? item = null) : base(item)
        {
            if (item is not null)
            {
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone()
        {
            return new LoopEndItem(this);
        }
    }

    [CommandDef.SimpleCommandBinding(typeof(LoopBreakCommand), typeof(ICommandSettings))]
    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.LoopBreak, typeof(LoopBreakCommand), typeof(ICommandSettings), CommandDef.CommandCategory.Control, displayPriority: 5, displaySubPriority: 2, displayNameJa: "ループ中断", displayNameEn: "Loop Break")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class LoopBreakItem : CommandListItem, ILoopBreakItem, ICommandSettings
    {
        public LoopBreakItem() { }

        public LoopBreakItem(LoopBreakItem? item = null) : base(item) { }

        public new ICommandListItem Clone()
        {
            return new LoopBreakItem(this);
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.Retry, typeof(RetryCommand), typeof(IRetryCommandSettings), CommandDef.CommandCategory.Control, isLoopCommand: true, displayPriority: 5, displaySubPriority: 4, displayNameJa: "リトライ開始", displayNameEn: "Retry Start")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class RetryItem : CommandListItem, IRetryItem, IRetryCommandSettings
    {
        new public bool IsEnable
        {
            get => base.IsEnable;
            set => SetIsEnableWithPair(Pair, value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("リトライ回数", EditorType.NumberBox, Group = "基本設定", Order = 1,
                         Description = "失敗時に再実行する最大回数", Min = 1)]
        private int _retryCount = 3;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("リトライ間隔", EditorType.Duration, Group = "基本設定", Order = 2,
                         Description = "再実行までの待機時間", Min = 0)]
        private int _retryInterval = 500;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;

        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / リトライ回数:{RetryCount} / 間隔:{DurationText.Format(RetryInterval)}";

        public RetryItem() { }

        public RetryItem(RetryItem? item = null) : base(item)
        {
            if (item is null)
            {
                return;
            }

            RetryCount = item.RetryCount;
            RetryInterval = item.RetryInterval;
            Pair = item.Pair;
        }

        public new ICommandListItem Clone()
        {
            return new RetryItem(this);
        }
    }

    [CommandDef.SimpleCommandBinding(typeof(RetryEndCommand), typeof(ICommandSettings))]
    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.RetryEnd, typeof(RetryEndCommand), typeof(ICommandSettings), CommandDef.CommandCategory.Control, isEndCommand: true, displayPriority: 5, displaySubPriority: 5, displayNameJa: "リトライ終了", displayNameEn: "Retry End")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class RetryEndItem : CommandListItem, IRetryEndItem, ICommandSettings
    {
        new public bool IsEnable
        {
            get => base.IsEnable;
            set => SetIsEnableWithPair(Pair, value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;

        new public string Description => $"{Pair?.LineNumber}->{LineNumber}";

        public RetryEndItem() { }

        public RetryEndItem(RetryEndItem? item = null) : base(item)
        {
            if (item is not null)
            {
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone()
        {
            return new RetryEndItem(this);
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfImageExistAI, typeof(IfImageExistAICommand), typeof(IIfImageExistAISettings), CommandDef.CommandCategory.Condition, isIfCommand: true, displayPriority: 4, displaySubPriority: 6, displayNameJa: "AI画像存在判定", displayNameEn: "If AI Image Exists")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class IfImageExistAIItem : CommandListItem, IIfItem, IIfImageExistAIItem, IIfImageExistAISettings
    {
        new public bool IsEnable
        {
            get => base.IsEnable;
            set => SetIsEnableWithPair(Pair, value);
        }

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
        [property: CommandProperty("ONNXモデル", EditorType.FilePicker, Group = "AI設定", Order = 1,
                         Description = "YOLOv8 ONNXモデルファイル")]
        private string _modelPath = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ラベルファイル", EditorType.FilePicker, Group = "AI設定", Order = 2,
                         Description = "未指定時はモデルmetadataと同階層のラベルファイルを利用")]
        private string _labelsPath = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ラベル名", EditorType.ComboBox, Group = "AI設定", Order = 3,
                         Description = "選択時はクラスIDより優先して一致判定")]
        private string _labelName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("クラスID", EditorType.NumberBox, Group = "AI設定", Order = 4,
                         Description = "検出する物体のクラス番号", Min = 0)]
        private int _classID = 0;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("信頼度しきい値", EditorType.Slider, Group = "AI設定", Order = 3,
                         Description = "検出の信頼度しきい値", Min = 0.01, Max = 1.0, Step = 0.01)]
        private double _confThreshold = 0.5;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("IoUしきい値", EditorType.Slider, Group = "AI設定", Order = 4,
                         Description = "重なり除去のしきい値", Min = 0.01, Max = 1.0, Step = 0.01)]
        private double _ioUThreshold = 0.25;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;

        new public string Description =>
             $"{LineNumber}->{Pair?.LineNumber} / 対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / {(string.IsNullOrWhiteSpace(LabelName) ? $"クラスID:{ClassID}" : $"ラベル:{LabelName}")} / 閾値:{ConfThreshold}";

        public IfImageExistAIItem() { }

        public IfImageExistAIItem(IfImageExistAIItem? item = null) : base(item)
        {
            if (item is not null)
            {
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                ModelPath = item.ModelPath;
                LabelsPath = item.LabelsPath;
                LabelName = item.LabelName;
                ClassID = item.ClassID;
                ConfThreshold = item.ConfThreshold;
                IoUThreshold = item.IoUThreshold;
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone()
        {
            return new IfImageExistAIItem(this);
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfImageNotExistAI, typeof(IfImageNotExistAICommand), typeof(IIfImageNotExistAISettings), CommandDef.CommandCategory.Condition, isIfCommand: true, displayPriority: 4, displaySubPriority: 7, displayNameJa: "AI画像非存在判定", displayNameEn: "If AI Image Not Exists")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class IfImageNotExistAIItem : CommandListItem, IIfItem, IIfImageNotExistAIItem, IIfImageNotExistAISettings
    {
        new public bool IsEnable
        {
            get => base.IsEnable;
            set => SetIsEnableWithPair(Pair, value);
        }

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
        [property: CommandProperty("ONNXモデル", EditorType.FilePicker, Group = "AI設定", Order = 1,
                         Description = "YOLOv8 ONNXモデルファイル")]
        private string _modelPath = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ラベルファイル", EditorType.FilePicker, Group = "AI設定", Order = 2,
                         Description = "未指定時はモデルmetadataと同階層のラベルファイルを利用")]
        private string _labelsPath = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ラベル名", EditorType.ComboBox, Group = "AI設定", Order = 3,
                         Description = "選択時はクラスIDより優先して一致判定")]
        private string _labelName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("クラスID", EditorType.NumberBox, Group = "AI設定", Order = 4,
                         Description = "検出する物体のクラス番号", Min = 0)]
        private int _classID = 0;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("信頼度しきい値", EditorType.Slider, Group = "AI設定", Order = 3,
                         Description = "検出の信頼度しきい値", Min = 0.01, Max = 1.0, Step = 0.01)]
        private double _confThreshold = 0.5;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("IoUしきい値", EditorType.Slider, Group = "AI設定", Order = 4,
                         Description = "重なり除去のしきい値", Min = 0.01, Max = 1.0, Step = 0.01)]
        private double _ioUThreshold = 0.25;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;

        new public string Description =>
            $"{LineNumber}->{Pair?.LineNumber} / 対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / {(string.IsNullOrWhiteSpace(LabelName) ? $"クラスID:{ClassID}" : $"ラベル:{LabelName}")} / 閾値:{ConfThreshold}";

        public IfImageNotExistAIItem() { }
        public IfImageNotExistAIItem(IfImageNotExistAIItem? item = null) : base(item)
        {
            if (item is not null)
            {
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                ModelPath = item.ModelPath;
                LabelsPath = item.LabelsPath;
                LabelName = item.LabelName;
                ClassID = item.ClassID;
                ConfThreshold = item.ConfThreshold;
                IoUThreshold = item.IoUThreshold;
                Pair = item.Pair;
            }
        }
        public new ICommandListItem Clone()
        {
            return new IfImageNotExistAIItem(this);
        }
    }


