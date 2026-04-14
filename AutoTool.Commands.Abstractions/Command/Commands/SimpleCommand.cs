using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Commands.Commands;

/// <summary>
/// Simple command that delegates execution to ICommandListItem.ExecuteAsync
/// Used for commands marked with [SimpleCommandBinding]
/// </summary>
public class SimpleCommand : BaseCommand
{
    private readonly ICommandListItem _item;
    private readonly IVariableStore _variableStore;
    private readonly IPathResolver _pathResolver;
    private readonly IMouseInput _mouseInput;
    private readonly IKeyboardInput _keyboardInput;
    private readonly IProcessLauncher? _processLauncher;
    private readonly IScreenCapturer? _screenCapturer;
    private readonly IImageMatcher? _imageMatcher;
    private readonly IObjectDetector? _objectDetector;
    private readonly IOcrEngine? _ocrEngine;

    public SimpleCommand(
        ICommand? parent, 
        ICommandSettings settings,
        ICommandListItem item,
        IVariableStore variableStore,
        IPathResolver pathResolver,
        IMouseInput mouseInput,
        IKeyboardInput keyboardInput,
        IProcessLauncher? processLauncher = null,
        IScreenCapturer? screenCapturer = null,
        IImageMatcher? imageMatcher = null,
        IObjectDetector? objectDetector = null,
        IOcrEngine? ocrEngine = null)
        : base(parent, settings)
    {
        _item = item ?? throw new ArgumentNullException(nameof(item));
        _variableStore = variableStore ?? throw new ArgumentNullException(nameof(variableStore));
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        _mouseInput = mouseInput ?? throw new ArgumentNullException(nameof(mouseInput));
        _keyboardInput = keyboardInput ?? throw new ArgumentNullException(nameof(keyboardInput));
        _processLauncher = processLauncher;
        _screenCapturer = screenCapturer;
        _imageMatcher = imageMatcher;
        _objectDetector = objectDetector;
        _ocrEngine = ocrEngine;
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        var context = new CommandExecutionContext(
            this,
            _variableStore,
            _pathResolver,
            _mouseInput,
            _keyboardInput,
            _processLauncher,
            _screenCapturer,
            _imageMatcher,
            _objectDetector,
            _ocrEngine,
            CommandEventBus);
        
        return await _item.ExecuteAsync(context, cancellationToken);
    }
}
