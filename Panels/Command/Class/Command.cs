using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MacroPanels.Command.Interface;
using OpenCVHelper;
using CommunityToolkit.Mvvm.Messaging;
using MacroPanels.Command.Message;
using System.Net.Mail;
using KeyHelper;
using MouseHelper;
using System.IO;

namespace MacroPanels.Command.Class
{
    public abstract class BaseCommand : ICommand
    {
        public int LineNumber { get; set; }
        public bool IsEnabled { get; set; }
        public ICommand? Parent { get; set; }
        public IEnumerable<ICommand> Children { get; set; }
        public int NestLevel { get; set; }
        public ICommandSettings Settings { get; }
        public EventHandler OnStartCommand { get; set; }
        public EventHandler OnFinishCommand { get; set; }
        public EventHandler<string> OnDoingCommand { get; set; }

        public BaseCommand()
        {
            Children = new List<ICommand>();
            Settings = new CommandSettings();
            OnStartCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new StartCommandMessage(this));
            OnDoingCommand += (sender, log) => WeakReferenceMessenger.Default.Send(new DoingCommandMessage(this, log));
            OnFinishCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
        }

        public BaseCommand(ICommand parent, ICommandSettings settings)
        {
            Parent = parent;
            NestLevel = parent == null ? 0 : parent.NestLevel + 1;
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Children = new List<ICommand>();

            OnStartCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new StartCommandMessage(this));
            OnDoingCommand += (sender, log) => WeakReferenceMessenger.Default.Send(new DoingCommandMessage(this, log));
            OnFinishCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
        }

        public virtual async Task<bool> Execute(CancellationToken cancellationToken)
        {
            OnStartCommand?.Invoke(this, EventArgs.Empty);
            bool result = await DoExecuteAsync(cancellationToken);
            OnFinishCommand?.Invoke(this, EventArgs.Empty);

            return result;
        }
        
        protected abstract Task<bool> DoExecuteAsync(CancellationToken cancellationToken);

        public bool CanExecute() => true;

        protected void ReportProgress(double elapsedMilliseconds, double totalMilliseconds)
        {
            int progress = (int)((elapsedMilliseconds / totalMilliseconds) * 100);
            WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(this, progress));
        }
    }

    public class RootCommand : BaseCommand, ICommand, IRootCommand
    {
        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    public class NothingCommand : BaseCommand, ICommand, IRootCommand
    {
        public NothingCommand() { }

        public NothingCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    public class WaitImageCommand : BaseCommand, ICommand, IWaitImageCommand
    {
        new public IWaitImageCommandSettings Settings => (IWaitImageCommandSettings)base.Settings;

        public WaitImageCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < Settings.Timeout)
            {
                var point = await ImageSearchHelper.SearchImage(Settings.ImagePath, cancellationToken, Settings.Threshold, Settings.SearchColor, Settings.WindowTitle, Settings.WindowClassName);

                if (point != null)
                {
                    OnDoingCommand?.Invoke(this, $"画像が見つかりました。({point.Value.X}, {point.Value.Y})");
                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, Settings.Timeout);

                await Task.Delay(Settings.Interval, cancellationToken);
            }

            OnDoingCommand?.Invoke(this, $"画像が見つかりませんでした。");

            return false;
        }
    }

    public class ClickImageCommand : BaseCommand, ICommand, IClickImageCommand
    {
        new public IClickImageCommandSettings Settings => (IClickImageCommandSettings)base.Settings;

        public ClickImageCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < Settings.Timeout)
            {
                var point = await ImageSearchHelper.SearchImage(Settings.ImagePath, cancellationToken, Settings.Threshold, Settings.SearchColor, Settings.WindowTitle, Settings.WindowClassName);

                if (point != null)
                {
                    switch (Settings.Button)
                    {
                        case System.Windows.Input.MouseButton.Left:
                            await Task.Run(() => MouseHelper.Input.Click(point.Value.X, point.Value.Y, Settings.WindowTitle, Settings.WindowClassName));
                            break;
                        case System.Windows.Input.MouseButton.Right:
                            await Task.Run(() => MouseHelper.Input.RightClick(point.Value.X, point.Value.Y, Settings.WindowTitle, Settings.WindowClassName));
                            break;
                        case System.Windows.Input.MouseButton.Middle:
                            await Task.Run(() => MouseHelper.Input.MiddleClick(point.Value.X, point.Value.Y, Settings.WindowTitle, Settings.WindowClassName));
                            break;
                        default:
                            throw new Exception("マウスボタンが不正です。");
                    }

                    OnDoingCommand?.Invoke(this, $"画像が見つかりました。({point.Value.X}, {point.Value.Y})");

                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, Settings.Timeout);

                await Task.Delay(Settings.Interval, cancellationToken);
            }

            OnDoingCommand?.Invoke(this, $"画像が見つかりませんでした。");

            return false;
        }

    }

    public class HotkeyCommand : BaseCommand, ICommand, IHotkeyCommand
    {
        new public IHotkeyCommandSettings Settings => (IHotkeyCommandSettings)base.Settings;

        public HotkeyCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() => KeyHelper.Input.KeyPress(Settings.Key, Settings.Ctrl, Settings.Alt, Settings.Shift, Settings.WindowTitle, Settings.WindowClassName));

            OnDoingCommand?.Invoke(this, $"ホットキーが押されました。");

            return true;
        }
    }

    public class ClickCommand : BaseCommand, ICommand, IClickCommand
    {
        new public IClickCommandSettings Settings => (IClickCommandSettings)base.Settings;

        public ClickCommand(ICommand parent, ICommandSettings settings) : base(parent, settings){ }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            switch (Settings.Button)
            {
                case System.Windows.Input.MouseButton.Left:
                    await Task.Run(() => MouseHelper.Input.Click(Settings.X, Settings.Y));
                    break;
                case System.Windows.Input.MouseButton.Right:
                    await Task.Run(() => MouseHelper.Input.RightClick(Settings.X, Settings.Y));
                    break;
                case System.Windows.Input.MouseButton.Middle:
                    await Task.Run(() => MouseHelper.Input.MiddleClick(Settings.X, Settings.Y));
                    break;
                default:
                    throw new Exception("マウスボタンが不正です。");
            }
            
            OnDoingCommand?.Invoke(this, $"クリックしました。({Settings.X}, {Settings.Y})");

            return true;
        }
    }

    public class WaitCommand : BaseCommand, ICommand, IWaitCommand
    {
        new public IWaitCommandSettings Settings => (IWaitCommandSettings)base.Settings;

        public WaitCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < Settings.Wait)
            {
                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, Settings.Wait);

                await Task.Delay(100, cancellationToken);
            }

            OnDoingCommand?.Invoke(this, $"待機しました。");

            return true;
        }
    }

    public class IfCommand : BaseCommand, ICommand, IIfCommand
    {
        public IfCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return await Task.FromResult(true);
        }
    }

    public class IfImageExistCommand : BaseCommand, ICommand, IIfImageExistCommand
    {
        new public IWaitImageCommandSettings Settings => (IWaitImageCommandSettings)base.Settings;


        public IfImageExistCommand(ICommand parent, ICommandSettings settings) : base(parent, settings)
        {
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (Children == null)
            {
                throw new Exception("If内に要素がありません。");
            }

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < Settings.Timeout)
            {
                var point = await ImageSearchHelper.SearchImage(Settings.ImagePath, cancellationToken, Settings.Threshold, Settings.SearchColor, Settings.WindowTitle, Settings.WindowClassName);

                if (point != null)
                {
                    OnDoingCommand?.Invoke(this, $"画像が見つかりました。({point.Value.X}, {point.Value.Y})");

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

            OnDoingCommand?.Invoke(this, $"画像が見つかりませんでした。");

            return true;
        }

    }

    public class IfImageNotExistCommand : BaseCommand, ICommand, IIfImageNotExistCommand
    {
        new public IWaitImageCommandSettings Settings => (IWaitImageCommandSettings)base.Settings;


        public IfImageNotExistCommand(ICommand parent, ICommandSettings settings) : base(parent, settings)
        {
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (Children == null)
            {
                throw new Exception("If内に要素がありません。");
            }

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < Settings.Timeout)
            {
                var point = await ImageSearchHelper.SearchImage(Settings.ImagePath, cancellationToken, Settings.Threshold, Settings.SearchColor, Settings.WindowTitle, Settings.WindowClassName);

                if (point != null)
                {
                    OnDoingCommand?.Invoke(this, $"画像が見つかりました。({point.Value.X}, {point.Value.Y})");
                    return true;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                ReportProgress(stopwatch.ElapsedMilliseconds, Settings.Timeout);

                await Task.Delay(Settings.Interval, cancellationToken);
            }

            OnDoingCommand?.Invoke(this, $"画像が見つかりませんでした。");

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

            OnFinishCommand?.Invoke(this, new EventArgs());

            return true;
        }
    }

    public class LoopCommand : BaseCommand, ICommand, ILoopCommand
    {
        new public ILoopCommandSettings Settings => (ILoopCommandSettings)base.Settings;

        public LoopCommand() { }

        public LoopCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if ( Children == null)
            {
                throw new Exception("ループ内に要素がありません。");
            }

            OnDoingCommand?.Invoke(this, $"ループを開始します。");

            for (int i = 0; i < Settings.LoopCount; i++)
            {
                foreach (var command in Children)
                {
                    WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(command, 0));
                }

                foreach (var command in Children)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return false;
                    }

                    if (!await command.Execute(cancellationToken))
                    {
                        return true;
                    }
                }

                ReportProgress(i, Settings.LoopCount);
            }

            OnDoingCommand?.Invoke(this, $"ループを終了します。");

            return true;
        }
    }

    public class EndLoopCommand : BaseCommand, ICommand, IEndLoopCommand
    {
        new public IEndLoopCommandSettings Settings => (IEndLoopCommandSettings)base.Settings;

        public EndLoopCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                foreach (var command in Children)
                {
                    WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(command, 0));
                }

                return true;
            });
        }
    }

    public class BreakCommand : BaseCommand, ICommand, IBreakCommand
    {
        public BreakCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() => false);
        }
    }
}
