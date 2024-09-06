using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panels.Command;

namespace Panels.List.Interface
{
    public interface ICommandListItem
    {
        public int LineNumber { get; set; }
        public bool IsRunning { get; set; }
        public string ItemType { get; set; }
    }

    public interface IWaitImageItem : ICommandListItem
    {
        public string ImagePath { get; set; }
        public double Threshold { get; set; }
        public TimeSpan Timeout { get; set; }
        public TimeSpan Interval { get; set; }
    }

    public interface IClickImageItem : ICommandListItem
    {
        public string ImagePath { get; set; }
        public double Threshold { get; set; }
        public TimeSpan Timeout { get; set; }
        public TimeSpan Interval { get; set; }
        public System.Windows.Input.MouseButton Button { get; set; }
    }

    public interface IHotkeyItem : ICommandListItem
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public System.Windows.Input.Key Key { get; set; }
    }

    public interface IClickItem : ICommandListItem
    {
        public System.Windows.Input.MouseButton Button { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public interface IWaitItem : ICommandListItem
    {
        public TimeSpan Wait { get; set; }
    }

    public interface IIfItem : ICommandListItem
    {
        public ConditionType ConditionType { get; set; }
    }

    public interface IEndIfItem : ICommandListItem
    {
    }

    public interface ILoopItem : ICommandListItem
    {
        public int LoopCount { get; set; }
    }

    public interface IEndLoopItem : ICommandListItem
    {
    }
}
