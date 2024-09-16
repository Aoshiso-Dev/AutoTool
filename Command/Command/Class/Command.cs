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
    public class BaseCommand : ICommand
    {
        public int LineNumber { get; set; }
        public bool IsEnabled { get; set; }
        public ICommand? Parent { get; set; }
        public IEnumerable<ICommand> Children { get; set; }
        public int NestLevel { get; set; }
        public ICommandSettings Settings { get; }
        public EventHandler OnStartCommand { get; set; }
        public EventHandler OnFinishCommand { get; set; }

        public BaseCommand() { }

        public BaseCommand(ICommand parent, ICommandSettings settings)
        {
            Parent = parent;
            NestLevel = parent == null ? 0 : parent.NestLevel + 1;
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Children = new List<ICommand>();

            OnStartCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new StartCommandMessage(this));
            OnFinishCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
        }

        public virtual async Task<bool> Execute(CancellationToken cancellationToken)
        {
            OnStartCommand?.Invoke(this, EventArgs.Empty);
            bool result = await Task.FromResult(true); // 子クラスで上書き
            OnFinishCommand?.Invoke(this, EventArgs.Empty);
            return result;
        }

        public bool CanExecute() => true;

        // 進捗報告を共通化
        protected void ReportProgress(double elapsedMilliseconds, double totalMilliseconds)
        {
            int progress = (int)((elapsedMilliseconds / totalMilliseconds) * 100);
            WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(this, progress));
        }
    }

    public class RootCommand : BaseCommand, ICommand, IRootCommand
    {
    }

    public class WaitImageCommand : BaseCommand, ICommand, IWaitImageCommand
    {
        new public IWaitImageCommandSettings Settings => (IWaitImageCommandSettings)base.Settings;

        public WaitImageCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        public override async Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < Settings.Timeout)
            {
                var point = ImageSearchHelper.SearchImage(Settings.ImagePath);

                if (point != null)
                {
                    OnFinishCommand?.Invoke(this, new EventArgs());

                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, Settings.Timeout);

                await Task.Delay(Settings.Interval, cancellationToken);
            }

            return false;
        }
    }

    public class ClickImageCommand : BaseCommand, ICommand, IClickImageCommand
    {
        new public IClickImageCommandSettings Settings => (IClickImageCommandSettings)base.Settings;

        public ClickImageCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        public override async Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < Settings.Timeout)
            {
                var point = ImageSearchHelper.SearchImage(Settings.ImagePath);

                if (point != null)
                {
                    switch (Settings.Button)
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
                        default:
                            throw new Exception("Invalid MouseButton.");
                    }

                    OnFinishCommand?.Invoke(this, new EventArgs());

                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, Settings.Timeout);

                await Task.Delay(Settings.Interval, cancellationToken);
            }

            return false;
        }

    }

    public class HotkeyCommand : BaseCommand, ICommand, IHotkeyCommand
    {
        new public IHotkeyCommandSettings Settings => (IHotkeyCommandSettings)base.Settings;

        public HotkeyCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        public override async Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            KeyControlHelper.KeyPress(Settings.Ctrl, Settings.Alt, Settings.Shift, Settings.Key);

            return true;
        }
    }

    public class ClickCommand : BaseCommand, ICommand, IClickCommand
    {
        new public IClickCommandSettings Settings => (IClickCommandSettings)base.Settings;

        public ClickCommand(ICommand parent, ICommandSettings settings) : base(parent, settings){ }

        public override async Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            switch(Settings.Button)
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
                    throw new Exception("マウスボタンが不正です。");
            }

            return true;
        }
    }

    public class WaitCommand : BaseCommand, ICommand, IWaitCommand
    {
        new public IWaitCommandSettings Settings => (IWaitCommandSettings)base.Settings;

        public WaitCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        public override async Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < Settings.Wait)
            {
                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, Settings.Wait);

                await Task.Delay(100, cancellationToken);
            }

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
                throw new Exception("If内に要素がありません。");
            }

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < Settings.Timeout)
            {

                var point = ImageSearchHelper.SearchImage(Settings.ImagePath);

                if (point != null)
                {
                    foreach (var command in Children)
                    {
                        WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(command, 0));
                    }

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

                    return true;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                ReportProgress(stopwatch.ElapsedMilliseconds, Settings.Timeout);

                await Task.Delay(Settings.Interval, cancellationToken);
            }

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
                throw new Exception("If内に要素がありません。");
            }

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < Settings.Timeout)
            {
                var point = ImageSearchHelper.SearchImage(Settings.ImagePath);

                if(point != null)
                {
                    return true;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                ReportProgress(stopwatch.ElapsedMilliseconds, Settings.Timeout);

                await Task.Delay(Settings.Interval, cancellationToken);
            }

            OnFinishCommand?.Invoke(this, new EventArgs());

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

            return true;
        }
    }

    public class LoopCommand : BaseCommand, ICommand, ILoopCommand
    {
        private CancellationTokenSource? _cts = null;
        new public ILoopCommandSettings Settings => (ILoopCommandSettings)base.Settings;

        public LoopCommand() { }

        public LoopCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        new public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            await base.Execute(cancellationToken);

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if ( Children == null)
            {
                throw new Exception("ループ内に要素がありません。");
            }

            try
            {
                for (int i = 0; i < Settings.LoopCount; i++)
                {
                    foreach (var command in Children)
                    {
                        WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(command, 0));
                    }

                    foreach (var command in Children)
                    {
                        //if (cancellationToken.IsCancellationRequested || _cts.IsCancellationRequested)
                        //{
                        //    return false;
                        //}


                        if (!await command.Execute(_cts.Token))
                        {
                            _cts.Cancel();
                            return true;
                        }
                    }

                    ReportProgress(i, Settings.LoopCount);
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
    }

    public class EndLoopCommand : BaseCommand, ICommand, IEndLoopCommand
    {
        public IEndLoopCommandSettings Settings => (IEndLoopCommandSettings)base.Settings;

        public EndLoopCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        public override Task<bool> Execute(CancellationToken cancellationToken)
        {
            foreach (var command in Children)
            {
                WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(command, 0));
            }

            return Task.FromResult(true);
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
    }
}
