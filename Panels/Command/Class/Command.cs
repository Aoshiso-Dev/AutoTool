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
using YoloWinLib;
using System.Collections.Concurrent;

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
            int progress;
            if (totalMilliseconds <= 0)
            {
                progress = 100;
            }
            else
            {
                progress = (int)Math.Round((elapsedMilliseconds / totalMilliseconds) * 100);
                if (progress < 0) progress = 0;
                if (progress > 100) progress = 100;
            }
            WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(this, progress));
        }

        protected void ResetChildrenProgress()
        {
            foreach (var command in Children)
            {
                WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(command, 0));
            }
        }

        protected async Task<bool> ExecuteChildrenAsync(CancellationToken cancellationToken)
        {
            // Children は常にリストで初期化される想定
            if (Children == null || !Children.Any())
            {
                throw new Exception("子要素がありません。");
            }

            ResetChildrenProgress();

            foreach (var command in Children)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                if (!await command.Execute(cancellationToken))
                {
                    return false;
                }
            }

            return true;
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

        public ClickCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            switch (Settings.Button)
            {
                case System.Windows.Input.MouseButton.Left:
                    await Task.Run(() => MouseHelper.Input.Click(Settings.X, Settings.Y, Settings.WindowTitle, Settings.WindowClassName));
                    break;
                case System.Windows.Input.MouseButton.Right:
                    await Task.Run(() => MouseHelper.Input.RightClick(Settings.X, Settings.Y, Settings.WindowTitle, Settings.WindowClassName));
                    break;
                case System.Windows.Input.MouseButton.Middle:
                    await Task.Run(() => MouseHelper.Input.MiddleClick(Settings.X, Settings.Y, Settings.WindowTitle, Settings.WindowClassName));
                    break;
                default:
                    throw new Exception("マウスボタンが不正です。");
            }

            var targetDescription = string.IsNullOrEmpty(Settings.WindowTitle) && string.IsNullOrEmpty(Settings.WindowClassName) 
                ? "グローバル" 
                : $"{Settings.WindowTitle}[{Settings.WindowClassName}]";
            OnDoingCommand?.Invoke(this, $"クリックしました。対象: {targetDescription} ({Settings.X}, {Settings.Y})");

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

    public class IfImageExistCommand : BaseCommand, ICommand, IIfCommand, IIfImageExistCommand
    {
        new public IIfImageCommandSettings Settings => (IIfImageCommandSettings)base.Settings;

        public IfImageExistCommand(ICommand parent, ICommandSettings settings) : base(parent, settings)
        {
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (Children == null || !Children.Any())
            {
                throw new Exception("If内に要素がありません。");
            }

            // タイムアウトなしで即座に判定
            var point = await ImageSearchHelper.SearchImage(Settings.ImagePath, cancellationToken, Settings.Threshold, Settings.SearchColor, Settings.WindowTitle, Settings.WindowClassName);

            if (point != null)
            {
                OnDoingCommand?.Invoke(this, $"画像が見つかりました。({point.Value.X}, {point.Value.Y})");
                return await ExecuteChildrenAsync(cancellationToken);
            }

            OnDoingCommand?.Invoke(this, $"画像が見つかりませんでした。");
            return true;
        }
    }

    public class IfImageNotExistCommand : BaseCommand, ICommand, IIfCommand, IIfImageNotExistCommand
    {
        new public IIfImageCommandSettings Settings => (IIfImageCommandSettings)base.Settings;

        public IfImageNotExistCommand(ICommand parent, ICommandSettings settings) : base(parent, settings)
        {
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (Children == null || !Children.Any())
            {
                throw new Exception("If内に要素がありません。");
            }

            // タイムアウトなしで即座に判定
            var point = await ImageSearchHelper.SearchImage(Settings.ImagePath, cancellationToken, Settings.Threshold, Settings.SearchColor, Settings.WindowTitle, Settings.WindowClassName);

            // 画像が「存在しない」ことを検知したら即座に子コマンドを実行
            if (point == null)
            {
                OnDoingCommand?.Invoke(this, $"画像が見つかりませんでした。");
                return await ExecuteChildrenAsync(cancellationToken);
            }

            // 画像が見つかった場合は条件不成立
            OnDoingCommand?.Invoke(this, $"画像が見つかりました。({point.Value.X}, {point.Value.Y})");
            return true;
        }
    }

    public class IfEndCommand : BaseCommand, ICommand
    {
        public IfEndCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }
        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                ResetChildrenProgress();
                return true;
            });
        }
    }

    public class LoopCommand : BaseCommand, ICommand, ILoopCommand
    {
        new public ILoopCommandSettings Settings => (ILoopCommandSettings)base.Settings;

        public LoopCommand() { }

        public LoopCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (Children == null || !Children.Any())
            {
                throw new Exception("ループ内に要素がありません。");
            }

            OnDoingCommand?.Invoke(this, $"ループを開始します。");

            for (int i = 0; i < Settings.LoopCount; i++)
            {
                ResetChildrenProgress();

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

                // 1-origin で進捗を報告して 100% に届くようにする
                ReportProgress(i + 1, Settings.LoopCount);
            }

            OnDoingCommand?.Invoke(this, $"ループを終了します。");

            return true;
        }
    }

    public class LoopEndCommand : BaseCommand, ICommand, IEndLoopCommand
    {
        new public ILoopEndCommandSettings Settings => (ILoopEndCommandSettings)base.Settings;

        public LoopEndCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                ResetChildrenProgress();

                return true;
            });
        }
    }

    public class LoopBreakCommand : BaseCommand, ICommand, ILoopBreakCommand
    {
        public LoopBreakCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() => false);
        }
    }

    public class IfImageExistAICommand : BaseCommand, ICommand, IIfCommand, IIfImageExistAICommand
    {
        new public IIfImageExistAISettings Settings => (IIfImageExistAISettings)base.Settings;

        public IfImageExistAICommand(ICommand parent, ICommandSettings settings) : base(parent, settings)
        {
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (Children == null || !Children.Any())
            {
                throw new Exception("If内に要素がありません。");
            }

            YoloWin.Init(Settings.ModelPath, 640, true);

            // AI検出は即座に実行し、ループやタイムアウトは行わない
            var det = YoloWin.DetectFromWindowTitle(Settings.WindowTitle, (float)Settings.ConfThreshold, (float)Settings.IoUThreshold).Detections;

            if (det.Count > 0)
            {
                var best = det.OrderByDescending(d => d.Score).FirstOrDefault();

                if (best.ClassId == Settings.ClassID)
                {
                    OnDoingCommand?.Invoke(this, $"画像が見つかりました。({best.Rect.X}, {best.Rect.Y}) ClassId: {best.ClassId}");
                    return await ExecuteChildrenAsync(cancellationToken);
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            OnDoingCommand?.Invoke(this, $"画像が見つかりませんでした。");
            return true;
        }
    }

    public class IfImageNotExistAICommand : BaseCommand, ICommand, IIfCommand, IIfImageNotExistAICommand
    {
        new public IIfImageNotExistAISettings Settings => (IIfImageNotExistAISettings)base.Settings;
        public IfImageNotExistAICommand(ICommand parent, ICommandSettings settings) : base(parent, settings)
        {
        }
        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (Children == null || !Children.Any())
            {
                throw new Exception("If内に要素がありません。");
            }

            YoloWin.Init(Settings.ModelPath, 640, true);

            // AI検出は即座に実行し、ループやタイムアウトは行わない
            var det = YoloWin.DetectFromWindowTitle(Settings.WindowTitle, (float)Settings.ConfThreshold, (float)Settings.IoUThreshold).Detections;

            // 指定クラスIDが検出されなかった場合に子コマンド実行
            var targetDetections = det.Where(d => d.ClassId == Settings.ClassID).ToList();

            if (targetDetections.Count == 0)
            {
                OnDoingCommand?.Invoke(this, $"クラスID {Settings.ClassID} の画像が見つかりませんでした。");
                return await ExecuteChildrenAsync(cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            OnDoingCommand?.Invoke(this, $"クラスID {Settings.ClassID} の画像が見つかりました。");
            return true;
        }
    }

    public class ExecuteCommand : BaseCommand, ICommand, IExecuteCommand
    {
        new public IExecuteCommandSettings Settings => (IExecuteCommandSettings)base.Settings;
        public ExecuteCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }
        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = Settings.ProgramPath,
                    Arguments = Settings.Arguments,
                    WorkingDirectory = Settings.WorkingDirectory,
                    UseShellExecute = true,
                };
                await Task.Run(() =>
                {
                    Process.Start(startInfo);
                    OnDoingCommand?.Invoke(this, $"プログラムを実行しました。");
                });
            }
            catch (Exception ex)
            {
                OnDoingCommand?.Invoke(this, $"プログラムの実行に失敗しました: {ex.Message}");
                return false;
            }
            return await Task.FromResult(true);
        }
    }

    // 共有変数ストア
    internal static class VariableStore
    {
        private static readonly ConcurrentDictionary<string, string> s_vars = new(StringComparer.OrdinalIgnoreCase);

        public static void Set(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            s_vars[name] = value ?? string.Empty;
        }

        public static string? Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return s_vars.TryGetValue(name, out var v) ? v : null;
        }

        public static void Clear() => s_vars.Clear();
    }

    public class SetVariableCommand : BaseCommand, ICommand, ISetVariableCommand
    {
        new public ISetVariableCommandSettings Settings => (ISetVariableCommandSettings)base.Settings;
        public SetVariableCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            VariableStore.Set(Settings.Name, Settings.Value);
            OnDoingCommand?.Invoke(this, $"変数を設定しました。{Settings.Name} = \"{Settings.Value}\"");
            return Task.FromResult(true);
        }
    }

    public class SetVariableAICommand : BaseCommand, ICommand, ISetVariableAICommand
    {
        new public ISetVariableAICommandSettings Settings => (ISetVariableAICommandSettings)base.Settings;
        public SetVariableAICommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }
        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            YoloWin.Init(Settings.ModelPath, 640, true);

            var stopwatch = Stopwatch.StartNew();

            var det = YoloWin.DetectFromWindowTitle(Settings.WindowTitle, (float)Settings.ConfThreshold, (float)Settings.IoUThreshold).Detections;

            if(det.Count == 0)
            {
                VariableStore.Set(Settings.Name, "-1");
                OnDoingCommand?.Invoke(this, $"画像が見つかりませんでした。{Settings.Name}に-1をセットしました。");
            }
            else
            {
                switch (Settings.AIDetectMode)
                {
                    case "Class":
                        // 最高スコアのものをセット
                        var best = det.OrderByDescending(d => d.Score).FirstOrDefault();
                        VariableStore.Set(Settings.Name, best.ClassId.ToString());
                        OnDoingCommand?.Invoke(this, $"画像が見つかりました。{Settings.Name}に{best.ClassId}をセットしました。");
                        break;
                    case "Count":
                        // 検出された数をセット
                        VariableStore.Set(Settings.Name, det.Count.ToString());
                        OnDoingCommand?.Invoke(this, $"画像が{det.Count}個見つかりました。{Settings.Name}に{det.Count}をセットしました。");
                        break;
                    default:
                        throw new Exception($"不明なモードです: {Settings.AIDetectMode}");
                }
            }

            return Task.FromResult(true);
        }
    }

    public class IfVariableCommand : BaseCommand, ICommand, IIfVariableCommand
    {
        new public IIfVariableCommandSettings Settings => (IIfVariableCommandSettings)base.Settings;
        public IfVariableCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (Children == null || !Children.Any())
            {
                throw new Exception("If内に要素がありません。");
            }

            var lhs = VariableStore.Get(Settings.Name) ?? string.Empty;
            var rhs = Settings.Value ?? string.Empty;

            bool result = Evaluate(lhs, rhs, Settings.Operator);
            OnDoingCommand?.Invoke(this, $"IfVariable: {Settings.Name}({lhs}) {Settings.Operator} {rhs} => {result}");

            if (result)
            {
                return await ExecuteChildrenAsync(cancellationToken);
            }

            return true;
        }

        private static bool Evaluate(string lhs, string rhs, string op)
        {
            op = (op ?? "").Trim();
            if (double.TryParse(lhs, out var lnum) && double.TryParse(rhs, out var rnum))
            {
                return op switch
                {
                    "==" => lnum == rnum,
                    "!=" => lnum != rnum,
                    ">" => lnum > rnum,
                    "<" => lnum < rnum,
                    ">=" => lnum >= rnum,
                    "<=" => lnum <= rnum,
                    _ => throw new Exception($"不明な数値比較演算子です: {op}"),
                };
            }
            else
            {
                return op switch
                {
                    "==" => string.Equals(lhs, rhs, StringComparison.Ordinal),
                    "!=" => !string.Equals(lhs, rhs, StringComparison.Ordinal),
                    "Contains" => lhs.Contains(rhs, StringComparison.Ordinal),
                    "StartsWith" => lhs.StartsWith(rhs, StringComparison.Ordinal),
                    "EndsWith" => lhs.EndsWith(rhs, StringComparison.Ordinal),
                    "IsEmpty" => string.IsNullOrEmpty(lhs),
                    "IsNotEmpty" => !string.IsNullOrEmpty(lhs),
                    _ => throw new Exception($"不明な文字列比較演算子です: {op}"),
                };
            }
        }
    }

    public class ScreenshotCommand : BaseCommand, ICommand, IScreenshotCommand
    {
        new public IScreenshotCommandSettings Settings => (IScreenshotCommandSettings)base.Settings;
        public ScreenshotCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var dir = string.IsNullOrWhiteSpace(Settings.SaveDirectory)
                    ? Path.Combine(Environment.CurrentDirectory, "Screenshots")
                    : Settings.SaveDirectory;

                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var file = $"{DateTime.Now:yyyyMMdd_HHmmssfff}.png";
                var fullPath = Path.Combine(dir, file);

                using var mat = (string.IsNullOrEmpty(Settings.WindowTitle) && string.IsNullOrEmpty(Settings.WindowClassName))
                    ? ScreenCaptureHelper.CaptureScreen()
                    : ScreenCaptureHelper.CaptureWindow(Settings.WindowTitle, Settings.WindowClassName);

                if (cancellationToken.IsCancellationRequested) return false;

                ScreenCaptureHelper.SaveCapture(mat, fullPath);

                OnDoingCommand?.Invoke(this, $"スクリーンショットを保存しました: {fullPath}");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnDoingCommand?.Invoke(this, $"スクリーンショットの保存に失敗しました: {ex.Message}");
                return false;
            }
        }
    }

    public class ClickImageAICommand : BaseCommand, ICommand, IClickImageAICommand
    {
        new public IClickImageAICommandSettings Settings => (IClickImageAICommandSettings)base.Settings;
        public ClickImageAICommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            YoloWin.Init(Settings.ModelPath, 640, true);

            // AI検出を実行
            var det = YoloWin.DetectFromWindowTitle(Settings.WindowTitle, (float)Settings.ConfThreshold, (float)Settings.IoUThreshold).Detections;

            // 指定クラスIDが検出された場合にクリック
            var targetDetections = det.Where(d => d.ClassId == Settings.ClassID).ToList();

            if (targetDetections.Count > 0)
            {
                // 最も信頼度の高い検出結果を選択
                var best = targetDetections.OrderByDescending(d => d.Score).First();
                
                // 検出領域の中心座標を計算
                int centerX = (int)(best.Rect.X + best.Rect.Width / 2);
                int centerY = (int)(best.Rect.Y + best.Rect.Height / 2);

                // マウスクリックを実行
                switch (Settings.Button)
                {
                    case System.Windows.Input.MouseButton.Left:
                        await Task.Run(() => MouseHelper.Input.Click(centerX, centerY, Settings.WindowTitle, Settings.WindowClassName));
                        break;
                    case System.Windows.Input.MouseButton.Right:
                        await Task.Run(() => MouseHelper.Input.RightClick(centerX, centerY, Settings.WindowTitle, Settings.WindowClassName));
                        break;
                    case System.Windows.Input.MouseButton.Middle:
                        await Task.Run(() => MouseHelper.Input.MiddleClick(centerX, centerY, Settings.WindowTitle, Settings.WindowClassName));
                        break;
                    default:
                        throw new Exception("マウスボタンが不正です。");
                }

                OnDoingCommand?.Invoke(this, $"AI画像クリックが完了しました。({centerX}, {centerY}) ClassId: {best.ClassId}, Score: {best.Score:F2}");
                return true;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            OnDoingCommand?.Invoke(this, $"クラスID {Settings.ClassID} の画像が見つかりませんでした。");
            return false;
        }
    }
}
