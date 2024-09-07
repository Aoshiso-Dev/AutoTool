using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Panels.Command.Define;
using Panels.Command.Interface;

namespace Panels.Command.Class
{
    public class RootCommand : IRootCommand
    {
        public int ListNumber { get; set; } = 0;
        public bool IsEnabled { get; set; } = true;
        public ICommand? Parent { get; set; } = null;
        public int NestLevel { get; set; } = 0;
        public ICommandSettings Settings { get; } = new CommandSettings();

        public IEnumerable<ICommand> Children { get; set; } = new List<ICommand>();
        public EventHandler<int> OnCommandRunning { get; set; }

        public RootCommand()
        {
            Children = new List<ICommand>();
        }

        public bool Execute(CancellationToken cancellationToken)
        {
            foreach (var command in Children)
            {
                if (!command.Execute(cancellationToken))
                {
                    return false;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
            }

            OnCommandRunning?.Invoke(this, 0);

            return true;
        }

        public bool CanExecute()
        {
            return true;
        }

        public RootCommand(ICommandSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Children = new List<ICommand>();
        }
    }

    public class BaseCommand : ICommand
    {
        public int ListNumber { get; set; }
        public bool IsEnabled { get; set; }
        public ICommand? Parent { get; set; }
        public IEnumerable<ICommand> Children { get; set; }
        public int NestLevel { get; set; }
        public ICommandSettings Settings { get; }
        public EventHandler<int> OnCommandRunning { get; set; }

        public BaseCommand(ICommand parent, ICommandSettings settings)
        {
            Parent = parent;
            NestLevel = parent == null ? 0 : parent.NestLevel + 1;
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Children = new List<ICommand>();
        }

        public bool Execute(CancellationToken cancellationToken )
        {
            OnCommandRunning?.Invoke(this, ListNumber);
            return true;
        }
        public bool CanExecute()
        {
            return true;
        }
    }

    public class WaitImageCommand : BaseCommand, ICommand, IImageCommand
    {
        new public IImageCommandSettings Settings => (IImageCommandSettings)base.Settings;

        public WaitImageCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new public bool Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);
            MessageBox.Show("WaitImageCommand executed.");
            return true;
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    public class ClickImageCommand : BaseCommand, ICommand, IImageCommand
    {
        new public IImageCommandSettings Settings => (IImageCommandSettings)base.Settings;

        public ClickImageCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new public bool Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);
            MessageBox.Show("ClickImageCommand executed.");
            return true;
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    public class HotkeyCommand : BaseCommand, ICommand, IHotkeyCommand
    {
        new public IHotkeyCommandSettings Settings => (IHotkeyCommandSettings)base.Settings;

        public HotkeyCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new public bool Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);
            MessageBox.Show("HotkeyCommand executed.");
            return true;
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    public class ClickCommand : BaseCommand, ICommand, IClickCommand
    {
        new public IClickCommandSettings Settings => (IClickCommandSettings)base.Settings;

        public ClickCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new public bool Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);
            MessageBox.Show("ClickCommand executed.");
            return true;
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    public class WaitCommand : BaseCommand, ICommand, IWaitCommand
    {
        new public IWaitCommandSettings Settings => (IWaitCommandSettings)base.Settings;

        public WaitCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new public bool Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);
            MessageBox.Show("WaitCommand executed.");
            return true;
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    internal class IfCommand : BaseCommand, ICommand, IIfCommand
    {
        new public IIfCommandSettings Settings => (IIfCommandSettings)base.Settings;

        public IfCommand(ICommand parent, ICommandSettings settings) : base(parent, settings)
        {
        }

        new public bool Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);
            throw new NotImplementedException();
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    internal class LoopCommand : BaseCommand, ICommand, ILoopCommand
    {
        new public ILoopCommandSettings Settings => (ILoopCommandSettings)base.Settings;

        public LoopCommand(ICommand? parent, ICommandSettings settings) : base(parent, settings)
        {
        }

        new public bool Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            if ( Children == null)
            {
                throw new Exception("Children is null");
            }


            for (int i = 0; i < Settings.LoopCount; i++)
            {
                foreach (var command in Children)
                {
                    if (!command.Execute(cancellationToken))
                    {
                        return false;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        new public bool CanExecute()
        {
            return true;
        }
    }
}
