using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Panels.Command.Interface;

namespace Panels.Command.Class
{
    public class RootCommand : IRootCommand
    {
        public IEnumerable<ICommand> Children { get; set; }

        public RootCommand(ICommandSettings settings, IEnumerable<ICommand> children)
        {
            Children = children;
        }

        public bool TryExecute(out Exception exception)
        {
            // TODO マクロ実行
            exception = null;
            return true;
        }
        public bool CanExecute()
        {
            return true;
        }
    }

    public class BaseCommand : ICommand
    {
        public bool IsEnabled { get; set; }
        public ICommand? Parent { get; set; }
        public int NestLevel { get; set; }
        public ICommandSettings Settings { get; }

        public BaseCommand(ICommand parent, ICommandSettings settings)
        {
            Parent = parent;
            NestLevel = parent.NestLevel + 1;
            Settings = settings;
        }

        public bool TryExecute(out Exception exception)
        {
            throw new NotImplementedException();
        }
        public bool CanExecute()
        {
            return true;
        }
    }

    public class WaitImageCommand : BaseCommand, ICommand, IImageCommand
    {
        new public IImageCommandSettings Settings => base.Settings as IImageCommandSettings;

        public WaitImageCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new public bool TryExecute(out Exception exception)
        {
            MessageBox.Show("WaitImageCommand executed.");
            exception = null;
            return true;
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    public class ClickImageCommand : BaseCommand, ICommand, IImageCommand
    {
        new public IImageCommandSettings Settings => base.Settings as IImageCommandSettings;

        public ClickImageCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new public bool TryExecute(out Exception exception)
        {
            MessageBox.Show("ClickImageCommand executed.");
            exception = null;
            return true;
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    public class HotkeyCommand : BaseCommand, ICommand, IHotkeyCommand
    {
        new public IHotkeyCommandSettings Settings => base.Settings as IHotkeyCommandSettings;

        public HotkeyCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new public bool TryExecute(out Exception exception)
        {
            MessageBox.Show("HotkeyCommand executed.");
            exception = null;
            return true;
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    public class ClickCommand : BaseCommand, ICommand, IClickCommand
    {
        new public IClickCommandSettings Settings => base.Settings as IClickCommandSettings;

        public ClickCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new public bool TryExecute(out Exception exception)
        {
            MessageBox.Show("ClickCommand executed.");
            exception = null;
            return true;
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    public class WaitCommand : BaseCommand, ICommand, IWaitCommand
    {
        new public IWaitCommandSettings Settings => base.Settings as IWaitCommandSettings;

        public WaitCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new public bool TryExecute(out Exception exception)
        {
            MessageBox.Show("WaitCommand executed.");
            exception = null;
            return true;
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    internal class IfCommand : BaseCommand, ICommand, IIfCommand
    {
        public IEnumerable<ICommand> Children { get; set; }
        new public IIfCommandSettings Settings => base.Settings as IIfCommandSettings;

        public IfCommand(ICommand parent, ICommandSettings settings, IEnumerable<ICommand> children) : base(parent, settings)
        {
            Children = children;
        }

        new public bool TryExecute(out Exception exception)
        {
            throw new NotImplementedException();
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    internal class EndIfCommand : BaseCommand, ICommand, IIfCommand
    {
        public IEnumerable<ICommand> Children { get; set; }
        new public IIfCommandSettings Settings => base.Settings as IIfCommandSettings;

        public EndIfCommand(ICommand parent, ICommandSettings settings, IEnumerable<ICommand> children) : base(parent, settings)
        {
            Children = children;
        }

        new public bool TryExecute(out Exception exception)
        {
            throw new NotImplementedException();
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    internal class LoopStartCommand : BaseCommand, ICommand, ILoopCommand
    {
        public int LoopCount { get; set; }
        public IEnumerable<ICommand> Children { get; set; }
        new public ILoopCommandSettings Settings => base.Settings as ILoopCommandSettings;

        public LoopStartCommand(ICommand parent, ICommandSettings settings, IEnumerable<ICommand> children) : base(parent, settings)
        {
            LoopCount = 0;
            Children = children;
        }

        new public bool TryExecute(out Exception exception)
        {
            throw new NotImplementedException();
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    internal class LoopEndCommand : BaseCommand, ICommand, ILoopCommand
    {
        public IEnumerable<ICommand> Children { get; set; }
        new public ILoopCommandSettings Settings => base.Settings as ILoopCommandSettings;

        public LoopEndCommand(ICommand parent, ICommandSettings settings, IEnumerable<ICommand> children) : base(parent, settings)
        {
            Children = children;
        }

        new public bool TryExecute(out Exception exception)
        {
            throw new NotImplementedException();
        }

        new public bool CanExecute()
        {
            return true;
        }
    }
}
