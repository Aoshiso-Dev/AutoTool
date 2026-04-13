using AutoTool.Commands.Services;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.ViewModel;

public partial class MacroPanelViewModel
{
    private void SubscribeToChildViewModelEvents()
    {
        _buttonPanel.RunRequested += HandleRunRequestedAsync;
        _buttonPanel.StopRequested += HandleStopRequested;
        _buttonPanel.SaveRequested += HandleSaveRequested;
        _buttonPanel.LoadRequested += HandleLoadRequested;
        _buttonPanel.ClearRequested += HandleClear;
        _buttonPanel.AddRequested += HandleAdd;
        _buttonPanel.UpRequested += HandleUp;
        _buttonPanel.DownRequested += HandleDown;
        _buttonPanel.DeleteRequested += HandleDelete;

        _listPanel.SelectedItemChanged += HandleSelectedItemChanged;
        _listPanel.ItemDoubleClicked += HandleItemDoubleClick;

        _editPanel.ItemEdited += HandleEdit;
    }

    private void UnsubscribeFromChildViewModelEvents()
    {
        _buttonPanel.RunRequested -= HandleRunRequestedAsync;
        _buttonPanel.StopRequested -= HandleStopRequested;
        _buttonPanel.SaveRequested -= HandleSaveRequested;
        _buttonPanel.LoadRequested -= HandleLoadRequested;
        _buttonPanel.ClearRequested -= HandleClear;
        _buttonPanel.AddRequested -= HandleAdd;
        _buttonPanel.UpRequested -= HandleUp;
        _buttonPanel.DownRequested -= HandleDown;
        _buttonPanel.DeleteRequested -= HandleDelete;

        _listPanel.SelectedItemChanged -= HandleSelectedItemChanged;
        _listPanel.ItemDoubleClicked -= HandleItemDoubleClick;
        _editPanel.ItemEdited -= HandleEdit;
    }

    private async Task HandleRunRequestedAsync()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            PrepareAllPanels();
            SetAllPanelsRunningState(true);
        });

        await Run();
    }

    private void HandleStopRequested()
    {
        _cts?.Cancel();
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            SetAllPanelsRunningState(false);
        });
    }

    private void HandleSaveRequested() => _listPanel.Save();

    private void HandleLoadRequested()
    {
        _listPanel.Load();
        _editPanel.SetListCount(_listPanel.GetCount());
        _commandHistory?.Clear();
    }

    private void HandleSelectedItemChanged(ICommandListItem? item)
    {
        _editPanel.SetItem(item);
    }

    private void RegisterCommandEventHandlers()
    {
        _commandEventBus.Started += OnCommandStarted;
        _commandEventBus.Finished += OnCommandFinished;
        _commandEventBus.Doing += OnCommandDoing;
        _commandEventBus.ProgressUpdated += OnCommandProgressUpdated;
    }

    private void UnsubscribeCommandEventHandlers()
    {
        _commandEventBus.Started -= OnCommandStarted;
        _commandEventBus.Finished -= OnCommandFinished;
        _commandEventBus.Doing -= OnCommandDoing;
        _commandEventBus.ProgressUpdated -= OnCommandProgressUpdated;
    }

    private void OnCommandStarted(object? sender, CommandEventArgs args) => HandleStartCommand(args.Command);
    private void OnCommandFinished(object? sender, CommandEventArgs args) => HandleFinishCommand(args.Command);
    private void OnCommandDoing(object? sender, CommandLogEventArgs args) => HandleDoingCommand(args.Command, args.Detail);
    private void OnCommandProgressUpdated(object? sender, CommandProgressEventArgs args) => HandleUpdateProgress(args.Command, args.Progress);

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
}
