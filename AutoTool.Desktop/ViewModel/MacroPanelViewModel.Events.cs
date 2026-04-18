using System.Collections.Generic;
using AutoTool.Commands.Services;
using AutoTool.Commands.Threading;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Desktop.ViewModel;

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
        _favoritePanel.AddRequested += HandleFavoriteAddRequested;
        _favoritePanel.DeleteRequested += HandleFavoriteDeleteRequested;
        _favoritePanel.LoadRequested += HandleFavoriteLoadRequested;
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
        _favoritePanel.AddRequested -= HandleFavoriteAddRequested;
        _favoritePanel.DeleteRequested -= HandleFavoriteDeleteRequested;
        _favoritePanel.LoadRequested -= HandleFavoriteLoadRequested;
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
        _commandEventSubscriptionCts?.Cancel();
        _commandEventSubscriptionCts?.Dispose();
        _lastObservedDroppedCommandEvents = _commandEventBus.DroppedEventCount;
        _commandEventSubscriptionCts = new();
        _commandEventSubscriptionTask = ConsumeCommandEventsAsync(_commandEventSubscriptionCts.Token);
    }

    private void UnsubscribeCommandEventHandlers()
    {
        _commandEventSubscriptionCts?.Cancel();
    }

    private async Task ConsumeCommandEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var ev in _commandEventBus.ReadEventsAsync().ConfigureAwaitFalse(cancellationToken))
            {
                NotifyDroppedCommandEventsIfNeeded();

                switch (ev.Kind)
                {
                    case CommandEventKind.Started:
                        HandleStartCommand(ev.Command);
                        break;
                    case CommandEventKind.Finished:
                        HandleFinishCommand(ev.Command);
                        break;
                    case CommandEventKind.Doing:
                        HandleDoingCommand(ev.Command, ev.Detail, ev.Payload);
                        break;
                    case CommandEventKind.ProgressUpdated:
                        HandleUpdateProgress(ev.Command, ev.Progress);
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void NotifyDroppedCommandEventsIfNeeded()
    {
        var dropped = _commandEventBus.DroppedEventCount;
        if (dropped <= _lastObservedDroppedCommandEvents)
        {
            return;
        }

        var delta = dropped - _lastObservedDroppedCommandEvents;
        _lastObservedDroppedCommandEvents = dropped;

        var detail = $"警告: コマンドイベントの取りこぼしが発生しました (+{delta}, 累計 {dropped})";
        _logWriter.WriteStructured(
            "CommandEventBus",
            "DropDetected",
            new Dictionary<string, object?>
            {
                ["DroppedTotal"] = dropped,
                ["DroppedDelta"] = delta,
                ["SubscriberCount"] = _commandEventBus.SubscriberCount
            });

        OnUiThread(() => _logPanel.WriteLog(string.Empty, "システム", detail));
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
}

