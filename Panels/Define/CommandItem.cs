using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Panels.Define
{
    [Serializable]
    public partial class CommandItem : ObservableObject
    {
        public event Action? StopEvent;

        [ObservableProperty]
        private ObservableCollection<CommandType> _commandTypes;

        [ObservableProperty]
        private CommandType _commandType;

        [ObservableProperty]
        private int _lineNumber;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private string _imagePath = string.Empty;

        [ObservableProperty]
        private int _timeOut;

        [ObservableProperty]
        private bool _timeOutStop;

        [ObservableProperty]
        private int _waitTime;

        [ObservableProperty]
        private bool _ctrl;

        [ObservableProperty]
        private bool _alt;

        [ObservableProperty]
        private bool _shift;

        [ObservableProperty]
        private Key _key = Key.D1;

        [ObservableProperty]
        private Func<bool>? _condition;

        [ObservableProperty]
        private Action? _doAction;

        [ObservableProperty]
        private Action? _onImageNotFoundAction;

        // ループ設定
        [ObservableProperty]
        private int _loopCount;

        [ObservableProperty]
        private ObservableCollection<ConditionType> _conditionTypes;

        [ObservableProperty]
        private ConditionType _conditionType;

        [ObservableProperty]
        private bool _isSkipped;

        public CommandItem()
        {
            CommandTypes = new ObservableCollection<CommandType>(Enum.GetValues(typeof(CommandType)).Cast<CommandType>());
            CommandType = CommandTypes.FirstOrDefault();
            ConditionTypes = new ObservableCollection<ConditionType>(Enum.GetValues(typeof(ConditionType)).Cast<ConditionType>());
            ConditionType = ConditionTypes.FirstOrDefault();
            LoopCount = 1;
        }

        public async Task ExecuteAsync(ObservableCollection<CommandItem> allCommands, CancellationToken token)
        {
            if (_isSkipped)
            {
                IsRunning = false;
                return; // スキップされたコマンドは実行しない
            }

            IsRunning = true;

            try
            {
                if (CommandType == CommandType.If)
                {
                    await ExecuteIfBlockAsync(allCommands, token);
                }
                else
                {
                    var task = CommandType switch
                    {
                        CommandType.ClickImage => ExecuteClickImageCommandAsync(token),
                        CommandType.WaitImage => ExecuteWaitImageCommandAsync(token),
                        CommandType.Hotkey => Task.Run(() => ExecuteHotkeyCommand(), token),
                        CommandType.Wait => ExecuteWaitCommandAsync(token),
                        CommandType.LoopStart => ExecuteLoopAsync(allCommands, token),
                        CommandType.EndIf => ExecuteEndIfAsync(allCommands, token),
                        _ => Task.CompletedTask
                    };

                    if (await Task.WhenAny(task, Task.Delay(_timeOut, token)) == task)
                    {
                        await task;
                    }
                    else if (_timeOutStop)
                    {
                        StopExecutionWithMessage("タイムアウトにより処理が停止されました。");
                    }
                }

                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                // タイムアウトが発生しても実行を続ける
            }
            finally
            {
                IsRunning = false;
            }
        }


        // IFブロックの実行メソッド
        private async Task ExecuteIfBlockAsync(ObservableCollection<CommandItem> allCommands, CancellationToken token)
        {
            bool conditionResult = await EvaluateConditionAsync();

            if (conditionResult)
            {
                int startIndex = allCommands.IndexOf(this);
                int endIndex = FindEndIfIndex(allCommands, startIndex);

                if (endIndex == -1)
                {
                    StopExecutionWithMessage("ENDIF が見つかりませんでした。");
                    return;
                }

                for (int i = startIndex + 1; i < endIndex; i++)
                {
                    await allCommands[i].ExecuteAsync(allCommands, token);
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            else
            {
                // IF条件が満たされない場合、ENDIFまでスキップ
                int startIndex = allCommands.IndexOf(this);
                int endIndex = FindEndIfIndex(allCommands, startIndex);
                if (endIndex == -1)
                {
                    StopExecutionWithMessage("ENDIF が見つかりませんでした。");
                    return;
                }

                // EndIfまでのコマンドをスキップ
                for (int i = startIndex + 1; i < endIndex; i++)
                {
                    allCommands[i].IsSkipped = true; // フラグを設定してスキップ
                }
            }
        }

        private async Task ExecuteEndIfAsync(ObservableCollection<CommandItem> allCommands, CancellationToken token)
        {
            int endIndex = allCommands.IndexOf(this);

            // EndIfの次のコマンドからスキップフラグを解除
            if (endIndex + 1 < allCommands.Count)
            {
                allCommands[endIndex + 1].IsSkipped = false;
            }

            await Task.CompletedTask;
        }


        // 条件評価メソッド
        private async Task<bool> EvaluateConditionAsync()
        {
            switch (ConditionType)
            {
                case ConditionType.ImageExists:
                    return await CheckImageExistsAsync();
                case ConditionType.ImageNotExists:
                    return !await CheckImageExistsAsync();
                default:
                    return false;
            }
        }

        private async Task<bool> CheckImageExistsAsync()
        {
            try
            {
                var point = await ImageFinder.WaitForImageAsync(_imagePath, 0.8, _timeOut, _waitTime, CancellationToken.None);
                return point != null;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private int FindEndIfIndex(ObservableCollection<CommandItem> allCommands, int startIndex)
        {
            for (int i = startIndex + 1; i < allCommands.Count; i++)
            {
                if (allCommands[i].CommandType == CommandType.EndIf)
                {
                    return i;
                }
            }
            return -1; // EndIf が見つからなかった場合
        }

        private void StopExecutionWithMessage(string message)
        {
            StopEvent?.Invoke();
            System.Windows.MessageBox.Show($"{message} 行番号: {_lineNumber}");
        }

        public bool CanExecute()
        {
            if(_commandType ==CommandType.If)
            {
                return ConditionType switch
                {
                    ConditionType.ImageExists => File.Exists(_imagePath),
                    ConditionType.ImageNotExists => !File.Exists(_imagePath),
                    _ => false,
                };
            }
            else
            { 
                return _commandType switch
                {
                    CommandType.WaitImage or CommandType.ClickImage => File.Exists(_imagePath) && _waitTime > 0 && _timeOut >= 0,
                    CommandType.Hotkey => _key != Key.None,
                    CommandType.Wait => _waitTime > 0,
                    CommandType.LoopStart => LoopCount > 0,
                    CommandType.LoopEnd => true,
                    CommandType.EndIf => true,
                    _ => false,
                };
            }
        }

        private async Task ExecuteClickImageCommandAsync(CancellationToken token)
        {
            await ImageFinder.ClickImageAsync(_imagePath, 0.8, _timeOut, _waitTime, token);

            /*
            var point = await ImageFinder.WaitForImageAsync(_imagePath, 0.8, _timeOut, _waitTime, token);
            if (point == null)
            {
                _onImageNotFoundAction?.Invoke();

                if (_timeOutStop)
                {
                    StopExecutionWithMessage("タイムアウトにより処理が停止されました。");
                }
            }
            else
            {
                MouseControlHelper.Click(point.Value.X, point.Value.Y);
            }
            */
        }

        private async Task ExecuteWaitImageCommandAsync(CancellationToken token)
        {
            var point = await ImageFinder.WaitForImageAsync(_imagePath, 0.8, _timeOut, _waitTime, token);
            if (point == null)
            {
                _onImageNotFoundAction?.Invoke();

                if (_timeOutStop)
                {
                    StopExecutionWithMessage("タイムアウトにより処理が停止されました。");
                }
            }
        }

        private void ExecuteHotkeyCommand()
        {
            KeyControlHelper.KeyPress(_key, _ctrl, _alt, _shift);
        }

        private async Task ExecuteWaitCommandAsync(CancellationToken token)
        {
             WaitHelper.Wait(WaitTime, token);
        }

        private async Task ExecuteConditionalCommandAsync(CancellationToken token)
        {
            if (_condition?.Invoke() == true)
            {
                _doAction?.Invoke();
            }
            else
            {
                StopExecutionWithMessage("条件が満たされていません。");
            }
        }

        private async Task ExecuteLoopAsync(ObservableCollection<CommandItem> allCommands, CancellationToken token)
        {
            var startIndex = allCommands.IndexOf(this);
            var endIndex = FindLoopEndIndex(allCommands, startIndex);

            if (endIndex == -1)
            {
                StopExecutionWithMessage("LoopEnd が見つかりませんでした。");
                return;
            }

            for (int i = 0; i < LoopCount; i++)
            {
                for (int j = startIndex + 1; j < endIndex; j++)
                {
                    await allCommands[j].ExecuteAsync(allCommands, token);
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }

        private int FindLoopEndIndex(ObservableCollection<CommandItem> allCommands, int startIndex)
        {
            for (int i = startIndex + 1; i < allCommands.Count; i++)
            {
                if (allCommands[i].CommandType == CommandType.LoopEnd)
                {
                    return i;
                }
            }
            return -1; // LoopEnd が見つからなかった場合
        }
    }
}
