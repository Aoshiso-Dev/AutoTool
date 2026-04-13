using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Commands;
using AutoTool.Panels.Model.List.Interface;
using AutoTool.Panels.Attributes;
using CommandDef = AutoTool.Panels.Model.CommandDefinition;

namespace AutoTool.Panels.List.Class;

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfImageExist, typeof(IfImageExistCommand), typeof(IIfImageCommandSettings), CommandDef.CommandCategory.Control, isIfCommand: true, displayPriority: 4, displaySubPriority: 1, displayNameJa: "条件 - 画像存在判定", displayNameEn: "If Image Exists")]
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
        private Color? _searchColor = null;
        
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
            if (item != null)
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

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfImageNotExist, typeof(IfImageNotExistCommand), typeof(IIfImageCommandSettings), CommandDef.CommandCategory.Control, isIfCommand: true, displayPriority: 4, displaySubPriority: 2, displayNameJa: "条件 - 画像非存在判定", displayNameEn: "If Image Not Exists")]
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
        private Color? _searchColor = null;
        
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
            if (item != null)
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


    [CommandDef.SimpleCommandBinding(typeof(IfEndCommand), typeof(ICommandSettings))]
    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfEnd, typeof(IfEndCommand), typeof(ICommandSettings), CommandDef.CommandCategory.Control, isEndCommand: true, displayPriority: 4, displaySubPriority: 6, displayNameJa: "条件 - 終了", displayNameEn: "If End")]
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

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.Loop, typeof(LoopCommand), typeof(ILoopCommandSettings), CommandDef.CommandCategory.Control, isLoopCommand: true, displayPriority: 3, displaySubPriority: 1, displayNameJa: "ループ - 開始", displayNameEn: "Loop Start")]
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
            if (item != null)
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
    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.LoopEnd, typeof(LoopEndCommand), typeof(ICommandSettings), CommandDef.CommandCategory.Control, isEndCommand: true, displayPriority: 3, displaySubPriority: 3, displayNameJa: "ループ - 終了", displayNameEn: "Loop End")]
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
            if (item != null)
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
    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.LoopBreak, typeof(LoopBreakCommand), typeof(ICommandSettings), CommandDef.CommandCategory.Control, displayPriority: 3, displaySubPriority: 2, displayNameJa: "ループ - 中断", displayNameEn: "Loop Break")]
    public partial class LoopBreakItem : CommandListItem, ILoopBreakItem, ICommandSettings
    {
        public LoopBreakItem() { }

        public LoopBreakItem(LoopBreakItem? item = null) : base(item) { }

        public new ICommandListItem Clone()
        {
            return new LoopBreakItem(this);
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfImageExistAI, typeof(IfImageExistAICommand), typeof(IIfImageExistAISettings), CommandDef.CommandCategory.AI, isIfCommand: true, displayPriority: 4, displaySubPriority: 3, displayNameJa: "条件 - 画像存在判定(AI検出)", displayNameEn: "If AI Image Exists")]
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
        [property: CommandProperty("クラスID", EditorType.NumberBox, Group = "AI設定", Order = 2,
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
             $"{LineNumber}->{Pair?.LineNumber} / 対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / クラスID:{ClassID} / 閾値:{ConfThreshold}";

        public IfImageExistAIItem() { }

        public IfImageExistAIItem(IfImageExistAIItem? item = null) : base(item)
        {
            if (item != null)
            {
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                ModelPath = item.ModelPath;
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

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfImageNotExistAI, typeof(IfImageNotExistAICommand), typeof(IIfImageNotExistAISettings), CommandDef.CommandCategory.AI, isIfCommand: true, displayPriority: 4, displaySubPriority: 4, displayNameJa: "条件 - 画像非存在判定(AI検出)", displayNameEn: "If AI Image Not Exists")]
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
        [property: CommandProperty("クラスID", EditorType.NumberBox, Group = "AI設定", Order = 2,
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
            $"{LineNumber}->{Pair?.LineNumber} / 対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / クラスID:{ClassID} / 閾値:{ConfThreshold}";

        public IfImageNotExistAIItem() { }
        public IfImageNotExistAIItem(IfImageNotExistAIItem? item = null) : base(item)
        {
            if (item != null)
            {
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                ModelPath = item.ModelPath;
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


