using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Domain.Macros;

namespace AutoTool.Desktop.Panels.ViewModel;

public interface IListPanelViewModel
{
    bool IsRunning { get; set; }
    bool IsAllSelectedVisual { get; set; }
    CommandList CommandList { get; }
    int SelectedLineNumber { get; set; }
    ICommandListItem? SelectedItem { get; set; }

    event Action<ICommandListItem?>? SelectedItemChanged;
    event Action<ICommandListItem?>? ItemDoubleClicked;

    void Prepare();
    void SetRunningState(bool isRunning);
    void Refresh();
    void Add(string itemType);
    void InsertAt(int index, ICommandListItem item);
    void RemoveAt(int index);
    void ReplaceAt(int index, ICommandListItem item);
    void MoveItem(int fromIndex, int toIndex);
    void AddItem(ICommandListItem item);
    void Up();
    void Down();
    void Delete();
    void Clear();
    void Save(string filePath = "");
    void Load(string filePath = "");
    int GetCount();
    ICommandListItem? GetItem(int lineNumber);
    IReadOnlyList<ICommandListItem> GetSelectedItems();
    void SetSelectedItems(IReadOnlyList<ICommandListItem> items);
    void SetSelectedItem(ICommandListItem? item);
    void SetSelectedLineNumber(int lineNumber);
}

public interface IEditPanelViewModel
{
    bool IsRunning { get; set; }

    event Action<ICommandListItem?>? ItemEdited;

    void Prepare();
    void SetRunningState(bool isRunning);
    void SetItem(ICommandListItem? item);
    void SetListCount(int count);
    ICommandListItem? GetItem();
}

public interface IButtonPanelViewModel
{
    bool IsRunning { get; set; }

    event Func<Task>? RunRequested;
    event Action? StopRequested;
    event Action? SaveRequested;
    event Action? LoadRequested;
    event Action? ClearRequested;
    event Action<string>? AddRequested;
    event Action? UpRequested;
    event Action? DownRequested;
    event Action? DeleteRequested;
    event Action? CopyRequested;
    event Action? PasteRequested;

    void Prepare();
    void SetRunningState(bool isRunning);
}

public interface ILogPanelViewModel
{
    bool IsRunning { get; set; }
    event Action<string>? StatusMessageRequested;

    void Prepare();
    void SetRunningState(bool isRunning);
    void WriteLog(string text);
    void WriteLog(string lineNumber, string commandName, string detail);
}

public interface IFavoritePanelViewModel
{
    bool IsRunning { get; set; }
    FavoriteMacroEntry? SelectedFavorite { get; set; }

    event Action<string>? AddRequested;
    event Action<FavoriteMacroEntry>? DeleteRequested;
    event Action<FavoriteMacroEntry>? LoadRequested;
    event Action<FavoriteMacroEntry>? InsertRequested;

    void Prepare();
    void SetRunningState(bool isRunning);
    void AddFavorite(FavoriteMacroEntry favorite);
    void RemoveFavorite(FavoriteMacroEntry favorite);
}
