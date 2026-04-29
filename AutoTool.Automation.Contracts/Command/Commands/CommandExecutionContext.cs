using AutoTool.Commands.Model.Input;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using AutoTool.Commands.Threading;
using System.IO;
using ICommand = AutoTool.Commands.Interface.ICommand;

namespace AutoTool.Commands.Commands;

/// <summary>
/// コマンド実行コンテキストの標準実装です。
/// </summary>
public class CommandExecutionContext : ICommandExecutionContext
{
    private readonly TimeProvider _timeProvider;
    private readonly ICommand _command;
    private readonly IVariableStore _variableStore;
    private readonly IPathResolver _pathResolver;
    private readonly IMouseInput _mouseInput;
    private readonly IKeyboardInput _keyboardInput;
    private readonly IProcessLauncher? _processLauncher;
    private readonly IScreenCapturer? _screenCapturer;
    private readonly IImageMatcher? _imageMatcher;
    private readonly IObjectDetector? _objectDetector;
    private readonly IOcrEngine? _ocrEngine;
    private readonly ICommandEventBus _commandEventBus;
    
    public CommandExecutionContext(
        ICommand command, 
        IVariableStore variableStore, 
        IPathResolver pathResolver,
        IMouseInput mouseInput,
        IKeyboardInput keyboardInput,
        IProcessLauncher? processLauncher = null,
        IScreenCapturer? screenCapturer = null,
        IImageMatcher? imageMatcher = null,
        IObjectDetector? objectDetector = null,
        IOcrEngine? ocrEngine = null,
        ICommandEventBus? commandEventBus = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(variableStore);
        ArgumentNullException.ThrowIfNull(pathResolver);
        ArgumentNullException.ThrowIfNull(mouseInput);
        ArgumentNullException.ThrowIfNull(keyboardInput);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _command = command;
        _variableStore = variableStore;
        _pathResolver = pathResolver;
        _mouseInput = mouseInput;
        _keyboardInput = keyboardInput;
        _processLauncher = processLauncher;
        _screenCapturer = screenCapturer;
        _imageMatcher = imageMatcher;
        _objectDetector = objectDetector;
        _ocrEngine = ocrEngine;
        _commandEventBus = commandEventBus ?? NullCommandEventBus.Instance;
    }

    public DateTimeOffset GetLocalNow() => _timeProvider.GetLocalNow();
    
    public void ReportProgress(int progress)
    {
        var clampedProgress = Math.Clamp(progress, 0, 100);
        _commandEventBus.PublishProgress(_command, clampedProgress);
    }
    
    public void Log(string message)
    {
        _commandEventBus.PublishDoing(_command, message);
    }
    
    public string? GetVariable(string name)
    {
        return _variableStore.Get(name);
    }
    
    public void SetVariable(string name, string value)
    {
        _variableStore.Set(name, value);
    }
    
    public string ToAbsolutePath(string relativePath)
    {
        return _pathResolver.ToAbsolutePath(relativePath);
    }
    
    public Task ClickAsync(int x, int y, CommandMouseButton button, string? windowTitle = null, string? windowClassName = null, int holdDurationMs = 20, string clickInjectionMode = "MouseEvent", bool simulateMouseMove = false, bool restoreCursorPositionAfterClick = false, bool restoreWindowZOrderAfterClick = false)
    {
        return _mouseInput.ClickAsync(x, y, button, windowTitle, windowClassName, holdDurationMs, clickInjectionMode, simulateMouseMove, restoreCursorPositionAfterClick, restoreWindowZOrderAfterClick);
    }
    
    public Task SendHotkeyAsync(CommandKey key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null)
    {
        return _keyboardInput.SendKeyAsync(key, ctrl, alt, shift, windowTitle, windowClassName);
    }
    
    public Task ExecuteProgramAsync(string programPath, string? arguments, string? workingDirectory, bool waitForExit, CancellationToken cancellationToken)
    {
        if (_processLauncher is null)
            throw new InvalidOperationException("プロセス起動サービスが構成されていません。");

        if (string.IsNullOrWhiteSpace(programPath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ProgramPathRequired,
                    nameof(programPath),
                    "実行ファイルのパスは必須です。"));
        }

        if (!File.Exists(programPath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ProgramPathNotFound,
                    nameof(programPath),
                    $"実行ファイルが見つかりません: {programPath}"));
        }

        return waitForExit
            ? ExecuteProgramWithOutputAsync(programPath, arguments, workingDirectory, cancellationToken)
            : _processLauncher.StartAsync(programPath, arguments, workingDirectory, waitForExit, cancellationToken);
    }

    private async Task ExecuteProgramWithOutputAsync(
        string programPath,
        string? arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        if (_processLauncher is null)
        {
            throw new InvalidOperationException("プロセス起動サービスが構成されていません。");
        }

        await foreach (var line in _processLauncher
                           .StartWithOutputAsync(programPath, arguments, workingDirectory)
                           .ConfigureAwaitFalse(cancellationToken))
        {
            _commandEventBus.PublishDoing(
                _command,
                line.Text,
                new ProcessOutputLogPayload(line.IsError, line.Text, line.Timestamp));
        }
    }
    
    public Task TakeScreenshotAsync(string filePath, string? windowTitle, string? windowClassName, CancellationToken cancellationToken)
    {
        if (_screenCapturer is null)
            throw new InvalidOperationException("スクリーンキャプチャサービスが構成されていません。");
        return _screenCapturer.CaptureToFileAsync(filePath, windowTitle, windowClassName, cancellationToken);
    }
    
    public Task<MatchPoint?> SearchImageAsync(string imagePath, double threshold, CommandColor? searchColor, string? windowTitle, string? windowClassName, CancellationToken cancellationToken)
    {
        if (_imageMatcher is null)
            throw new InvalidOperationException("画像検索サービスが構成されていません。");

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ImagePathRequired,
                    nameof(imagePath),
                    "画像パスは必須です。"));
        }

        if (!File.Exists(imagePath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ImagePathNotFound,
                    nameof(imagePath),
                    $"画像ファイルが見つかりません: {imagePath}"));
        }

        return _imageMatcher.SearchImageAsync(imagePath, cancellationToken, threshold, searchColor, windowTitle, windowClassName);
    }
    
    public void InitializeAIModel(string modelPath, int inputSize = 640, bool useGpu = true)
    {
        if (_objectDetector is null)
            throw new InvalidOperationException("AI検出サービスが構成されていません。");

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ModelPathRequired,
                    nameof(modelPath),
                    "モデルファイルのパスは必須です。"));
        }

        if (!File.Exists(modelPath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ModelPathNotFound,
                    nameof(modelPath),
                    $"モデルファイルが見つかりません: {modelPath}"));
        }

        _objectDetector.Initialize(modelPath, inputSize, useGpu);
    }
    
    public IReadOnlyList<DetectionResult> DetectAI(string? windowTitle, float confThreshold, float iouThreshold)
    {
        if (_objectDetector is null)
            throw new InvalidOperationException("AI検出サービスが構成されていません。");
        return _objectDetector.Detect(windowTitle, confThreshold, iouThreshold);
    }

    public int ResolveAiClassId(string modelPath, int fallbackClassId, string? labelName, string? labelsPath)
    {
        if (_objectDetector is null)
        {
            throw new InvalidOperationException("AI検出サービスが構成されていません。");
        }

        if (string.IsNullOrWhiteSpace(labelName))
        {
            return fallbackClassId;
        }

        if (_objectDetector.TryResolveClassId(modelPath, labelName, labelsPath, out var classId))
        {
            return classId;
        }

        throw new CommandSettingsValidationException(
            new CommandValidationIssue(
                CommandValidationErrorCodes.AiLabelNotFound,
                "LabelName",
                $"ラベル '{labelName}' をクラスIDへ解決できません。モデルのmetadataまたはラベルファイルを確認してください。"));
    }

    public Task<OcrExtractionResult> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken)
    {
        if (_ocrEngine is null)
            throw new InvalidOperationException("OCRサービスが構成されていません。");

        if (!string.IsNullOrWhiteSpace(request.TessdataPath))
        {
            if (!Directory.Exists(request.TessdataPath))
            {
                throw new CommandSettingsValidationException(
                    new CommandValidationIssue(
                        CommandValidationErrorCodes.TessdataPathNotFound,
                        nameof(request.TessdataPath),
                        $"tessdata フォルダが見つかりません: {request.TessdataPath}"));
            }

            var hasTrainedData = Directory.EnumerateFiles(request.TessdataPath, "*.traineddata", SearchOption.TopDirectoryOnly).Any();
            if (!hasTrainedData)
            {
                throw new CommandSettingsValidationException(
                    new CommandValidationIssue(
                        CommandValidationErrorCodes.TessdataDataMissing,
                        nameof(request.TessdataPath),
                        "*.traineddata が見つかりません。tessdata フォルダを確認してください。"));
            }
        }

        return _ocrEngine.ExtractTextAsync(request, cancellationToken);
    }

    /// <summary>
    /// イベント通知を行わない無効実装として動作し、イベントバスが不要な場面で安全に置き換えられるようにします。
    /// </summary>

    private sealed class NullCommandEventBus : ICommandEventBus
    {
        public static NullCommandEventBus Instance { get; } = new();

        public event EventHandler<CommandEventArgs>? Started { add { } remove { } }
        public event EventHandler<CommandEventArgs>? Finished { add { } remove { } }
        public event EventHandler<CommandLogEventArgs>? Doing { add { } remove { } }
        public event EventHandler<CommandProgressEventArgs>? ProgressUpdated { add { } remove { } }
        public long DroppedEventCount => 0;
        public int SubscriberCount => 0;

        public void PublishStarted(ICommand command) { }
        public void PublishFinished(ICommand command) { }
        public void PublishDoing(ICommand command, string detail) { }
        public void PublishDoing(ICommand command, string detail, CommandLogPayload payload) { }
        public void PublishProgress(ICommand command, int progress) { }
        public async IAsyncEnumerable<CommandBusEvent> ReadEventsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}








