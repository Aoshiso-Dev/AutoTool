using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MacroPanels.Command.Interface;
using System.Windows.Media;

namespace MacroPanels.Command.Class
{
    public class CommandSettings : ICommandSettings
    {
        public CommandSettings()
        {
        }
    }

    public class WaitImageCommandSettings : ICommandSettings, IWaitImageCommandSettings
    {
        public string ImagePath { get; set; } = string.Empty;
        public double Threshold { get; set; }
        public Color? SearchColor { get; set; }
        public int Timeout { get; set; }
        public int Interval { get; set; }
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;
    }

    public class ClickImageCommandSettings : ICommandSettings, IClickImageCommandSettings
    {
        public string ImagePath { get; set; } = string.Empty;
        public double Threshold { get; set; }
        public Color? SearchColor { get; set; }
        public int Timeout { get; set; }
        public int Interval { get; set; }
        public System.Windows.Input.MouseButton Button { get; set; }
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;
    }

    public class HotkeyCommandSettings : ICommandSettings, IHotkeyCommandSettings
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public System.Windows.Input.Key Key { get; set; }
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;

        public HotkeyCommandSettings()
        {
        }
    }

    public class ClickCommandSettings : ICommandSettings, IClickCommandSettings
    {
        public System.Windows.Input.MouseButton Button { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;

        public ClickCommandSettings()
        {
        }
    }

    public class WaitCommandSettings : ICommandSettings, IWaitCommandSettings
    {
        public int Wait { get; set; }

        public WaitCommandSettings()
        {
        }
    }

    public class LoopCommandSettings : ICommandSettings, ILoopCommandSettings
    {
        public int LoopCount { get; set; }
        public ICommand? Pair { get; set; }

        public LoopCommandSettings()
        {
        }
    }

    public class EndLoopCommandSettings : ICommandSettings, IEndLoopCommandSettings
    {
        public int LoopCount { get; set; }
        public ICommand? Pair { get; set; }

        public EndLoopCommandSettings()
        {
        }
    }
}
