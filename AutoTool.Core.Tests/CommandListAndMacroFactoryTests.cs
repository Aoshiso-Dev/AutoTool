using AutoTool.Commands.Commands;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;
using AutoTool.Core.Ports;
using AutoTool.Model;
using AutoTool.Panels.List.Class;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Panels.Model.List.Interface;
using AutoTool.Panels.Model.MacroFactory;
using AutoTool.Panels.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.IO;

namespace AutoTool.Core.Tests;

public class CommandListTests
{
    private static CommandList CreateList()
    {
        var registry = new ReflectionCommandRegistry();
        registry.Initialize();
        var serializer = new MacroFileSerializer(registry);
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
        services.AddSingleton<ICommandFactory, CommandFactory>();
        services.AddSingleton<ReflectionCommandRegistry>();
        services.AddSingleton<ICommandRegistry>(sp => sp.GetRequiredService<ReflectionCommandRegistry>());
        services.AddSingleton<ICommandDefinitionProvider>(sp => sp.GetRequiredService<ReflectionCommandRegistry>());
        services.AddTransient<ICompositeCommandBuilder, IfCompositeCommandBuilder>();
        services.AddTransient<ICompositeCommandBuilder, LoopCompositeCommandBuilder>();
        services.AddSingleton<IMacroFactory, MacroFactory>();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ICommandRegistry>();
        registry.Initialize();

        var serializer = new MacroFileSerializer((ICommandDefinitionProvider)provider.GetRequiredService<ICommandRegistry>());
        var list = new CommandList((ICommandDefinitionProvider)provider.GetRequiredService<ICommandRegistry>(), serializer);

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

        var history = manager.GetHistoryDetails();

        Assert.Equal(50, history.UndoHistory.Length);
        Assert.Contains("cmd-11", history.UndoHistory);
        Assert.DoesNotContain("cmd-1", history.UndoHistory);
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
            Files = new ObservableCollection<FileManager.RecentFile>
            {
                new() { FileName = Path.GetFileName(missingPath), FilePath = missingPath }
            }
        };
        var manager = CreateFileManager(picker, store);

        var opened = manager.OpenFile();

        Assert.False(opened);
        Assert.Empty(manager.RecentFiles!);
        Assert.True(store.SaveCalled);
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

    private static FileManager CreateFileManager(FakeFilePicker picker, FakeRecentFileStore store)
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
            _ => { },
            _ => { },
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
        public ObservableCollection<FileManager.RecentFile>? Files { get; set; } = new();
        public bool SaveCalled { get; private set; }

        public ObservableCollection<FileManager.RecentFile>? Load(string key) => Files;

        public void Save(string key, ObservableCollection<FileManager.RecentFile>? files)
        {
            SaveCalled = true;
            Files = files;
        }
    }
}
