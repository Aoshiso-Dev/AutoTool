using AutoTool.Commands.Commands;
using AutoTool.Commands.Interface;
using AutoTool.Panels.Model.MacroFactory;

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

        try
        {
            var macro = _macroFactory.CreateMacro(listItems) as LoopCommand;
            if (macro == null)
            {
                throw new InvalidOperationException("マクロの生成に失敗しました。");
            }

            _cts = new CancellationTokenSource();
            await Task.Run(async () => await macro.Execute(_cts.Token), _cts.Token);
        }
        catch (Exception ex)
        {
            _logWriter.Write(ex);
            var isCancellation = _cts is { Token.IsCancellationRequested: true };
            if (!isCancellation)
            {
                var message = BuildUserFriendlyErrorMessage(ex);
                OnUiThread(() => _notifier.ShowError(message, "Error"));
            }
        }
        finally
        {
            OnUiThread(() =>
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
            _ = dispatcher.BeginInvoke(action);
        }
    }

    private static string BuildUserFriendlyErrorMessage(Exception ex)
    {
        var creationError = FindCommandCreationException(ex);
        if (creationError != null)
        {
            var line = creationError.LineNumber.HasValue ? $"（{creationError.LineNumber}行目）" : string.Empty;
            return creationError switch
            {
                PairMismatchException =>
                    $"コマンドの組み合わせが不完全です{line}。\n開始した条件/ループに対応する「終了」コマンドを追加してください。",
                EmptyStructureException =>
                    $"条件またはループの中身が空です{line}。\n中に実行したいコマンドを1つ以上追加してください。",
                UnsupportedCommandTypeException =>
                    $"このコマンドはまだ実行に対応していません{line}。\n別のコマンドを使うか、対応版に更新してください。",
                _ =>
                    $"コマンドの構成に問題があります{line}。\n詳細: {creationError.Message}"
            };
        }

        var rootCause = ex.GetBaseException().Message;
        return ex.Message == rootCause
            ? ex.Message
            : $"{ex.Message}\n原因: {rootCause}";
    }

    private static CommandCreationException? FindCommandCreationException(Exception ex)
    {
        Exception? current = ex;
        while (current != null)
        {
            if (current is CommandCreationException creationException)
            {
                return creationException;
            }
            current = current.InnerException;
        }

        return null;
    }
}
