using CommunityToolkit.Mvvm.ComponentModel;
using Panels.Command.Interface;
using Panels.List.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panels.List.Class
{
    internal partial class CommandListItem : ObservableObject, ICommandListItem
    {
        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private int _lineNumber = 0;

        [ObservableProperty]
        private string _itemType = "None";
    }

    internal partial class WaitImageItem : CommandListItem, IImageCommandSettings
    {
        [ObservableProperty]
        private string _imagePath = string.Empty;
        [ObservableProperty]
        private double _threshold = 0.8;
        [ObservableProperty]
        private double _timeout = 5000;
        [ObservableProperty]
        private double _interval = 500;
    }

    internal partial class ClickImageItem : CommandListItem, IImageCommandSettings
    {
        [ObservableProperty]
        private string _imagePath = string.Empty;
        [ObservableProperty]
        private double _threshold = 0.8;
        [ObservableProperty]
        private double _timeout = 5000;
        [ObservableProperty]
        private double _interval = 500;
        [ObservableProperty]
        private System.Windows.Input.MouseButton _button = System.Windows.Input.MouseButton.Left;
    }

    internal partial class HotkeyItem : CommandListItem, IHotkeyCommandSettings
    {
        [ObservableProperty]
        private bool _ctrl = false;
        [ObservableProperty]
        private bool _alt = false;
        [ObservableProperty]
        private bool _shift = false;
        [ObservableProperty]
        private System.Windows.Input.Key _key = System.Windows.Input.Key.None;
    }

    internal partial class ClickItem : CommandListItem, IClickCommandSettings
    {
        [ObservableProperty]
        private int _x = 0;
        [ObservableProperty]
        private int _y = 0;
        [ObservableProperty]
        private System.Windows.Input.MouseButton _button = System.Windows.Input.MouseButton.Left;
    }

    internal partial class WaitItem : CommandListItem, IWaitCommandSettings
    {
        [ObservableProperty]
        private double _timeout = 5000;
    }

    internal partial class IfItem : CommandListItem, IIfCommandSettings
    {
        [ObservableProperty]
        private ICondition _condition = null;
        [ObservableProperty]
        private ICommand _start = null;
        [ObservableProperty]
        private ICommand _end = null;
    }

    internal partial class EndIfItem : CommandListItem, IIfCommandSettings
    {
        [ObservableProperty]
        private ICondition _condition = null;
        [ObservableProperty]
        private ICommand _start = null;
        [ObservableProperty]
        private ICommand _end = null;
    }

    internal partial class LoopItem : CommandListItem, ILoopCommandSettings
    {
        [ObservableProperty]
        private int _loopCount = 5;
        [ObservableProperty]
        private ICommand _start = null;
        [ObservableProperty]
        private ICommand _end = null;
    }

    internal partial class EndLoopItem : CommandListItem, ILoopCommandSettings
    {
        [ObservableProperty]
        private int _loopCount = 5;
        [ObservableProperty]
        private ICommand _start = null;
        [ObservableProperty]
        private ICommand _end = null;
    }
}
