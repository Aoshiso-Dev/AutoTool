using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
using System.Windows.Media;

namespace MacroPanels.Command.Class
{
    /// <summary>
    /// ループ中断用の専用例外
    /// </summary>
    public class LoopBreakException : Exception
    {
        public LoopBreakException() : base("ループが中断されました") { }
        public LoopBreakException(string message) : base(message) { }
    }

    /// <summary>
    /// コマンドの基底クラス
    /// </summary>
    public abstract class BaseCommand : ICommand
    {
        // プライベートフィールド
        private readonly List<ICommand> _children = new();

        // ICommandインターフェースの実装
        public int LineNumber { get; set; }
        public bool IsEnabled { get; set; } = true;
        public ICommand? Parent { get; private set; }
        public IEnumerable<ICommand> Children => _children;
        public int NestLevel { get; set; }
        public object? Settings { get; set; }
        public string Description { get; protected set; } = string.Empty;

        // イベント
        public event EventHandler? OnStartCommand;
        public event EventHandler? OnFinishCommand;
        public event EventHandler<string>? OnDoingCommand;

        protected BaseCommand(ICommand? parent = null, object? settings = null)
        {
            Parent = parent;
            Settings = settings;
            NestLevel = parent?.NestLevel + 1 ?? 0;
            
            // メッセージング設定
            OnStartCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new StartCommandMessage(this));
            OnDoingCommand += (sender, log) => WeakReferenceMessenger.Default.Send(new DoingCommandMessage(this, log ?? ""));
            OnFinishCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
        }

        public virtual void AddChild(ICommand child)
        {
            _children.Add(child);
        }

        public virtual void RemoveChild(ICommand child)
        {
            _children.Remove(child);
        }

        public virtual IEnumerable<ICommand> GetChildren()
        {
            return _children;
        }

        /// <summary>
        /// ファイルパスの有効性を検証
        /// </summary>
        protected virtual void ValidateFiles()
        {
            // 基底クラスでは何もしない（派生クラスでオーバーライド）
        }

        /// <summary>
        /// ファイル存在チェック（エラー時に例外を投げる）
        /// </summary>
        protected void ValidateFileExists(string filePath, string fileDescription)
        {
            if (string.IsNullOrEmpty(filePath))
                return; // 空の場合はチェックしない

            var absolutePath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(Environment.CurrentDirectory, filePath);
            
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException($"{fileDescription}が見つかりません: {filePath}\n確認したパス: {absolutePath}");
            }
        }

        /// <summary>
        /// ディレクトリ存在チェック（エラー時に例外を投げる）
        /// </summary>
        protected void ValidateDirectoryExists(string directoryPath, string directoryDescription)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return; // 空の場合はチェックしない

            var absolutePath = Path.IsPathRooted(directoryPath) ? directoryPath : Path.Combine(Environment.CurrentDirectory, directoryPath);
            
            if (!Directory.Exists(absolutePath))
            {
                throw new DirectoryNotFoundException($"{directoryDescription}が見つかりません: {directoryPath}\n確認したパス: {absolutePath}");
            }
        }

        /// <summary>
        /// 保存先ディレクトリの親フォルダ存在チェック
        /// </summary>
        protected void ValidateSaveDirectoryParentExists(string directoryPath, string directoryDescription)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return; // 空の場合はチェックしない

            var absolutePath = Path.IsPathRooted(directoryPath) ? directoryPath : Path.Combine(Environment.CurrentDirectory, directoryPath);
            
            // ディレクトリが既に存在する場合はOK
            if (Directory.Exists(absolutePath))
                return;
                
            // 親ディレクトリが存在するかチェック
            var parentDir = Path.GetDirectoryName(absolutePath);
            if (string.IsNullOrEmpty(parentDir) || !Directory.Exists(parentDir))
            {
                throw new DirectoryNotFoundException($"{directoryDescription}の親フォルダが見つかりません: {directoryPath}\n親フォルダ: {parentDir ?? "不明"}");
            }
        }

        /// <summary>
        /// コマンドを実行
        /// </summary>
        public virtual async Task<bool> Execute(CancellationToken cancellationToken)
        {
            if (!IsEnabled)
                return true;

            OnStartCommand?.Invoke(this, EventArgs.Empty);
            WeakReferenceMessenger.Default.Send(new StartCommandMessage(this));

            try
            {
                // 実行前にファイル検証を行う
                ValidateFiles();
                
                var result = await DoExecuteAsync(cancellationToken);
                WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
                return result;
            }
            catch (OperationCanceledException)
            {
                WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
                throw;
            }
            catch (LoopBreakException)
            {
                // LoopBreakExceptionはそのまま上位に伝播
                WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
                throw;
            }
            catch (FileNotFoundException ex)
            {
                LogMessage($"❌ ファイルエラー: {ex.Message}");
                WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
                return false;
            }
            catch (DirectoryNotFoundException ex)
            {
                LogMessage($"❌ ディレクトリエラー: {ex.Message}");
                WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 実行エラー: {ex.Message}");
                WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
                return false;
            }
        }

        /// <summary>
        /// 実際の実行処理（派生クラスで実装）
        /// </summary>
        protected abstract Task<bool> DoExecuteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 子コマンドを順次実行
        /// </summary>
        protected async Task<bool> ExecuteChildrenAsync(CancellationToken cancellationToken)
        {
            foreach (var child in _children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var result = await child.Execute(cancellationToken);
                if (!result)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 進捗報告
        /// </summary>
        protected void ReportProgress(double elapsedMilliseconds, double totalMilliseconds)
        {
            int progress = totalMilliseconds <= 0 ? 100 : 
                Math.Max(0, Math.Min(100, (int)Math.Round((elapsedMilliseconds / totalMilliseconds) * 100)));
            
            WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(this, progress));
        }

        /// <summary>
        /// 子要素の進捗をリセット
        /// </summary>
        protected void ResetChildrenProgress()
        {
            foreach (var command in Children)
            {
                WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(command, 0));
            }
        }

        /// <summary>
        /// ログ出力
        /// </summary>
        protected void LogMessage(string message)
        {
            OnDoingCommand?.Invoke(this, message);
            WeakReferenceMessenger.Default.Send(new DoingCommandMessage(this, message));
        }
    }

    /// <summary>
    /// ルートコマンド
    /// </summary>
    public class RootCommand : BaseCommand, IRootCommand
    {
        public RootCommand() : base(null, null)
        {
            Description = "ルートコマンド";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return ExecuteChildrenAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 何もしないコマンド
    /// </summary>
    public class NothingCommand : BaseCommand, IRootCommand
    {
        public NothingCommand() : base(null, null)
        {
            Description = "何もしない";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// 画像待機コマンド
    /// </summary>
    public class WaitImageCommand : BaseCommand, IWaitImageCommand
    {
        public new IWaitImageCommandSettings Settings => (IWaitImageCommandSettings)base.Settings!;

        public WaitImageCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "画像待機";
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null)
            {
                ValidateFileExists(settings.ImagePath, "画像ファイル");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Timeout)
            {
                var point = await ImageSearchHelper.SearchImage(
                    settings.ImagePath, cancellationToken, settings.Threshold, 
                    settings.SearchColor, settings.WindowTitle, settings.WindowClassName);

                if (point != null)
                {
                    LogMessage($"画像が見つかりました。({point.Value.X}, {point.Value.Y})");
                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, settings.Timeout);
                await Task.Delay(settings.Interval, cancellationToken);
            }

            LogMessage("画像が見つかりませんでした。");
            return false;
        }
    }

    /// <summary>
    /// 画像クリックコマンド
    /// </summary>
    public class ClickImageCommand : BaseCommand, IClickImageCommand
    {
        public new IClickImageCommandSettings Settings => (IClickImageCommandSettings)base.Settings!;

        public ClickImageCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "画像クリック";
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null)
            {
                ValidateFileExists(settings.ImagePath, "画像ファイル");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Timeout)
            {
                var point = await ImageSearchHelper.SearchImage(
                    settings.ImagePath, cancellationToken, settings.Threshold,
                    settings.SearchColor, settings.WindowTitle, settings.WindowClassName);

                if (point != null)
                {
                    await ExecuteMouseClick(point.Value.X, point.Value.Y, settings.Button, 
                        settings.WindowTitle, settings.WindowClassName);
                    
                    LogMessage($"画像をクリックしました。({point.Value.X}, {point.Value.Y})");
                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, settings.Timeout);
                await Task.Delay(settings.Interval, cancellationToken);
            }

            LogMessage("画像が見つかりませんでした。");
            return false;
        }

        private static async Task ExecuteMouseClick(int x, int y, System.Windows.Input.MouseButton button, 
            string windowTitle, string windowClassName)
        {
            switch (button)
            {
                case System.Windows.Input.MouseButton.Left:
                    await MouseHelper.Input.ClickAsync(x, y, windowTitle, windowClassName);
                    break;
                case System.Windows.Input.MouseButton.Right:
                    await MouseHelper.Input.RightClickAsync(x, y, windowTitle, windowClassName);
                    break;
                case System.Windows.Input.MouseButton.Middle:
                    await MouseHelper.Input.MiddleClickAsync(x, y, windowTitle, windowClassName);
                    break;
                default:
                    throw new ArgumentException($"サポートされていないマウスボタン: {button}");
            }
        }
    }

    /// <summary>
    /// ホットキーコマンド
    /// </summary>
    public class HotkeyCommand : BaseCommand, IHotkeyCommand
    {
        public new IHotkeyCommandSettings Settings => (IHotkeyCommandSettings)base.Settings!;

        public HotkeyCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "ホットキー";
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            await Task.Run(() => KeyHelper.Input.KeyPress(
                settings.Key, settings.Ctrl, settings.Alt, settings.Shift,
                settings.WindowTitle, settings.WindowClassName));

            LogMessage("ホットキーを実行しました。");
            return true;
        }
    }

    /// <summary>
    /// クリックコマンド
    /// </summary>
    public class ClickCommand : BaseCommand, IClickCommand
    {
        public new IClickCommandSettings Settings => (IClickCommandSettings)base.Settings!;

        public ClickCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "クリック";
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            await ExecuteMouseClick(settings.X, settings.Y, settings.Button,
                settings.WindowTitle, settings.WindowClassName);

            var target = string.IsNullOrEmpty(settings.WindowTitle) && string.IsNullOrEmpty(settings.WindowClassName)
                ? "グローバル" : $"{settings.WindowTitle}[{settings.WindowClassName}]";
            
            LogMessage($"クリックしました。対象: {target} ({settings.X}, {settings.Y})");
            return true;
        }

        private static async Task ExecuteMouseClick(int x, int y, System.Windows.Input.MouseButton button,
            string windowTitle, string windowClassName)
        {
            switch (button)
            {
                case System.Windows.Input.MouseButton.Left:
                    await MouseHelper.Input.ClickAsync(x, y, windowTitle, windowClassName);
                    break;
                case System.Windows.Input.MouseButton.Right:
                    await MouseHelper.Input.RightClickAsync(x, y, windowClassName);
                    break;
                case System.Windows.Input.MouseButton.Middle:
                    await MouseHelper.Input.MiddleClickAsync(x, y, windowClassName);
                    break;
                default:
                    throw new ArgumentException($"サポートされていないマウスボタン: {button}");
            }
        }
    }

    /// <summary>
    /// 待機コマンド
    /// </summary>
    public class WaitCommand : BaseCommand, IWaitCommand
    {
        public new IWaitCommandSettings Settings => (IWaitCommandSettings)base.Settings!;

        public WaitCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "待機";
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Wait)
            {
                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, settings.Wait);
                await Task.Delay(50, cancellationToken);
            }

            LogMessage("待機が完了しました。");
            return true;
        }
    }

    /// <summary>
    /// ループコマンド
    /// </summary>
    public class LoopCommand : BaseCommand, ILoopCommand
    {
        public new ILoopCommandSettings Settings => (ILoopCommandSettings)base.Settings!;

        public LoopCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "ループ";
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            LogMessage($"ループを開始します。({settings.LoopCount}回)");

            for (int i = 0; i < settings.LoopCount; i++)
            {
                if (cancellationToken.IsCancellationRequested) return false;

                ResetChildrenProgress();

                try
                {
                    var result = await ExecuteChildrenAsync(cancellationToken);
                    if (!result) return false;
                }
                catch (LoopBreakException)
                {
                    // LoopBreakExceptionをキャッチしてこのループを中断
                    LogMessage($"ループが中断されました。(実行回数: {i + 1}/{settings.LoopCount})");
                    break; // このループのみを抜ける
                }

                ReportProgress(i + 1, settings.LoopCount);
            }

            LogMessage("ループが完了しました。");
            return true;
        }

        /// <summary>
        /// 子コマンドを順次実行（LoopBreakException対応版）
        /// </summary>
        protected new async Task<bool> ExecuteChildrenAsync(CancellationToken cancellationToken)
        {
            foreach (var child in Children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    var result = await child.Execute(cancellationToken);
                    if (!result)
                        return false;
                }
                catch (LoopBreakException)
                {
                    // LoopBreakExceptionは上位のLoopCommandに伝播
                    throw;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// If文の基底クラス
    /// </summary>
    public abstract class IfCommand : BaseCommand
    {
        protected IfCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var condition = await EvaluateConditionAsync(cancellationToken);
            if (condition)
            {
                return await ExecuteChildrenAsync(cancellationToken);
            }
            return true; // 条件が偽でも成功として扱う
        }

        protected abstract Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// 画像存在確認If文
    /// </summary>
    public class IfImageExistCommand : IfCommand, IIfImageExistCommand
    {
        public new IIfImageCommandSettings Settings => (IIfImageCommandSettings)base.Settings!;

        public IfImageExistCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "画像存在確認";
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null)
            {
                ValidateFileExists(settings.ImagePath, "画像ファイル");
            }
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            var point = await ImageSearchHelper.SearchImage(
                settings.ImagePath, cancellationToken, settings.Threshold,
                settings.SearchColor, settings.WindowTitle, settings.WindowClassName);

            if (point != null)
            {
                LogMessage($"画像が見つかりました。({point.Value.X}, {point.Value.Y})");
                return true;
            }

            LogMessage("画像が見つかりませんでした。");
            return false;
        }
    }

    /// <summary>
    /// 画像非存在確認If文
    /// </summary>
    public class IfImageNotExistCommand : IfCommand, IIfImageNotExistCommand
    {
        public new IIfImageCommandSettings Settings => (IIfImageCommandSettings)base.Settings!;

        public IfImageNotExistCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "画像非存在確認";
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null)
            {
                ValidateFileExists(settings.ImagePath, "画像ファイル");
            }
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            var point = await ImageSearchHelper.SearchImage(
                settings.ImagePath, cancellationToken, settings.Threshold,
                settings.SearchColor, settings.WindowTitle, settings.WindowClassName);

            if (point == null)
            {
                LogMessage("画像が見つかりませんでした。");
                return true;
            }

            LogMessage($"画像が見つかりました。({point.Value.X}, {point.Value.Y})");
            return false;
        }
    }

    /// <summary>
    /// AI画像存在確認If文
    /// </summary>
    public class IfImageExistAICommand : IfCommand, IIfImageExistAICommand
    {
        public new IIfImageExistAISettings Settings => (IIfImageExistAISettings)base.Settings!;

        public IfImageExistAICommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "AI画像存在確認";
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null)
            {
                ValidateFileExists(settings.ModelPath, "ONNXモデルファイル");
            }
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            if (Children == null || !Children.Any())
            {
                throw new Exception("If内に要素がありません。");
            }

            YoloWin.Init(settings.ModelPath, 640, true);

            // AI検出は即座に実行し、ループやタイムアウトは行わない
            var det = YoloWin.DetectFromWindowTitle(settings.WindowTitle, (float)settings.ConfThreshold, (float)settings.IoUThreshold).Detections;

            if (det.Count > 0)
            {
                var best = det.OrderByDescending(d => d.Score).FirstOrDefault();

                if (best.ClassId == settings.ClassID)
                {
                    LogMessage($"画像が見つかりました。({best.Rect.X}, {best.Rect.Y}) ClassId: {best.ClassId}");
                    return true;
                }
            }

            LogMessage("画像が見つかりませんでした。");
            return false;
        }
    }

    /// <summary>
    /// AI画像非存在確認If文
    /// </summary>
    public class IfImageNotExistAICommand : IfCommand, IIfImageNotExistAICommand
    {
        public new IIfImageNotExistAISettings Settings => (IIfImageNotExistAISettings)base.Settings!;

        public IfImageNotExistAICommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "AI画像非存在確認";
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null)
            {
                ValidateFileExists(settings.ModelPath, "ONNXモデルファイル");
            }
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            if (Children == null || !Children.Any())
            {
                throw new Exception("If内に要素がありません。");
            }

            YoloWin.Init(settings.ModelPath, 640, true);

            // AI検出は即座に実行し、ループやタイムアウトは行わない
            var det = YoloWin.DetectFromWindowTitle(settings.WindowTitle, (float)settings.ConfThreshold, (float)settings.IoUThreshold).Detections;

            // 指定クラスIDが検出されなかった場合に子コマンド実行
            var targetDetections = det.Where(d => d.ClassId == settings.ClassID).ToList();

            if (targetDetections.Count == 0)
            {
                LogMessage($"クラスID {settings.ClassID} の画像が見つかりませんでした。");
                return true;
            }

            LogMessage($"クラスID {settings.ClassID} の画像が見つかりました。");
            return false;
        }
    }

    /// <summary>
    /// 変数条件確認If文
    /// </summary>
    public class IfVariableCommand : IfCommand, IIfVariableCommand
    {
        public new IIfVariableCommandSettings Settings => (IIfVariableCommandSettings)base.Settings!;

        public IfVariableCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "変数条件確認";
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            if (Children == null || !Children.Any())
            {
                throw new Exception("If内に要素がありません。");
            }

            var lhs = VariableStore.Get(settings.Name) ?? string.Empty;
            var rhs = settings.Value ?? string.Empty;

            bool result = Evaluate(lhs, rhs, settings.Operator);
            LogMessage($"IfVariable: {settings.Name}({lhs}) {settings.Operator} {rhs} => {result}");

            return result;
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

    // 終了コマンド類
    public class IfEndCommand : BaseCommand
    {
        public IfEndCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "If終了";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            ResetChildrenProgress();
            return Task.FromResult(true);
        }
    }

    public class LoopEndCommand : BaseCommand
    {
        public LoopEndCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "ループ終了";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            ResetChildrenProgress();
            return Task.FromResult(true);
        }
    }

    public class LoopBreakCommand : BaseCommand, ILoopBreakCommand
    {
        public LoopBreakCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "ループ中断";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            LogMessage("ループ中断を実行します。");
            
            // LoopBreakExceptionを投げて最も内側のループのみを中断
            throw new LoopBreakException("ループ中断コマンドが実行されました");
        }
    }

    // その他のコマンド
    public class ExecuteCommand : BaseCommand, IExecuteCommand
    {
        public new IExecuteCommandSettings Settings => (IExecuteCommandSettings)base.Settings!;

        public ExecuteCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "プログラム実行";
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null)
            {
                ValidateFileExists(settings.ProgramPath, "実行ファイル");
                if (!string.IsNullOrEmpty(settings.WorkingDirectory))
                {
                    ValidateDirectoryExists(settings.WorkingDirectory, "ワーキングディレクトリ");
                }
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = settings.ProgramPath,
                    Arguments = settings.Arguments,
                    WorkingDirectory = settings.WorkingDirectory,
                    UseShellExecute = true,
                };
                await Task.Run(() =>
                {
                    Process.Start(startInfo);
                    LogMessage($"プログラムを実行しました。");
                });
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"プログラムの実行に失敗しました: {ex.Message}");
                return false;
            }
        }
    }

    public class SetVariableCommand : BaseCommand, ISetVariableCommand
    {
        public new ISetVariableCommandSettings Settings => (ISetVariableCommandSettings)base.Settings!;

        public SetVariableCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "変数設定";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return Task.FromResult(false);

            VariableStore.Set(settings.Name, settings.Value);
            LogMessage($"変数を設定しました。{settings.Name} = \"{settings.Value}\"");
            return Task.FromResult(true);
        }
    }

    public class SetVariableAICommand : BaseCommand, ISetVariableAICommand
    {
        public new ISetVariableAICommandSettings Settings => (ISetVariableAICommandSettings)base.Settings!;

        public SetVariableAICommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "AI変数設定";
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null)
            {
                ValidateFileExists(settings.ModelPath, "ONNXモデルファイル");
            }
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return Task.FromResult(false);

            YoloWin.Init(settings.ModelPath, 640, true);

            var det = YoloWin.DetectFromWindowTitle(settings.WindowTitle, (float)settings.ConfThreshold, (float)settings.IoUThreshold).Detections;

            if (det.Count == 0)
            {
                VariableStore.Set(settings.Name, "-1");
                LogMessage($"画像が見つかりませんでした。{settings.Name}に-1をセットしました。");
            }
            else
            {
                switch (settings.AIDetectMode)
                {
                    case "Class":
                        // 最高スコアのものをセット
                        var best = det.OrderByDescending(d => d.Score).FirstOrDefault();
                        VariableStore.Set(settings.Name, best.ClassId.ToString());
                        LogMessage($"画像が見つかりました。{settings.Name}に{best.ClassId}をセットしました。");
                        break;
                    case "Count":
                        // 検出された数をセット
                        VariableStore.Set(settings.Name, det.Count.ToString());
                        LogMessage($"画像が{det.Count}個見つかりました。{settings.Name}に{det.Count}をセットしました。");
                        break;
                    default:
                        throw new Exception($"不明なモードです: {settings.AIDetectMode}");
                }
            }

            return Task.FromResult(true);
        }
    }

    public class ScreenshotCommand : BaseCommand, IScreenshotCommand
    {
        public new IScreenshotCommandSettings Settings => (IScreenshotCommandSettings)base.Settings!;

        public ScreenshotCommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "スクリーンショット";
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null && !string.IsNullOrEmpty(settings.SaveDirectory))
            {
                ValidateSaveDirectoryParentExists(settings.SaveDirectory, "保存先ディレクトリ");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            try
            {
                var dir = string.IsNullOrWhiteSpace(settings.SaveDirectory)
                    ? Path.Combine(Environment.CurrentDirectory, "Screenshots")
                    : settings.SaveDirectory;

                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var file = $"{DateTime.Now:yyyyMMdd_HHmmssfff}.png";
                var fullPath = Path.Combine(dir, file);

                using var mat = (string.IsNullOrEmpty(settings.WindowTitle) && string.IsNullOrEmpty(settings.WindowClassName))
                    ? ScreenCaptureHelper.CaptureScreen()
                    : ScreenCaptureHelper.CaptureWindow(settings.WindowTitle, settings.WindowClassName);

                if (cancellationToken.IsCancellationRequested) return false;

                ScreenCaptureHelper.SaveCapture(mat, fullPath);

                LogMessage($"スクリーンショットを保存しました: {fullPath}");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"スクリーンショットの保存に失敗しました: {ex.Message}");
                return false;
            }
        }
    }

    public class ClickImageAICommand : BaseCommand, IClickImageAICommand
    {
        public new IClickImageAICommandSettings Settings => (IClickImageAICommandSettings)base.Settings!;

        public ClickImageAICommand(ICommand? parent = null, object? settings = null) : base(parent, settings)
        {
            Description = "AI画像クリック";
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null)
            {
                ValidateFileExists(settings.ModelPath, "ONNXモデルファイル");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            YoloWin.Init(settings.ModelPath, 640, true);

            // AI検出を実行
            var det = YoloWin.DetectFromWindowTitle(settings.WindowTitle, (float)settings.ConfThreshold, (float)settings.IoUThreshold).Detections;

            // 指定クラスIDが検出された場合にクリック
            var targetDetections = det.Where(d => d.ClassId == settings.ClassID).ToList();

            if (targetDetections.Count > 0)
            {
                // 最も信頼度の高い検出結果を選択
                var best = targetDetections.OrderByDescending(d => d.Score).First();

                // 検出領域の中心座標を計算
                int centerX = (int)(best.Rect.X + best.Rect.Width / 2);
                int centerY = (int)(best.Rect.Y + best.Rect.Height / 2);

                // マウスクリックを実行（非同期）
                switch (settings.Button)
                {
                    case System.Windows.Input.MouseButton.Left:
                        await MouseHelper.Input.ClickAsync(centerX, centerY, settings.WindowTitle, settings.WindowClassName);
                        break;
                    case System.Windows.Input.MouseButton.Right:
                        await MouseHelper.Input.RightClickAsync(centerX, centerY, settings.WindowTitle, settings.WindowClassName);
                        break;
                    case System.Windows.Input.MouseButton.Middle:
                        await MouseHelper.Input.MiddleClickAsync(centerX, centerY, settings.WindowTitle, settings.WindowClassName);
                        break;
                    default:
                        throw new Exception("マウスボタンが不正です。");
                }

                LogMessage($"AI画像クリックが完了しました。({centerX}, {centerY}) ClassId: {best.ClassId}, Score: {best.Score:F2}");
                return true;
            }

            if (cancellationToken.IsCancellationRequested)
                return false;

            LogMessage($"クラスID {settings.ClassID} の画像が見つかりませんでした。");
            return false;
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
}
