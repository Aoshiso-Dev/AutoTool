using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Commands;
using AutoTool.Panels.Model.List.Interface;
using AutoTool.Panels.Attributes;
using CommandDef = AutoTool.Panels.Model.CommandDefinition;

namespace AutoTool.Panels.List.Class;

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.WaitImage, typeof(SimpleCommand), typeof(IWaitImageCommandSettings), CommandDef.CommandCategory.Action, displayPriority: 2, displaySubPriority: 3, displayNameJa: "画像待機", displayNameEn: "Wait for Image")]
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

        new public string Description => $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]") } / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";

        public WaitImageItem() { }

        public WaitImageItem(WaitImageItem? item = null) : base(item)
        {
            if (item != null)
            {
                ImagePath = item.ImagePath;
                Threshold = item.Threshold;
                SearchColor = item.SearchColor;
                Timeout = item.Timeout;
                Interval = item.Interval;
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
            }
        }

        public new ICommandListItem Clone()
        {
            return new WaitImageItem(this);
        }
        
        public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var absolutePath = context.ToAbsolutePath(ImagePath);

            while (stopwatch.ElapsedMilliseconds < Timeout)
            {
                var point = await context.SearchImageAsync(
                    absolutePath,
                    Threshold,
                    SearchColor,
                    WindowTitle,
                    WindowClassName,
                    cancellationToken);

                if (point != null)
                {
                    context.Log($"画像が見つかりました。({point.Value.X}, {point.Value.Y})");
                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                var progress = Timeout > 0 ? (int)((stopwatch.ElapsedMilliseconds * 100) / Timeout) : 100;
                context.ReportProgress(Math.Clamp(progress, 0, 100));

                await Task.Delay(Interval, cancellationToken);
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

        new public string Description => $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]") } / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms / ボタン:{Button}";

        public ClickImageItem() { }

        public ClickImageItem(ClickImageItem? item = null) : base(item)
        {
            if (item != null)
            {
                ImagePath = item.ImagePath;
                Threshold = item.Threshold;
                SearchColor = item.SearchColor;
                Timeout = item.Timeout;
                Interval = item.Interval;
                Button = item.Button;
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
            }
        }

        public new ICommandListItem Clone()
        {
            return new ClickImageItem(this);
        }
        
        public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var absolutePath = context.ToAbsolutePath(ImagePath);

            while (stopwatch.ElapsedMilliseconds < Timeout)
            {
                var point = await context.SearchImageAsync(
                    absolutePath,
                    Threshold,
                    SearchColor,
                    WindowTitle,
                    WindowClassName,
                    cancellationToken);

                if (point != null)
                {
                    await context.ClickAsync(point.Value.X, point.Value.Y, Button, WindowTitle, WindowClassName);
                    context.Log($"画像をクリックしました。({point.Value.X}, {point.Value.Y})");
                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                var progress = Timeout > 0 ? (int)((stopwatch.ElapsedMilliseconds * 100) / Timeout) : 100;
                context.ReportProgress(Math.Clamp(progress, 0, 100));

                await Task.Delay(Interval, cancellationToken);
            }

            context.Log("画像が見つかりませんでした。");
            return false;
        }
    }

