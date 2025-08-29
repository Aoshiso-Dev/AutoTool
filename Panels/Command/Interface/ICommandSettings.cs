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

    public interface IIfImageExistAISettings : ICommandSettings
    {
        string ModelPath { get; set; }
        int ClassID { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
        int Timeout { get; set; }
        int Interval { get; set; }
    }

    public interface IExecuteProgramCommandSettings : ICommandSettings
    {
        string ProgramPath { get; set; }
        string Arguments { get; set; }
        string WorkingDirectory { get; set; }
        bool WaitForExit { get; set; }
    }

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
}
