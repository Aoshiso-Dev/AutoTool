using System.IO;
using System.Runtime.CompilerServices;
using AutoTool.Application.Ports;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Model.Input;
using AutoTool.Commands.Services;
using AutoTool.Desktop.Services;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;
using INotifier = AutoTool.Commands.Services.INotifier;

namespace AutoTool.Plugin.Host.Tests;

public sealed class PluginQuickActionExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_BuildsPluginCommandListItemAndUsesDispatcher()
    {
        var dispatcher = new FakePluginCommandDispatcher();
        var logWriter = new FakeLogWriter();
        var executor = new PluginQuickActionExecutor(
            dispatcher,
            new FakeVariableStore(),
            new FakePathResolver(),
            new FakeMouseInput(),
            new FakeKeyboardInput(),
            new FakeCommandEventBus(),
            logWriter,
            new FakeNotifier(),
            TimeProvider.System);

        var descriptor = new PluginQuickActionDescriptor
        {
            PluginId = "Quick.Plugin",
            ActionId = "stage-console",
            DisplayName = "ステージ",
            CommandType = "Quick.Plugin.StageConsole",
            ParameterJson = """{"mode":"console"}""",
            IsAvailable = true,
        };

        var success = await executor.ExecuteAsync(descriptor, CancellationToken.None);

        Assert.True(success);
        Assert.NotNull(dispatcher.Item);
        Assert.Equal("Quick.Plugin", dispatcher.Item.PluginId);
        Assert.Equal("Quick.Plugin.StageConsole", dispatcher.Item.ItemType);
        Assert.Equal("""{"mode":"console"}""", dispatcher.Item.ParameterJson);
        Assert.Contains(logWriter.StructuredMessages, x => x.EventName == "QuickActionExecute" && x.Fields["Message"]?.ToString() == "QuickAction 実行: ステージ");
    }

    private sealed class FakePluginCommandDispatcher : IPluginCommandDispatcher
    {
        public PluginCommandListItem? Item { get; private set; }

        public ValueTask<bool> ExecuteAsync(
            PluginCommandListItem item,
            ICommandExecutionContext context,
            CancellationToken cancellationToken)
        {
            Item = item;
            return ValueTask.FromResult(true);
        }
    }

    private sealed class FakeVariableStore : IVariableStore
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
        public void Set(string name, string value) => _values[name] = value;
        public string? Get(string name) => _values.TryGetValue(name, out var value) ? value : null;
        public void Clear() => _values.Clear();
    }

    private sealed class FakePathResolver : IPathResolver
    {
        public string BaseDirectory => AppContext.BaseDirectory;
        public string ToAbsolutePath(string relativePath) => Path.GetFullPath(relativePath);
        public string ToRelativePath(string absolutePath) => absolutePath;
    }

    private sealed class FakeMouseInput : IMouseInput
    {
        public Task ClickAsync(int x, int y, CommandMouseButton button, string? windowTitle = null, string? windowClassName = null, int holdDurationMs = 20, string clickInjectionMode = "MouseEvent", bool simulateMouseMove = false, bool restoreCursorPositionAfterClick = false, bool restoreWindowZOrderAfterClick = false)
            => Task.CompletedTask;
    }

    private sealed class FakeKeyboardInput : IKeyboardInput
    {
        public Task SendKeyAsync(CommandKey key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null)
            => Task.CompletedTask;
    }

    private sealed class FakeCommandEventBus : ICommandEventBus
    {
        public event EventHandler<CommandEventArgs>? Started;
        public event EventHandler<CommandEventArgs>? Finished;
        public event EventHandler<CommandLogEventArgs>? Doing;
        public event EventHandler<CommandProgressEventArgs>? ProgressUpdated;
        public long DroppedEventCount => 0;
        public int SubscriberCount => 0;
        public void PublishStarted(ICommand command) => Started?.Invoke(this, new CommandEventArgs(command));
        public void PublishFinished(ICommand command) => Finished?.Invoke(this, new CommandEventArgs(command));
        public void PublishDoing(ICommand command, string detail) => Doing?.Invoke(this, new CommandLogEventArgs(command, detail));
        public void PublishDoing(ICommand command, string detail, CommandLogPayload payload) => Doing?.Invoke(this, new CommandLogEventArgs(command, detail, payload));
        public void PublishProgress(ICommand command, int progress) => ProgressUpdated?.Invoke(this, new CommandProgressEventArgs(command, progress));
        public async IAsyncEnumerable<CommandBusEvent> ReadEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class FakeLogWriter : ILogWriter
    {
        public List<(string EventName, IReadOnlyDictionary<string, object?> Fields)> StructuredMessages { get; } = [];
        public void Write(params string[] messages) { }
        public void WriteStructured(string category, string eventName, IReadOnlyDictionary<string, object?> fields)
            => StructuredMessages.Add((eventName, fields));
        public void Write(Exception exception) { }
    }

    private sealed class FakeNotifier : INotifier
    {
        public void ShowInfo(string message, string title) { }
        public void ShowWarning(string message, string title) { }
        public void ShowError(string message, string title) { }
        public bool ShowConfirm(string message, string title) => true;
    }
}
