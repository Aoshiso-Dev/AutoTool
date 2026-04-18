using AutoTool.Commands.Model.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Commands;
using AutoTool.Commands.Services;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Attributes;
using CommandDef = AutoTool.Automation.Runtime.Definitions;

namespace AutoTool.Automation.Runtime.Lists;

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.Execute, typeof(SimpleCommand), typeof(IExecuteCommandSettings), CommandDef.CommandCategory.System, displayPriority: 6, displaySubPriority: 1, displayNameJa: "プログラム実行", displayNameEn: "Execute Program")]
    public partial class ExecuteItem : CommandListItem, IExecuteItem, IExecuteCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("プログラムパス", EditorType.FilePicker, Group = "実行設定", Order = 1,
                         Description = "実行するプログラム")]
        private string _programPath = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("引数", EditorType.TextBox, Group = "実行設定", Order = 2,
                         Description = "コマンドライン引数")]
        private string _arguments = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("作業ディレクトリ", EditorType.DirectoryPicker, Group = "実行設定", Order = 3,
                         Description = "作業ディレクトリ")]
        private string _workingDirectory = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("終了を待つ", EditorType.CheckBox, Group = "実行設定", Order = 4,
                         Description = "プログラム終了まで待機")]
        private bool _waitForExit = false;

        new public string Description => $"ファイルパス:{System.IO.Path.GetFileName(ProgramPath)} / 引数:{Arguments} / 作業フォルダ:{WorkingDirectory}";
        public ExecuteItem() { }
        public ExecuteItem(ExecuteItem? item = null) : base(item)
        {
            if (item is not null)
            {
                ProgramPath = item.ProgramPath;
                Arguments = item.Arguments;
                WorkingDirectory = item.WorkingDirectory;
                WaitForExit = item.WaitForExit;
            }
        }
        public new ICommandListItem Clone()
        {
            return new ExecuteItem(this);
        }
        
        public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            try
            {
                var absoluteProgramPath = context.ToAbsolutePath(ProgramPath);
                var absoluteWorkingDirectory = string.IsNullOrWhiteSpace(WorkingDirectory)
                    ? WorkingDirectory
                    : context.ToAbsolutePath(WorkingDirectory);

                await context.ExecuteProgramAsync(absoluteProgramPath, Arguments, absoluteWorkingDirectory, WaitForExit, cancellationToken).ConfigureAwait(false);
                context.Log($"プログラムを実行しました: {absoluteProgramPath}");
                return true;
            }
            catch (Exception ex)
            {
                context.Log($"プログラム実行エラー: {ex.Message}");
                return false;
            }
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.SetVariable, typeof(SimpleCommand), typeof(ISetVariableCommandSettings), CommandDef.CommandCategory.Variable, displayPriority: 5, displaySubPriority: 1, displayNameJa: "変数設定", displayNameEn: "Set Variable")]
    public partial class SetVariableItem : CommandListItem, ISetVariableItem, ISetVariableCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("変数名", EditorType.TextBox, Group = "変数設定", Order = 1,
                         Description = "設定する変数の名前")]
        private string _name = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("値", EditorType.TextBox, Group = "変数設定", Order = 2,
                         Description = "設定する値")]
        private string _value = string.Empty;

        new public string Description => $"変数:{Name} = \"{Value}\"";

        public SetVariableItem() { }
        public SetVariableItem(SetVariableItem? item = null) : base(item)
        {
            if (item is not null)
            {
                Name = item.Name;
                Value = item.Value;
            }
        }

        public new ICommandListItem Clone() => new SetVariableItem(this);
        
        public override ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            context.SetVariable(Name, Value);
            context.Log($"変数 {Name} = \"{Value}\" を設定しました");
            return ValueTask.FromResult(true);
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.SetVariableAI, typeof(SimpleCommand), typeof(ISetVariableAICommandSettings), CommandDef.CommandCategory.AI, displayPriority: 5, displaySubPriority: 2, displayNameJa: "変数設定(AI検出)", displayNameEn: "Set AI Variable")]
    public partial class SetVariableAIItem : CommandListItem, ISetVariableAIItem, ISetVariableAICommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1,
                         Description = "操作対象のウィンドウタイトル（空欄で全画面）")]
        private string _windowTitle = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("検出モード", EditorType.ComboBox, Group = "AI設定", Order = 1,
                         Description = "取得する値の種類", Options = "Class,Count,X,Y,Width,Height")]
        private string _aIDetectMode = "Class";
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2,
                         Description = "ウィンドウのクラス名")]
        private string _windowClassName = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ONNXモデル", EditorType.FilePicker, Group = "AI設定", Order = 2,
                         Description = "YOLOv8 ONNXモデルファイル")]
        private string _modelPath = string.Empty;
        
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
        [property: CommandProperty("変数名", EditorType.TextBox, Group = "変数設定", Order = 1,
                         Description = "結果を格納する変数名")]
        private string _name = string.Empty;

        new public string Description =>
            $"変数:{Name} / モード:{AIDetectMode} / モデル:{System.IO.Path.GetFileName(ModelPath)} / 閾値:C{ConfThreshold}/I{IoUThreshold}";

        public SetVariableAIItem() { }
        public SetVariableAIItem(SetVariableAIItem? item = null) : base(item)
        {
            if (item is not null)
            {
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                AIDetectMode = item.AIDetectMode;
                ModelPath = item.ModelPath;
                ConfThreshold = item.ConfThreshold;
                IoUThreshold = item.IoUThreshold;
                Name = item.Name;
            }
        }
        public new ICommandListItem Clone()
        {
            return new SetVariableAIItem(this);
        }
        
        public override ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            var absoluteModelPath = context.ToAbsolutePath(ModelPath);
            context.InitializeAIModel(absoluteModelPath, 640, true);

            var detections = context.DetectAI(WindowTitle, (float)ConfThreshold, (float)IoUThreshold);
        string value = AIDetectMode switch
        {
            "Class" => detections.Count > 0 ? detections[0].ClassId.ToString() : "-1",
            "Count" => detections.Count.ToString(),
            "X" => detections.Count > 0 ? (detections[0].Rect.X + detections[0].Rect.Width / 2).ToString() : "-1",
            "Y" => detections.Count > 0 ? (detections[0].Rect.Y + detections[0].Rect.Height / 2).ToString() : "-1",
            "Width" => detections.Count > 0 ? detections[0].Rect.Width.ToString() : "-1",
            "Height" => detections.Count > 0 ? detections[0].Rect.Height.ToString() : "-1",
            _ => "0",
        };
        context.SetVariable(Name, value);
            context.Log($"AI検出結果: {Name} = {value} (モード: {AIDetectMode}, 検出数: {detections.Count})");
            return ValueTask.FromResult(true);
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.SetVariableOCR, typeof(SimpleCommand), typeof(ISetVariableOCRCommandSettings), CommandDef.CommandCategory.Variable, displayPriority: 5, displaySubPriority: 3, displayNameJa: "変数設定(OCR)", displayNameEn: "Set OCR Variable")]
    public partial class SetVariableOCRItem : CommandListItem, ISetVariableOCRItem, ISetVariableOCRCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("変数名", EditorType.TextBox, Group = "変数設定", Order = 1, Description = "結果を格納する変数名")]
        private string _name = string.Empty;

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
        [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1, Description = "操作対象のウィンドウタイトル（空欄で全画面）")]
        private string _windowTitle = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2, Description = "ウィンドウのクラス名")]
        private string _windowClassName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("言語", EditorType.ComboBox, Group = "OCR設定", Order = 1, Description = "Tesseract OCRの言語", Options = "jpn,jpn+eng,eng")]
        private string _language = "jpn";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("PSM", EditorType.ComboBox, Group = "OCR設定", Order = 2, Description = "ページ分割モード", Options = "6,7,11,12,13")]
        private string _pageSegmentationMode = "6";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("最小信頼度", EditorType.Slider, Group = "OCR設定", Order = 3, Description = "この値未満なら空文字を保存", Min = 0.0, Max = 100.0, Step = 1.0)]
        private double _minConfidence = 50.0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("前処理", EditorType.ComboBox, Group = "OCR設定", Order = 4, Description = "OCR前の画像前処理", Options = "Gray,Binarize,AdaptiveThreshold,None")]
        private string _preprocessMode = "Gray";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("文字種制限", EditorType.TextBox, Group = "OCR設定", Order = 5, Description = "空欄で無効。例: 0123456789")]
        private string _whitelist = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("tessdataディレクトリ", EditorType.DirectoryPicker, Group = "詳細設定", Order = 1, Description = "必要な場合のみ指定")]
        private string _tessdataPath = string.Empty;

        new public string Description =>
            $"変数:{Name} / 領域:({X},{Y},{Width},{Height}) / 言語:{Language} / PSM:{PageSegmentationMode} / 最小信頼度:{MinConfidence:F0}";

        public SetVariableOCRItem() { }

        public SetVariableOCRItem(SetVariableOCRItem? item = null) : base(item)
        {
            if (item is not null)
            {
                Name = item.Name;
                X = item.X;
                Y = item.Y;
                Width = item.Width;
                Height = item.Height;
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                Language = item.Language;
                PageSegmentationMode = item.PageSegmentationMode;
                Whitelist = item.Whitelist;
                MinConfidence = item.MinConfidence;
                PreprocessMode = item.PreprocessMode;
                TessdataPath = item.TessdataPath;
            }
        }

        public new ICommandListItem Clone() => new SetVariableOCRItem(this);

        public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            try
            {
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

                var value = result.Confidence >= MinConfidence ? result.Text : string.Empty;
                context.SetVariable(Name, value);
                context.Log($"OCR結果: {Name} = \"{value}\" (信頼度: {result.Confidence:F1})");
                return true;
            }
            catch (Exception ex)
            {
                context.Log($"OCRエラー: {ex.Message}");
                return false;
            }
        }
    }


    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfVariable, typeof(IfVariableCommand), typeof(IIfVariableCommandSettings), CommandDef.CommandCategory.Variable, isIfCommand: true, displayPriority: 4, displaySubPriority: 5, displayNameJa: "条件 - 変数比較", displayNameEn: "If Variable")]
    public partial class IfVariableItem : CommandListItem, IIfVariableItem, IIfVariableCommandSettings, IIfItem
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("変数名", EditorType.TextBox, Group = "条件設定", Order = 1,
                         Description = "比較する変数の名前")]
        private string _name = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("演算子", EditorType.ComboBox, Group = "条件設定", Order = 2,
                         Description = "比較演算子", Options = "==,!=,>,<,>=,<=")]
        private string _operator = "==";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("比較値", EditorType.TextBox, Group = "条件設定", Order = 3,
                         Description = "比較する値")]
        private string _value = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;

        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / If {Name} {Operator} \"{Value}\"";

        public IfVariableItem() { }
        public IfVariableItem(IfVariableItem? item = null) : base(item)
        {
            if (item is not null)
            {
                Name = item.Name;
                Operator = item.Operator;
                Value = item.Value;
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone() => new IfVariableItem(this);
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.Screenshot, typeof(SimpleCommand), typeof(IScreenshotCommandSettings), CommandDef.CommandCategory.System, displayPriority: 6, displaySubPriority: 2, displayNameJa: "スクリーンショット", displayNameEn: "Screenshot")]
    public partial class ScreenshotItem : CommandListItem, IScreenshotItem, IScreenshotCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("保存先ディレクトリ", EditorType.DirectoryPicker, Group = "保存設定", Order = 1,
                         Description = "スクリーンショットの保存先")]
        private string _saveDirectory = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1,
                         Description = "キャプチャ対象のウィンドウ（空欄で全画面）")]
        private string _windowTitle = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2,
                         Description = "ウィンドウのクラス名")]
        private string _windowClassName = string.Empty;

        new public string Description =>
            $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "全画面" : $"{WindowTitle}[{WindowClassName}]")} / 保存先:{(string.IsNullOrEmpty(SaveDirectory) ? "(./Screenshots)" : SaveDirectory)}";

        public ScreenshotItem() { }
        public ScreenshotItem(ScreenshotItem? item = null) : base(item)
        {
            if (item is not null)
            {
                SaveDirectory = item.SaveDirectory;
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
            }
        }

        public new ICommandListItem Clone() => new ScreenshotItem(this);
        
        public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            try
            {
                var dir = string.IsNullOrWhiteSpace(SaveDirectory)
                    ? System.IO.Path.Combine(Environment.CurrentDirectory, "Screenshots")
                    : SaveDirectory;

                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                var fileName = $"screenshot_{context.GetLocalNow():yyyyMMdd_HHmmss}.png";
                var filePath = System.IO.Path.Combine(dir, fileName);

                await context.TakeScreenshotAsync(filePath, WindowTitle, WindowClassName, cancellationToken).ConfigureAwait(false);
                
                context.Log($"スクリーンショットを保存しました: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                context.Log($"スクリーンショットの保存に失敗しました: {ex.Message}");
                return false;
            }
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.ClickImageAI, typeof(SimpleCommand), typeof(IClickImageAICommandSettings), CommandDef.CommandCategory.AI, displayPriority: 1, displaySubPriority: 3, displayNameJa: "画像クリック(AI検出)", displayNameEn: "AI Click")]
    public partial class ClickImageAIItem : CommandListItem, IClickImageAIItem, IClickImageAICommandSettings
    {
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

        new public string Description =>
            $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / モデル:{System.IO.Path.GetFileName(ModelPath)} / クラスID:{ClassID} / 閾値:{ConfThreshold} / ボタン:{Button} / 押下維持:{HoldDurationMs}ms / 方式:{ClickInjectionMode} / 移動シミュレート:{(SimulateMouseMove ? "ON" : "OFF")}";

        public ClickImageAIItem() { }
        public ClickImageAIItem(ClickImageAIItem? item = null) : base(item)
        {
            if (item is not null)
            {
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                ModelPath = item.ModelPath;
                ClassID = item.ClassID;
                ConfThreshold = item.ConfThreshold;
                IoUThreshold = item.IoUThreshold;
                Button = item.Button;
                HoldDurationMs = item.HoldDurationMs;
                ClickInjectionMode = item.ClickInjectionMode;
                SimulateMouseMove = item.SimulateMouseMove;
            }
        }

        public new ICommandListItem Clone() => new ClickImageAIItem(this);
        
        public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            var absoluteModelPath = context.ToAbsolutePath(ModelPath);
            context.InitializeAIModel(absoluteModelPath, 640, true);

            var detections = context.DetectAI(WindowTitle, (float)ConfThreshold, (float)IoUThreshold);
            var targetDetections = detections.Where(d => d.ClassId == ClassID).ToList();

            if (targetDetections.Count > 0)
            {
                var best = targetDetections.OrderByDescending(d => d.Score).First();
                int centerX = best.Rect.X + best.Rect.Width / 2;
                int centerY = best.Rect.Y + best.Rect.Height / 2;

                await context.ClickAsync(centerX, centerY, Button, WindowTitle, WindowClassName, HoldDurationMs, ClickInjectionMode, SimulateMouseMove).ConfigureAwait(false);
                context.Log($"AI画像をクリックしました。({centerX}, {centerY}) / クラスID: {best.ClassId} / スコア: {best.Score:F2}");
                return true;
            }

            context.Log($"クラスID {ClassID} の画像が見つかりませんでした。");
            return false;
        }
    }

