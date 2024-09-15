using CommunityToolkit.Mvvm.ComponentModel;
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

namespace Panels.List.Class
{
    internal partial class CommandListItem : ObservableObject, ICommandListItem
    {
        [ObservableProperty]
        private bool _isRunning = false;
        [ObservableProperty]
        private bool _isSelected = false;
        [ObservableProperty]
        private int _lineNumber = 0;
        [ObservableProperty]
        private string _itemType = "None";
        [ObservableProperty]
        private string _description = string.Empty;
        [ObservableProperty]
        private int _nestLevel = 0;
        [ObservableProperty]
        private bool _isInLoop = false;
        [ObservableProperty]
        private bool _isInIf = false;

        public CommandListItem() { }

        public CommandListItem(CommandListItem? item)
        {
            if (item != null)
            {
                IsRunning = item.IsRunning;
                IsSelected = item.IsSelected;
                LineNumber = item.LineNumber;
                ItemType = item.ItemType;
                NestLevel = item.NestLevel;
                IsInLoop = item.IsInLoop;
                
            }
        }

        public ICommandListItem Clone()
        {
            return new CommandListItem(this);
        }
    }

    internal partial class WaitImageItem : CommandListItem, IWaitImageItem, IWaitImageCommandSettings
    {
        [ObservableProperty]
        private string _imagePath = string.Empty;
        [ObservableProperty]
        private double _threshold = 0.8;
        [ObservableProperty]
        private int _timeout = 5000;
        [ObservableProperty]
        private int _interval = 500;

        [JsonIgnore]
        new public string Description => $"パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";

        public WaitImageItem() { }

        public WaitImageItem(WaitImageItem? item = null) : base(item)
        {
            if (item != null)
            {
                ImagePath = item.ImagePath;
                Threshold = item.Threshold;
                Timeout = item.Timeout;
                Interval = item.Interval;
            }
        }

        public new ICommandListItem Clone()
        {
            return new WaitImageItem(this);
        }
    }

    internal partial class ClickImageItem : CommandListItem, IClickImageItem, IClickImageCommandSettings
    {
        [ObservableProperty]
        private string _imagePath = string.Empty;
        [ObservableProperty]
        private double _threshold = 0.8;
        [ObservableProperty]
        private int _timeout = 5000;
        [ObservableProperty]
        private int _interval = 500;
        [ObservableProperty]
        private System.Windows.Input.MouseButton _button = System.Windows.Input.MouseButton.Left;

        [JsonIgnore]
        new public string Description => $"パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms / ボタン:{Button}";

        public ClickImageItem() { }

        public ClickImageItem(ClickImageItem? item = null) : base(item)
        {
            if (item != null)
            {
                ImagePath = item.ImagePath;
                Threshold = item.Threshold;
                Timeout = item.Timeout;
                Interval = item.Interval;
                Button = item.Button;
            }
        }

        public new ICommandListItem Clone()
        {
            return new ClickImageItem(this);
        }
    }

    internal partial class HotkeyItem : CommandListItem, IHotkeyItem, IHotkeyCommandSettings
    {
        [ObservableProperty]
        private bool _ctrl = false;
        [ObservableProperty]
        private bool _alt = false;
        [ObservableProperty]
        private bool _shift = false;
        [ObservableProperty]
        private System.Windows.Input.Key _key = System.Windows.Input.Key.Escape;

        [JsonIgnore]
        new public string Description => $"Ctrl:{Ctrl} / Alt:{Alt} / Shift:{Shift} / キー:{Key}";

        public HotkeyItem() { }

        public HotkeyItem(HotkeyItem? item = null) : base(item)
        {
            if (item != null)
            {
                Ctrl = item.Ctrl;
                Alt = item.Alt;
                Shift = item.Shift;
                Key = item.Key;
            }
        }

        public new ICommandListItem Clone()
        {
            return new HotkeyItem(this);
        }
    }

    internal partial class ClickItem : CommandListItem, IClickItem, IClickCommandSettings
    {
        [ObservableProperty]
        private int _x = 0;
        [ObservableProperty]
        private int _y = 0;
        [ObservableProperty]
        private System.Windows.Input.MouseButton _button = System.Windows.Input.MouseButton.Left;

        [JsonIgnore]
        new public string Description => $"X座標:{X} / Y座標:{Y} / ボタン:{Button}";

        public ClickItem() { }

        public ClickItem(ClickItem? item = null) : base(item)
        {
            if (item != null)
            {
                X = item.X;
                Y = item.Y;
                Button = item.Button;
            }
        }

        public new ICommandListItem Clone()
        {
            return new ClickItem(this);
        }
    }

    internal partial class WaitItem : CommandListItem, IWaitItem, IWaitCommandSettings
    {
        [ObservableProperty]
        private int _wait = 5000;

        [JsonIgnore]
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

    internal partial class IfImageExistItem : CommandListItem, IIfItem, IIfImageExistItem, IWaitImageCommandSettings
    {
        [ObservableProperty]
        private string _imagePath = string.Empty;
        [ObservableProperty]
        private double _threshold = 0.8;
        [ObservableProperty]
        private int _timeout = 5000;
        [ObservableProperty]
        private int _interval = 500;
        [ObservableProperty, JsonIgnore]
        private ICommandListItem? _pair = null;

        [JsonIgnore]
        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";

        public IfImageExistItem() { }

        public IfImageExistItem(IfImageExistItem? item = null) : base(item)
        {
            if (item != null)
            {
                ImagePath = item.ImagePath;
                Threshold = item.Threshold;
                Timeout = item.Timeout;
                Interval = item.Interval;
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone()
        {
            return new IfImageExistItem(this);
        }
    }

    internal partial class IfImageNotExistItem : CommandListItem, IIfItem, IIfImageNotExistItem, IWaitImageCommandSettings
    {
        [ObservableProperty]
        private string _imagePath = string.Empty;
        [ObservableProperty]
        private double _threshold = 0.8;
        [ObservableProperty]
        private int _timeout = 5000;
        [ObservableProperty]
        private int _interval = 500;
        [ObservableProperty, JsonIgnore]
        private ICommandListItem? _pair = null;

        [JsonIgnore]
        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / パス:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold} / タイムアウト:{Timeout}ms / 間隔:{Interval}ms";

        public IfImageNotExistItem() { }

        public IfImageNotExistItem(IfImageNotExistItem? item = null) : base(item)
        {
            if (item != null)
            {
                ImagePath = item.ImagePath;
                Threshold = item.Threshold;
                Timeout = item.Timeout;
                Interval = item.Interval;
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone()
        {
            return new IfImageNotExistItem(this);
        }
    }

    internal partial class EndIfItem : CommandListItem, IEndIfItem
    {
        [ObservableProperty, JsonIgnore]
        private ICommandListItem? _pair = null;

        [JsonIgnore]
        new public string Description => $"{Pair?.LineNumber}->{LineNumber}";

        public EndIfItem() { }
        public EndIfItem(EndIfItem? item = null) : base(item) { }

        public new ICommandListItem Clone()
        {
            return new EndIfItem(this);
        }
    }

    internal partial class LoopItem : CommandListItem, ILoopItem, ICommandListItem, ILoopCommandSettings
    {
        [ObservableProperty]
        private int _loopCount = 2;
        [ObservableProperty, JsonIgnore]
        private ICommandListItem? _pair = null;

        [JsonIgnore]
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

    internal partial class EndLoopItem : CommandListItem, IEndLoopItem
    {
        [ObservableProperty, JsonIgnore]
        private ICommandListItem? _pair = null;

        [JsonIgnore]
        new public string Description => $"{Pair?.LineNumber}->{LineNumber}";

        public EndLoopItem() { }

        public EndLoopItem(EndLoopItem? item = null) : base(item) { }

        public new ICommandListItem Clone()
        {
            return new EndLoopItem(this);
        }
    }

    internal partial class BreakItem : CommandListItem, IBreakItem
    {
        public BreakItem() { }

        public BreakItem(BreakItem? item = null) : base(item) { }

        public new ICommandListItem Clone()
        {
            return new BreakItem(this);
        }
    }

}
