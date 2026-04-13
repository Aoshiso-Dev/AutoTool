using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Commands;
using AutoTool.Panels.Model.List.Interface;
using AutoTool.Panels.Attributes;
using CommandDef = AutoTool.Panels.Model.CommandDefinition;

namespace AutoTool.Panels.List.Class;

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.Hotkey, typeof(SimpleCommand), typeof(IHotkeyCommandSettings), CommandDef.CommandCategory.Action, displayPriority: 2, displaySubPriority: 1, displayNameJa: "ホットキー", displayNameEn: "Hotkey")]
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

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.Click, typeof(SimpleCommand), typeof(IClickCommandSettings), CommandDef.CommandCategory.Action, displayPriority: 1, displaySubPriority: 1, displayNameJa: "クリック", displayNameEn: "Click")]
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

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.Wait, typeof(SimpleCommand), typeof(IWaitCommandSettings), CommandDef.CommandCategory.Action, displayPriority: 2, displaySubPriority: 2, displayNameJa: "待機", displayNameEn: "Wait")]
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

