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

        _logPanel.WriteLog(lineNumber, commandName, logString);
        _logWriter.Write(lineNumber, commandName, logString);

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
        _logWriter.Write(lineNumber, commandName, detail);
    }

    private void HandleUpdateProgress(ICommand command, int progress)
    {
        var commandItem = _listPanel.GetItem(command.LineNumber);
        if (commandItem != null)
        {
            commandItem.Progress = progress;
        }
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
}
