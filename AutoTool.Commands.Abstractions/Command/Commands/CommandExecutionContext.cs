using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using AutoTool.Commands.Threading;
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
        if (_processLauncher is null)
            throw new InvalidOperationException("・ｽv・ｽ・ｽ・ｽO・ｽ・ｽ・ｽ・ｽ・ｽ・ｽ・ｽs・ｽT・ｽ[・ｽr・ｽX・ｽ・ｽ・ｽ・ｽ・ｽp・ｽﾅゑｿｽ・ｽﾜゑｿｽ・ｽ・ｽB");

        if (string.IsNullOrWhiteSpace(programPath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ProgramPathRequired,
                    nameof(programPath),
                    "・ｽ・ｽ・ｽs・ｽt・ｽ@・ｽC・ｽ・ｽ・ｽﾌパ・ｽX・ｽﾍ必・ｽ{・ｽﾅゑｿｽ・ｽB"));
        }

        if (!File.Exists(programPath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ProgramPathNotFound,
                    nameof(programPath),
                    $"・ｽ・ｽ・ｽs・ｽt・ｽ@・ｽC・ｽ・ｽ・ｽ・ｽ・ｽ・ｽ・ｽﾂゑｿｽ・ｽ・ｽﾜゑｿｽ・ｽ・ｽ: {programPath}"));
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
            throw new InvalidOperationException("・ｽv・ｽ・ｽ・ｽO・ｽ・ｽ・ｽ・ｽ・ｽ・ｽ・ｽs・ｽT・ｽ[・ｽr・ｽX・ｽ・ｽ・ｽ・ｽ・ｽp・ｽﾅゑｿｽ・ｽﾜゑｿｽ・ｽ・ｽB");
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
            throw new InvalidOperationException("・ｽ・ｽﾊキ・ｽ・ｽ・ｽv・ｽ`・ｽ・ｽ・ｽT・ｽ[・ｽr・ｽX・ｽ・ｽ・ｽ・ｽ・ｽp・ｽﾅゑｿｽ・ｽﾜゑｿｽ・ｽ・ｽB");
        return _screenCapturer.CaptureToFileAsync(filePath, windowTitle, windowClassName, cancellationToken);
    }
    
    public Task<MatchPoint?> SearchImageAsync(string imagePath, double threshold, Color? searchColor, string? windowTitle, string? windowClassName, CancellationToken cancellationToken)
    {
        if (_imageMatcher is null)
            throw new InvalidOperationException("・ｽ鞫懶ｿｽ・ｽ・ｽ・ｽ・ｽT・ｽ[・ｽr・ｽX・ｽ・ｽ・ｽ・ｽ・ｽp・ｽﾅゑｿｽ・ｽﾜゑｿｽ・ｽ・ｽB");

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ImagePathRequired,
                    nameof(imagePath),
                    "・ｽ鞫懶ｿｽp・ｽX・ｽﾍ必・ｽ{・ｽﾅゑｿｽ・ｽB"));
        }

        if (!File.Exists(imagePath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ImagePathNotFound,
                    nameof(imagePath),
                    $"・ｽ・ｽ・ｽ・ｽ・ｽ鞫懶ｿｽ・ｽ・ｽ・ｽ・ｽﾂゑｿｽ・ｽ・ｽﾜゑｿｽ・ｽ・ｽ: {imagePath}"));
        }

        return _imageMatcher.SearchImageAsync(imagePath, cancellationToken, threshold, searchColor, windowTitle, windowClassName);
    }
    
    public void InitializeAIModel(string modelPath, int inputSize = 640, bool useGpu = true)
    {
        if (_objectDetector is null)
            throw new InvalidOperationException("AI・ｽ・ｽ・ｽo・ｽT・ｽ[・ｽr・ｽX・ｽ・ｽ・ｽ・ｽ・ｽp・ｽﾅゑｿｽ・ｽﾜゑｿｽ・ｽ・ｽB");

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ModelPathRequired,
                    nameof(modelPath),
                    "・ｽ・ｽ・ｽf・ｽ・ｽ・ｽp・ｽX・ｽﾍ必・ｽ{・ｽﾅゑｿｽ・ｽB"));
        }

        if (!File.Exists(modelPath))
        {
            throw new CommandSettingsValidationException(
                new CommandValidationIssue(
                    CommandValidationErrorCodes.ModelPathNotFound,
                    nameof(modelPath),
                    $"・ｽ・ｽ・ｽf・ｽ・ｽ・ｽt・ｽ@・ｽC・ｽ・ｽ・ｽ・ｽ・ｽ・ｽ・ｽﾂゑｿｽ・ｽ・ｽﾜゑｿｽ・ｽ・ｽ: {modelPath}"));
        }

        _objectDetector.Initialize(modelPath, inputSize, useGpu);
    }
    
    public IReadOnlyList<DetectionResult> DetectAI(string? windowTitle, float confThreshold, float iouThreshold)
    {
        if (_objectDetector is null)
            throw new InvalidOperationException("AI・ｽ・ｽ・ｽo・ｽT・ｽ[・ｽr・ｽX・ｽ・ｽ・ｽ・ｽ・ｽp・ｽﾅゑｿｽ・ｽﾜゑｿｽ・ｽ・ｽB");
        return _objectDetector.Detect(windowTitle, confThreshold, iouThreshold);
    }

    public Task<OcrExtractionResult> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken)
    {
        if (_ocrEngine is null)
            throw new InvalidOperationException("OCR・ｽT・ｽ[・ｽr・ｽX・ｽ・ｽ・ｽ・ｽ・ｽp・ｽﾅゑｿｽ・ｽﾜゑｿｽ・ｽ・ｽB");

        if (!string.IsNullOrWhiteSpace(request.TessdataPath))
        {
            if (!Directory.Exists(request.TessdataPath))
            {
                throw new CommandSettingsValidationException(
                    new CommandValidationIssue(
                        CommandValidationErrorCodes.TessdataPathNotFound,
                        nameof(request.TessdataPath),
                        $"・ｽt・ｽH・ｽ・ｽ・ｽ_・ｽ・ｽ・ｽ・ｽ・ｽﾂゑｿｽ・ｽ・ｽﾜゑｿｽ・ｽ・ｽ: {request.TessdataPath}"));
            }

            var hasTrainedData = Directory.EnumerateFiles(request.TessdataPath, "*.traineddata", SearchOption.TopDirectoryOnly).Any();
            if (!hasTrainedData)
            {
                throw new CommandSettingsValidationException(
                    new CommandValidationIssue(
                        CommandValidationErrorCodes.TessdataDataMissing,
                        nameof(request.TessdataPath),
                        "*.traineddata ・ｽ・ｽ・ｽ・ｽ・ｽﾂゑｿｽ・ｽ・ｽﾜゑｿｽ・ｽ・ｽBtessdata ・ｽt・ｽH・ｽ・ｽ・ｽ_・ｽ・ｽI・ｽ・ｽ・ｽ・ｽﾄゑｿｽ・ｽ・ｽ・ｽ・ｽ・ｽ・ｽ・ｽB"));
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








