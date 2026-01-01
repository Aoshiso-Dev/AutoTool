using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Commands;
using MacroPanels.Model.List.Interface;
using MacroPanels.Attributes;
using CommandDef = MacroPanels.Model.CommandDefinition;

namespace MacroPanels.List.Class
{
    public partial class CommandListItem : ObservableObject, ICommandListItem
    {
        [ObservableProperty]
        protected bool _isEnable = true;
        [ObservableProperty]
        protected bool _isRunning = false;
        [ObservableProperty]
        protected bool _isSelected = false;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        protected int _lineNumber = 0;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayName))]
        protected string _itemType = "None";
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FullDescription))]
        protected string _description = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FullDescription))]
        [property: CommandProperty("コメント", EditorType.MultiLineTextBox, Group = "その他", Order = 99,
                         Description = "このコマンドのメモ")]
        protected string _comment = string.Empty;
        [ObservableProperty]
        protected int _nestLevel = 0;
        [ObservableProperty]
        protected bool _isInLoop = false;
        [ObservableProperty]
        protected bool _isInIf = false;
        [ObservableProperty]
        protected int _progress = 0;

        /// <summary>
        /// UI表示用の日本語名を取得
        /// </summary>
        public string DisplayName => CommandDef.CommandRegistry.DisplayOrder.GetDisplayName(ItemType);

        /// <summary>
        /// カテゴリ名を取得
        /// </summary>
        public string CategoryName => CommandDef.CommandRegistry.DisplayOrder.GetCategoryName(ItemType);

        /// <summary>
        /// コメント付きの完全な説明を取得
        /// </summary>
        public string FullDescription
        {
            get
            {
                var baseDesc = Description;
                if (!string.IsNullOrWhiteSpace(Comment))
                {
                    return string.IsNullOrWhiteSpace(baseDesc) 
                        ? $"💬 {Comment}" 
                        : $"{baseDesc} 💬 {Comment}";
                }
                return baseDesc;
            }
        }

        /// <summary>
        /// コメントが設定されているかどうか
        /// </summary>
        public bool HasComment => !string.IsNullOrWhiteSpace(Comment);

        public CommandListItem() { }

        public CommandListItem(CommandListItem? item)
        {
            if (item != null)
            {
                IsEnable = item.IsEnable;
                IsRunning = item.IsRunning;
                IsSelected = item.IsSelected;
                LineNumber = item.LineNumber;
                ItemType = item.ItemType;
                Comment = item.Comment;
                NestLevel = item.NestLevel;
                IsInLoop = item.IsInLoop;
                IsInIf = item.IsInIf;
                Progress = item.Progress;
            }
        }

        public ICommandListItem Clone()
        {
            return new CommandListItem(this);
        }
        
        /// <summary>
        /// Execute the command logic (override in derived classes)
        /// </summary>
        public virtual Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    [CommandDef.CommandDefinition("Wait_Image", typeof(SimpleCommand), typeof(IWaitImageCommandSettings), CommandDef.CommandCategory.Action)]
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


    [CommandDef.CommandDefinition("Click_Image", typeof(SimpleCommand), typeof(IClickImageCommandSettings), CommandDef.CommandCategory.Action)]
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

    [CommandDef.CommandDefinition("Hotkey", typeof(SimpleCommand), typeof(IHotkeyCommandSettings), CommandDef.CommandCategory.Action)]
    public partial class HotkeyItem : CommandListItem, IHotkeyItem, IHotkeyCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("Ctrl", EditorType.CheckBox, Group = "修飾キー", Order = 1)]
        private bool _ctrl = false;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("Alt", EditorType.CheckBox, Group = "修飾キー", Order = 2)]
        private bool _alt = false;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("Shift", EditorType.CheckBox, Group = "修飾キー", Order = 3)]
        private bool _shift = false;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("キー", EditorType.KeyPicker, Group = "キー設定", Order = 1,
                         Description = "押すキー")]
        private System.Windows.Input.Key _key = System.Windows.Input.Key.Escape;
        
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

        new public string Description
        {
            get
            {
                var target = string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]";

                var keys = new List<string>();

                if (Ctrl) keys.Add("Ctrl");
                if (Alt) keys.Add("Alt");
                if (Shift) keys.Add("Shift");
                keys.Add(Key.ToString());

                return $"対象：{target} / キー：{string.Join(" + ", keys)}";
            }
        }

        public HotkeyItem() { }

        public HotkeyItem(HotkeyItem? item = null) : base(item)
        {
            if (item != null)
            {
                Ctrl = item.Ctrl;
                Alt = item.Alt;
                Shift = item.Shift;
                Key = item.Key;
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
            }
        }

        public new ICommandListItem Clone()
        {
            return new HotkeyItem(this);
        }
        
        public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            await context.SendHotkeyAsync(Key, Ctrl, Alt, Shift, WindowTitle, WindowClassName);
            
            var keys = new List<string>();
            if (Ctrl) keys.Add("Ctrl");
            if (Alt) keys.Add("Alt");
            if (Shift) keys.Add("Shift");
            keys.Add(Key.ToString());
            
            context.Log($"キー送信: {string.Join(" + ", keys)}");
            return true;
        }
    }

    [CommandDef.CommandDefinition("Click", typeof(SimpleCommand), typeof(IClickCommandSettings), CommandDef.CommandCategory.Action)]
    public partial class ClickItem : CommandListItem, IClickItem, IClickCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("座標", EditorType.PointPicker, Group = "座標", Order = 1,
                         Description = "クリックする座標")]
        private int _x = 0;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _y = 0;
        
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

        new public string Description => $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / X座標:{X} / Y座標:{Y} / ボタン:{Button}";

        public ClickItem() { }

        public ClickItem(ClickItem? item = null) : base(item)
        {
            if (item != null)
            {
                X = item.X;
                Y = item.Y;
                Button = item.Button;
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
            }
        }

        public new ICommandListItem Clone()
        {
            return new ClickItem(this);
        }
        
        public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            await context.ClickAsync(X, Y, Button, WindowTitle, WindowClassName);
            
            var targetDescription = string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName)
                ? "グローバル"
                : $"{WindowTitle}[{WindowClassName}]";
            
            context.Log($"クリックしました。対象: {targetDescription} ({X}, {Y})");
            return true;
        }
    }

    [CommandDef.CommandDefinition("Wait", typeof(SimpleCommand), typeof(IWaitCommandSettings), CommandDef.CommandCategory.Action)]
    public partial class WaitItem : CommandListItem, IWaitItem, IWaitCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("待機時間", EditorType.NumberBox, Group = "基本設定", Order = 1,
                         Description = "指定した時間待機します", Unit = "ミリ秒", Min = 0)]
        private int _wait = 5000;

        new public string Description => $"待機時間:{Wait}ms";
        public WaitItem() { }

        public WaitItem(WaitItem? item = null) : base(item)
        {
            if (item != null)
            {
                Wait = item.Wait;
            }
        }

        public new ICommandListItem Clone()
        {
            return new WaitItem(this);
        }
        
        public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            while (stopwatch.ElapsedMilliseconds < Wait)
            {
                if (cancellationToken.IsCancellationRequested) return false;
                
                var progress = Wait > 0 ? (int)((stopwatch.ElapsedMilliseconds * 100) / Wait) : 100;
                context.ReportProgress(Math.Clamp(progress, 0, 100));
                
                await Task.Delay(100, cancellationToken);
            }
            
            context.Log("待機完了");
            return true;
        }
    }

    [CommandDef.CommandDefinition("IF_ImageExist", typeof(IfImageExistCommand), typeof(IIfImageCommandSettings), CommandDef.CommandCategory.Control, isIfCommand: true)]
    public partial class IfImageExistItem : CommandListItem, IIfItem, IIfImageExistItem, IIfImageCommandSettings
    {
        new public bool IsEnable
        {
            get
            {
                return base.IsEnable;
            }
            set
            {
                if (base.IsEnable != value)
                {
                    base.IsEnable = value;

                    if (Pair != null)
                    {
                        Pair.IsEnable = value;
                    }
                }
            }
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

    [CommandDef.CommandDefinition("IF_ImageNotExist", typeof(IfImageNotExistCommand), typeof(IIfImageCommandSettings), CommandDef.CommandCategory.Control, isIfCommand: true)]
    public partial class IfImageNotExistItem : CommandListItem, IIfItem, IIfImageNotExistItem, IIfImageCommandSettings
    {
        new public bool IsEnable
        {
            get
            {
                return base.IsEnable;
            }
            set
            {
                if (base.IsEnable != value)
                {
                    base.IsEnable = value;

                    if (Pair != null)
                    {
                        Pair.IsEnable = value;
                    }
                }
            }
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
    [CommandDef.CommandDefinition("IF_End", typeof(IfEndCommand), typeof(ICommandSettings), CommandDef.CommandCategory.Control)]
    public partial class IfEndItem : CommandListItem, IIfEndItem, ICommandSettings
    {
        new public bool IsEnable
        {
            get
            {
                return base.IsEnable;
            }
            set
            {
                if (base.IsEnable != value)
                {
                    base.IsEnable = value;

                    if (Pair != null)
                    {
                        Pair.IsEnable = value;
                    }
                }
            }
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

    [CommandDef.CommandDefinition("Loop", typeof(LoopCommand), typeof(ILoopCommandSettings), CommandDef.CommandCategory.Control, isLoopCommand: true)]
    public partial class LoopItem : CommandListItem, ILoopItem, ICommandListItem
    {
        new public bool IsEnable
        {
            get
            {
                return base.IsEnable;
            }
            set
            {
                if (base.IsEnable != value)
                {
                    base.IsEnable = value;

                    if (Pair != null)
                    {
                        Pair.IsEnable = value;
                    }
                }
            }
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
    [CommandDef.CommandDefinition("Loop_End", typeof(LoopEndCommand), typeof(ICommandSettings), CommandDef.CommandCategory.Control)]
    public partial class LoopEndItem : CommandListItem, ILoopEndItem, ICommandSettings
    {
        new public bool IsEnable
        {
            get
            {
                return base.IsEnable;
            }
            set
            {
                if (base.IsEnable != value)
                {
                    base.IsEnable = value;

                    if (Pair != null)
                    {
                        Pair.IsEnable = value;
                    }
                }
            }
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
    [CommandDef.CommandDefinition("Loop_Break", typeof(LoopBreakCommand), typeof(ICommandSettings), CommandDef.CommandCategory.Control)]
    public partial class LoopBreakItem : CommandListItem, ILoopBreakItem, ICommandSettings
    {
        public LoopBreakItem() { }

        public LoopBreakItem(LoopBreakItem? item = null) : base(item) { }

        public new ICommandListItem Clone()
        {
            return new LoopBreakItem(this);
        }
    }

    [CommandDef.CommandDefinition("IF_ImageExist_AI", typeof(IfImageExistAICommand), typeof(IIfImageExistAISettings), CommandDef.CommandCategory.AI, isIfCommand: true)]
    public partial class IfImageExistAIItem : CommandListItem, IIfItem, IIfImageExistAIItem, IIfImageExistAISettings
    {
        new public bool IsEnable
        {
            get => base.IsEnable;
            set
            {
                if (base.IsEnable != value)
                {
                    base.IsEnable = value;
                    if (Pair != null) Pair.IsEnable = value;
                }
            }
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

    [CommandDef.CommandDefinition("IF_ImageNotExist_AI", typeof(IfImageNotExistAICommand), typeof(IIfImageNotExistAISettings), CommandDef.CommandCategory.AI, isIfCommand: true)]
    public partial class IfImageNotExistAIItem : CommandListItem, IIfItem, IIfImageNotExistAIItem, IIfImageNotExistAISettings
    {
        new public bool IsEnable
        {
            get => base.IsEnable;
            set
            {
                if (base.IsEnable != value)
                {
                    base.IsEnable = value;
                    if (Pair != null) Pair.IsEnable = value;
                }
            }
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

    [CommandDef.CommandDefinition("Execute", typeof(SimpleCommand), typeof(IExecuteCommandSettings), CommandDef.CommandCategory.System)]
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
            if (item != null)
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
        
        public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            try
            {
                await context.ExecuteProgramAsync(ProgramPath, Arguments, WorkingDirectory, WaitForExit, cancellationToken);
                context.Log($"プログラムを実行しました: {ProgramPath}");
                return true;
            }
            catch (Exception ex)
            {
                context.Log($"プログラム実行エラー: {ex.Message}");
                return false;
            }
        }
    }

    [CommandDef.CommandDefinition("SetVariable", typeof(SimpleCommand), typeof(ISetVariableCommandSettings), CommandDef.CommandCategory.Variable)]
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
            if (item != null)
            {
                Name = item.Name;
                Value = item.Value;
            }
        }

        public new ICommandListItem Clone() => new SetVariableItem(this);
        
        public override Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            context.SetVariable(Name, Value);
            context.Log($"変数 {Name} = \"{Value}\" を設定しました");
            return Task.FromResult(true);
        }
    }

    [CommandDef.CommandDefinition("SetVariable_AI", typeof(SimpleCommand), typeof(ISetVariableAICommandSettings), CommandDef.CommandCategory.AI)]
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
            if (item != null)
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
        
        public override Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            var absoluteModelPath = context.ToAbsolutePath(ModelPath);
            context.InitializeAIModel(absoluteModelPath, 640, true);

            var detections = context.DetectAI(WindowTitle, (float)ConfThreshold, (float)IoUThreshold);
            
            string value;
            switch (AIDetectMode)
            {
                case "Class":
                    value = detections.Count > 0 ? detections[0].ClassId.ToString() : "-1";
                    break;
                case "Count":
                    value = detections.Count.ToString();
                    break;
                case "X":
                    value = detections.Count > 0 ? (detections[0].Rect.X + detections[0].Rect.Width / 2).ToString() : "-1";
                    break;
                case "Y":
                    value = detections.Count > 0 ? (detections[0].Rect.Y + detections[0].Rect.Height / 2).ToString() : "-1";
                    break;
                case "Width":
                    value = detections.Count > 0 ? detections[0].Rect.Width.ToString() : "-1";
                    break;
                case "Height":
                    value = detections.Count > 0 ? detections[0].Rect.Height.ToString() : "-1";
                    break;
                default:
                    value = "0";
                    break;
            }
            
            context.SetVariable(Name, value);
            context.Log($"AI検出結果: {Name} = {value} (モード: {AIDetectMode}, 検出数: {detections.Count})");
            return Task.FromResult(true);
        }
    }


    [CommandDef.CommandDefinition("IF_Variable", typeof(IfVariableCommand), typeof(IIfVariableCommandSettings), CommandDef.CommandCategory.Variable, isIfCommand: true)]
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
            if (item != null)
            {
                Name = item.Name;
                Operator = item.Operator;
                Value = item.Value;
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone() => new IfVariableItem(this);
    }

    [CommandDef.CommandDefinition("Screenshot", typeof(SimpleCommand), typeof(IScreenshotCommandSettings), CommandDef.CommandCategory.System)]
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
            if (item != null)
            {
                SaveDirectory = item.SaveDirectory;
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
            }
        }

        public new ICommandListItem Clone() => new ScreenshotItem(this);
        
        public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
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

                var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var filePath = System.IO.Path.Combine(dir, fileName);

                await context.TakeScreenshotAsync(filePath, WindowTitle, WindowClassName, cancellationToken);
                
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

    [CommandDef.CommandDefinition("Click_Image_AI", typeof(SimpleCommand), typeof(IClickImageAICommandSettings), CommandDef.CommandCategory.AI)]
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
        private System.Windows.Input.MouseButton _button = System.Windows.Input.MouseButton.Left;

        new public string Description =>
            $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / モデル:{System.IO.Path.GetFileName(ModelPath)} / クラスID:{ClassID} / 閾値:{ConfThreshold} / ボタン:{Button}";

        public ClickImageAIItem() { }
        public ClickImageAIItem(ClickImageAIItem? item = null) : base(item)
        {
            if (item != null)
            {
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                ModelPath = item.ModelPath;
                ClassID = item.ClassID;
                ConfThreshold = item.ConfThreshold;
                IoUThreshold = item.IoUThreshold;
                Button = item.Button;
            }
        }

        public new ICommandListItem Clone() => new ClickImageAIItem(this);
        
        public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
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

                await context.ClickAsync(centerX, centerY, Button, WindowTitle, WindowClassName);
                context.Log($"AI画像クリックしました。({centerX}, {centerY}) ClassId: {best.ClassId}, Score: {best.Score:F2}");
                return true;
            }

            context.Log($"クラスID {ClassID} の画像が見つかりませんでした。");
            return false;
        }
    }
}
