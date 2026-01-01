using MacroPanels.Command.Interface;
using MacroPanels.Command.Services;
using MacroPanels.Model.List.Interface;

namespace MacroPanels.Command.Commands;

/// <summary>
/// Simple command that delegates execution to ICommandListItem.ExecuteAsync
/// Used for commands marked with [SimpleCommandBinding]
/// </summary>
public class SimpleCommand : BaseCommand
{
    private readonly ICommandListItem _item;
    private readonly IVariableStore _variableStore;
    private readonly IPathService _pathService;
    private readonly IMouseService _mouseService;
    private readonly IKeyboardService _keyboardService;
    private readonly IProcessService? _processService;
    private readonly IScreenCaptureService? _screenCaptureService;
    private readonly IImageSearchService? _imageSearchService;
    private readonly IAIDetectionService? _aiDetectionService;

    public SimpleCommand(
        ICommand? parent, 
        ICommandSettings settings,
        ICommandListItem item,
        IVariableStore variableStore,
        IPathService pathService,
        IMouseService mouseService,
        IKeyboardService keyboardService,
        IProcessService? processService = null,
        IScreenCaptureService? screenCaptureService = null,
        IImageSearchService? imageSearchService = null,
        IAIDetectionService? aiDetectionService = null)
        : base(parent, settings)
    {
        _item = item ?? throw new ArgumentNullException(nameof(item));
        _variableStore = variableStore ?? throw new ArgumentNullException(nameof(variableStore));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _mouseService = mouseService ?? throw new ArgumentNullException(nameof(mouseService));
        _keyboardService = keyboardService ?? throw new ArgumentNullException(nameof(keyboardService));
        _processService = processService;
        _screenCaptureService = screenCaptureService;
        _imageSearchService = imageSearchService;
        _aiDetectionService = aiDetectionService;
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        var context = new CommandExecutionContext(
            this,
            _variableStore,
            _pathService,
            _mouseService,
            _keyboardService,
            _processService,
            _screenCaptureService,
            _imageSearchService,
            _aiDetectionService);
        
        return await _item.ExecuteAsync(context, cancellationToken);
    }
}
