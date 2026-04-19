using AutoTool.Application.Files;
using AutoTool.Application.History;
using AutoTool.Application.History.Commands;
using AutoTool.Application.Ports;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Contracts.Lists;
using System.Collections.ObjectModel;
using System.IO;

namespace AutoTool.Automation.Runtime.Tests;

public class HistoryCommandRegressionTests
{
    [Fact]
    public void AddItemCommand_ExecuteAndUndo_InvokeExpectedActions()
    {
        var item = new WaitItem { ItemType = "Wait", Wait = 10 };
        ICommandListItem? addedItem = null;
        var addedIndex = -1;
        var removedIndex = -1;

        var command = new AddItemCommand(
            item,
            3,
            (x, index) => { addedItem = x; addedIndex = index; },
            index => removedIndex = index);

        command.Execute();
        command.Undo();

        Assert.Same(item, addedItem);
        Assert.Equal(3, addedIndex);
        Assert.Equal(3, removedIndex);
    }

    [Fact]
    public void RemoveItemCommand_ExecuteAndUndo_InvokeExpectedActions()
    {
        var item = new ClickItem { ItemType = "Click", X = 10, Y = 20 };
        ICommandListItem? restoredItem = null;
        var restoredIndex = -1;
        var removedIndex = -1;

        var command = new RemoveItemCommand(
            item,
            2,
            (x, index) => { restoredItem = x; restoredIndex = index; },
            index => removedIndex = index);

        command.Execute();
        command.Undo();

        Assert.Equal(2, removedIndex);
        Assert.Same(item, restoredItem);
        Assert.Equal(2, restoredIndex);
    }

    [Fact]
    public void MoveItemCommand_ExecuteAndUndo_InvokeExpectedActions()
    {
        var moves = new List<(int From, int To)>();
        var command = new MoveItemCommand(1, 4, (from, to) => moves.Add((from, to)));

        command.Execute();
        command.Undo();

        Assert.Equal([(1, 4), (4, 1)], moves);
    }

    [Fact]
    public void EditItemCommand_UsesClonedItemsForExecuteAndUndo()
    {
        var oldItem = new WaitItem { ItemType = "Wait", Wait = 1 };
        var newItem = new WaitItem { ItemType = "Wait", Wait = 2 };
        var replacedItems = new List<ICommandListItem>();

        var command = new EditItemCommand(
            oldItem,
            newItem,
            0,
            (item, _) => replacedItems.Add(item));

        oldItem.Wait = 100;
        newItem.Wait = 200;

        command.Execute();
        command.Undo();

        var executed = Assert.IsType<WaitItem>(replacedItems[0]);
        var undone = Assert.IsType<WaitItem>(replacedItems[1]);

        Assert.Equal(2, executed.Wait);
        Assert.Equal(1, undone.Wait);
    }

    [Fact]
    public void ClearAllCommand_Undo_RestoresClonedSnapshot()
    {
        var original = new WaitItem { ItemType = "Wait", Wait = 5 };
        var sourceItems = new List<ICommandListItem> { original };
        var cleared = false;
        IReadOnlyList<ICommandListItem>? restored = null;

        var command = new ClearAllCommand(
            sourceItems,
            () => cleared = true,
            items => restored = items.ToList());

        original.Wait = 99;

        command.Execute();
        command.Undo();

        Assert.True(cleared);
        Assert.NotNull(restored);
        var restoredItem = Assert.IsType<WaitItem>(Assert.Single(restored!));
        Assert.Equal(5, restoredItem.Wait);
        Assert.NotSame(original, restoredItem);
    }

    [Fact]
    public void CommandHistoryManager_NewExecuteAfterUndo_ClearsRedoStack()
    {
        var manager = new CommandHistoryManager();
        var first = new RecordingCommand("first");
        var second = new RecordingCommand("second");
        var third = new RecordingCommand("third");
        var changedCount = 0;
        manager.HistoryChanged += (_, _) => changedCount++;

        manager.ExecuteCommand(first);
        manager.ExecuteCommand(second);
        manager.Undo();
        manager.ExecuteCommand(third);

        Assert.False(manager.CanRedo);
        Assert.Equal("third", manager.UndoDescription);
        Assert.Equal("なし", manager.RedoDescription);
        Assert.Equal(4, changedCount);
    }

    private sealed class RecordingCommand(string description) : IUndoRedoCommand
    {
        public string Description { get; } = description;
        public int ExecuteCount { get; private set; }
        public int UndoCount { get; private set; }

        public void Execute() => ExecuteCount++;
        public void Undo() => UndoCount++;
    }
}

public class FileManagerRegressionTests
{
    [Fact]
    public void OpenFile_KeepsOnlyLatest10RecentFiles()
    {
        var paths = Enumerable.Range(0, 11)
            .Select(i => Path.Combine(Path.GetTempPath(), $"autotool-recent-{Guid.NewGuid()}-{i}.macro"))
            .ToArray();

        foreach (var path in paths)
        {
            File.WriteAllText(path, "macro");
        }

        var picker = new TestFilePicker();
        var store = new TestRecentFileStore();
        var manager = CreateManager(picker, store);

        try
        {
            foreach (var path in paths)
            {
                Assert.True(manager.OpenFile(path));
            }

            Assert.NotNull(manager.RecentFiles);
            Assert.Equal(10, manager.RecentFiles!.Count);
            Assert.Equal(paths[^1], manager.RecentFiles[0].FilePath);
            Assert.DoesNotContain(manager.RecentFiles, x =>
                string.Equals(x.FilePath, paths[0], StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }

    private static FileManager CreateManager(IFilePicker picker, IRecentFileStore store)
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
            store,
            new TestFileSystemPathService());
    }

    private sealed class TestFilePicker : IFilePicker
    {
        public string? OpenFile(FileDialogOptions options) => null;
        public string? SaveFile(FileDialogOptions options) => null;
    }

    private sealed class TestRecentFileStore : IRecentFileStore
    {
        public ObservableCollection<RecentFileEntry>? Files { get; set; } = [];

        public ObservableCollection<RecentFileEntry>? Load(string key) => Files;

        public void Save(string key, ObservableCollection<RecentFileEntry>? files)
        {
            Files = files;
        }
    }

    private sealed class TestFileSystemPathService : IFileSystemPathService
    {
        public bool FileExists(string filePath) => File.Exists(filePath);

        public string GetFileName(string filePath) => Path.GetFileName(filePath);
    }
}
