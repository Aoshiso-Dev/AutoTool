using MacroPanels.List.Class;
using MacroPanels.Model.List.Interface;

namespace MacroPanels.ViewModel;

/// <summary>
/// ListPanelViewModelのインターフェース
/// </summary>
public interface IListPanelViewModel
{
    bool IsRunning { get; set; }
    CommandList CommandList { get; }
    int SelectedLineNumber { get; set; }
    ICommandListItem? SelectedItem { get; set; }
    
    // イベント
    event Action<ICommandListItem?>? SelectedItemChanged;
    event Action<ICommandListItem?>? ItemDoubleClicked;
    
    void SetCommandHistory(object commandHistory);
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
    void SetSelectedItem(ICommandListItem? item);
    void SetSelectedLineNumber(int lineNumber);
}

/// <summary>
/// EditPanelViewModelのインターフェース
/// </summary>
public interface IEditPanelViewModel
{
    bool IsRunning { get; set; }
    
    // イベント
    event Action<ICommandListItem?>? ItemEdited;
    event Action? RefreshRequested;
    
    void Prepare();
    void SetRunningState(bool isRunning);
    void SetItem(ICommandListItem? item);
    void SetListCount(int count);
    ICommandListItem? GetItem();
}

/// <summary>
/// ButtonPanelViewModelのインターフェース
/// </summary>
public interface IButtonPanelViewModel
{
    bool IsRunning { get; set; }
    
    // イベント
    event Func<Task>? RunRequested;
    event Action? StopRequested;
    event Action? SaveRequested;
    event Action? LoadRequested;
    event Action? ClearRequested;
    event Action<string>? AddRequested;
    event Action? UpRequested;
    event Action? DownRequested;
    event Action? DeleteRequested;
    
    void Prepare();
    void SetRunningState(bool isRunning);
}

/// <summary>
/// LogPanelViewModelのインターフェース
/// </summary>
public interface ILogPanelViewModel
{
    bool IsRunning { get; set; }
    
    void Prepare();
    void SetRunningState(bool isRunning);
    void WriteLog(string text);
    void WriteLog(string lineNumber, string commandName, string detail);
}

/// <summary>
/// FavoritePanelViewModelのインターフェース
/// </summary>
public interface IFavoritePanelViewModel
{
    bool IsRunning { get; set; }
    
    void Prepare();
    void SetRunningState(bool isRunning);
}
