using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Command.Interface;
using OpenCVHelper;
using InputHelper;
using CommunityToolkit.Mvvm.Messaging;
using Command.Message;

namespace Command.Class
{
    public class RootCommand : IRootCommand
    {
        public int LineNumber { get; set; } = 0;
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
        public int LineNumber { get; set; }
        public bool IsEnabled { get; set; }
        public ICommand? Parent { get; set; }
        public IEnumerable<ICommand> Children { get; set; }
        public int NestLevel { get; set; }
        public ICommandSettings Settings { get; }
        public EventHandler<int>? OnCommandRunning { get; set; } = null;

        public BaseCommand() { }

        public BaseCommand(ICommand parent, ICommandSettings settings)
        {
            Parent = parent;
            NestLevel = parent == null ? 0 : parent.NestLevel + 1;
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Children = new List<ICommand>();
        }

        public async Task<bool> Execute(CancellationToken cancellationToken )
        {
            WeakReferenceMessenger.Default.Send(new ExecuteCommandMessage(this));

            return true;
        }
        public bool CanExecute()
        {
            return true;
        }
    }

    public class WaitImageCommand : BaseCommand, ICommand, IWaitImageCommand
    {
        new public IWaitImageCommandSettings Settings => (IWaitImageCommandSettings)base.Settings;

        public WaitImageCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new async public Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            var point = await ImageSearchHelper.WaitForImageAsync(Settings.ImagePath, Settings.Threshold, Settings.Timeout, Settings.Interval, cancellationToken);

            return point != null;
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    public class ClickImageCommand : BaseCommand, ICommand, IClickImageCommand
    {
        new public IClickImageCommandSettings Settings => (IClickImageCommandSettings)base.Settings;

        public ClickImageCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            var point = await ImageSearchHelper.WaitForImageAsync(Settings.ImagePath, Settings.Threshold, Settings.Timeout, Settings.Interval, cancellationToken);
                
            if( point != null)
            {
                switch(Settings.Button)
                {
                    case System.Windows.Input.MouseButton.Left:
                        MouseControlHelper.Click(point.Value.X, point.Value.Y);
                        break;
                    case System.Windows.Input.MouseButton.Right:
                        MouseControlHelper.RightClick(point.Value.X, point.Value.Y);
                        break;
                    case System.Windows.Input.MouseButton.Middle:
                        MouseControlHelper.MiddleClick(point.Value.X, point.Value.Y);
                        break;
                    case System.Windows.Input.MouseButton.XButton1:
                        //MouseControlHelper.XButton1Click(point.Value.X, point.Value.Y);
                        break;
                    case System.Windows.Input.MouseButton.XButton2:
                        //MouseControlHelper.XButton2Click(point.Value.X, point.Value.Y);
                        break;
                }

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

        public ClickCommand(ICommand parent, ICommandSettings settings) : base(parent, settings){ }

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
            Debug.WriteLine($"{LineNumber} : WaitCommand");

            await base.Execute(cancellationToken);

            await Task.Delay(Settings.Wait, cancellationToken);

            return true;
        }
        new public bool CanExecute()
        {
            return true;
        }
    }

    public class IfCommand : BaseCommand, ICommand, IIfCommand
    {
        public IfCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }
    }
    public class IfImageExistCommand : BaseCommand, ICommand, IIfImageExistCommand
    {
        new public IWaitImageCommandSettings Settings => (IWaitImageCommandSettings)base.Settings;


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

            var point = await ImageSearchHelper.WaitForImageAsync(Settings.ImagePath, Settings.Threshold, Settings.Timeout, Settings.Interval, cancellationToken);

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

    public class IfImageNotExistCommand : BaseCommand, ICommand, IIfImageNotExistCommand
    {
        new public IWaitImageCommandSettings Settings => (IWaitImageCommandSettings)base.Settings;


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

            var point = await ImageSearchHelper.WaitForImageAsync(Settings.ImagePath, Settings.Threshold, Settings.Timeout, Settings.Interval, cancellationToken);

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

    public class LoopCommand : BaseCommand, ICommand, ILoopCommand
    {
        private CancellationTokenSource? _cts = null;
        new public ILoopCommandSettings Settings => (ILoopCommandSettings)base.Settings;

        public LoopCommand() { }

        public LoopCommand(ICommand parent, ICommandSettings settings) : base(parent, settings)
        {
        }

        new public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await base.Execute(cancellationToken);

            if ( Children == null)
            {
                throw new Exception("Children is null");
            }

            try
            {
                for (int i = 0; i < Settings.LoopCount; i++)
                {
                    foreach (var command in Children)
                    {
                        if (cancellationToken.IsCancellationRequested || _cts.IsCancellationRequested)
                        {
                            return false;
                        }


                        if (!await command.Execute(_cts.Token))
                        {
                            _cts.Cancel();
                            return true;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }

            return true;
        }

        new public bool CanExecute()
        {
            return true;
        }
    }

    public class BreakCommand : BaseCommand, ICommand, IBreakCommand
    {
        public BreakCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            return false;
        }

        new public bool CanExecute()
        {
            return true;
        }
    }
}
