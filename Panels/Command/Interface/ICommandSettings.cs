using Panels.Command.Define;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Panels.Command.Interface
{
    public interface ICommandSettings
    {
    }

    public interface IWaitImageCommandSettings : ICommandSettings
    {
        string ImagePath { get; set; }
        double Threshold { get; set; }
        int Timeout { get; set; }
        int Interval { get; set; }
    }

    public interface IClickImageCommandSettings : ICommandSettings
    {
        string ImagePath { get; set; }
        double Threshold { get; set; }
        int Timeout { get; set; }
        int Interval { get; set; }
        MouseButton Button { get; set; }
    }
    public interface IHotkeyCommandSettings : ICommandSettings
    {
        bool Ctrl { get; set; }
        bool Alt { get; set; }
        bool Shift { get; set; }
        Key Key { get; set; }
    }

    public interface IClickCommandSettings : ICommandSettings
    {
        MouseButton Button { get; set; }
        int X { get; set; }
        int Y { get; set; }
    }

    public interface IWaitCommandSettings : ICommandSettings
    {
        int Wait { get; set; }
    }

    public interface ILoopCommandSettings : ICommandSettings
    {
        int LoopCount { get; set; }
    }
}
