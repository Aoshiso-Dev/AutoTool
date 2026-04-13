using AutoTool.Commands.Commands;
using AutoTool.Commands.Interface;

namespace AutoTool.ViewModel;

public partial class MacroPanelViewModel
{
    private void HandleStartCommand(ICommand command)
    {
        var lineNumber = command.LineNumber.ToString().PadLeft(2, ' ');
        var commandName = command.GetType().Name.Replace("Command", "").PadRight(20, ' ');

        var settingDict = command.Settings.GetType().GetProperties()
            .ToDictionary(x => x.Name, x => x.GetValue(command.Settings, null));
        var logString = string.Join(", ", settingDict.Select(s => $"({s.Key} = {s.Value})"));

        _logWriter.Write(lineNumber, commandName, logString);
        OnUiThread(() =>
        {
            _logPanel.WriteLog(lineNumber, commandName, logString);
            var commandItem = _listPanel.GetItem(command.LineNumber);
            if (commandItem != null)
            {
                commandItem.Progress = 0;
                commandItem.IsRunning = true;
            }
        });
    }

    private void HandleFinishCommand(ICommand command)
    {
        OnUiThread(() =>
        {
            var commandItem = _listPanel.GetItem(command.LineNumber);
            if (commandItem != null)
            {
                commandItem.Progress = 0;
                commandItem.IsRunning = false;
            }
        });
    }

    private void HandleDoingCommand(ICommand command, string detail)
    {
        var lineNumber = command.LineNumber.ToString().PadLeft(2, ' ');
        var commandName = command.GetType().Name.Replace("Command", "").PadRight(20, ' ');
        _logWriter.Write(lineNumber, commandName, detail);
        OnUiThread(() => _logPanel.WriteLog(lineNumber, commandName, detail));
    }

    private void HandleUpdateProgress(ICommand command, int progress)
    {
        OnUiThread(() =>
        {
            var commandItem = _listPanel.GetItem(command.LineNumber);
            if (commandItem != null)
            {
                commandItem.Progress = progress;
            }
        });
    }

    public async Task Run()
    {
        var listItems = _listPanel.CommandList.Items;
        var macro = _macroFactory.CreateMacro(listItems) as LoopCommand;

        if (macro == null)
        {
            OnUiThread(() => SetRunningState(false));
            return;
        }

        try
        {
            _cts = new CancellationTokenSource();
            await macro.Execute(_cts.Token);
        }
        catch (Exception ex)
        {
            _logWriter.Write(ex);
            if (_cts is { Token.IsCancellationRequested: false })
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _notifier.ShowError(ex.Message, "Error");
                });
            }
        }
        finally
        {
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

    private static void OnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }
}
