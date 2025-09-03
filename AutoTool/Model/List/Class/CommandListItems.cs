using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using AutoTool.Model.List.Interface;
using AutoTool.Command.Interface;
using AutoTool.Command.Class;
using AutoTool.Model.CommandDefinition;
using System.Collections.Generic;

namespace AutoTool.Model.List.Class
{
    public partial class CommandListItem : ObservableObject, ICommandListItem
    {
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

        // IsEnable�͎蓮�Ŏ������邽�߁AObservableProperty�͎g�p���Ȃ�
        private bool _isEnable = true;

        /// <summary>
        /// IsEnable�̕ύX����Pair���������邩�ǂ���
        /// </summary>
        protected virtual bool SyncPairOnIsEnableChange => false;

        /// <summary>
        /// Pair�A�C�e�����擾�i�h���N���X�ŃI�[�o�[���C�h�j
        /// </summary>
        protected virtual ICommandListItem? GetPair() => null;

        /// <summary>
        /// IsEnable�v���p�e�B - Pair�Ƃ̓����������܂�
        /// </summary>
        public virtual bool IsEnable
        {
            get => _isEnable;
            set
            {
                if (_isEnable != value)
                {
                    _isEnable = value;
                    OnPropertyChanged();

                    // Pair�Ƃ̓������K�v�ȏꍇ
                    if (SyncPairOnIsEnableChange)
                    {
                        var pair = GetPair();
                        if (pair != null)
                        {
                            pair.IsEnable = value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// UI�\���p�̓��{�ꖼ���擾
        /// </summary>
        public string DisplayName => AutoTool.Model.CommandDefinition.CommandRegistry.DisplayOrder.GetDisplayName(ItemType);

        /// <summary>
        /// �J�e�S�������擾
        /// </summary>
        public string CategoryName => AutoTool.Model.CommandDefinition.CommandRegistry.DisplayOrder.GetCategoryName(ItemType);

        /// <summary>
        /// �R�����g�t���̊��S�Ȑ������擾
        /// </summary>
        public string FullDescription
        {
            get
            {
                var baseDesc = Description;
                if (!string.IsNullOrWhiteSpace(Comment))
                {
                    return string.IsNullOrWhiteSpace(baseDesc)
                        ? $"?? {Comment}"
                        : $"{baseDesc} ?? {Comment}";
                }
                return baseDesc;
            }
        }

        /// <summary>
        /// �R�����g���ݒ肳��Ă��邩�ǂ���
        /// </summary>
        public bool HasComment => !string.IsNullOrWhiteSpace(Comment);

        /// <summary>
        /// �E�B���h�E�Ώۂ̕\�������擾�i���ʃw���p�[���\�b�h�j
        /// </summary>
        protected static string FormatWindowTarget(string windowTitle, string windowClassName, string defaultName = "�O���[�o��")
        {
            return string.IsNullOrEmpty(windowTitle) && string.IsNullOrEmpty(windowClassName)
                ? defaultName
                : $"{windowTitle}[{windowClassName}]";
        }

        /// <summary>
        /// �t�@�C�����݂̂�\���p�Ɏ擾�i���ʃw���p�[���\�b�h�j
        /// </summary>
        protected static string FormatFileName(string filePath)
        {
            return string.IsNullOrEmpty(filePath) ? filePath : System.IO.Path.GetFileName(filePath);
        }

        /// <summary>
        /// Pair�͈̔͂�\���p�Ƀt�H�[�}�b�g�i���ʃw���p�[���\�b�h�j
        /// </summary>
        protected string FormatPairRange(ICommandListItem? pair)
        {
            return $"{LineNumber}->{pair?.LineNumber}";
        }

        public CommandListItem() { }

        public CommandListItem(CommandListItem? item)
        {
            if (item != null)
            {
                _isEnable = item._isEnable;  // �v���C�x�[�g�t�B�[���h�𒼐ڐݒ�
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

    [CommandDefinition("Wait_Image", typeof(WaitImageCommand), typeof(IWaitImageCommandSettings), CommandCategory.Action)]
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

        new public string Description => $"�ΏہF{FormatWindowTarget(WindowTitle, WindowClassName)} / �p�X:{FormatFileName(ImagePath)} / 臒l:{Threshold} / �^�C���A�E�g:{Timeout}ms / �Ԋu:{Interval}ms";

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

    [CommandDefinition("Click_Image", typeof(ClickImageCommand), typeof(IClickImageCommandSettings), CommandCategory.Action)]
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
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private bool _useBackgroundClick = false;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _backgroundClickMethod = 0;

        private string GetBgMethodTag()
        {
            if (!UseBackgroundClick) return string.Empty;
            string name = BackgroundClickMethod switch
            {
                0 => "Send",
                1 => "Post",
                2 => "Child",
                3 => "TryAll",
                4 => "GDirect",
                5 => "GFull",
                6 => "GLow",
                7 => "GVirtual",
                _ => "?"
            };
            return $" [BG:{name}]";
        }

        new public string Description => $"�ΏہF{FormatWindowTarget(WindowTitle, WindowClassName)} / �p�X:{FormatFileName(ImagePath)} / 臒l:{Threshold} / �^�C���A�E�g:{Timeout}ms / �Ԋu:{Interval}ms / �{�^��:{Button}{GetBgMethodTag()}";

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
                UseBackgroundClick = item.UseBackgroundClick;
                BackgroundClickMethod = item.BackgroundClickMethod;
            }
        }

        public new ICommandListItem Clone()
        {
            return new ClickImageItem(this);
        }
    }

    [CommandDefinition("Hotkey", typeof(HotkeyCommand), typeof(IHotkeyCommandSettings), CommandCategory.Action)]
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
        private string _hotkeyText = string.Empty;
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
                var target = FormatWindowTarget(WindowTitle, WindowClassName);
                
                // HotkeyText���ݒ肳��Ă���ꍇ�͂�����g�p
                if (!string.IsNullOrEmpty(HotkeyText))
                {
                    return $"�ΏہF{target} / �L�[�F{HotkeyText}";
                }
                
                var keys = new List<string>();

                if (Ctrl) keys.Add("Ctrl");
                if (Alt) keys.Add("Alt");
                if (Shift) keys.Add("Shift");
                keys.Add(Key.ToString());

                return $"�ΏہF{target} / �L�[�F{string.Join(" + ", keys)}";
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
                HotkeyText = item.HotkeyText;
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
            }
        }

        public new ICommandListItem Clone()
        {
            return new HotkeyItem(this);
        }
    }

    [CommandDefinition("Click", typeof(ClickCommand), typeof(IClickCommandSettings), CommandCategory.Action)]
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
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private bool _useBackgroundClick = false;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _backgroundClickMethod = 0;

        private string GetBgMethodTag()
        {
            if (!UseBackgroundClick) return string.Empty;
            string name = BackgroundClickMethod switch
            {
                0 => "Send",
                1 => "Post",
                2 => "Child",
                3 => "TryAll",
                4 => "GDirect",
                5 => "GFull",
                6 => "GLow",
                7 => "GVirtual",
                _ => "?"
            };
            return $" [BG:{name}]";
        }

        new public string Description => $"�ΏہF{FormatWindowTarget(WindowTitle, WindowClassName)} / X���W:{X} / Y���W:{Y} / �{�^��:{Button}{GetBgMethodTag()}";

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
                UseBackgroundClick = item.UseBackgroundClick;
                BackgroundClickMethod = item.BackgroundClickMethod;
            }
        }

        public new ICommandListItem Clone()
        {
            return new ClickItem(this);
        }
    }

    [CommandDefinition("Wait", typeof(WaitCommand), typeof(IWaitCommandSettings), CommandCategory.Action)]
    public partial class WaitItem : CommandListItem, IWaitItem, IWaitCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _wait = 5000;

        new public string Description => $"�ҋ@����:{Wait}ms";
        
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

    [CommandDefinition("Loop", typeof(LoopCommand), typeof(ILoopCommandSettings), CommandCategory.Control, isLoopCommand: true)]
    public partial class LoopItem : CommandListItem, ILoopItem, ILoopCommandSettings
    {
        protected override bool SyncPairOnIsEnableChange => true;
        protected override ICommandListItem? GetPair() => Pair;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _loopCount = 2;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;

        // ILoopCommandSettings�p�̃y�A�iAutoTool.Command.Interface.ICommand�^�j
        AutoTool.Command.Interface.ICommand? ILoopCommandSettings.Pair { get; set; } = null;

        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / ���[�v��:{LoopCount}";

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

    [CommandDefinition("Loop_End", typeof(LoopEndCommand), typeof(ICommandSettings), CommandCategory.Control)]
    public partial class LoopEndItem : CommandListItem, ILoopEndItem, ICommandSettings
    {
        protected override bool SyncPairOnIsEnableChange => true;
        protected override ICommandListItem? GetPair() => Pair;

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

    [CommandDefinition("Loop_Break", typeof(LoopBreakCommand), typeof(ICommandSettings), CommandCategory.Control)]
    public partial class LoopBreakItem : CommandListItem, ILoopBreakItem, ICommandSettings
    {
        public LoopBreakItem() { }

        public LoopBreakItem(LoopBreakItem? item = null) : base(item) { }

        public new ICommandListItem Clone()
        {
            return new LoopBreakItem(this);
        }
    }

    [CommandDefinition("IF_ImageExist", typeof(IfImageExistCommand), typeof(IIfImageCommandSettings), CommandCategory.Control, isIfCommand: true)]
    public partial class IfImageExistItem : CommandListItem, IIfItem, IIfImageExistItem, IIfImageCommandSettings
    {
        protected override bool SyncPairOnIsEnableChange => true;
        protected override ICommandListItem? GetPair() => Pair;

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

        new public string Description => $"{FormatPairRange(Pair)} / �ΏہF{FormatWindowTarget(WindowTitle, WindowClassName)} / �p�X:{FormatFileName(ImagePath)} / 臒l:{Threshold}";

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

    [CommandDefinition("IF_ImageNotExist", typeof(IfImageNotExistCommand), typeof(IIfImageCommandSettings), CommandCategory.Control, isIfCommand: true)]
    public partial class IfImageNotExistItem : CommandListItem, IIfItem, IIfImageNotExistItem, IIfImageCommandSettings
    {
        protected override bool SyncPairOnIsEnableChange => true;
        protected override ICommandListItem? GetPair() => Pair;

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

        new public string Description => $"{FormatPairRange(Pair)} / �ΏہF{FormatWindowTarget(WindowTitle, WindowClassName)} / �p�X:{FormatFileName(ImagePath)} / 臒l:{Threshold}";

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

    [CommandDefinition("IF_End", typeof(IfEndCommand), typeof(ICommandSettings), CommandCategory.Control)]
    public partial class IfEndItem : CommandListItem, IIfEndItem, ICommandSettings
    {
        protected override bool SyncPairOnIsEnableChange => true;
        protected override ICommandListItem? GetPair() => Pair;

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

    [CommandDefinition("IF_ImageExist_AI", typeof(IfImageExistAICommand), typeof(IIfImageExistAISettings), CommandCategory.AI, isIfCommand: true)]
    public partial class IfImageExistAIItem : CommandListItem, IIfItem, IIfImageExistAIItem, IIfImageExistAISettings
    {
        protected override bool SyncPairOnIsEnableChange => true;
        protected override ICommandListItem? GetPair() => Pair;

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
             $"{FormatPairRange(Pair)} / �ΏہF{FormatWindowTarget(WindowTitle, WindowClassName)} / �N���XID:{ClassID} / 臒l:{ConfThreshold}";

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

    [CommandDefinition("IF_ImageNotExist_AI", typeof(IfImageNotExistAICommand), typeof(IIfImageNotExistAISettings), CommandCategory.AI, isIfCommand: true)]
    public partial class IfImageNotExistAIItem : CommandListItem, IIfItem, IIfImageNotExistAIItem, IIfImageNotExistAISettings
    {
        protected override bool SyncPairOnIsEnableChange => true;
        protected override ICommandListItem? GetPair() => Pair;

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
            $"{FormatPairRange(Pair)} / �ΏہF{FormatWindowTarget(WindowTitle, WindowClassName)} / �N���XID:{ClassID} / 臒l:{ConfThreshold}";

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

    [CommandDefinition("Execute", typeof(ExecuteCommand), typeof(IExecuteCommandSettings), CommandCategory.System)]
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

        new public string Description => $"�t�@�C���p�X:{FormatFileName(ProgramPath)} / ����:{Arguments} / ��ƃt�H���_:{WorkingDirectory}";
        
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

    [CommandDefinition("SetVariable", typeof(SetVariableCommand), typeof(ISetVariableCommandSettings), CommandCategory.Variable)]
    public partial class SetVariableItem : CommandListItem, ISetVariableItem, ISetVariableCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _name = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _value = string.Empty;

        new public string Description => $"�ϐ�:{Name} = \"{Value}\"";

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

    [CommandDefinition("SetVariable_AI", typeof(SetVariableAICommand), typeof(ISetVariableAICommandSettings), CommandCategory.AI)]
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
            $"�ϐ�:{Name} / ���[�h:{AIDetectMode} / ���f��:{FormatFileName(ModelPath)} / 臒l:C{ConfThreshold}/I{IoUThreshold}";

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

    [CommandDefinition("IF_Variable", typeof(IfVariableCommand), typeof(IIfVariableCommandSettings), CommandCategory.Variable, isIfCommand: true)]
    public partial class IfVariableItem : CommandListItem, IIfVariableItem, IIfVariableCommandSettings, IIfItem
    {
        protected override bool SyncPairOnIsEnableChange => true;
        protected override ICommandListItem? GetPair() => Pair;

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

        new public string Description => $"{FormatPairRange(Pair)} / If {Name} {Operator} \"{Value}\"";

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

    [CommandDefinition("Screenshot", typeof(ScreenshotCommand), typeof(IScreenshotCommandSettings), CommandCategory.System)]
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
            $"�ΏہF{FormatWindowTarget(WindowTitle, WindowClassName, "�S���")} / �ۑ���:{(string.IsNullOrEmpty(SaveDirectory) ? "(./Screenshots)" : SaveDirectory)}";

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

    [CommandDefinition("Click_Image_AI", typeof(ClickImageAICommand), typeof(IClickImageAICommandSettings), CommandCategory.AI)]
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
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private bool _useBackgroundClick = false;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private int _backgroundClickMethod = 0;

        private string GetBgMethodTag()
        {
            if (!UseBackgroundClick) return string.Empty;
            string name = BackgroundClickMethod switch
            {
                0 => "Send",
                1 => "Post",
                2 => "Child",
                3 => "TryAll",
                4 => "GDirect",
                5 => "GFull",
                6 => "GLow",
                7 => "GVirtual",
                _ => "?"
            };
            return $" [BG:{name}]";
        }

        new public string Description =>
            $"�ΏہF{FormatWindowTarget(WindowTitle, WindowClassName)} / ���f��:{FormatFileName(ModelPath)} / �N���XID:{ClassID} / 臒l:{ConfThreshold} / �{�^��:{Button}{GetBgMethodTag()}";

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
                UseBackgroundClick = item.UseBackgroundClick;
                BackgroundClickMethod = item.BackgroundClickMethod;
            }
        }

        public new ICommandListItem Clone() => new ClickImageAIItem(this);
    }
}