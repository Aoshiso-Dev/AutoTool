using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
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
        public EventHandler<int>? OnCommandRunning { get; set; }

        public RootCommand()
        {
            Children = new List<ICommand>();
        }

        public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            foreach (var command in Children)
            {
                if (!await command.Execute(cancellationToken))
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
        public EventHandler<int>? OnCommandRunning { get; set; } = null;

        public BaseCommand(ICommand parent, ICommandSettings settings)
        {
            Parent = parent;
            NestLevel = parent == null ? 0 : parent.NestLevel + 1;
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Children = new List<ICommand>();
        }

        public async Task<bool> Execute(CancellationToken cancellationToken )
        {
            OnCommandRunning?.Invoke(this, ListNumber);
            await Task.Delay(0, cancellationToken);

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

        new async public Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            var point = await ImageFinder.WaitForImageAsync(Settings.ImagePath, Settings.Threshold, Settings.Timeout, Settings.Interval, cancellationToken);

            return point != null;
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

        new public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            var point = await ImageFinder.WaitForImageAsync(Settings.ImagePath, Settings.Threshold, Settings.Timeout, Settings.Interval, cancellationToken);
                
            if( point != null)
            {
                MouseControlHelper.Click(point.Value.X, point.Value.Y);

                return true;
            }

            return false;
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

        new public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            KeyControlHelper.KeyPress(Settings.Key, Settings.Ctrl, Settings.Alt, Settings.Shift);

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

        public ClickCommand(ICommand parent, ICommandSettings settings) : base(parent, settings)
        {
            Settings.Button = Settings.Button;
            Settings.X = Settings.X;
            Settings.Y = Settings.Y;
        }

        new public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            switch (Settings.Button)
            {
                case System.Windows.Input.MouseButton.Left:
                    MouseControlHelper.Click(Settings.X, Settings.Y);
                    break;
                case System.Windows.Input.MouseButton.Right:
                    MouseControlHelper.RightClick(Settings.X, Settings.Y);
                    break;
                case System.Windows.Input.MouseButton.Middle:
                    MouseControlHelper.MiddleClick(Settings.X, Settings.Y);
                    break;
                default:
                    throw new Exception("Invalid MouseButton.");
            }

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

        new public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            Debug.WriteLine($"{ListNumber} : WaitCommand");

            await base.Execute(cancellationToken);

            await Task.Delay(Settings.Wait, cancellationToken);

            return true;
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    internal class IfCommand : BaseCommand, ICommand, IIfCommand
    {
        public IfCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }
    }
    internal class IfImageExistCommand : BaseCommand, ICommand, IIfImageExistCommand
    {
        new public IImageCommandSettings Settings => (IImageCommandSettings)base.Settings;


        public IfImageExistCommand(ICommand parent, ICommandSettings settings) : base(parent, settings)
        {
        }

        new public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            if (Children == null)
            {
                throw new Exception("Children is null");
            }

            var point = await ImageFinder.WaitForImageAsync(Settings.ImagePath, Settings.Threshold, Settings.Timeout, Settings.Interval, cancellationToken);

            if (point != null)
            {
                foreach (var command in Children)
                {
                    if (!await command.Execute(cancellationToken))
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

    internal class IfImageNotExistCommand : BaseCommand, ICommand, IIfImageNotExistCommand
    {
        new public IImageCommandSettings Settings => (IImageCommandSettings)base.Settings;


        public IfImageNotExistCommand(ICommand parent, ICommandSettings settings) : base(parent, settings)
        {
        }

        new public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            if (Children == null)
            {
                throw new Exception("Children is null");
            }

            var point = await ImageFinder.WaitForImageAsync(Settings.ImagePath, Settings.Threshold, Settings.Timeout, Settings.Interval, cancellationToken);

            if (point == null)
            {
                foreach (var command in Children)
                {
                    if (!await command.Execute(cancellationToken))
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

        internal class LoopCommand : BaseCommand, ICommand, ILoopCommand
    {
        new public ILoopCommandSettings Settings => (ILoopCommandSettings)base.Settings;

        public LoopCommand(ICommand parent, ICommandSettings settings) : base(parent, settings)
        {
        }

        new public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            Debug.WriteLine($"{ListNumber} : {nameof(LoopCommand)}");

            await base.Execute(cancellationToken);

            if ( Children == null)
            {
                throw new Exception("Children is null");
            }


            for (int i = 0; i < Settings.LoopCount; i++)
            {
                foreach (var command in Children)
                {
                    if (!await command.Execute(cancellationToken))
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
