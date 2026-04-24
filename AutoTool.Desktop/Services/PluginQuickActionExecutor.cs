using AutoTool.Application.Ports;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Commands.Commands;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;
using INotifier = AutoTool.Commands.Services.INotifier;
using ICommand = AutoTool.Commands.Interface.ICommand;

namespace AutoTool.Desktop.Services;

public sealed class PluginQuickActionExecutor(
    IPluginCommandDispatcher dispatcher,
    IVariableStore variableStore,
    IPathResolver pathResolver,
    IMouseInput mouseInput,
    IKeyboardInput keyboardInput,
    ICommandEventBus commandEventBus,
    ILogWriter logWriter,
    INotifier notifier,
    TimeProvider timeProvider,
    IProcessLauncher? processLauncher = null,
    IScreenCapturer? screenCapturer = null,
    IImageMatcher? imageMatcher = null,
    IObjectDetector? objectDetector = null,
    IOcrEngine? ocrEngine = null)
{
    private readonly IPluginCommandDispatcher _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    private readonly IVariableStore _variableStore = variableStore ?? throw new ArgumentNullException(nameof(variableStore));
    private readonly IPathResolver _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
    private readonly IMouseInput _mouseInput = mouseInput ?? throw new ArgumentNullException(nameof(mouseInput));
    private readonly IKeyboardInput _keyboardInput = keyboardInput ?? throw new ArgumentNullException(nameof(keyboardInput));
    private readonly ICommandEventBus _commandEventBus = commandEventBus ?? throw new ArgumentNullException(nameof(commandEventBus));
    private readonly ILogWriter _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
    private readonly INotifier _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly IProcessLauncher? _processLauncher = processLauncher;
    private readonly IScreenCapturer? _screenCapturer = screenCapturer;
    private readonly IImageMatcher? _imageMatcher = imageMatcher;
    private readonly IObjectDetector? _objectDetector = objectDetector;
    private readonly IOcrEngine? _ocrEngine = ocrEngine;

    public async Task<bool> ExecuteAsync(PluginQuickActionDescriptor descriptor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var item = new PluginCommandListItem
        {
            PluginId = descriptor.PluginId,
            ItemType = descriptor.CommandType,
            ParameterJson = string.IsNullOrWhiteSpace(descriptor.ParameterJson) ? "{}" : descriptor.ParameterJson,
        };

        var command = new QuickActionCommand(descriptor.DisplayName);
        var context = new CommandExecutionContext(
            command,
            _variableStore,
            _pathResolver,
            _mouseInput,
            _keyboardInput,
            _processLauncher,
            _screenCapturer,
            _imageMatcher,
            _objectDetector,
            _ocrEngine,
            _commandEventBus,
            _timeProvider);

        _logWriter.WriteStructured(
            "Plugin",
            "QuickActionExecute",
            new Dictionary<string, object?>
            {
                ["Message"] = $"QuickAction 実行: {descriptor.DisplayName}",
                ["PluginId"] = descriptor.PluginId,
                ["ActionId"] = descriptor.ActionId,
                ["CommandType"] = descriptor.CommandType,
            });

        try
        {
            return await _dispatcher.ExecuteAsync(item, context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logWriter.Write(ex);
            _notifier.ShowError($"QuickAction の実行に失敗しました。\n{ex.GetBaseException().Message}", "プラグイン拡張");
            return false;
        }
    }

    private sealed class QuickActionCommand(string displayName) : ICommand
    {
        public int LineNumber { get; set; }
        public bool IsEnabled { get; set; } = true;
        public ICommand? Parent { get; set; }
        public IEnumerable<ICommand> Children { get; set; } = [];
        public int NestLevel { get; set; }
        public ICommandSettings Settings { get; } = new CommandSettings();
        public EventHandler? OnStartCommand { get; set; }
        public EventHandler? OnFinishCommand { get; set; }
        public Task<bool> Execute(CancellationToken cancellationToken) => Task.FromResult(true);
        public bool CanExecute() => IsEnabled;
        public override string ToString() => displayName;
    }
}
