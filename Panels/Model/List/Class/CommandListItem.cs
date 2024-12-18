﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panels.List;
using System.Windows;
using Command.Interface;
using Command.Class;
using Panels.Model.List.Interface;
using System.Windows.Controls;
using System.Text.Json.Serialization;
using OpenCvSharp.Features2D;
using System.Windows.Input;
using System.Windows.Media;

namespace Panels.List.Class
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

    public partial class WaitImageItem : CommandListItem, IWaitImageItem, IWaitImageCommandSettings
    {
        [ObservableProperty]
        private string _imagePath = string.Empty;
        [ObservableProperty]
        private double _threshold = 0.8;
        [ObservableProperty]
        private Color? _searchColor = null;
        [ObservableProperty]
        private int _timeout = 5000;
        [ObservableProperty]
        private int _interval = 500;
        [ObservableProperty]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        private string _windowClassName = string.Empty;

        new public string Description => $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";

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

    public partial class ClickImageItem : CommandListItem, IClickImageItem, IClickImageCommandSettings
    {
        [ObservableProperty]
        private string _imagePath = string.Empty;
        [ObservableProperty]
        private double _threshold = 0.8;
        [ObservableProperty]
        private Color? _searchColor = null;
        [ObservableProperty]
        private int _timeout = 5000;
        [ObservableProperty]
        private int _interval = 500;
        [ObservableProperty]
        private System.Windows.Input.MouseButton _button = System.Windows.Input.MouseButton.Left;
        [ObservableProperty]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        private string _windowClassName = string.Empty;

        new public string Description => $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms / ボタン:{Button}";

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

    public partial class HotkeyItem : CommandListItem, IHotkeyItem, IHotkeyCommandSettings
    {
        [ObservableProperty]
        private bool _ctrl = false;
        [ObservableProperty]
        private bool _alt = false;
        [ObservableProperty]
        private bool _shift = false;
        [ObservableProperty]
        private System.Windows.Input.Key _key = System.Windows.Input.Key.Escape;
        [ObservableProperty]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
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

    public partial class ClickItem : CommandListItem, IClickItem, IClickCommandSettings
    {
        [ObservableProperty]
        private int _x = 0;
        [ObservableProperty]
        private int _y = 0;
        [ObservableProperty]
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

    public partial class WaitItem : CommandListItem, IWaitItem, IWaitCommandSettings
    {
        [ObservableProperty]
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
        private string _imagePath = string.Empty;
        [ObservableProperty]
        private double _threshold = 0.8;
        [ObservableProperty]
        private Color? _searchColor = null;
        [ObservableProperty]
        private int _timeout = 5000;
        [ObservableProperty]
        private int _interval = 500;
        [ObservableProperty]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        private string _windowClassName = string.Empty;
        [ObservableProperty]
        private ICommandListItem? _pair = null;


        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / 対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";

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
        private string _imagePath = string.Empty;
        [ObservableProperty]
        private double _threshold = 0.8;
        [ObservableProperty]
        private Color? _searchColor = null;
        [ObservableProperty]
        private int _timeout = 5000;
        [ObservableProperty]
        private int _interval = 500;
        [ObservableProperty]
        private string _windowTitle = string.Empty;
        [ObservableProperty]
        private string _windowClassName = string.Empty;
        [ObservableProperty]
        private ICommandListItem? _pair = null;

        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / 対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";

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

    public partial class EndIfItem : CommandListItem, IEndIfItem
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
        private ICommandListItem? _pair = null;


        new public string Description => $"{Pair?.LineNumber}->{LineNumber}";

        public EndIfItem() { }
        public EndIfItem(EndIfItem? item = null) : base(item) { }

        public new ICommandListItem Clone()
        {
            return new EndIfItem(this);
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
        private int _loopCount = 2;
        [ObservableProperty]
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

    public partial class EndLoopItem : CommandListItem, IEndLoopItem
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
        private ICommandListItem? _pair = null;

        new public string Description => $"{Pair?.LineNumber}->{LineNumber}";

        public EndLoopItem() { }

        public EndLoopItem(EndLoopItem? item = null) : base(item)
        {
            if (item != null)
            {
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone()
        {
            return new EndLoopItem(this);
        }
    }

    public partial class BreakItem : CommandListItem, IBreakItem
    {
        public BreakItem() { }

        public BreakItem(BreakItem? item = null) : base(item) { }

        public new ICommandListItem Clone()
        {
            return new BreakItem(this);
        }
    }

}
