using AutoTool.Command.Definition;
using AutoTool.Message;
using AutoTool.Services;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Command.Base
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
    /// コマンド実行コンテキスト
    /// </summary>
    public class CommandExecutionContext
    {
        public CancellationToken CancellationToken { get; }
        public IServiceProvider? ServiceProvider { get; }
        public Dictionary<string, object> Properties { get; } = new();

        public CommandExecutionContext(CancellationToken cancellationToken, IServiceProvider? serviceProvider = null)
        {
            CancellationToken = cancellationToken;
            ServiceProvider = serviceProvider;
        }
    }

    /// <summary>
    /// コマンド実行統計
    /// </summary>
    public class CommandExecutionStats
    {
        public int TotalCommands { get; set; }
        public int ExecutedCommands { get; set; }
        public int SuccessfulCommands { get; set; }
        public int FailedCommands { get; set; }
        public int SkippedCommands { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        public double SuccessRate => TotalCommands > 0 ? (double)SuccessfulCommands / TotalCommands * 100 : 0;
        public bool IsCompleted => EndTime.HasValue;
    }

    /// <summary>
    /// コマンドファクトリー（DI対応）
    /// </summary>
    public static class CommandFactory
    {
        private static IServiceProvider? _serviceProvider;
        private static ILogger? _logger;

        /// <summary>
        /// サービスプロバイダーを設定
        /// </summary>
        public static void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("CommandFactory");
        }
    }

    /// <summary>
    /// コマンドの基底クラス（DI対応・拡張版）
    /// </summary>
    public abstract class BaseCommand : IAutoToolCommand
    {
        // プライベートフィールド
        private readonly List<IAutoToolCommand> _children = new();
        protected readonly ILogger? _logger;
        protected readonly IServiceProvider? _serviceProvider;
        protected CommandExecutionContext? _executionContext;

        // 追加: マクロファイルのベースパス（相対パス解決用）
        private static string _macroFileBasePath = string.Empty;

        // ICommandインターフェースの実装
        [Browsable(false)]
        public int LineNumber { get; set; }
        [Browsable(false)]
        public bool IsEnabled { get; set; } = true;
        [Browsable(false)]
        public IAutoToolCommand? Parent { get; private set; }
        [Browsable(false)]
        public IEnumerable<IAutoToolCommand> Children => _children;
        [Browsable(false)]
        public int NestLevel { get; set; }
        [Browsable(false)]
        public string Description { get; protected set; } = string.Empty;
        [Browsable(false)]
        public bool IsRunning { get; private set; }
        [Browsable(false)]
        public CommandExecutionStats ExecutionStats { get; } = new();

        // イベント
        public event EventHandler? OnStartCommand;
        public event EventHandler? OnFinishCommand;
        public event EventHandler<string>? OnDoingCommand;
        public event EventHandler<Exception>? OnErrorCommand;

        /// <summary>
        /// マクロファイルのベースパスを設定（全コマンドで共有）
        /// </summary>
        public static void SetMacroFileBasePath(string? macroFilePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(macroFilePath) && File.Exists(macroFilePath))
                {
                    _macroFileBasePath = Path.GetDirectoryName(macroFilePath) ?? string.Empty;
                    System.Diagnostics.Debug.WriteLine($"[BaseCommand] マクロファイルベースパス設定: {_macroFileBasePath}");
                }
                else
                {
                    _macroFileBasePath = string.Empty;
                    System.Diagnostics.Debug.WriteLine($"[BaseCommand] マクロファイルベースパスクリア");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BaseCommand] マクロファイルベースパス設定エラー: {ex.Message}");
                _macroFileBasePath = string.Empty;
            }
        }

        /// <summary>
        /// 相対パスまたは絶対パスを解決して、実際のファイルパスを返す
        /// </summary>
        protected string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            try
            {
                // 既に絶対パスの場合
                if (Path.IsPathRooted(path))
                {
                    return path;
                }

                // 相対パスの場合、マクロファイルのベースパスから解決
                if (!string.IsNullOrEmpty(_macroFileBasePath))
                {
                    var resolvedPath = Path.Combine(_macroFileBasePath, path);
                    resolvedPath = Path.GetFullPath(resolvedPath); // 正規化

                    // 解決されたパスにファイルが存在するか確認
                    if (File.Exists(resolvedPath) || Directory.Exists(resolvedPath))
                    {
                        _logger?.LogTrace("相対パス解決成功: {RelativePath} -> {AbsolutePath}", path, resolvedPath);
                        return resolvedPath;
                    }
                }

                // カレントディレクトリからの相対パスとして試行
                var currentDirPath = Path.GetFullPath(path);
                if (File.Exists(currentDirPath) || Directory.Exists(currentDirPath))
                {
                    _logger?.LogTrace("カレントディレクトリから解決: {RelativePath} -> {AbsolutePath}", path, currentDirPath);
                    return currentDirPath;
                }

                _logger?.LogTrace("パス解決失敗、元のパスを返す: {Path}", path);
                return path;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "パス解決中にエラー: {Path}", path);
                return path;
            }
        }

        protected BaseCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
        {
            Parent = parent;
            NestLevel = parent?.NestLevel + 1 ?? 0;
            _serviceProvider = serviceProvider;
            _logger = serviceProvider?.GetService<ILogger<BaseCommand>>();

            // メッセージング設定
            OnStartCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new StartCommandMessage(this));
            OnDoingCommand += (sender, log) => WeakReferenceMessenger.Default.Send(new DoingCommandMessage(this, log ?? ""));
            OnFinishCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
            OnErrorCommand += (sender, ex) => WeakReferenceMessenger.Default.Send(new CommandErrorMessage(this, ex));
        }

        public virtual void AddChild(IAutoToolCommand child)
        {
            _children.Add(child);
        }

        public virtual void RemoveChild(IAutoToolCommand child)
        {
            _children.Remove(child);
        }

        public virtual IEnumerable<IAutoToolCommand> GetChildren()
        {
            return _children;
        }

        /// <summary>
        /// 実行コンテキストを設定
        /// </summary>
        public virtual void SetExecutionContext(CommandExecutionContext context)
        {
            _executionContext = context;
            foreach (var child in _children)
            {
                if (child is BaseCommand baseChild)
                {
                    baseChild.SetExecutionContext(context);
                }
            }
        }

        /// <summary>
        /// 設定の検証（派生クラスでオーバーライド）
        /// </summary>
        protected virtual void ValidateSettings()
        {
            // 基底クラスでは何もしない
        }

        /// <summary>
        /// ファイルパスの有効性を検証
        /// </summary>
        protected virtual void ValidateFiles()
        {
            // 基底クラスでは何もしない（派生クラスでオーバーライド）
        }

        /// <summary>
        /// ファイル存在チェック（エラー時に例外を投げる）- 相対パス対応版
        /// </summary>
        protected void ValidateFileExists(string filePath, string fileDescription)
        {
            if (string.IsNullOrEmpty(filePath))
                return; // 空の場合はチェックしない

            var resolvedPath = ResolvePath(filePath);
            
            _logger?.LogDebug("[ValidateFileExists] ファイル検証: 元パス={OriginalPath}, 解決パス={ResolvedPath}", filePath, resolvedPath);

            if (!File.Exists(resolvedPath))
            {
                var errorMessage = $"{fileDescription}が見つかりません: {filePath}";
                if (filePath != resolvedPath)
                {
                    errorMessage += $"\n解決されたパス: {resolvedPath}";
                }
                errorMessage += $"\nマクロファイルベースパス: {_macroFileBasePath}";
                
                _logger?.LogError("[ValidateFileExists] ファイル不存在: {ErrorMessage}", errorMessage);
                throw new FileNotFoundException(errorMessage);
            }
            
            _logger?.LogDebug("[ValidateFileExists] ファイル存在確認成功: {ResolvedPath}", resolvedPath);
        }

        /// <summary>
        /// ディレクトリ存在チェック（エラー時に例外を投げる）- 相対パス対応版
        /// </summary>
        protected void ValidateDirectoryExists(string directoryPath, string directoryDescription)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return; // 空の場合はチェックしない

            var resolvedPath = ResolvePath(directoryPath);
            
            _logger?.LogDebug("[ValidateDirectoryExists] ディレクトリ検証: 元パス={OriginalPath}, 解決パス={ResolvedPath}", directoryPath, resolvedPath);

            if (!Directory.Exists(resolvedPath))
            {
                var errorMessage = $"{directoryDescription}が見つかりません: {directoryPath}";
                if (directoryPath != resolvedPath)
                {
                    errorMessage += $"\n解決されたパス: {resolvedPath}";
                }
                errorMessage += $"\nマクロファイルベースパス: {_macroFileBasePath}";
                
                _logger?.LogError("[ValidateDirectoryExists] ディレクトリ存在しません: {ErrorMessage}", errorMessage);
                throw new DirectoryNotFoundException(errorMessage);
            }
            
            _logger?.LogDebug("[ValidateDirectoryExists] ディレクトリ存在確認成功: {ResolvedPath}", resolvedPath);
        }

        /// <summary>
        /// 保存先ディレクトリの親フォルダ存在チェック - 相対パス対応版
        /// </summary>
        protected void ValidateSaveDirectoryParentExists(string directoryPath, string directoryDescription)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return; // 空の場合はチェックしない

            var resolvedPath = ResolvePath(directoryPath);

            // ディレクトリが既に存在する場合はOK
            if (Directory.Exists(resolvedPath))
                return;

            // 親ディレクトリが存在するかチェック
            var parentDir = Path.GetDirectoryName(resolvedPath);
            if (string.IsNullOrEmpty(parentDir) || !Directory.Exists(parentDir))
            {
                var errorMessage = $"{directoryDescription}の親フォルダが見つかりません: {directoryPath}";
                if (directoryPath != resolvedPath)
                {
                    errorMessage += $"\n解決されたパス: {resolvedPath}";
                }
                errorMessage += $"\n親フォルダ: {parentDir ?? "不明"}";
                errorMessage += $"\nマクロファイルベースパス: {_macroFileBasePath}";
                
                throw new DirectoryNotFoundException(errorMessage);
            }
        }

        /// <summary>
        /// コマンドを実行
        /// </summary>
        public virtual async Task<bool> Execute(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger?.LogDebug("[Execute] 実行開始: {Description} (Line: {LineNumber}, Type: {Type})", Description, LineNumber, GetType().Name);

                // 実行前検証を実行
                _logger?.LogDebug("[Execute] 実行前検証開始: {Description}", Description);
                ValidateSettings();
                ValidateFiles();
                _logger?.LogDebug("[Execute] 実行前検証完了: {Description}", Description);

                // ここでIsRunningを設定（CanExecuteチェック後）
                IsRunning = true;

                // コマンド実行開始メッセージを送信
                _logger?.LogDebug("[Execute] コマンド実行開始メッセージ送信: {Description} (Line: {LineNumber})", Description, LineNumber);
                WeakReferenceMessenger.Default.Send(new StartCommandMessage(this));

                // 実際のコマンド実行
                _logger?.LogDebug("[Execute] DoExecuteAsync開始: {Description}", Description);
                var result = await DoExecuteAsync(cancellationToken);
                _logger?.LogDebug("[Execute] DoExecuteAsync完了: {Description} - Result: {Result}", Description, result);

                ExecutionStats.ExecutedCommands++;
                if (result)
                {
                    ExecutionStats.SuccessfulCommands++;
                }
                else
                {
                    ExecutionStats.FailedCommands++;
                }

                // コマンド実行完了メッセージを送信
                _logger?.LogDebug("[Execute] コマンド完了メッセージ送信: {Description} (Line: {LineNumber})", Description, LineNumber);
                WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("コマンドがキャンセルされました");
                WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[Execute] コマンド実行中にエラーが発生しました: {Description}", Description);
                ExecutionStats.ExecutedCommands++;
                ExecutionStats.FailedCommands++;

                // エラーハンドラを呼び出して、UI側に CommandErrorMessage を送出する
                var errorHandler = OnErrorCommand;

                _logger?.LogDebug("[Execute] CommandErrorMessage を送信前: Line={Line}, CommandType={Type}, Exception={ExceptionType}",
                    LineNumber, GetType().Name, ex.GetType().Name);

                if (errorHandler != null)
                {
                    try
                    {
                        // 単一呼び出しに修正
                        errorHandler.Invoke(this, ex);
                    }
                    catch (Exception handlerEx)
                    {
                        _logger?.LogWarning(handlerEx, "[Execute] OnErrorCommand 呼び出し中に例外が発生しました");

                        // フォールバックでメッセージを直接送信
                        try
                        {
                            WeakReferenceMessenger.Default.Send(new CommandErrorMessage(this, ex));
                        }
                        catch (Exception sendEx)
                        {
                            _logger?.LogWarning(sendEx, "[Execute] CommandErrorMessage 直接送信中に例外が発生しました");
                        }
                    }
                }
                else
                {
                    // ハンドラが存在しない場合はメッセージを直接送信
                    try
                    {
                        WeakReferenceMessenger.Default.Send(new CommandErrorMessage(this, ex));
                    }
                    catch (Exception sendEx)
                    {
                        _logger?.LogWarning(sendEx, "[Execute] CommandErrorMessage 直接送信中に例外が発生しました");
                    }
                }

                // 完了メッセージは必ず送る（UIの状態を確実にクリアするため）
                try
                {
                    WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
                }
                catch (Exception sendEx)
                {
                    _logger?.LogWarning(sendEx, "[Execute] FinishCommandMessage 送信中に例外が発生しました");
                }

                // 例外は上位へ再送出（MacroExecutionService 等で捕捉される）
                throw;
            }
            finally
            {
                stopwatch.Stop();
                ExecutionStats.TotalExecutionTime = ExecutionStats.TotalExecutionTime.Add(stopwatch.Elapsed);
                IsRunning = false;
                _logger?.LogDebug("[Execute] 実行終了: {Description} (Line: {LineNumber})", Description, LineNumber);
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

                // 実行コンテキストを設定
                if (child is BaseCommand baseChild && _executionContext != null)
                {
                    baseChild.SetExecutionContext(_executionContext);
                }

                // 子コマンドのLineNumberは既に設定済みなので、ここでは変更しない
                // （MacroFactoryでペア再構築時に正しく設定されている）
                _logger?.LogDebug("[ExecuteChildrenAsync] 子コマンド実行: {ChildType} (Line: {LineNumber})", 
                    child.GetType().Name, child.LineNumber);

                var result = await child.Execute(cancellationToken);
                if (!result)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 進捗報告（改良版）
        /// </summary>
        protected void ReportProgress(double elapsedMilliseconds, double totalMilliseconds)
        {
            int progress = totalMilliseconds <= 0 ? 100 :
                Math.Max(0, Math.Min(100, (int)Math.Round((elapsedMilliseconds / totalMilliseconds) * 100)));

            _logger?.LogTrace("[ReportProgress] 進捗報告: {Progress}% ({Elapsed}/{Total}ms) - {Description} (Line: {LineNumber})", 
                progress, elapsedMilliseconds, totalMilliseconds, Description, LineNumber);

            // 進捗更新メッセージを送信
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
            _logger?.LogInformation(message);
            
            // DoingCommandMessageを送信してUIに実行中状態を通知
            //WeakReferenceMessenger.Default.Send(new DoingCommandMessage(this, message));
        }

        /// <summary>
        /// サービスを取得
        /// </summary>
        protected T? GetService<T>() where T : class
        {
            return _serviceProvider?.GetService<T>();
        }

        /// <summary>
        /// 必須サービスを取得
        /// </summary>
        protected T GetRequiredService<T>() where T : class
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("ServiceProvider が設定されていません");

            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// 変数ストアから値を取得
        /// </summary>
        protected string GetVariable(string name, string defaultValue = "")
        {
            var variableStore = GetService<IVariableStoreService>();
            return variableStore?.Get(name) ?? defaultValue;
        }

        /// <summary>
        /// 変数ストアに値を設定
        /// </summary>
        protected void SetVariable(string name, string value)
        {
            var variableStore = GetService<IVariableStoreService>();
            variableStore?.Set(name, value);
        }

        /// <summary>
        /// コマンドを複製
        /// </summary>
        public virtual IAutoToolCommand Clone()
        {
            var clonedType = GetType();
            var cloned = Activator.CreateInstance(clonedType, Parent, _serviceProvider) as BaseCommand;
            
            if (cloned != null)
            {
                cloned.LineNumber = LineNumber;
                cloned.IsEnabled = IsEnabled;
                cloned.NestLevel = NestLevel;
                cloned.Description = Description;
                
                // 子コマンドも複製
                foreach (var child in _children)
                {
                    if (child is BaseCommand baseChild)
                    {
                        cloned.AddChild(baseChild.Clone());
                    }
                }
            }
            
            return cloned ?? new NothingCommand(_serviceProvider);
        }
    }

    /// <summary>
    /// If文の基底クラス（DI対応）
    /// </summary>
    public abstract class IfCommand : BaseCommand
    {
        protected IfCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            LogMessage("条件評価を開始します");
            
            var condition = await EvaluateConditionAsync(cancellationToken);
            
            if (condition)
            {
                LogMessage("条件が真のため、子コマンドを実行します");
                return await ExecuteChildrenAsync(cancellationToken);
            }
            else
            {
                LogMessage("条件が偽のため、子コマンドをスキップします");
            }
            
            return true; // 条件が偽でも成功として扱う
        }

        protected abstract Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// If文用の子コマンド実行（LineNumber保持）
        /// </summary>
        protected new async Task<bool> ExecuteChildrenAsync(CancellationToken cancellationToken)
        {
            foreach (var child in Children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 実行コンテキストを設定
                if (child is BaseCommand baseChild && _executionContext != null)
                {
                    baseChild.SetExecutionContext(_executionContext);
                }

                // 子コマンドのLineNumberは既に設定済み
                _logger?.LogDebug("[IfCommand.ExecuteChildrenAsync] 子コマンド実行: {ChildType} (Line: {LineNumber})", 
                    child.GetType().Name, child.LineNumber);

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
    /// ルートコマンド
    /// </summary>
    public class RootCommand : BaseCommand, IRootCommand
    {
        public RootCommand(IServiceProvider? serviceProvider = null) : base(null, serviceProvider)
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
        public NothingCommand(IServiceProvider? serviceProvider = null) : base(null, serviceProvider)
        {
            Description = "何もしない";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}