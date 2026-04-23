using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Commands.Commands;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using AutoTool.Plugin.Host.Abstractions;

namespace AutoTool.Plugin.Host.Services;

public sealed class PluginCommand : BaseCommand
{
    private readonly ICommandListItem _item;
    private readonly IPluginCommandDispatcher _dispatcher;
    private readonly IVariableStore _variableStore;
    private readonly IPathResolver _pathResolver;
    private readonly IMouseInput _mouseInput;
    private readonly IKeyboardInput _keyboardInput;
    private readonly IProcessLauncher? _processLauncher;
    private readonly IScreenCapturer? _screenCapturer;
    private readonly IImageMatcher? _imageMatcher;
    private readonly IObjectDetector? _objectDetector;
    private readonly IOcrEngine? _ocrEngine;
    private readonly TimeProvider _timeProvider;

    public PluginCommand(
        ICommand? parent,
        ICommandSettings settings,
        ICommandListItem item,
        IPluginCommandDispatcher dispatcher,
        IVariableStore variableStore,
        IPathResolver pathResolver,
        IMouseInput mouseInput,
        IKeyboardInput keyboardInput,
        IProcessLauncher? processLauncher = null,
        IScreenCapturer? screenCapturer = null,
        IImageMatcher? imageMatcher = null,
        IObjectDetector? objectDetector = null,
        IOcrEngine? ocrEngine = null,
        TimeProvider? timeProvider = null)
        : base(parent, settings)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(variableStore);
        ArgumentNullException.ThrowIfNull(pathResolver);
        ArgumentNullException.ThrowIfNull(mouseInput);
        ArgumentNullException.ThrowIfNull(keyboardInput);

        _item = item;
        _dispatcher = dispatcher;
        _variableStore = variableStore;
        _pathResolver = pathResolver;
        _mouseInput = mouseInput;
        _keyboardInput = keyboardInput;
        _processLauncher = processLauncher;
        _screenCapturer = screenCapturer;
        _imageMatcher = imageMatcher;
        _objectDetector = objectDetector;
        _ocrEngine = ocrEngine;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected override ValueTask<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (_item is not PluginCommandListItem pluginItem)
        {
            throw new InvalidOperationException("PluginCommand には PluginCommandListItem が必要です。");
        }

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
            CommandEventBus,
            _timeProvider);

        return _dispatcher.ExecuteAsync(pluginItem, context, cancellationToken);
    }
}

