﻿using Panels.Command.Interface;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panels.Command.Define
{
    public class CommandSettings : ICommandSettings
    {
        public CommandSettings()
        {
        }
    }

    public class ImageCommandSettings : ICommandSettings, IImageCommandSettings
    {
        public string ImagePath { get; set; } = string.Empty;
        public double Threshold { get; set; }
        public int Timeout { get; set; }
        public int Interval { get; set; }
    }

    public class HotkeyCommandSettings : ICommandSettings, IHotkeyCommandSettings
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public System.Windows.Input.Key Key { get; set; }

        public HotkeyCommandSettings()
        {
        }
    }

    public class ClickCommandSettings : ICommandSettings, IClickCommandSettings
    {
        public System.Windows.Input.MouseButton Button { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

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

        public LoopCommandSettings()
        {
        }
    }
}
