using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Panels.Define;
using Panels.View;
using Panels.Model;

namespace Panels.ViewModel
{
    internal partial class ListPanelViewModel : ObservableObject
    {
        private CancellationTokenSource? _cts;

        [ObservableProperty]
        private string _runButtonText = "Run";

        [ObservableProperty]
        private Brush _runButtonColor = Brushes.Green;

        [ObservableProperty]
        private ObservableCollection<CommandItem> _items = new();

        public ListPanelViewModel()
        {
            Items = new ObservableCollection<CommandItem>();
        }

        [RelayCommand]
        public void Clear() => Items.Clear();

        [RelayCommand]
        public void Add() => Items.Add(new CommandItem { LineNumber = Items.Count + 1 });

        [RelayCommand]
        public void Up(int lineNumber) => MoveItem(lineNumber, -1);

        [RelayCommand]
        public void Down(int lineNumber) => MoveItem(lineNumber, 1);

        [RelayCommand]
        public void Delete(int lineNumber)
        {
            var item = Items.FirstOrDefault(x => x.LineNumber == lineNumber);
            if (item != null)
            {
                Items.Remove(item);
                ReorderItems();
            }
        }

        [RelayCommand]
        public void Save() => ExecuteFileDialogOperation("保存", filename =>
        {
            JsonSerializerHelper.SerializeToFile(Items, filename);
        });

        [RelayCommand]
        public void Load() => ExecuteFileDialogOperation("読込", filename =>
        {
            Items = JsonSerializerHelper.DeserializeFromFile<ObservableCollection<CommandItem>>(filename);
            ReorderItems();
        });

        [RelayCommand]
        public void Browse(int lineNumber)
        {
            var item = Items.FirstOrDefault(x => x.LineNumber == lineNumber);
            if (item == null) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG|All files (*.*)|*.*",
                Title = "画像ファイルを選択してください",
                RestoreDirectory = true,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                item.ImagePath = dialog.FileName;
            }
        }

        [RelayCommand]
        public void Capture(int lineNumber)
        {
            var item = Items.FirstOrDefault(x => x.LineNumber == lineNumber);
            if (item == null) return;


            // 現在時間をファイル名として指定する
            var capturePath = Path.GetCurrentDirectory() + @"\Capture\" + $"{DateTime.Now:yyyyMMddHHmmss}.png";

            var captureWindow = new CaptureWindow { FileName = capturePath };
            bool? result = captureWindow.ShowDialog();

            if (result == true)
            {
                item.ImagePath = captureWindow.FileName;
            }
        }

            [RelayCommand]
        public void Run()
        {
            if (_cts != null)
            {
                StopExecution();
            }
            else
            {
                StartExecution();
            }
        }

        private void StartExecution()
        {
            foreach(var item in Items)
            {
                if (!item.CanExecute())
                {
                    MessageBox.Show($"実行できないコマンドがあります。行番号: {item.LineNumber}");
                    return;
                }
            }

            _cts = new CancellationTokenSource();
            UpdateRunButton("Stop", Brushes.Red);

            foreach (var item in Items)
            {
                item.StopEvent += StopExecution;
            }

            Task.Run(() => ExecuteCommandsAsync(_cts.Token), _cts.Token);
        }

        private async Task ExecuteCommandsAsync(CancellationToken token)
        {
            try
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    if (token.IsCancellationRequested) break;

                    if (item.CommandType == CommandType.LoopStart)
                    {
                        i = await ExecuteLoopAsync(i, token);
                    }
                    else if (!await ExecuteCommandWithTimeoutAsync(item, token))
                    {
                        if (item.TimeOutStop)
                        {
                            Application.Current.Dispatcher.Invoke(() => UpdateRunButton("Run", Brushes.Orange));
                            return;
                        }
                    }

                    await Task.Delay(100, token);
                }
            }
            finally
            {
                StopExecution();
            }
        }

        private async Task<int> ExecuteLoopAsync(int startIndex, CancellationToken token)
        {
            var loopItem = Items[startIndex];
            var endIndex = FindLoopEndIndex(startIndex);
            if (endIndex == -1)
            {
                StopExecutionWithMessage("LoopEnd が見つかりませんでした。");
                return startIndex;
            }

            for (int i = 0; i < loopItem.LoopCount; i++)
            {
                for (int j = startIndex + 1; j < endIndex; j++)
                {
                    var item = Items[j];
                    if (token.IsCancellationRequested) break;

                    if (!await ExecuteCommandWithTimeoutAsync(item, token))
                    {
                        if (item.TimeOutStop)
                        {
                            Application.Current.Dispatcher.Invoke(() => UpdateRunButton("Run", Brushes.Orange));
                            return endIndex;
                        }
                    }

                    await Task.Delay(100, token);
                }
            }

            return endIndex; // ループが終了した後に、endIndexに移動
        }

        private int FindLoopEndIndex(int startIndex)
        {
            for (int i = startIndex + 1; i < Items.Count; i++)
            {
                if (Items[i].CommandType == CommandType.LoopEnd)
                {
                    return i;
                }
            }
            return -1; // LoopEnd が見つからなかった場合
        }

        private async Task<bool> ExecuteCommandWithTimeoutAsync(CommandItem item, CancellationToken token)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

            if (item.TimeOut > 0)
            {
                //var timeout = TimeSpan.FromMilliseconds(item.TimeOut);
                cts.CancelAfter(item.TimeOut);
            }

            try
            {
                await item.ExecuteAsync(Items, cts.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private void StopExecution()
        {
            _cts?.Cancel();
            _cts = null;

            foreach (var item in Items)
            {
                item.StopEvent -= StopExecution;
                item.IsRunning = false;
            }
            
            Application.Current.Dispatcher.Invoke(() => UpdateRunButton("Run", Brushes.Green));
        }

        private void StopExecutionWithMessage(string message)
        {
            StopExecution();
            MessageBox.Show(message);
        }

        private void MoveItem(int lineNumber, int offset)
        {
            var item = Items.FirstOrDefault(x => x.LineNumber == lineNumber);
            if (item == null) return;

            var currentIndex = Items.IndexOf(item);
            var newIndex = currentIndex + offset;

            if (newIndex >= 0 && newIndex < Items.Count)
            {
                Items.Move(currentIndex, newIndex);
                ReorderItems();
            }
        }

        private void ReorderItems()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].LineNumber = i + 1;
            }
        }

        private void UpdateRunButton(string text, Brush color)
        {
            RunButtonText = text;
            RunButtonColor = color;
        }

        private void ExecuteFileDialogOperation(string action, Action<string> fileOperation)
        {
            switch (action)
            {
                case "保存":
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "Macro Files(*.macro)|*.macro|All files (*.*)|*.*",
                        Title = "ファイルを保存",
                        RestoreDirectory = true
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        fileOperation(saveFileDialog.FileName);
                    }
                    break;

                case "読込":
                    var openFileDialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Filter = "Macro Files(*.macro)|*.macro|All files (*.*)|*.*",
                        Title = "ファイルを読み込む",
                        RestoreDirectory = true,
                        CheckFileExists = true
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        fileOperation(openFileDialog.FileName);
                    }
                    break;
            }
        }
    }
}
