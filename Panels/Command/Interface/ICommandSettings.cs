using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MacroPanels.Command.Interface
{
    public interface ICommandSettings { }

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

    public interface IIfImageCommandSettings : ICommandSettings
    {
        string ImagePath { get; set; }
        double Threshold { get; set; }
        Color? SearchColor { get; set; }
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
        bool UseBackgroundClick { get; set; }
        int BackgroundClickMethod { get; set; }
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
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        bool UseBackgroundClick { get; set; }
        int BackgroundClickMethod { get; set; }
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

    public interface ILoopEndCommandSettings : ICommandSettings
    {
        int LoopCount { get; set; }
        ICommand? Pair { get; set; }
    }

    public interface IIfWaitImageCommandSettings : ICommandSettings
    {
        string ImagePath { get; set; }
        double Threshold { get; set; }
        Color? SearchColor { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
    }

    // Align to actual usage in Items/Commands (YOLO by ClassId)
    public interface IIfImageExistAISettings : ICommandSettings
    {
        string ModelPath { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        int ClassID { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
    }
    public interface IIfImageNotExistAISettings : ICommandSettings
    {
        string ModelPath { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        int ClassID { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
    }

    public interface IExecuteCommandSettings : ICommandSettings
    {
        string ProgramPath { get; set; }
        string Arguments { get; set; }
        string WorkingDirectory { get; set; }
        bool WaitForExit { get; set; }
    }

    // Variable commands
    public interface ISetVariableCommandSettings : ICommandSettings
    {
        string Name { get; set; }
        string Value { get; set; }
    }

    public interface IIfVariableCommandSettings : ICommandSettings
    {
        string Name { get; set; }
        string Operator { get; set; }
        string Value { get; set; }
    }

    // AI variable set command
    public interface ISetVariableAICommandSettings : ICommandSettings
    {
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        string AIDetectMode { get; set; }
        string ModelPath { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
        string Name { get; set; }
    }

    // AI Click Image command
    public interface IClickImageAICommandSettings : ICommandSettings
    {
        string ModelPath { get; set; }
        int ClassID { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
        MouseButton Button { get; set; }
        bool UseBackgroundClick { get; set; }
        int BackgroundClickMethod { get; set; }
    }

    // Screenshot command
    public interface IScreenshotCommandSettings : ICommandSettings
    {
        string SaveDirectory { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
    }
}
