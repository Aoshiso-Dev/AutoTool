using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Domain.Macros;

namespace AutoTool.Desktop.Panels.ViewModel;

/// <summary>
/// コマンド一覧パネルの表示状態と編集操作を提供する ViewModel 契約です。
/// </summary>
public interface IListPanelViewModel
{
    bool IsRunning { get; set; }
    bool IsAllSelectedVisual { get; set; }
    CommandList CommandList { get; }
    IReadOnlyList<ICommandListItem> ValidationErrorItems { get; }
    int SelectedLineNumber { get; set; }
    ICommandListItem? SelectedItem { get; set; }

    /// <summary>選択項目が変わったときに通知します。</summary>
    event Action<ICommandListItem?>? SelectedItemChanged;
    /// <summary>項目がダブルクリックされたときに通知します。</summary>
    event Action<ICommandListItem?>? ItemDoubleClicked;
    /// <summary>一覧とのユーザー操作が発生したときに通知します。</summary>
    event Action? InteractionRequested;
    /// <summary>項目移動が要求されたときに通知します。</summary>
    event Action<int, int>? MoveItemRequested;
    /// <summary>項目削除が要求されたときに通知します。</summary>
    event Action? DeleteRequested;

    void Prepare();
    void SetRunningState(bool isRunning);
    void Refresh();
    void Add(string itemType);
    void InsertAt(int index, ICommandListItem item);
    void RemoveAt(int index);
    void ReplaceAt(int index, ICommandListItem item);
    void MoveItem(int fromIndex, int toIndex);
    void RequestMoveItem(int fromIndex, int toIndex);
    void RequestDelete();
    void AddItem(ICommandListItem item);
    void Up();
    void Down();
    void Delete();
    void Clear();
    void Save(string filePath = "");
    void Load(string filePath = "");
    bool IsBlockStartCommand(ICommandListItem item);
    bool IsBlockCollapsed(ICommandListItem item);
    void ToggleBlockCollapse(ICommandListItem item);
    bool ShouldHideCommandInCollapsedScope(ICommandListItem item);
    int GetCount();
    ICommandListItem? GetItem(int lineNumber);
    IReadOnlyList<ICommandListItem> GetSelectedItems();
    void SetSelectedItems(IReadOnlyList<ICommandListItem> items);
    void SetSelectedItem(ICommandListItem? item);
    void SetSelectedLineNumber(int lineNumber);
    void SetValidationErrorItems(IEnumerable<ICommandListItem> items);
    void NotifyInteraction();
    void OnItemDoubleClick();
}

/// <summary>
/// 選択中コマンドのプロパティ編集を担当する ViewModel 契約です。
/// </summary>
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

/// <summary>
/// 実行・停止・保存など主要ボタン操作を仲介する ViewModel 契約です。
/// </summary>
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
    void RequestStop();
}

/// <summary>
/// ログ表示パネルの出力と状態を管理する ViewModel 契約です。
/// </summary>
public interface ILogPanelViewModel
{
    bool IsRunning { get; set; }
    event Action<string>? StatusMessageRequested;

    void Prepare();
    void SetRunningState(bool isRunning);
    void WriteLog(string text);
    void WriteLog(string lineNumber, string commandName, string detail);
}

/// <summary>
/// 変数表示パネルの状態と操作を管理する ViewModel 契約です。
/// </summary>
public interface IVariablePanelViewModel
{
    bool IsRunning { get; set; }
    event Action<string>? StatusMessageRequested;

    void Prepare();
    void SetRunningState(bool isRunning);
    void Refresh();
}

/// <summary>
/// お気に入りマクロの登録・読込・削除を管理する ViewModel 契約です。
/// </summary>
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
