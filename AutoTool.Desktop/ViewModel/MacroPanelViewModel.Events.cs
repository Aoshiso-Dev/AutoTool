using AutoTool.Commands.Services;

namespace AutoTool.ViewModel;

public partial class MacroPanelViewModel
{
    private void SubscribeToChildViewModelEvents()
    {
        _buttonPanel.RunRequested += async () =>
        {
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

        _listPanel.SelectedItemChanged += item => _editPanel.SetItem(item);
        _listPanel.ItemDoubleClicked += HandleItemDoubleClick;

        _editPanel.ItemEdited += HandleEdit;
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
