using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MacroPanels.Command.Commands;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Message;
using MacroPanels.Command.Services;
using MacroPanels.ViewModel;
using MacroPanels.Message;
using MacroPanels.Model.MacroFactory;
using MacroPanels.Model.List.Interface;
using MacroPanels.Model.CommandDefinition;
using AutoTool.Model;

namespace AutoTool.ViewModel;

public partial class MacroPanelViewModel : ObservableObject, IDisposable
{
    private readonly INotificationService _notificationService;
    private readonly AutoTool.Services.Interfaces.ILogService _logService;
    private readonly IMacroFactory _macroFactory;
    private readonly ICommandRegistry _commandRegistry;
    private readonly IListPanelViewModel _listPanel;
    private readonly IEditPanelViewModel _editPanel;
    private readonly IButtonPanelViewModel _buttonPanel;
    private readonly ILogPanelViewModel _logPanel;
    private readonly IFavoritePanelViewModel _favoritePanel;
    private CancellationTokenSource? _cts;
    private CommandHistoryManager? _commandHistory;
    private bool _disposed;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _selectedListTabIndex;

    // UI バインディング用のプロパティ（具体型を公開）
    public ListPanelViewModel ListPanelViewModel => (ListPanelViewModel)_listPanel;
    public EditPanelViewModel EditPanelViewModel => (EditPanelViewModel)_editPanel;
    public ButtonPanelViewModel ButtonPanelViewModel => (ButtonPanelViewModel)_buttonPanel;
    public LogPanelViewModel LogPanelViewModel => (LogPanelViewModel)_logPanel;
    public FavoritePanelViewModel FavoritePanelViewModel => (FavoritePanelViewModel)_favoritePanel;

    public MacroPanelViewModel(
        INotificationService notificationService, 
        AutoTool.Services.Interfaces.ILogService logService,
        IMacroFactory macroFactory,
        ICommandRegistry commandRegistry,
        IListPanelViewModel listPanelViewModel,
        IEditPanelViewModel editPanelViewModel,
        IButtonPanelViewModel buttonPanelViewModel,
        ILogPanelViewModel logPanelViewModel,
        IFavoritePanelViewModel favoritePanelViewModel)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _macroFactory = macroFactory ?? throw new ArgumentNullException(nameof(macroFactory));
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _listPanel = listPanelViewModel ?? throw new ArgumentNullException(nameof(listPanelViewModel));
        _editPanel = editPanelViewModel ?? throw new ArgumentNullException(nameof(editPanelViewModel));
        _buttonPanel = buttonPanelViewModel ?? throw new ArgumentNullException(nameof(buttonPanelViewModel));
        _logPanel = logPanelViewModel ?? throw new ArgumentNullException(nameof(logPanelViewModel));
        _favoritePanel = favoritePanelViewModel ?? throw new ArgumentNullException(nameof(favoritePanelViewModel));

        SubscribeToChildViewModelEvents();
        RegisterCommandMessages();
    }

    /// <summary>
    /// CommandHistoryManagerを設定
    /// </summary>
    public void SetCommandHistory(CommandHistoryManager commandHistory)
    {
        _commandHistory = commandHistory;
        _listPanel.SetCommandHistory(commandHistory);
    }

    /// <summary>
    /// 子ViewModelのイベントを購読
    /// </summary>
    private void SubscribeToChildViewModelEvents()
    {
        // ButtonPanelViewModel のイベント
        _buttonPanel.RunRequested += async () =>
        {
            // UIスレッドでパネルの準備と状態設定
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                PrepareAllPanels();
                SetAllPanelsRunningState(true);
            });
            
            await Run();
        };
        _buttonPanel.StopRequested += () =>
        {
            _cts?.Cancel();
            // UIスレッドで状態を更新
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                SetAllPanelsRunningState(false);
            });
        };
        _buttonPanel.SaveRequested += () => _listPanel.Save();
        _buttonPanel.LoadRequested += () =>
        {
            _listPanel.Load();
            _editPanel.SetListCount(_listPanel.GetCount());
            _commandHistory?.Clear();
        };
        _buttonPanel.ClearRequested += HandleClear;
        _buttonPanel.AddRequested += HandleAdd;
        _buttonPanel.UpRequested += HandleUp;
        _buttonPanel.DownRequested += HandleDown;
        _buttonPanel.DeleteRequested += HandleDelete;

        // ListPanelViewModel のイベント
        _listPanel.SelectedItemChanged += item => _editPanel.SetItem(item);
        _listPanel.ItemDoubleClicked += HandleItemDoubleClick;

        // EditPanelViewModel のイベント
        _editPanel.ItemEdited += HandleEdit;
        // RefreshRequestedはOnPropertyChangedで自動的に更新されるため、
        // Refresh()を呼ばない（ToggleSwitchのリセット問題を回避）
        _editPanel.RefreshRequested += () => 
        {
            // 何もしない - アイテムのプロパティ変更はINotifyPropertyChangedで自動通知される
        };
    }

    /// <summary>
    /// コマンド実行関連のメッセージのみ登録（疎結合が必要な部分）
    /// </summary>
    private void RegisterCommandMessages()
    {
        // From Commands（実行中のコマンドからの通知 - 疎結合が必要）
        WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (_, msg) => HandleStartCommand(msg.Command));
        WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, (_, msg) => HandleFinishCommand(msg.Command));
        WeakReferenceMessenger.Default.Register<DoingCommandMessage>(this, (_, msg) => HandleDoingCommand(msg.Command, msg.Detail));
        WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, (_, msg) => HandleUpdateProgress(msg.Command, msg.Progress));

        // グローバルログ
        WeakReferenceMessenger.Default.Register<LogMessage>(this, (_, msg) =>
        {
            _logPanel.WriteLog(msg.Text);
            _logService.Write(msg.Text);
        });
    }

    private void PrepareAllPanels()
    {
        _listPanel.Prepare();
        _editPanel.Prepare();
        _logPanel.Prepare();
        _favoritePanel.Prepare();
        _buttonPanel.Prepare();
    }

    private void SetAllPanelsRunningState(bool isRunning)
    {
        _listPanel.SetRunningState(isRunning);
        _editPanel.SetRunningState(isRunning);
        _favoritePanel.SetRunningState(isRunning);
        _logPanel.SetRunningState(isRunning);
        _buttonPanel.SetRunningState(isRunning);
    }

    private void HandleClear()
    {
        if (_commandHistory != null)
        {
            var clearCommand = new ClearAllCommand(
                _listPanel.CommandList.Items.ToList(),
                () => _listPanel.Clear(),
                RestoreItems
            );
            _commandHistory.ExecuteCommand(clearCommand);
        }
        else
        {
            _listPanel.Clear();
        }
        _editPanel.SetListCount(_listPanel.GetCount());
    }

    private void HandleAdd(string itemType)
    {
        if (_commandHistory != null)
        {
            var newItem = _commandRegistry.CreateCommandItem(itemType);
            if (newItem != null)
            {
                var targetIndex = _listPanel.SelectedLineNumber + 1;
                var addCommand = new AddItemCommand(
                    newItem,
                    targetIndex,
                    (item, index) => _listPanel.InsertAt(index, item),
                    index => _listPanel.RemoveAt(index)
                );
                _commandHistory.ExecuteCommand(addCommand);
            }
        }
        else
        {
            _listPanel.Add(itemType);
        }
        _editPanel.SetListCount(_listPanel.GetCount());
    }

    private void HandleUp()
    {
        var fromIndex = _listPanel.SelectedLineNumber;
        var toIndex = fromIndex - 1;

        if (toIndex >= 0 && _commandHistory != null)
        {
            var moveCommand = new MoveItemCommand(
                fromIndex, toIndex,
                (from, to) => _listPanel.MoveItem(from, to)
            );
            _commandHistory.ExecuteCommand(moveCommand);
        }
        else
        {
            _listPanel.Up();
        }
    }

    private void HandleDown()
    {
        var fromIndex = _listPanel.SelectedLineNumber;
        var toIndex = fromIndex + 1;

        if (toIndex < _listPanel.GetCount() && _commandHistory != null)
        {
            var moveCommand = new MoveItemCommand(
                fromIndex, toIndex,
                (from, to) => _listPanel.MoveItem(from, to)
            );
            _commandHistory.ExecuteCommand(moveCommand);
        }
        else
        {
            _listPanel.Down();
        }
    }

    private void HandleDelete()
    {
        var selectedItem = _listPanel.SelectedItem;
        var selectedIndex = _listPanel.SelectedLineNumber;

        if (selectedItem != null && _commandHistory != null)
        {
            var removeCommand = new RemoveItemCommand(
                selectedItem.Clone(),
                selectedIndex,
                (item, index) => _listPanel.InsertAt(index, item),
                index => _listPanel.RemoveAt(index)
            );
            _commandHistory.ExecuteCommand(removeCommand);
        }
        else
        {
            _listPanel.Delete();
        }
        _editPanel.SetListCount(_listPanel.GetCount());
    }

    private void HandleEdit(ICommandListItem? item)
    {
        if (item == null) return;

            // From EditPanelViewModel
            WeakReferenceMessenger.Default.Register<EditCommandMessage>(this, (sender, message) =>
            {
                var item = (message as EditCommandMessage).Item;
                if (item != null)
                {
                    var oldItem = ListPanelViewModel.SelectedItem;
                    var index = item.LineNumber - 1;
                    
                    // 編集操作をUndoスタックに追加
                    if (oldItem != null && _commandHistory != null)
                    {
                        var editCommand = new EditItemCommand(
                            oldItem, item, index,
                            (editedItem, editIndex) => ListPanelViewModel.ReplaceAt(editIndex, editedItem)
                        );
                        _commandHistory.ExecuteCommand(editCommand);
                    }
                    else
                    {
                        ListPanelViewModel.SetSelectedItem(item);
                        ListPanelViewModel.SetSelectedLineNumber(item.LineNumber - 1);
                    }
                }
            });
            WeakReferenceMessenger.Default.Register<RefreshListViewMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.Refresh();
            });

            // From Commands
            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (sender, message) =>
            {
                var command = (message as StartCommandMessage).Command;
                var lineNumber = command.LineNumber.ToString().PadLeft(2, ' ');
                var commandName = command.GetType().ToString().Split('.').Last().Replace("Command", "").PadRight(20, ' ');

                var settingDict = command.Settings.GetType().GetProperties().ToDictionary(x => x.Name, x => x.GetValue(command.Settings, null));
                var logString = string.Empty;
                foreach (var setting in settingDict)
                {
                    logString += $"({setting.Key} = {setting.Value}), ";
                }

                LogPanelViewModel.WriteLog(lineNumber, commandName, logString);

        _logPanel.WriteLog(lineNumber, commandName, logString);
        _logService.Write(lineNumber, commandName, logString);

        var commandItem = _listPanel.GetItem(command.LineNumber);
        if (commandItem != null)
        {
            commandItem.Progress = 0;
            commandItem.IsRunning = true;
        }
    }

    private void HandleFinishCommand(ICommand command)
    {
        var commandItem = _listPanel.GetItem(command.LineNumber);
        if (commandItem != null)
        {
            commandItem.Progress = 0;
            commandItem.IsRunning = false;
        }
    }

    private void HandleDoingCommand(ICommand command, string detail)
    {
        var lineNumber = command.LineNumber.ToString().PadLeft(2, ' ');
        var commandName = command.GetType().Name.Replace("Command", "").PadRight(20, ' ');
        _logPanel.WriteLog(lineNumber, commandName, detail);
        _logService.Write(lineNumber, commandName, detail);
    }

    private void HandleUpdateProgress(ICommand command, int progress)
    {
        var commandItem = _listPanel.GetItem(command.LineNumber);
        if (commandItem != null)
        {
            commandItem.Progress = progress;
        }
    }

    private void RestoreItems(System.Collections.Generic.IEnumerable<ICommandListItem> items)
    {
        _listPanel.Clear();
        foreach (var item in items)
        {
            _listPanel.AddItem(item.Clone());
        }
        _editPanel.SetListCount(_listPanel.GetCount());
    }

    public async Task Run()
    {
        var listItems = _listPanel.CommandList.Items;
        var macro = _macroFactory.CreateMacro(listItems) as LoopCommand;

        if (macro == null) return;

        try
        {
            _cts = new CancellationTokenSource();
            await macro.Execute(_cts.Token);
        }
        catch (Exception ex)
        {
            if (_cts is { Token.IsCancellationRequested: false })
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _notificationService.ShowError(ex.Message, "Error");
                });
            }
        }
        finally
        {
            // UIスレッドで後処理
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var item in listItems.Where(x => x.IsRunning))
                {
                    item.IsRunning = false;
                }
                foreach (var item in _listPanel.CommandList.Items)
                {
                    item.Progress = 0;
                }

                _cts?.Dispose();
                _cts = null;
                SetRunningState(false);
            });
        }
    }

    public void SetRunningState(bool isRunning)
    {
        IsRunning = isRunning;
        _buttonPanel.SetRunningState(isRunning);
        _editPanel.SetRunningState(isRunning);
        _favoritePanel.SetRunningState(isRunning);
        _listPanel.SetRunningState(isRunning);
        _logPanel.SetRunningState(isRunning);
    }

    public void SaveMacroFile(string filePath) => _listPanel.Save(filePath);

    public void LoadMacroFile(string filePath)
    { 
        _listPanel.Load(filePath); 
        _editPanel.SetListCount(_listPanel.GetCount()); 
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        // コマンド実行関連のメッセージのみ解除
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _cts?.Dispose();
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }
}
