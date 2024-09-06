using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Panels.Command.Define;

namespace Panels.Command.Interface
{
    public interface IRootCommand
    {
        public IEnumerable<ICommand> Children { get; set; }
    }

    public interface ICommand
    {
        bool IsEnabled { get; set; }
        ICommand? Parent { get; set; }
        int NestLevel { get; set; }
        ICommandSettings Settings { get; }

        bool TryExecute(out Exception exception);

        bool CanExecute();
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

    public interface IIfCommand : ICommand
    {
        public IEnumerable<ICommand> Children { get; set; }
        new IIfCommandSettings Settings { get; }
    }

    public interface ILoopCommand : ICommand
    {
        public IEnumerable<ICommand> Children { get; set; }
        new ILoopCommandSettings Settings { get; }
    }
}
