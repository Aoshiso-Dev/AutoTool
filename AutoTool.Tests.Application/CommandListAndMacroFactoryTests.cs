using AutoTool.Commands.Commands;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Model.Input;
using AutoTool.Commands.Services;
using AutoTool.Application.Ports;
using AutoTool.Application.Files;
using AutoTool.Application.History;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.MacroFactory;
using AutoTool.Automation.Runtime.Serialization;
using AutoTool.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace AutoTool.Automation.Runtime.Tests;

public class CommandListTests
{
    private static CommandList CreateList()
    {
        var registry = new ReflectionCommandRegistry();
        registry.Initialize();
        var serializer = new MacroFileSerializer();
        return new CommandList(registry, serializer);
    }

    [Fact]
    public void Add_LoopBlock_RebuildsLineNumbersNestLevelAndPairs()
    {
        var list = CreateList();

        var loop = new LoopItem { ItemType = CommandTypeNames.Loop, LoopCount = 2 };
        var click = new ClickItem { ItemType = CommandTypeNames.Click };
        var loopEnd = new LoopEndItem { ItemType = CommandTypeNames.LoopEnd };

        list.Add(loop);
        list.Add(click);
        list.Add(loopEnd);

        Assert.Equal(1, loop.LineNumber);
        Assert.Equal(2, click.LineNumber);
        Assert.Equal(3, loopEnd.LineNumber);

        Assert.Equal(0, loop.NestLevel);
        Assert.Equal(1, click.NestLevel);
        Assert.Equal(0, loopEnd.NestLevel);

        Assert.Same(loopEnd, loop.Pair);
        Assert.Same(loop, loopEnd.Pair);
    }

    [Fact]
    public void Move_UpdatesLineNumbers()
    {
        var list = CreateList();

        var first = new WaitItem { ItemType = CommandTypeNames.Wait, Wait = 10 };
        var second = new ClickItem { ItemType = CommandTypeNames.Click };

        list.Add(first);
        list.Add(second);
        list.Move(0, 1);

        Assert.Equal(1, second.LineNumber);
        Assert.Equal(2, first.LineNumber);
    }

    [Fact]
    public void Load_UnknownItemType_ThrowsInvalidDataException()
    {
        var registry = new ReflectionCommandRegistry();
        registry.Initialize();

        var serializer = new FakeMacroFileSerializer(new ObservableCollection<ICommandListItem>
        {
            new WaitItem { ItemType = "UnknownItemType" }
        });

        var list = new CommandList(registry, serializer);

        Assert.Throws<InvalidDataException>(() => list.Load("dummy-path.json"));
    }
}

public class MacroFactoryTests
{
    [Fact]
    public void CreateMacro_DoesNotMutateInputItemsStateFlags()
    {
        var services = new ServiceCollection();
        services.AddCommandServices();
        services.AddMacroRuntimeCoreServices();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ICommandRegistry>();
        registry.Initialize();

        var serializer = new MacroFileSerializer();
        var list = new CommandList(provider.GetRequiredService<ICommandDefinitionProvider>(), serializer);

        var ifItem = new IfVariableItem
        {
            ItemType = CommandTypeNames.IfVariable,
            Name = "test",
            Operator = "==",
            Value = "ok"
        };
        var waitItem = new WaitItem { ItemType = CommandTypeNames.Wait, Wait = 1 };
        var ifEnd = new IfEndItem { ItemType = CommandTypeNames.IfEnd };

        list.Add(ifItem);
        list.Add(waitItem);
        list.Add(ifEnd);

        Assert.False(waitItem.IsInIf);
        Assert.False(waitItem.IsInLoop);

        var factory = provider.GetRequiredService<IMacroFactory>();
        var macro = factory.CreateMacro(list.Items);

        Assert.NotNull(macro);
        Assert.False(waitItem.IsInIf);
        Assert.False(waitItem.IsInLoop);
    }

    [Fact]
    public void CreateMacro_WithIfTextExist_BuildsIfTextCommand()
    {
        var services = new ServiceCollection();
        services.AddCommandServices();
        services.AddMacroRuntimeCoreServices();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ICommandRegistry>();
        registry.Initialize();

        var serializer = new MacroFileSerializer();
        var list = new CommandList(provider.GetRequiredService<ICommandDefinitionProvider>(), serializer);

        var ifItem = new IfTextExistItem
        {
            ItemType = CommandTypeNames.IfTextExist,
            TargetText = "ログイン"
        };
        var waitItem = new WaitItem { ItemType = CommandTypeNames.Wait, Wait = 1 };
        var ifEnd = new IfEndItem { ItemType = CommandTypeNames.IfEnd };

        list.Add(ifItem);
        list.Add(waitItem);
        list.Add(ifEnd);

        var factory = provider.GetRequiredService<IMacroFactory>();
        var macro = factory.CreateMacro(list.Items);

        var topLoop = Assert.IsType<LoopCommand>(macro);
        var firstChild = Assert.Single(topLoop.Children);
        Assert.IsType<IfTextExistCommand>(firstChild);
    }

    [Fact]
    public void CreateMacro_WithIfTextExistWithoutEnd_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddCommandServices();
        services.AddMacroRuntimeCoreServices();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ICommandRegistry>();
        registry.Initialize();

        var serializer = new MacroFileSerializer();
        var list = new CommandList(provider.GetRequiredService<ICommandDefinitionProvider>(), serializer);

        list.Add(new IfTextExistItem
        {
            ItemType = CommandTypeNames.IfTextExist,
            TargetText = "ログイン"
        });

        var factory = provider.GetRequiredService<IMacroFactory>();
        var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateMacro(list.Items));
        Assert.Contains("生成に失敗", ex.Message);
    }
}

public class CommandHistoryManagerTests
{
    [Fact]
    public void ExecuteCommand_TrimsOldHistory_WhenExceedingLimit()
    {
        var manager = new CommandHistoryManager();

        for (var i = 1; i <= 60; i++)
        {
            manager.ExecuteCommand(new TestUndoRedoCommand($"cmd-{i}"));
        }

        var (UndoHistory, RedoHistory) = manager.GetHistoryDetails();

        Assert.Equal(50, UndoHistory.Length);
        Assert.Contains("cmd-11", UndoHistory);
        Assert.DoesNotContain("cmd-1", UndoHistory);
    }

    private sealed class TestUndoRedoCommand : IUndoRedoCommand
    {
        public TestUndoRedoCommand(string description) => Description = description;

        public string Description { get; }
        public void Execute() { }
        public void Undo() { }
    }

    [Fact]
    public void ExecuteCommand_Null_ThrowsArgumentNullException()
    {
        var manager = new CommandHistoryManager();
        Assert.Throws<ArgumentNullException>(() => manager.ExecuteCommand(null!));
    }
}

internal sealed class FakeMacroFileSerializer : IMacroFileSerializer
{
    private readonly object? _deserializedObject;

    public FakeMacroFileSerializer(object? deserializedObject)
    {
        _deserializedObject = deserializedObject;
    }

    public void SerializeToFile<T>(T obj, string path) { }

    public T? DeserializeFromFile<T>(string path)
    {
        return _deserializedObject is T casted ? casted : default;
    }
}

public class FileManagerTests
{
    [Fact]
    public void OpenFile_WhenPickerCancelled_ReturnsFalse()
    {
        var picker = new FakeFilePicker { OpenFilePath = null };
        var store = new FakeRecentFileStore();
        var manager = CreateFileManager(picker, store);

        var opened = manager.OpenFile();

        Assert.False(opened);
        Assert.False(manager.IsFileOpened);
    }

    [Fact]
    public void OpenFile_WhenMissingPath_RemovesRecentEntryAndReturnsFalse()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid()}.macro");
        var picker = new FakeFilePicker { OpenFilePath = missingPath };
        var store = new FakeRecentFileStore
        {
            Files =
            [
                new() { FileName = Path.GetFileName(missingPath), FilePath = missingPath }
            ]
        };
        var manager = CreateFileManager(picker, store);

        var opened = manager.OpenFile();

        Assert.False(opened);
        Assert.Empty(manager.RecentFiles!);
        Assert.True(store.SaveCalled);
    }

    [Fact]
    public void OpenFile_WhenFileExists_ReturnsTrueAndInvokesLoadAction()
    {
        var existingPath = Path.Combine(Path.GetTempPath(), $"existing-{Guid.NewGuid()}.macro");
        File.WriteAllText(existingPath, "test");
        var picker = new FakeFilePicker { OpenFilePath = existingPath };
        var store = new FakeRecentFileStore();
        var loadCalled = false;
        var manager = CreateFileManager(
            picker,
            store,
            loadAction: _ => loadCalled = true);

        try
        {
            var opened = manager.OpenFile();

            Assert.True(opened);
            Assert.True(loadCalled);
            Assert.True(manager.IsFileOpened);
            Assert.Equal(existingPath, manager.CurrentFilePath);
            Assert.Equal(Path.GetFileName(existingPath), manager.CurrentFileName);
        }
        finally
        {
            if (File.Exists(existingPath))
            {
                File.Delete(existingPath);
            }
        }
    }

    [Fact]
    public void OpenFile_WhenMissingPathWithDifferentCase_PrunesRecentEntry()
    {
        var actualPath = Path.Combine(Path.GetTempPath(), $"case-{Guid.NewGuid()}.macro");
        var recentPath = actualPath.ToUpperInvariant();
        var picker = new FakeFilePicker { OpenFilePath = actualPath };
        var store = new FakeRecentFileStore
        {
            Files =
            [
                new() { FileName = Path.GetFileName(recentPath), FilePath = recentPath }
            ]
        };
        var manager = CreateFileManager(picker, store);

        var opened = manager.OpenFile();

        Assert.False(opened);
        Assert.Empty(manager.RecentFiles!);
    }

    [Fact]
    public void SaveFileAs_WhenPickerCancelled_ReturnsFalseAndKeepsState()
    {
        var picker = new FakeFilePicker { SaveFilePath = null };
        var store = new FakeRecentFileStore();
        var manager = CreateFileManager(picker, store);

        var saved = manager.SaveFileAs();

        Assert.False(saved);
        Assert.False(manager.IsFileOpened);
        Assert.False(store.SaveCalled);
    }

    [Fact]
    public void SaveFileAs_WhenPathProvided_ReturnsTrueAndUpdatesCurrentFile()
    {
        var savePath = Path.Combine(Path.GetTempPath(), $"save-{Guid.NewGuid()}.macro");
        var picker = new FakeFilePicker { SaveFilePath = savePath };
        var store = new FakeRecentFileStore();
        var manager = CreateFileManager(picker, store);

        var saved = manager.SaveFileAs();

        Assert.True(saved);
        Assert.True(manager.IsFileOpened);
        Assert.Equal(savePath, manager.CurrentFilePath);
        Assert.Equal(Path.GetFileName(savePath), manager.CurrentFileName);
        Assert.True(store.SaveCalled);
    }

    private static FileManager CreateFileManager(
        FakeFilePicker picker,
        FakeRecentFileStore store,
        Action<string>? saveAction = null,
        Action<string>? loadAction = null)
    {
        return new FileManager(
            new FileManager.FileTypeInfo
            {
                DefaultExt = "macro",
                Filter = "Macro|*.macro",
                FilterIndex = 1,
                RestoreDirectory = true,
                Title = "Open"
            },
            saveAction ?? (_ => { }),
            loadAction ?? (_ => { }),
            picker,
            store);
    }

    private sealed class FakeFilePicker : IFilePicker
    {
        public string? OpenFilePath { get; init; }
        public string? SaveFilePath { get; init; }

        public string? OpenFile(FileDialogOptions options) => OpenFilePath;
        public string? SaveFile(FileDialogOptions options) => SaveFilePath;
    }

    private sealed class FakeRecentFileStore : IRecentFileStore
    {
        public ObservableCollection<RecentFileEntry>? Files { get; set; } = [];
        public bool SaveCalled { get; private set; }

        public ObservableCollection<RecentFileEntry>? Load(string key) => Files;

        public void Save(string key, ObservableCollection<RecentFileEntry>? files)
        {
            SaveCalled = true;
            Files = files;
        }
    }
}

public class FindTextItemTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTextFound_ReturnsTrueAndSetsVariables()
    {
        var item = new FindTextItem
        {
            TargetText = "ログイン",
            MatchMode = "Contains",
            Strict = true,
            FoundVariableName = "found",
            TextVariableName = "text",
            ConfidenceVariableName = "conf",
            Timeout = 0
        };

        var context = new FakeCommandExecutionContext(new OcrExtractionResult("ログイン成功", 88.5));

        var result = await item.ExecuteAsync(context, CancellationToken.None);

        Assert.True(result);
        Assert.Equal("true", context.GetVariable("found"));
        Assert.Equal("ログイン成功", context.GetVariable("text"));
        Assert.Equal("88.5", context.GetVariable("conf"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTextNotFoundAndStrictFalse_ReturnsTrue()
    {
        var item = new FindTextItem
        {
            TargetText = "ログイン",
            MatchMode = "Contains",
            Strict = false,
            Timeout = 0
        };

        var context = new FakeCommandExecutionContext(new OcrExtractionResult("サインアップ", 90.0));

        var result = await item.ExecuteAsync(context, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTextNotFoundAndStrictTrue_ReturnsFalse()
    {
        var item = new FindTextItem
        {
            TargetText = "ログイン",
            MatchMode = "Equals",
            Strict = true,
            Timeout = 0
        };

        var context = new FakeCommandExecutionContext(new OcrExtractionResult("サインアップ", 95.0));

        var result = await item.ExecuteAsync(context, CancellationToken.None);

        Assert.False(result);
    }

    private sealed class FakeCommandExecutionContext : ICommandExecutionContext
    {
        private readonly OcrExtractionResult _ocrResult;
        private readonly Dictionary<string, string> _vars = new(StringComparer.Ordinal);

        public FakeCommandExecutionContext(OcrExtractionResult ocrResult)
        {
            _ocrResult = ocrResult;
        }

        public DateTimeOffset GetLocalNow() => DateTimeOffset.Parse("2026-01-01T00:00:00+09:00");
        public void ReportProgress(int progress) { }
        public void Log(string message) { }
        public string? GetVariable(string name) => _vars.TryGetValue(name, out var v) ? v : null;
        public void SetVariable(string name, string value) => _vars[name] = value;
        public string ToAbsolutePath(string relativePath) => relativePath;
        public Task ClickAsync(int x, int y, CommandMouseButton button, string? windowTitle = null, string? windowClassName = null) => Task.CompletedTask;
        public Task SendHotkeyAsync(CommandKey key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null) => Task.CompletedTask;
        public Task ExecuteProgramAsync(string programPath, string? arguments, string? workingDirectory, bool waitForExit, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task TakeScreenshotAsync(string filePath, string? windowTitle, string? windowClassName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<MatchPoint?> SearchImageAsync(string imagePath, double threshold, CommandColor? searchColor, string? windowTitle, string? windowClassName, CancellationToken cancellationToken) => Task.FromResult<MatchPoint?>(null);
        public void InitializeAIModel(string modelPath, int inputSize = 640, bool useGpu = true) { }
        public IReadOnlyList<DetectionResult> DetectAI(string? windowTitle, float confThreshold, float iouThreshold) => [];
        public Task<OcrExtractionResult> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken) => Task.FromResult(_ocrResult);
    }
}
