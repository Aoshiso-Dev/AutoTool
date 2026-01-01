using CommunityToolkit.Mvvm.Messaging;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Message;
using MacroPanels.Command.Services;
using System.Windows.Input;
using System.Windows.Media;
using ICommand = MacroPanels.Command.Interface.ICommand;

namespace MacroPanels.Command.Commands;

/// <summary>
/// Command execution context implementation
/// </summary>
public class CommandExecutionContext : ICommandExecutionContext
{
    private readonly ICommand _command;
    private readonly IVariableStore _variableStore;
    private readonly IPathService _pathService;
    private readonly IMouseService _mouseService;
    private readonly IKeyboardService _keyboardService;
    private readonly IProcessService? _processService;
    private readonly IScreenCaptureService? _screenCaptureService;
    private readonly IImageSearchService? _imageSearchService;
    private readonly IAIDetectionService? _aiDetectionService;
    
    public CommandExecutionContext(
        ICommand command, 
        IVariableStore variableStore, 
        IPathService pathService,
        IMouseService mouseService,
        IKeyboardService keyboardService,
        IProcessService? processService = null,
        IScreenCaptureService? screenCaptureService = null,
        IImageSearchService? imageSearchService = null,
        IAIDetectionService? aiDetectionService = null)
    {
        _command = command;
        _variableStore = variableStore;
        _pathService = pathService;
        _mouseService = mouseService;
        _keyboardService = keyboardService;
        _processService = processService;
        _screenCaptureService = screenCaptureService;
        _imageSearchService = imageSearchService;
        _aiDetectionService = aiDetectionService;
    }
    
    public void ReportProgress(int progress)
    {
        var clampedProgress = Math.Clamp(progress, 0, 100);
        WeakReferenceMessenger.Default.Send(new UpdateProgressMessage(_command, clampedProgress));
    }
    
    public void Log(string message)
    {
        WeakReferenceMessenger.Default.Send(new DoingCommandMessage(_command, message));
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
        return _pathService.ToAbsolutePath(relativePath);
    }
    
    public Task ClickAsync(int x, int y, MouseButton button, string? windowTitle = null, string? windowClassName = null)
    {
        return _mouseService.ClickAsync(x, y, button, windowTitle, windowClassName);
    }
    
    public Task SendHotkeyAsync(Key key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null)
    {
        return _keyboardService.SendKeyAsync(key, ctrl, alt, shift, windowTitle, windowClassName);
    }
    
    public Task ExecuteProgramAsync(string programPath, string? arguments, string? workingDirectory, bool waitForExit, CancellationToken cancellationToken)
    {
        if (_processService == null)
            throw new InvalidOperationException("ProcessService is not available");
        return _processService.StartAsync(programPath, arguments, workingDirectory, waitForExit, cancellationToken);
    }
    
    public Task TakeScreenshotAsync(string filePath, string? windowTitle, string? windowClassName, CancellationToken cancellationToken)
    {
        if (_screenCaptureService == null)
            throw new InvalidOperationException("ScreenCaptureService is not available");
        return _screenCaptureService.CaptureToFileAsync(filePath, windowTitle, windowClassName, cancellationToken);
    }
    
    public Task<OpenCvSharp.Point?> SearchImageAsync(string imagePath, double threshold, Color? searchColor, string? windowTitle, string? windowClassName, CancellationToken cancellationToken)
    {
        if (_imageSearchService == null)
            throw new InvalidOperationException("ImageSearchService is not available");
        return _imageSearchService.SearchImageAsync(imagePath, cancellationToken, threshold, searchColor, windowTitle, windowClassName);
    }
    
    public void InitializeAIModel(string modelPath, int inputSize = 640, bool useGpu = true)
    {
        if (_aiDetectionService == null)
            throw new InvalidOperationException("AIDetectionService is not available");
        _aiDetectionService.Initialize(modelPath, inputSize, useGpu);
    }
    
    public IReadOnlyList<DetectionResult> DetectAI(string? windowTitle, float confThreshold, float iouThreshold)
    {
        if (_aiDetectionService == null)
            throw new InvalidOperationException("AIDetectionService is not available");
        return _aiDetectionService.Detect(windowTitle, confThreshold, iouThreshold);
    }
}
