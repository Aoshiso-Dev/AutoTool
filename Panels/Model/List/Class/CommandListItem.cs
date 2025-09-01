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
    }

    [CommandDef.CommandDefinition("Wait_Image", typeof(WaitImageCommand), typeof(IWaitImageCommandSettings), CommandDef.CommandCategory.Action)]
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

    [CommandDef.CommandDefinition("Click_Image", typeof(ClickImageCommand), typeof(IClickImageCommandSettings), CommandDef.CommandCategory.Action)]
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

    [CommandDef.CommandDefinition("Hotkey", typeof(HotkeyCommand), typeof(IHotkeyCommandSettings), CommandDef.CommandCategory.Action)]
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

    [CommandDef.CommandDefinition("Click", typeof(ClickCommand), typeof(IClickCommandSettings), CommandDef.CommandCategory.Action)]
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
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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
    }

    [CommandDef.CommandDefinition("Wait", typeof(WaitCommand), typeof(IWaitCommandSettings), CommandDef.CommandCategory.Action)]
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
        private string _imagePath = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private double _threshold = 0.8;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private Color? _searchColor = null;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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
        private string _imagePath = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private double _threshold = 0.8;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private Color? _searchColor = null;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowClassName = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _modelPath = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _classID = 0;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private double _confThreshold = 0.5;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowClassName = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _modelPath = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _classID = 0;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private double _confThreshold = 0.5;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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

    [CommandDef.CommandDefinition("Execute", typeof(ExecuteCommand), typeof(IExecuteCommandSettings), CommandDef.CommandCategory.System)]
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

    [CommandDef.CommandDefinition("SetVariable", typeof(SetVariableCommand), typeof(ISetVariableCommandSettings), CommandDef.CommandCategory.Variable)]
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

    [CommandDef.CommandDefinition("SetVariable_AI", typeof(SetVariableAICommand), typeof(ISetVariableAICommandSettings), CommandDef.CommandCategory.AI)]
    public partial class SetVariableAIItem : CommandListItem, ISetVariableAIItem, ISetVariableAICommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _aIDetectMode = "Class";
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowClassName = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _modelPath = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private double _confThreshold = 0.5;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private double _ioUThreshold = 0.25;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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
    }


    [CommandDef.CommandDefinition("IF_Variable", typeof(IfVariableCommand), typeof(IIfVariableCommandSettings), CommandDef.CommandCategory.Variable, isIfCommand: true)]
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

    [CommandDef.CommandDefinition("Screenshot", typeof(ScreenshotCommand), typeof(IScreenshotCommandSettings), CommandDef.CommandCategory.System)]
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
    }

    [CommandDef.CommandDefinition("Click_Image_AI", typeof(ClickImageAICommand), typeof(IClickImageAICommandSettings), CommandDef.CommandCategory.AI)]
    public partial class ClickImageAIItem : CommandListItem, IClickImageAIItem, IClickImageAICommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _windowClassName = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _modelPath = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _classID = 0;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private double _confThreshold = 0.5;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private double _ioUThreshold = 0.25;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
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
    }
}
