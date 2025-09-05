using AutoTool.Command.Class;
using AutoTool.Command.Interface;
using AutoTool.Message;
using CommunityToolkit.Mvvm.Messaging;
using KeyHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MouseHelper;
using OpenCVHelper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using YoloWinLib;

namespace AutoTool.Command.Class
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

        /// <summary>
        /// 設定からコマンドを作成
        /// </summary>
        public static T? CreateCommand<T>(object? settings = null, ICommand? parent = null) where T : BaseCommand
        {
            try
            {
                var commandType = typeof(T);
                var command = Activator.CreateInstance(commandType, parent, settings, _serviceProvider) as T;
                
                if (command != null)
                {
                    _logger?.LogDebug("コマンドを作成しました: {CommandType}", commandType.Name);
                }
                
                return command;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "コマンド作成に失敗しました: {CommandType}", typeof(T).Name);
                return null;
            }
        }

        /// <summary>
        /// 型名からコマンドを作成
        /// </summary>
        public static ICommand? CreateCommandByTypeName(string typeName, object? settings = null, ICommand? parent = null)
        {
            try
            {
                return typeName switch
                {
                    "WaitImage" => CreateCommand<WaitImageCommand>(settings, parent),
                    "ClickImage" => CreateCommand<ClickImageCommand>(settings, parent),
                    "ClickImageAI" => CreateCommand<ClickImageAICommand>(settings, parent),
                    "Hotkey" => CreateCommand<HotkeyCommand>(settings, parent),
                    "Click" => CreateCommand<ClickCommand>(settings, parent),
                    "Wait" => CreateCommand<WaitCommand>(settings, parent),
                    "Loop" => CreateCommand<LoopCommand>(settings, parent),
                    "LoopBreak" => CreateCommand<LoopBreakCommand>(settings, parent),
                    "IfImageExist" => CreateCommand<IfImageExistCommand>(settings, parent),
                    "IfImageNotExist" => CreateCommand<IfImageNotExistCommand>(settings, parent),
                    "IfImageExistAI" => CreateCommand<IfImageExistAICommand>(settings, parent),
                    "IfImageNotExistAI" => CreateCommand<IfImageNotExistAICommand>(settings, parent),
                    "IfVariable" => CreateCommand<IfVariableCommand>(settings, parent),
                    "Execute" => CreateCommand<ExecuteCommand>(settings, parent),
                    "SetVariable" => CreateCommand<SetVariableCommand>(settings, parent),
                    "SetVariableAI" => CreateCommand<SetVariableAICommand>(settings, parent),
                    "Screenshot" => CreateCommand<ScreenshotCommand>(settings, parent),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "型名からのコマンド作成に失敗しました: {TypeName}", typeName);
                return null;
            }
        }
    }

    /// <summary>
    /// コマンド実行コンテキスト
    /// </summary>
    public class CommandExecutionContext
    {
        public CancellationToken CancellationToken { get; }
        public IVariableStore? VariableStore { get; }
        public IServiceProvider? ServiceProvider { get; }
        public Dictionary<string, object> Properties { get; } = new();

        public CommandExecutionContext(CancellationToken cancellationToken, IVariableStore? variableStore = null, IServiceProvider? serviceProvider = null)
        {
            CancellationToken = cancellationToken;
            VariableStore = variableStore;
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
    /// コマンドの基底クラス（DI対応・拡張版）
    /// </summary>
    public abstract class BaseCommand : ICommand
    {
        // プライベートフィールド
        private readonly List<ICommand> _children = new();
        protected readonly ILogger? _logger;
        protected readonly IServiceProvider? _serviceProvider;
        protected CommandExecutionContext? _executionContext;

        // 追加: マクロファイルのベースパス（相対パス解決用）
        private static string _macroFileBasePath = string.Empty;

        // ICommandインターフェースの実装
        public int LineNumber { get; set; }
        public bool IsEnabled { get; set; } = true;
        public ICommand? Parent { get; private set; }
        public IEnumerable<ICommand> Children => _children;
        public int NestLevel { get; set; }
        public object? Settings { get; set; }
        public string Description { get; protected set; } = string.Empty;

        // 実行状態管理
        public bool IsRunning { get; private set; }
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

        protected BaseCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
        {
            Parent = parent;
            Settings = settings;
            NestLevel = parent?.NestLevel + 1 ?? 0;
            _serviceProvider = serviceProvider;
            _logger = serviceProvider?.GetService<ILogger<BaseCommand>>();

            // メッセージング設定
            OnStartCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new StartCommandMessage(this));
            OnDoingCommand += (sender, log) => WeakReferenceMessenger.Default.Send(new DoingCommandMessage(this, log ?? ""));
            OnFinishCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
            OnErrorCommand += (sender, ex) => WeakReferenceMessenger.Default.Send(new CommandErrorMessage(this, ex));
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
        /// コマンドが実行可能かチェック
        /// </summary>
        public virtual bool CanExecute()
        {
            _logger?.LogDebug("[CanExecute] チェック開始: {Description} (Line: {LineNumber})", Description, LineNumber);
            
            if (!IsEnabled) 
            {
                _logger?.LogDebug("[CanExecute] IsEnabled=false: {Description}", Description);
                return false;
            }
            
            // IsRunningのチェックを完全に削除（Execute()内でCanExecute()を呼ぶため）
            
            try
            {
                _logger?.LogDebug("[CanExecute] ValidateFiles開始: {Description}", Description);
                ValidateFiles();
                _logger?.LogDebug("[CanExecute] ValidateFiles成功: {Description}", Description);
                
                _logger?.LogDebug("[CanExecute] ValidateSettings開始: {Description}", Description);
                ValidateSettings();
                _logger?.LogDebug("[CanExecute] ValidateSettings成功: {Description}", Description);
                
                _logger?.LogDebug("[CanExecute] チェック成功: {Description}", Description);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[CanExecute] 検証失敗: {Description} - {Message}", Description, ex.Message);
                return false;
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
                
                _logger?.LogError("[ValidateDirectoryExists] ディレクトリ不存在: {ErrorMessage}", errorMessage);
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

                // 実行前の検証（IsRunning=falseの状態でチェック）
                if (!CanExecute())
                {
                    _logger?.LogWarning("[Execute] CanExecute() が false を返しました: {Description}", Description);
                    return false;
                }

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
                WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
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
            WeakReferenceMessenger.Default.Send(new DoingCommandMessage(this, message));
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
            var variableStore = GetService<global::AutoTool.Services.IVariableStore>();
            return variableStore?.Get(name) ?? defaultValue;
        }

        /// <summary>
        /// 変数ストアに値を設定
        /// </summary>
        protected void SetVariable(string name, string value)
        {
            var variableStore = GetService<global::AutoTool.Services.IVariableStore>();
            variableStore?.Set(name, value);
        }

        /// <summary>
        /// コマンドを複製
        /// </summary>
        public virtual ICommand Clone()
        {
            var clonedType = GetType();
            var cloned = Activator.CreateInstance(clonedType, Parent, Settings, _serviceProvider) as BaseCommand;
            
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
    /// ルートコマンド
    /// </summary>
    public class RootCommand : BaseCommand, IRootCommand
    {
        public RootCommand(IServiceProvider? serviceProvider = null) : base(null, null, serviceProvider)
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
        public NothingCommand(IServiceProvider? serviceProvider = null) : base(null, null, serviceProvider)
        {
            Description = "何もしない";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// 画像待機コマンド（DI対応）
    /// </summary>
    public class WaitImageCommand : BaseCommand, IWaitImageCommand
    {
        public new IWaitImageCommandSettings Settings => (IWaitImageCommandSettings)base.Settings!;

        public WaitImageCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
        {
            Description = "画像待機";
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null && !string.IsNullOrEmpty(settings.ImagePath))
            {
                _logger?.LogDebug("[ValidateFiles] WaitImage ImagePath検証開始: {ImagePath}", settings.ImagePath);
                ValidateFileExists(settings.ImagePath, "画像ファイル");
                _logger?.LogDebug("[ValidateFiles] WaitImage ImagePath検証成功: {ImagePath}", settings.ImagePath);
            }
            else
            {
                _logger?.LogDebug("[ValidateFiles] WaitImage ImagePathが空またはSettingsがnull: Settings={Settings}, ImagePath={ImagePath}", 
                    settings?.ToString() ?? "null", settings?.ImagePath ?? "null");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            // 相対パスを解決して実際の検索に使用
            var resolvedImagePath = ResolvePath(settings.ImagePath);
            _logger?.LogDebug("[DoExecuteAsync] WaitImage 解決されたImagePath: {OriginalPath} -> {ResolvedPath}", settings.ImagePath, resolvedImagePath);

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Timeout)
            {
                var point = await ImageSearchHelper.SearchImage(
                    resolvedImagePath, cancellationToken, settings.Threshold,
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
    /// 画像クリックコマンド（DI対応）
    /// </summary>
    public class ClickImageCommand : BaseCommand, IClickImageCommand
    {
        public new IClickImageCommandSettings Settings => (IClickImageCommandSettings)base.Settings!;

        public ClickImageCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
        {
            Description = "画像クリック";
        }

        // 背景クリック方式名取得
        private static string GetBgMethodName(int method) => method switch
        {
            0 => "SendMessage",
            1 => "PostMessage",
            2 => "AutoDetectChild",
            3 => "TryAll",
            4 => "GameDirectInput",
            5 => "GameFullscreen",
            6 => "GameLowLevel",
            7 => "GameVirtualMouse",
            _ => $"Unknown({method})"
        };

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null && !string.IsNullOrEmpty(settings.ImagePath))
            {
                _logger?.LogDebug("[ValidateFiles] ClickImage ImagePath検証開始: {ImagePath}", settings.ImagePath);
                ValidateFileExists(settings.ImagePath, "画像ファイル");
                _logger?.LogDebug("[ValidateFiles] ClickImage ImagePath検証成功: {ImagePath}", settings.ImagePath);
            }
            else
            {
                _logger?.LogDebug("[ValidateFiles] ClickImage ImagePathが空またはSettingsがnull: Settings={Settings}, ImagePath={ImagePath}", 
                    settings?.ToString() ?? "null", settings?.ImagePath ?? "null");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            // 相対パスを解決して実際の検索に使用
            var resolvedImagePath = ResolvePath(settings.ImagePath);
            _logger?.LogDebug("[DoExecuteAsync] ClickImage 解決されたImagePath: {OriginalPath} -> {ResolvedPath}", settings.ImagePath, resolvedImagePath);

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Timeout)
            {
                var point = await ImageSearchHelper.SearchImage(
                    resolvedImagePath, cancellationToken, settings.Threshold,
                    settings.SearchColor, settings.WindowTitle, settings.WindowClassName);

                if (point != null)
                {
                    await ExecuteMouseClick(point.Value.X, point.Value.Y, settings.Button,
                        settings.WindowTitle, settings.WindowClassName, settings.UseBackgroundClick, settings.BackgroundClickMethod);
                    var extra = settings.UseBackgroundClick ? $"[BG:{GetBgMethodName(settings.BackgroundClickMethod)}]" : string.Empty;
                    LogMessage($"画像をクリックしました。{extra} ({point.Value.X}, {point.Value.Y})");
                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, settings.Timeout);
                await Task.Delay(settings.Interval, cancellationToken);
            }

            LogMessage("画像が見つかりませんでした。");
            return false;
        }

        private async Task ExecuteMouseClick(int x, int y, System.Windows.Input.MouseButton button,
            string windowTitle, string windowClassName, bool useBackgroundClick, int backgroundMethod)
        {
            if (useBackgroundClick)
            {
                var method = (MouseHelper.Input.BackgroundClickMethod)backgroundMethod;
                switch (button)
                {
                    case System.Windows.Input.MouseButton.Left:
                        await MouseHelper.Input.BackgroundClickAsync(x, y, windowTitle, windowClassName, method);
                        break;
                    case System.Windows.Input.MouseButton.Right:
                        await MouseHelper.Input.BackgroundRightClickAsync(x, y, windowTitle, windowClassName, method);
                        break;
                    case System.Windows.Input.MouseButton.Middle:
                        await MouseHelper.Input.BackgroundMiddleClickAsync(x, y, windowTitle, windowClassName, method);
                        break;
                    default:
                        throw new ArgumentException($"サポートされていないマウスボタン: {button}");
                }
            }
            else
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
    }

    /// <summary>
    /// ホットキーコマンド（DI対応）
    /// </summary>
    public class HotkeyCommand : BaseCommand, IHotkeyCommand
    {
        public new IHotkeyCommandSettings Settings => (IHotkeyCommandSettings)base.Settings!;

        public HotkeyCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
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
    /// クリックコマンド（DI対応）
    /// </summary>
    public class ClickCommand : BaseCommand, IClickCommand
    {
        public new IClickCommandSettings Settings => (IClickCommandSettings)base.Settings!;

        public ClickCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
        {
            Description = "クリック";
        }

        private static string GetBgMethodName(int method) => method switch
        {
            0 => "SendMessage",
            1 => "PostMessage",
            2 => "AutoDetectChild",
            3 => "TryAll",
            4 => "GameDirectInput",
            5 => "GameFullscreen",
            6 => "GameLowLevel",
            7 => "GameVirtualMouse",
            _ => $"Unknown({method})"
        };

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            await ExecuteMouseClick(settings.X, settings.Y, settings.Button,
                settings.WindowTitle, settings.WindowClassName, settings.UseBackgroundClick, settings.BackgroundClickMethod);

            var target = string.IsNullOrEmpty(settings.WindowTitle) && string.IsNullOrEmpty(settings.WindowClassName)
                ? "グローバル" : $"{settings.WindowTitle}[{settings.WindowClassName}]";
            var clickType = settings.UseBackgroundClick ? $"バックグラウンドクリック[{GetBgMethodName(settings.BackgroundClickMethod)}]" : "クリック";
            LogMessage($"{clickType}しました。対象: {target} ({settings.X}, {settings.Y})");
            return true;
        }

        private async Task ExecuteMouseClick(int x, int y, System.Windows.Input.MouseButton button,
            string windowTitle, string windowClassName, bool useBackgroundClick, int backgroundMethod)
        {
            if (useBackgroundClick)
            {
                var method = (MouseHelper.Input.BackgroundClickMethod)backgroundMethod;
                switch (button)
                {
                    case System.Windows.Input.MouseButton.Left:
                        await MouseHelper.Input.BackgroundClickAsync(x, y, windowTitle, windowClassName, method);
                        break;
                    case System.Windows.Input.MouseButton.Right:
                        await MouseHelper.Input.BackgroundRightClickAsync(x, y, windowTitle, windowClassName, method);
                        break;
                    case System.Windows.Input.MouseButton.Middle:
                        await MouseHelper.Input.BackgroundMiddleClickAsync(x, y, windowTitle, windowClassName, method);
                        break;
                    default:
                        throw new ArgumentException($"サポートされていないマウスボタン: {button}");
                }
            }
            else
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
    }

    /// <summary>
    /// 待機コマンド（DI対応）
    /// </summary>
    public class WaitCommand : BaseCommand, IWaitCommand
    {
        public new IWaitCommandSettings Settings => (IWaitCommandSettings)base.Settings!;

        public WaitCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
        {
            Description = "待機";
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            var stopwatch = Stopwatch.StartNew();
            var totalWaitMs = settings.Wait;
            int lastReportedProgress = -1;

            LogMessage($"待機開始 ({totalWaitMs}ms)");

            while (stopwatch.ElapsedMilliseconds < totalWaitMs)
            {
                if (cancellationToken.IsCancellationRequested) 
                {
                    LogMessage("待機がキャンセルされました");
                    return false;
                }

                var elapsed = stopwatch.ElapsedMilliseconds;
                var currentProgress = totalWaitMs > 0 ? (int)((elapsed / (double)totalWaitMs) * 100) : 100;
                
                // 進捗が変わった場合のみ報告（頻度を減らすため）
                if (currentProgress != lastReportedProgress)
                {
                    ReportProgress(elapsed, totalWaitMs);
                    lastReportedProgress = currentProgress;
                    
                    // より詳細な状態報告
                    var remaining = totalWaitMs - elapsed;
                    LogMessage($"待機中... {currentProgress}% (残り約{remaining}ms)");
                }

                await Task.Delay(100, cancellationToken); // 100msごとに確認
            }

            LogMessage("待機が完了しました");
            return true;
        }
    }

    /// <summary>
    /// ループコマンド（DI対応）
    /// </summary>
    public class LoopCommand : BaseCommand, ILoopCommand
    {
        public new ILoopCommandSettings Settings => (ILoopCommandSettings)base.Settings!;

        public LoopCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
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
        /// 子コマンドを順次実行（LoopBreakException対応版・LineNumber同期強化）
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

                // 子コマンドのLineNumberは既に設定済みなので、ここでは変更しない
                // （MacroFactoryでペア再構築時に正しく設定されている）
                _logger?.LogDebug("[LoopCommand.ExecuteChildrenAsync] 子コマンド実行: {ChildType} (Line: {LineNumber})", 
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
    /// If文の基底クラス（DI対応）
    /// </summary>
    public abstract class IfCommand : BaseCommand
    {
        protected IfCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
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
    /// 画像存在確認If文（DI対応）
    /// </summary>
    public class IfImageExistCommand : IfCommand, IIfImageExistCommand
    {
        public new IIfImageCommandSettings Settings => (IIfImageCommandSettings)base.Settings!;

        public IfImageExistCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
        {
            Description = "画像存在確認";
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null && !string.IsNullOrEmpty(settings.ImagePath))
            {
                _logger?.LogDebug("[ValidateFiles] IfImageExist ImagePath検証開始: {ImagePath}", settings.ImagePath);
                ValidateFileExists(settings.ImagePath, "画像ファイル");
                _logger?.LogDebug("[ValidateFiles] IfImageExist ImagePath検証成功: {ImagePath}", settings.ImagePath);
            }
            else
            {
                _logger?.LogDebug("[ValidateFiles] IfImageExist ImagePathが空またはSettingsがnull: Settings={Settings}, ImagePath={ImagePath}", 
                    settings?.ToString() ?? "null", settings?.ImagePath ?? "null");
            }
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) 
            {
                LogMessage("設定が無効です");
                return false;
            }

            // 相対パスを解決して実際の検索に使用
            var resolvedImagePath = ResolvePath(settings.ImagePath);
            _logger?.LogDebug("[EvaluateConditionAsync] IfImageExist 解決されたImagePath: {OriginalPath} -> {ResolvedPath}", settings.ImagePath, resolvedImagePath);

            LogMessage($"画像の存在を確認中: {Path.GetFileName(resolvedImagePath)}");

            var point = await ImageSearchHelper.SearchImage(
                resolvedImagePath, cancellationToken, settings.Threshold,
                settings.SearchColor, settings.WindowTitle, settings.WindowClassName);

            if (point != null)
            {
                LogMessage($"画像が見つかりました（条件: 真）: ({point.Value.X}, {point.Value.Y})");
                return true;
            }

            LogMessage("画像が見つかりませんでした（条件: 偽）");
            return false;
        }
    }

    /// <summary>
    /// 画像非存在確認If文（DI対応）
    /// </summary>
    public class IfImageNotExistCommand : IfCommand, IIfImageNotExistCommand
    {
        public new IIfImageCommandSettings Settings => (IIfImageCommandSettings)base.Settings!;

        public IfImageNotExistCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
        {
            Description = "画像非存在確認";
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null && !string.IsNullOrEmpty(settings.ImagePath))
            {
                _logger?.LogDebug("[ValidateFiles] ImagePath検証開始: {ImagePath}", settings.ImagePath);
                ValidateFileExists(settings.ImagePath, "画像ファイル");
                _logger?.LogDebug("[ValidateFiles] ImagePath検証成功: {ImagePath}", settings.ImagePath);
            }
            else
            {
                _logger?.LogDebug("[ValidateFiles] ImagePathが空またはSettingsがnull: Settings={Settings}, ImagePath={ImagePath}", 
                    settings?.ToString() ?? "null", settings?.ImagePath ?? "null");
            }
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) 
            {
                LogMessage("設定が無効です");
                return false;
            }

            // 相対パスを解決して実際の検索に使用
            var resolvedImagePath = ResolvePath(settings.ImagePath);
            _logger?.LogDebug("[EvaluateConditionAsync] IfImageNotExist 解決されたImagePath: {OriginalPath} -> {ResolvedPath}", settings.ImagePath, resolvedImagePath);

            LogMessage($"画像の非存在を確認中: {Path.GetFileName(resolvedImagePath)}");

            var point = await ImageSearchHelper.SearchImage(
                resolvedImagePath, cancellationToken, settings.Threshold,
                settings.SearchColor, settings.WindowTitle, settings.WindowClassName);

            if (point == null)
            {
                LogMessage("画像が見つかりませんでした（条件: 真）");
                return true;
            }

            LogMessage($"画像が見つかりました（条件: 偽）: ({point.Value.X}, {point.Value.Y})");
            return false;
        }
    }

    /// <summary>
    /// AI画像存在確認If文（DI対応）
    /// </summary>
    public class IfImageExistAICommand : IfCommand, IIfImageExistAICommand
    {
        public new IIfImageExistAISettings Settings => (IIfImageExistAISettings)base.Settings!;

        public IfImageExistAICommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
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

            // 相対パスを解決して実際のモデル読み込みに使用
            var resolvedModelPath = ResolvePath(settings.ModelPath);
            _logger?.LogDebug("[EvaluateConditionAsync] IfImageExistAI 解決されたModelPath: {OriginalPath} -> {ResolvedPath}", settings.ModelPath, resolvedModelPath);

            LogMessage($"AI検出を開始中: ClassID {settings.ClassID}");

            YoloWin.Init(resolvedModelPath, 640, true);

            // AI検出は即座に実行し、ループやタイムアウトは行わない
            var det = YoloWin.DetectFromWindowTitle(settings.WindowTitle, (float)settings.ConfThreshold, (float)settings.IoUThreshold).Detections;

            if (det.Count > 0)
            {
                var best = det.OrderByDescending(d => d.Score).FirstOrDefault();

                if (best.ClassId == settings.ClassID)
                {
                    LogMessage($"AI画像が見つかりました（条件: 真）: ({best.Rect.X}, {best.Rect.Y}) ClassId: {best.ClassId}");
                    return true;
                }
            }

            LogMessage("AI画像が見つかりませんでした（条件: 偽）");
            return false;
        }
    }

    /// <summary>
    /// AI画像非存在確認If文（DI対応）
    /// </summary>
    public class IfImageNotExistAICommand : IfCommand, IIfImageNotExistAICommand
    {
        public new IIfImageNotExistAISettings Settings => (IIfImageNotExistAISettings)base.Settings!;

        public IfImageNotExistAICommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
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
            if (settings == null) 
            {
                LogMessage("設定が無効です");
                return false;
            }

            // 相対パスを解決して実際のモデル読み込みに使用
            var resolvedModelPath = ResolvePath(settings.ModelPath);
            _logger?.LogDebug("[EvaluateConditionAsync] IfImageNotExistAI 解決されたModelPath: {OriginalPath} -> {ResolvedPath}", settings.ModelPath, resolvedModelPath);

            LogMessage($"AI非検出を確認中: ClassID {settings.ClassID}");

            YoloWin.Init(resolvedModelPath, 640, true);

            // AI検出は即座に実行し、ループやタイムアウトは行わない
            var det = YoloWin.DetectFromWindowTitle(settings.WindowTitle, (float)settings.ConfThreshold, (float)settings.IoUThreshold).Detections;

            // 指定クラスIDが検出されなかった場合に子コマンド実行
            var targetDetections = det.Where(d => d.ClassId == settings.ClassID).ToList();

            if (targetDetections.Count == 0)
            {
                LogMessage($"AI画像が見つかりませんでした（条件: 真）: ClassID {settings.ClassID}");
                return true;
            }

            LogMessage($"AI画像が見つかりました（条件: 偽）: ClassID {settings.ClassID}");
            return false;
        }
    }

    /// <summary>
    /// 変数条件確認If文（DI対応）
    /// </summary>
    public class IfVariableCommand : IfCommand, IIfVariableCommand
    {
        private readonly IVariableStore? _variableStore;
        public new IIfVariableCommandSettings Settings => (IIfVariableCommandSettings)base.Settings!;

        public IfVariableCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
        {
            Description = "変数条件確認";
            _variableStore = GetService<IVariableStore>();
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) 
            {
                LogMessage("設定が無効です");
                return false;
            }

            var lhs = _variableStore?.Get(settings.Name) ?? string.Empty;
            var rhs = settings.Value ?? string.Empty;

            LogMessage($"変数条件を評価中: {settings.Name}({lhs}) {settings.Operator} {rhs}");

            bool result = Evaluate(lhs, rhs, settings.Operator);
            
            LogMessage($"変数条件の結果: {settings.Name}({lhs}) {settings.Operator} {rhs} => {(result ? "真" : "偽")}");

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

    // 終了コマンド類（DI対応）
    public class IfEndCommand : BaseCommand
    {
        public IfEndCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
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
        public LoopEndCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
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
        public LoopBreakCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
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

    // その他のコマンド（DI対応）
    public class ExecuteCommand : BaseCommand, IExecuteCommand
    {
        public new IExecuteCommandSettings Settings => (IExecuteCommandSettings)base.Settings!;

        public ExecuteCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
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
                // 相対パスを解決して実際の実行に使用
                var resolvedProgramPath = ResolvePath(settings.ProgramPath);
                var resolvedWorkingDirectory = !string.IsNullOrEmpty(settings.WorkingDirectory) ? ResolvePath(settings.WorkingDirectory) : string.Empty;
                
                _logger?.LogDebug("[DoExecuteAsync] Execute 解決されたProgramPath: {OriginalPath} -> {ResolvedPath}", settings.ProgramPath, resolvedProgramPath);
                if (!string.IsNullOrEmpty(settings.WorkingDirectory))
                {
                    _logger?.LogDebug("[DoExecuteAsync] Execute 解決されたWorkingDirectory: {OriginalPath} -> {ResolvedPath}", settings.WorkingDirectory, resolvedWorkingDirectory);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = resolvedProgramPath,
                    Arguments = settings.Arguments,
                    WorkingDirectory = resolvedWorkingDirectory,
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
        private readonly IVariableStore? _variableStore;
        public new ISetVariableCommandSettings Settings => (ISetVariableCommandSettings)base.Settings!;

        public SetVariableCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
        {
            Description = "変数設定";
            _variableStore = GetService<IVariableStore>();
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            _variableStore?.Set(settings.Name, settings.Value);
            LogMessage($"変数を設定しました。{settings.Name} = \"{settings.Value}\"");
            return true;
        }
    }

    public class SetVariableAICommand : BaseCommand, ISetVariableAICommand
    {
        private readonly IVariableStore? _variableStore;
        public new ISetVariableAICommandSettings Settings => (ISetVariableAICommandSettings)base.Settings!;

        public SetVariableAICommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
        {
            Description = "AI変数設定";
            _variableStore = GetService<IVariableStore>();
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

            // 相対パスを解決して実際のモデル読み込みに使用
            var resolvedModelPath = ResolvePath(settings.ModelPath);
            _logger?.LogDebug("[DoExecuteAsync] SetVariableAI 解決されたModelPath: {OriginalPath} -> {ResolvedPath}", settings.ModelPath, resolvedModelPath);

            YoloWin.Init(resolvedModelPath, 640, true);

            var det = YoloWin.DetectFromWindowTitle(settings.WindowTitle, (float)settings.ConfThreshold, (float)settings.IoUThreshold).Detections;

            if (det.Count == 0)
            {
                _variableStore?.Set(settings.Name, "-1");
                LogMessage($"画像が見つかりませんでした。{settings.Name}に-1をセットしました。");
            }
            else
            {
                switch (settings.AIDetectMode)
                {
                    case "Class":
                        // 最高スコアのものをセット
                        var best = det.OrderByDescending(d => d.Score).FirstOrDefault();
                        _variableStore?.Set(settings.Name, best.ClassId.ToString());
                        LogMessage($"画像が見つかりました。{settings.Name}に{best.ClassId}をセットしました。");
                        break;
                    case "Count":
                        // 検出された数をセット
                        _variableStore?.Set(settings.Name, det.Count.ToString());
                        LogMessage($"画像が{det.Count}個見つかりました。{settings.Name}に{det.Count}をセットしました。");
                        break;
                    default:
                        throw new Exception($"不明なモードです: {settings.AIDetectMode}");
                }
            }

            return true;
        }
    }

    public class ScreenshotCommand : BaseCommand, IScreenshotCommand
    {
        public new IScreenshotCommandSettings Settings => (IScreenshotCommandSettings)base.Settings!;

        public ScreenshotCommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
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
                    : ResolvePath(settings.SaveDirectory); // 相対パスを解決

                _logger?.LogDebug("[DoExecuteAsync] Screenshot 解決された保存ディレクトリ: {OriginalPath} -> {ResolvedPath}", settings.SaveDirectory ?? "(empty)", dir);

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

        public ClickImageAICommand(ICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, settings, serviceProvider)
        {
            Description = "AI画像クリック";
        }

        private static string GetBgMethodName(int method) => method switch
        {
            0 => "SendMessage",
            1 => "PostMessage",
            2 => "AutoDetectChild",
            3 => "TryAll",
            4 => "GameDirectInput",
            5 => "GameFullscreen",
            6 => "GameLowLevel",
            7 => "GameVirtualMouse",
            _ => $"Unknown({method})"
        };

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

            // 相対パスを解決して実際のモデル読み込みに使用
            var resolvedModelPath = ResolvePath(settings.ModelPath);
            _logger?.LogDebug("[DoExecuteAsync] ClickImageAI 解決されたModelPath: {OriginalPath} -> {ResolvedPath}", settings.ModelPath, resolvedModelPath);

            YoloWin.Init(resolvedModelPath, 640, true);

            var det = YoloWin.DetectFromWindowTitle(settings.WindowTitle, (float)settings.ConfThreshold, (float)settings.IoUThreshold).Detections;
            var targetDetections = det.Where(d => d.ClassId == settings.ClassID).ToList();

            if (targetDetections.Count > 0)
            {
                var best = targetDetections.OrderByDescending(d => d.Score).First();
                int centerX = (int)(best.Rect.X + best.Rect.Width / 2);
                int centerY = (int)(best.Rect.Y + best.Rect.Height / 2);

                await ExecuteMouseClick(centerX, centerY, settings.Button,
                    settings.WindowTitle, settings.WindowClassName, settings.UseBackgroundClick, settings.BackgroundClickMethod);

                var clickType = settings.UseBackgroundClick ? $"バックグラウンドAI画像クリック[{GetBgMethodName(settings.BackgroundClickMethod)}]" : "AI画像クリック";
                LogMessage($"{clickType}が完了しました。({centerX}, {centerY}) ClassId: {best.ClassId}, Score: {best.Score:F2}");
                return true;
            }

            if (cancellationToken.IsCancellationRequested)
                return false;

            LogMessage($"クラスID {settings.ClassID} の画像が見つかりませんでした。");
            return false;
        }

        private async Task ExecuteMouseClick(int x, int y, System.Windows.Input.MouseButton button,
            string windowTitle, string windowClassName, bool useBackgroundClick, int backgroundMethod)
        {
            if (useBackgroundClick)
            {
                var method = (MouseHelper.Input.BackgroundClickMethod)backgroundMethod;
                switch (button)
                {
                    case System.Windows.Input.MouseButton.Left:
                        await MouseHelper.Input.BackgroundClickAsync(x, y, windowTitle, windowClassName, method);
                        break;
                    case System.Windows.Input.MouseButton.Right:
                        await MouseHelper.Input.BackgroundRightClickAsync(x, y, windowTitle, windowClassName, method);
                        break;
                    case System.Windows.Input.MouseButton.Middle:
                        await MouseHelper.Input.BackgroundMiddleClickAsync(x, y, windowTitle, windowClassName, method);
                        break;
                    default:
                        throw new Exception("マウスボタンが不正です。");
                }
            }
            else
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
                        throw new Exception("マウスボタンが不正です。");
                }
            }
        }
    }

    /// <summary>
    /// 変数ストアのインターフェース（DI対応・拡張版）
    /// </summary>
    namespace AutoTool.Command.Interface
    {
        /// <summary>
        /// 変数ストアのインターフェース
        /// </summary>
        public interface IVariableStore
        {
            void Set(string name, string value);
            string? Get(string name);
            void Clear();
            Dictionary<string, string> GetAll();
        }
    }

    /// <summary>
    /// 変数ストアの実装（DI対応・拡張版）
    /// </summary>
    public class VariableStore : IVariableStore
    {
        private readonly ConcurrentDictionary<string, string> _vars = new(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<VariableStore>? _logger;

        public VariableStore(ILogger<VariableStore>? logger = null)
        {
            _logger = logger;
        }

        public void Set(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            _vars[name] = value ?? string.Empty;
            _logger?.LogDebug("変数設定: {Name} = {Value}", name, value);
            
            // 変数変更をメッセージで通知
            WeakReferenceMessenger.Default.Send(new VariableChangedMessage(name, value));
        }

        public string? Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var result = _vars.TryGetValue(name, out var v) ? v : null;
            _logger?.LogDebug("変数取得: {Name} = {Value}", name, result ?? "null");
            return result;
        }

        public void Clear()
        {
            _vars.Clear();
            _logger?.LogDebug("変数ストアをクリアしました");
            
            // 変数クリアをメッセージで通知
            WeakReferenceMessenger.Default.Send(new VariablesClearedMessage());
        }

        public Dictionary<string, string> GetAll()
        {
            return new Dictionary<string, string>(_vars);
        }
    }
}
