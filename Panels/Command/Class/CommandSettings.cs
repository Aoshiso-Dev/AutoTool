using Panels.Command.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panels.Command.Define
{
    public class ImageCommandSettings : IImageCommandSettings
    {
        public string ImagePath { get; set; }
        public double Threshold { get; set; }
        public double Timeout { get; set; }
        public double Interval { get; set; }

        public ImageCommandSettings()
        {
        }
    }

    public class HotkeyCommandSettings : IHotkeyCommandSettings
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public System.Windows.Input.Key Key { get; set; }

        public HotkeyCommandSettings()
        {
        }
    }

    public class ClickCommandSettings : IClickCommandSettings
    {
        public System.Windows.Input.MouseButton Button { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public ClickCommandSettings()
        {
        }
    }

    public class WaitCommandSettings : IWaitCommandSettings
    {
        public double Timeout { get; set; }

        public WaitCommandSettings()
        {
        }
    }

    public class IfCommandSettings : IIfCommandSettings
    {
        public ICondition Condition { get; set; }
        public ICommand Start { get; set; }
        public ICommand End { get; set; }

        public IfCommandSettings()
        {
        }
    }

    public class LoopCommandSettings : ILoopCommandSettings
    {
        public int LoopCount { get; set; }
        public ICommand Start { get; set; }
        public ICommand End { get; set; }

        public LoopCommandSettings()
        {
        }
    }
}
