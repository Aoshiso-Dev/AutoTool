using MacroPanels.Command.Services;
using System.Windows.Input;
using System.Windows.Media;

namespace MacroPanels.Command.Interface;

/// <summary>
/// Command execution context - provides services to commands during execution
/// </summary>
public interface ICommandExecutionContext
{
    /// <summary>
    /// Report progress (0-100)
    /// </summary>
    void ReportProgress(int progress);
    
    /// <summary>
    /// Log a message
    /// </summary>
    void Log(string message);
    
    /// <summary>
    /// Get a variable value
    /// </summary>
    string? GetVariable(string name);
    
    /// <summary>
    /// Set a variable value
    /// </summary>
    void SetVariable(string name, string value);
    
    /// <summary>
    /// Convert relative path to absolute path
    /// </summary>
    string ToAbsolutePath(string relativePath);
    
    /// <summary>
    /// Mouse click service
    /// </summary>
    Task ClickAsync(int x, int y, MouseButton button, string? windowTitle = null, string? windowClassName = null);
    
    /// <summary>
    /// Send hotkey
    /// </summary>
    Task SendHotkeyAsync(Key key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null);
    
    /// <summary>
    /// Execute a program
    /// </summary>
    Task ExecuteProgramAsync(string programPath, string? arguments, string? workingDirectory, bool waitForExit, CancellationToken cancellationToken);
    
    /// <summary>
    /// Take a screenshot
    /// </summary>
    Task TakeScreenshotAsync(string filePath, string? windowTitle, string? windowClassName, CancellationToken cancellationToken);
    
    /// <summary>
    /// Search for an image on screen
    /// </summary>
    Task<OpenCvSharp.Point?> SearchImageAsync(string imagePath, double threshold, Color? searchColor, string? windowTitle, string? windowClassName, CancellationToken cancellationToken);
    
    /// <summary>
    /// Initialize AI detection model
    /// </summary>
    void InitializeAIModel(string modelPath, int inputSize = 640, bool useGpu = true);
    
    /// <summary>
    /// Detect objects using AI
    /// </summary>
    IReadOnlyList<DetectionResult> DetectAI(string? windowTitle, float confThreshold, float iouThreshold);
}
