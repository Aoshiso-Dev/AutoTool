﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Panels.Command.Define;

namespace Panels.Command.Interface
{

    public interface ICommand
    {
        int ListNumber { get; set; }
        bool IsEnabled { get; set; }
        ICommand? Parent { get; set; }
        IEnumerable<ICommand> Children { get; set; }
        int NestLevel { get; set; }
        ICommandSettings Settings { get; }
        EventHandler<int>? OnCommandRunning { get; set; }

        Task<bool> Execute(CancellationToken cancellationToken);

        bool CanExecute();
    }

    public interface IRootCommand : ICommand
    {
    }

    public interface IIfCommand : ICommand
    {
    }

    public interface IImageCommand : ICommand
    {
        new IImageCommandSettings Settings { get; }
    }

    public interface IHotkeyCommand : ICommand
    {
        new IHotkeyCommandSettings Settings { get; }
    }

    public interface IClickCommand : ICommand
    {
        new IClickCommandSettings Settings { get; }
    }

    public interface IWaitCommand : ICommand
    {
        new IWaitCommandSettings Settings { get; }
    }

    public interface IIfImageExistCommand : ICommand, IIfCommand
    {
        new IImageCommandSettings Settings { get; }
    }

    public interface IIfImageNotExistCommand : ICommand, IIfCommand
    {
        new IImageCommandSettings Settings { get; }
    }

    public interface ILoopCommand : ICommand
    {
        new ILoopCommandSettings Settings { get; }
    }

    public interface IBreakCommand : ICommand
    {
    }
}
