using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MacroPanels.Command.Interface
{
    public interface ICommandSettings
    {
    }

    public interface IWaitImageCommandSettings : ICommandSettings
    {
        string ImagePath { get; set; }
        double Threshold { get; set; }
        Color? SearchColor { get; set; }
        int Timeout { get; set; }
        int Interval { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
    }

    public interface IClickImageCommandSettings : ICommandSettings
    {
        string ImagePath { get; set; }
        double Threshold { get; set; }
        Color? SearchColor { get; set; }
        int Timeout { get; set; }
        int Interval { get; set; }
        MouseButton Button { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
    }
    public interface IHotkeyCommandSettings : ICommandSettings
    {
        bool Ctrl { get; set; }
        bool Alt { get; set; }
        bool Shift { get; set; }
        Key Key { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
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
        ICommand? Pair { get; set; }
    }

    public interface IEndLoopCommandSettings : ICommandSettings
    {
        int LoopCount { get; set; }
        ICommand? Pair { get; set; }
    }

    // Added for YOLO based image exist AI command
    public interface IIfImageExistAISettings : ICommandSettings
    {
        string ModelPath { get; set; }
        string NamesFilePath { get; set; }
        string TargetLabel { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
        int Timeout { get; set; }
        int Interval { get; set; }
    }
}
