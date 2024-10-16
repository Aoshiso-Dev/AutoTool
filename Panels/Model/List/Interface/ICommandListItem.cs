using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Panels.Model.List.Interface
{
    public interface ICommandListItem
    {
        public bool IsEnable { get; set; }
        public int LineNumber { get; set; }
        public bool IsRunning { get; set; }
        public bool IsSelected { get; set; }
        public string ItemType { get; set; }
        public string Description { get; set; }
        public int NestLevel { get; set; }
        public bool IsInLoop { get; set; }
        public bool IsInIf { get; set; }
        public int Progress { get; set; }

        ICommandListItem Clone();
    }

    public interface IWaitImageItem : ICommandListItem
    {
        public string ImagePath { get; set; }
        public double Threshold { get; set; }
        public Color? SearchColor { get; set; }
        public int Timeout { get; set; }
        public int Interval { get; set; }
        public string WindowTitle { get; set; }
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
    }

    public interface IHotkeyItem : ICommandListItem
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public System.Windows.Input.Key Key { get; set; }
        public string WindowTitle { get; set; }
    }

    public interface IClickItem : ICommandListItem
    {
        public System.Windows.Input.MouseButton Button { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string WindowTitle { get; set; }
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
        public int Timeout { get; set; }
        public int Interval { get; set; }
        public string WindowTitle { get; set; }
    }

    public interface IIfImageNotExistItem : ICommandListItem, IIfItem
    {
        public string ImagePath { get; set; }
        public double Threshold { get; set; }
        public Color? SearchColor { get; set; }
        public int Timeout { get; set; }
        public int Interval { get; set; }
        public string WindowTitle { get; set; }
    }

    public interface IEndIfItem : ICommandListItem
    {
        public ICommandListItem? Pair { get; set; }
    }

    public interface ILoopItem : ICommandListItem
    {
        public int LoopCount { get; set; }
        public ICommandListItem? Pair { get; set; }
    }

    public interface IEndLoopItem : ICommandListItem
    {
        public ICommandListItem? Pair { get; set; }
    }

    public interface IBreakItem : ICommandListItem
    {
    }
}
