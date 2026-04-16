using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using ICommand = AutoTool.Commands.Interface.ICommand;

namespace AutoTool.Commands.Commands;

/// <summary>
/// Command execution context implementation
/// </summary>
public class CommandExecutionContext : ICommandExecutionContext
{
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
        ICommandEventBus? commandEventBus = null)
    {
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
    
    public Task ClickAsync(int x, int y, MouseButton button, string? windowTitle = null, string? windowClassName = null)
    {
        return _mouseInput.ClickAsync(x, y, button, windowTitle, windowClassName);
    }
    
    public Task SendHotkeyAsync(Key key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null)
    {
        return _keyboardInput.SendKeyAsync(key, ctrl, alt, shift, windowTitle, windowClassName);
    }
    
    public Task ExecuteProgramAsync(string programPath, string? arguments, string? workingDirectory, bool waitForExit, CancellationToken cancellationToken)
    {
        if (_processLauncher == null)
            throw new InvalidOperationException("プログラム実行サービスが利用できません。");

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

        return _processLauncher.StartAsync(programPath, arguments, workingDirectory, waitForExit, cancellationToken);
    }
    
    public Task TakeScreenshotAsync(string filePath, string? windowTitle, string? windowClassName, CancellationToken cancellationToken)
    {
        if (_screenCapturer == null)
            throw new InvalidOperationException("画面キャプチャサービスが利用できません。");
        return _screenCapturer.CaptureToFileAsync(filePath, windowTitle, windowClassName, cancellationToken);
    }
    
    public Task<MatchPoint?> SearchImageAsync(string imagePath, double threshold, Color? searchColor, string? windowTitle, string? windowClassName, CancellationToken cancellationToken)
    {
        if (_imageMatcher == null)
            throw new InvalidOperationException("画像検索サービスが利用できません。");

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
                    $"検索画像が見つかりません: {imagePath}"));
        }

        return _imageMatcher.SearchImageAsync(imagePath, cancellationToken, threshold, searchColor, windowTitle, windowClassName);
    }
    
    public void InitializeAIModel(string modelPath, int inputSize = 640, bool useGpu = true)
    {
        if (_objectDetector == null)
            throw new InvalidOperationException("AI検出サービスが利用できません。");

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ModelPathRequired,
                    nameof(modelPath),
                    "モデルパスは必須です。"));
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
        if (_objectDetector == null)
            throw new InvalidOperationException("AI検出サービスが利用できません。");
        return _objectDetector.Detect(windowTitle, confThreshold, iouThreshold);
    }

    public Task<OcrExtractionResult> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken)
    {
        if (_ocrEngine == null)
            throw new InvalidOperationException("OCRサービスが利用できません。");

        if (!string.IsNullOrWhiteSpace(request.TessdataPath))
        {
            if (!Directory.Exists(request.TessdataPath))
            {
                throw new CommandSettingsValidationException(
                    new CommandValidationIssue(
                        CommandValidationErrorCodes.TessdataPathNotFound,
                        nameof(request.TessdataPath),
                        $"フォルダが見つかりません: {request.TessdataPath}"));
            }

            var hasTrainedData = Directory.EnumerateFiles(request.TessdataPath, "*.traineddata", SearchOption.TopDirectoryOnly).Any();
            if (!hasTrainedData)
            {
                throw new CommandSettingsValidationException(
                    new CommandValidationIssue(
                        CommandValidationErrorCodes.TessdataDataMissing,
                        nameof(request.TessdataPath),
                        "*.traineddata が見つかりません。tessdata フォルダを選択してください。"));
            }
        }

        return _ocrEngine.ExtractTextAsync(request, cancellationToken);
    }

    private sealed class NullCommandEventBus : ICommandEventBus
    {
        public static NullCommandEventBus Instance { get; } = new();

        public event EventHandler<CommandEventArgs>? Started { add { } remove { } }
        public event EventHandler<CommandEventArgs>? Finished { add { } remove { } }
        public event EventHandler<CommandLogEventArgs>? Doing { add { } remove { } }
        public event EventHandler<CommandProgressEventArgs>? ProgressUpdated { add { } remove { } }

        public void PublishStarted(ICommand command) { }
        public void PublishFinished(ICommand command) { }
        public void PublishDoing(ICommand command, string detail) { }
        public void PublishProgress(ICommand command, int progress) { }
    }
}





