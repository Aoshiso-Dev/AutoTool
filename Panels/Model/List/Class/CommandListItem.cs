using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MacroPanels.List;
using System.Windows;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.Model.List.Interface;
using System.Windows.Controls;
using System.Text.Json.Serialization;
using OpenCvSharp.Features2D;
using System.Windows.Input;
using System.Windows.Media;
using MacroPanels.Model.MacroFactory;

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
        protected string _itemType = "None";
        [ObservableProperty]
        protected string _description = string.Empty;
        [ObservableProperty]
        protected int _nestLevel = 0;
        [ObservableProperty]
        protected bool _isInLoop = false;
        [ObservableProperty]
        protected bool _isInIf = false;
        [ObservableProperty]
        protected int _progress = 0;

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
    }

    [SimpleCommandBinding(typeof(WaitImageCommand), typeof(IWaitImageCommandSettings))]
    public partial class WaitImageItem : CommandListItem, IWaitImageItem, IWaitImageCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _imagePath = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private double _threshold = 0.8;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private Color? _searchColor = null;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _timeout = 5000;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _interval = 500;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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
    }

    [SimpleCommandBinding(typeof(ClickImageCommand), typeof(IClickImageCommandSettings))]
    public partial class ClickImageItem : CommandListItem, IClickImageItem, IClickImageCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _imagePath = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private double _threshold = 0.8;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private Color? _searchColor = null;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _timeout = 5000;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _interval = 500;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private System.Windows.Input.MouseButton _button = System.Windows.Input.MouseButton.Left;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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
    }

    [SimpleCommandBinding(typeof(HotkeyCommand), typeof(IHotkeyCommandSettings))]
    public partial class HotkeyItem : CommandListItem, IHotkeyItem, IHotkeyCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private bool _ctrl = false;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private bool _alt = false;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private bool _shift = false;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private System.Windows.Input.Key _key = System.Windows.Input.Key.Escape;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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
    }

    [SimpleCommandBinding(typeof(ClickCommand), typeof(IClickCommandSettings))]
    public partial class ClickItem : CommandListItem, IClickItem, IClickCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _x = 0;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _y = 0;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private System.Windows.Input.MouseButton _button = System.Windows.Input.MouseButton.Left;
        //[ObservableProperty]
        //private string _windowTitle = string.Empty;
        //[ObservableProperty]
        //private string _windowClassName = string.Empty;

        //new public string Description => $"対象：{(string.IsNullOrEmpty(WindowTitle) ? "グローバル" : WindowTitle)} / X座標:{X} / Y座標:{Y} / ボタン:{Button}";
        
        new public string Description => $"X座標:{X} / Y座標:{Y} / ボタン:{Button}";

        public ClickItem() { }

        public ClickItem(ClickItem? item = null) : base(item)
        {
            if (item != null)
            {
                X = item.X;
                Y = item.Y;
                Button = item.Button;
                //WindowTitle = item.WindowTitle;
                //WindowClassName = item.WindowClassName;
            }
        }

        public new ICommandListItem Clone()
        {
            return new ClickItem(this);
        }
    }

    [SimpleCommandBinding(typeof(WaitCommand), typeof(IWaitCommandSettings))]
    public partial class WaitItem : CommandListItem, IWaitItem, IWaitCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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
    }

    public partial class IfImageExistItem : CommandListItem, IIfItem, IIfImageExistItem, IWaitImageCommandSettings
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
        private string _imagePath = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private double _threshold = 0.8;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private Color? _searchColor = null;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _timeout = 5000;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _interval = 500;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowClassName = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;


        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / 対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]") } / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";

        public IfImageExistItem() { }

        public IfImageExistItem(IfImageExistItem? item = null) : base(item)
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
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone()
        {
            return new IfImageExistItem(this);
        }
    }

    public partial class IfImageNotExistItem : CommandListItem, IIfItem, IIfImageNotExistItem, IWaitImageCommandSettings
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
        private string _imagePath = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private double _threshold = 0.8;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private Color? _searchColor = null;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _timeout = 5000;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _interval = 500;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowClassName = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;

        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / 対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]") } / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";

        public IfImageNotExistItem() { }

        public IfImageNotExistItem(IfImageNotExistItem? item = null) : base(item)
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
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone()
        {
            return new IfImageNotExistItem(this);
        }
    }


    [SimpleCommandBinding(typeof(IfEndCommand), typeof(ICommandSettings))]
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

    [SimpleCommandBinding(typeof(LoopEndCommand), typeof(ICommandSettings))]
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

    [SimpleCommandBinding(typeof(LoopBreakCommand), typeof(ICommandSettings))]
    public partial class LoopBreakItem : CommandListItem, ILoopBreakItem, ICommandSettings
    {
        public LoopBreakItem() { }

        public LoopBreakItem(LoopBreakItem? item = null) : base(item) { }

        public new ICommandListItem Clone()
        {
            return new LoopBreakItem(this);
        }
    }

    public partial class IfImageExistAIItem : CommandListItem, IIfItem, IIfImageExistAIItem, IIfImageExistAISettings
    {
        [ObservableProperty]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        private string _windowClassName = string.Empty;
        [ObservableProperty]
        private string _modelPath = string.Empty;
        [ObservableProperty]
        private int _classID = 0;
        [ObservableProperty]
        private double _confThreshold = 0.5f;
        [ObservableProperty]
        private double _ioUThreshold = 0.25f;
        [ObservableProperty]
        private string[] _targetLabels = Array.Empty<string>();
        [ObservableProperty]
        private int _timeout = 5000;
        [ObservableProperty]
        private int _interval = 500;
        [ObservableProperty]
        private ICommandListItem? _pair = null;

        new public string Description =>
             $"{LineNumber}->{Pair?.LineNumber} 対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / クラスID:{ClassID} / 閾値:{ConfThreshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";

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
                Timeout = item.Timeout;
                Interval = item.Interval;
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone()
        {
            return new IfImageExistAIItem(this);
        }
    }

    public partial class IfImageNotExistAIItem : CommandListItem, IIfItem, IIfImageNotExistAIItem, IIfImageExistAISettings
    {
        [ObservableProperty]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        private string _windowClassName = string.Empty;
        [ObservableProperty]
        private string _modelPath = string.Empty;
        [ObservableProperty]
        private int _classID = 0;
        [ObservableProperty]
        private double _confThreshold = 0.45f;
        [ObservableProperty]
        private double _ioUThreshold = 0.25f;
        [ObservableProperty]
        private int _timeout = 5000;
        [ObservableProperty]
        private int _interval = 500;
        [ObservableProperty]
        private ICommandListItem? _pair = null;

        new public string Description =>
            $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / クラスID:{ClassID} / 閾値:{ConfThreshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";
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
                Timeout = item.Timeout;
                Interval = item.Interval;
                Pair = item.Pair;
            }
        }
        public new ICommandListItem Clone()
        {
            return new IfImageNotExistAIItem(this);
        }
    }

    [SimpleCommandBinding(typeof(ExecuteCommand), typeof(IExecuteCommandSettings))]
    public partial class ExecuteItem : CommandListItem, IExecuteItem, IExecuteCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _programPath = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _arguments = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _workingDirectory = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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
    }

    [SimpleCommandBinding(typeof(SetVariableCommand), typeof(ISetVariableCommandSettings))]
    public partial class SetVariableItem : CommandListItem, ISetVariableItem, ISetVariableCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _name = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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
    }

    [SimpleCommandBinding(typeof(SetVariableAICommand), typeof(ISetVariableAICommandSettings))]
    public partial class SetVariableAIItem : CommandListItem, ISetVariableAIItem, ISetVariableAICommandSettings
    {
        [ObservableProperty]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        private string _windowClassName = string.Empty;
        [ObservableProperty]
        private string _modelPath = string.Empty;
        [ObservableProperty]
        private double _confThreshold = 0.45f;
        [ObservableProperty]
        private double _ioUThreshold = 0.25f;
        [ObservableProperty]
        private int _timeout = 5000;
        [ObservableProperty]
        private int _interval = 500;
        [ObservableProperty]
        private string _name = string.Empty;
        new public string Description =>
            $"変数:{Name} / モデル:{System.IO.Path.GetFileName(ModelPath)} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";
        public SetVariableAIItem() { }
        public SetVariableAIItem(SetVariableAIItem? item = null) : base(item)
        {
            if (item != null)
            {
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                ModelPath = item.ModelPath;
                ConfThreshold = item.ConfThreshold;
                IoUThreshold = item.IoUThreshold;
                Timeout = item.Timeout;
                Interval = item.Interval;
                Name = item.Name;
            }
        }
        public new ICommandListItem Clone()
        {
            return new SetVariableAIItem(this);
        }
    }


    public partial class IfVariableItem : CommandListItem, IIfVariableItem, IIfVariableCommandSettings, IIfItem
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _name = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _operator = "==";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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

    [SimpleCommandBinding(typeof(ScreenshotCommand), typeof(IScreenshotCommandSettings))]
    public partial class ScreenshotItem : CommandListItem, IScreenshotItem, IScreenshotCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _saveDirectory = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowTitle = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowClassName = string.Empty;

        new public string Description =>
            $"対象：{(string.IsNullOrEmpty(_windowTitle) && string.IsNullOrEmpty(_windowClassName) ? "全画面" : $"{_windowTitle}[{_windowClassName}]")} / 保存先:{(string.IsNullOrEmpty(_saveDirectory) ? "(./Screenshots)" : _saveDirectory)}";

        public ScreenshotItem() { }
        public ScreenshotItem(ScreenshotItem? item = null) : base(item)
        {
            if (item != null)
            {
                _saveDirectory = item._saveDirectory;
                _windowTitle = item._windowTitle;
                _windowClassName = item._windowClassName;
            }
        }

        public new ICommandListItem Clone() => new ScreenshotItem(this);
    }
}
