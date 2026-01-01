using CommunityToolkit.Mvvm.ComponentModel;
using MacroPanels.Command.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MacroPanels.Model.List.Interface
{
    public interface ICommandListItem
    {
        public bool IsEnable { get; set; }
        public int LineNumber { get; set; }
        public bool IsRunning { get; set; }
        public bool IsSelected { get; set; }
        public string ItemType { get; set; }
        public string Description { get; set; }
        public string Comment { get; set; }
        public int NestLevel { get; set; }
        public bool IsInLoop { get; set; }
        public bool IsInIf { get; set; }
        public int Progress { get; set; }

        ICommandListItem Clone();
        
        /// <summary>
        /// Execute the command logic (override in derived classes)
        /// </summary>
        Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }

    public interface IWaitImageItem : ICommandListItem
    {
        public string ImagePath { get; set; }
        public double Threshold { get; set; }
        public Color? SearchColor { get; set; }
        public int Timeout { get; set; }
        public int Interval { get; set; }
        public string WindowTitle { get; set; }
        public string WindowClassName { get; set; }
    }

    public interface IClickImageItem : ICommandListItem
    {
        public string ImagePath { get; set; }
        public double Threshold { get; set; }
        public Color? SearchColor { get; set; }
        public int Timeout { get; set; }
        public int Interval { get; set; }
        public System.Windows.Input.MouseButton Button { get; set; }
        public string WindowTitle { get; set; }
        public string WindowClassName { get; set; }
    }

    public interface IHotkeyItem : ICommandListItem
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public System.Windows.Input.Key Key { get; set; }
        public string WindowTitle { get; set; }
        public string WindowClassName { get; set; }
    }

    public interface IClickItem : ICommandListItem
    {
        public System.Windows.Input.MouseButton Button { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string WindowTitle { get; set; }
        public string WindowClassName { get; set; }
    }

    public interface IWaitItem : ICommandListItem
    {
        public int Wait { get; set; }
    }

    public interface IIfItem : ICommandListItem
    {
        public ICommandListItem? Pair { get; set; }
    }

    public interface IIfImageExistItem : ICommandListItem, IIfItem
    {
        public string ImagePath { get; set; }
        public double Threshold { get; set; }
        public Color? SearchColor { get; set; }
        public string WindowTitle { get; set; }
        public string WindowClassName { get; set; }
    }

    public interface IIfImageNotExistItem : ICommandListItem, IIfItem
    {
        public string ImagePath { get; set; }
        public double Threshold { get; set; }
        public Color? SearchColor { get; set; }
        public string WindowTitle { get; set; }
        public string WindowClassName { get; set; }
    }

    public interface IIfEndItem : ICommandListItem
    {
        public ICommandListItem? Pair { get; set; }
    }

    public interface ILoopItem : ICommandListItem
    {
        public int LoopCount { get; set; }
        public ICommandListItem? Pair { get; set; }
    }

    public interface ILoopEndItem : ICommandListItem
    {
        public ICommandListItem? Pair { get; set; }
    }

    public interface ILoopBreakItem : ICommandListItem
    {
    }

    public interface IIfImageExistAIItem : ICommandListItem
    {
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        string ModelPath { get; set; }
        int ClassID { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
    }
    public interface IIfImageNotExistAIItem : ICommandListItem
    {
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        string ModelPath { get; set; }
        int ClassID { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
    }

    public interface IClickImageAIItem : ICommandListItem
    {
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        string ModelPath { get; set; }
        int ClassID { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
        System.Windows.Input.MouseButton Button { get; set; }
    }

    public interface IExecuteItem : ICommandListItem
    {
        public string ProgramPath { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public bool WaitForExit { get; set; }
    }

    public interface ISetVariableItem : ICommandListItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public interface ISetVariableAIItem : ICommandListItem
    {
        string WindowTitle { get; set; }
        string AIDetectMode { get; set; }
        string WindowClassName { get; set; }
        string ModelPath { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
        public string Name { get; set; }
    }

    public interface IIfVariableItem : ICommandListItem, IIfItem
    {
        public string Name { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }

    // Screenshot item interface for panel editing
    public interface IScreenshotItem : ICommandListItem
    {
        string SaveDirectory { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
    }
}
