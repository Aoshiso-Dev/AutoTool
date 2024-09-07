using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Panels.View;
using Panels.Model;
using Panels.List.Interface;
using Panels.List.Class;
using Panels.Command.Interface;
using Panels.Command.Factory;
using Panels.List.Type;
using System.Windows.Controls;
using System.Windows.Input;


namespace Panels.ViewModel
{
    internal partial class ListPanelViewModel : ObservableObject
    {
        private CancellationTokenSource? _cts = null;

        [ObservableProperty]
        private string _runButtonText = "Run";

        [ObservableProperty]
        private Brush _runButtonColor = Brushes.Green;

        [ObservableProperty]
        private CommandList _commandList = new();

        [ObservableProperty]
        private ObservableCollection<string> _itemTypes = new();

        [ObservableProperty]
        private string _selectedItemType = string.Empty;

        [ObservableProperty]
        private ObservableCollection<MouseButton> _buttons = new();

        [ObservableProperty]
        private int _executedLineNumber = 0;

        public ListPanelViewModel()
        {
            ItemTypes.Add(ItemType.WaitImage);
            ItemTypes.Add(ItemType.ClickImage);
            ItemTypes.Add(ItemType.Click);
            ItemTypes.Add(ItemType.Hotkey);
            ItemTypes.Add(ItemType.Wait);
            ItemTypes.Add(ItemType.Loop);
            ItemTypes.Add(ItemType.EndLoop);
            ItemTypes.Add(ItemType.IfImageExist);
            ItemTypes.Add(ItemType.IfImageNotExist);
            ItemTypes.Add(ItemType.EndIf);

            Buttons.Add(MouseButton.Left);
            Buttons.Add(MouseButton.Right);
            Buttons.Add(MouseButton.Middle);
        }

        [RelayCommand]
        public void Clear() => CommandList.Clear();

        [RelayCommand]
        public void Add()
        {
            switch (SelectedItemType)
            {
                case nameof(ItemType.WaitImage):
                    CommandList.Add(new WaitImageItem { LineNumber = CommandList.Items.Count + 1, ItemType = SelectedItemType, });
                    break;
                case nameof(ItemType.ClickImage):
                    CommandList.Add(new ClickImageItem { LineNumber = CommandList.Items.Count + 1, ItemType = SelectedItemType, });
                    break;
                case nameof(ItemType.Click):
                    CommandList.Add(new ClickItem { LineNumber = CommandList.Items.Count + 1, ItemType = SelectedItemType, });
                    break;
                case nameof(ItemType.Hotkey):
                    CommandList.Add(new HotkeyItem { LineNumber = CommandList.Items.Count + 1, ItemType = SelectedItemType, });
                    break;
                case nameof(ItemType.Wait):
                    CommandList.Add(new WaitItem { LineNumber = CommandList.Items.Count + 1, ItemType = SelectedItemType, });
                    break;
                case nameof(ItemType.Loop):
                    CommandList.Add(new LoopItem { LineNumber = CommandList.Items.Count + 1, ItemType = SelectedItemType, });
                    break;
                case nameof(ItemType.EndLoop):
                    CommandList.Add(new EndLoopItem { LineNumber = CommandList.Items.Count + 1, ItemType = SelectedItemType, });
                    break;
                case nameof(ItemType.IfImageExist):
                     CommandList.Add(new IfImageExistItem { LineNumber = CommandList.Items.Count + 1, ItemType = SelectedItemType, });
                      break;
                case nameof(ItemType.IfImageNotExist):
                    CommandList.Add(new IfImageNotExistItem { LineNumber = CommandList.Items.Count + 1, ItemType = SelectedItemType, });
                    break;
                case nameof(ItemType.EndIf):
                      CommandList.Add(new EndIfItem { LineNumber = CommandList.Items.Count + 1, ItemType = SelectedItemType, });
                    break;
            }
        }

        [RelayCommand]
        public void Up(int lineNumber) => CommandList.Move(lineNumber - 1, lineNumber - 2);

        [RelayCommand]
        public void Down(int lineNumber) => CommandList.Move(lineNumber - 1, lineNumber);

        [RelayCommand]
        public void Delete(int lineNumber)
        {
            var item = CommandList.Items.FirstOrDefault(x => x.LineNumber == lineNumber);
            if (item != null)
            {
                CommandList.Remove(item);
            }
        }

        [RelayCommand]
        public void Save() => CommandList.Save();

        [RelayCommand]
        public void Load() => CommandList.Load();

        [RelayCommand]
        public void Capture(int lineNumber)
        {
            var item = CommandList[lineNumber - 1];

            if (item == null) return;

            if (item is IImageCommandSettings imageItem)
            {
                // キャプチャウィンドウを表示
                var captureWindow = new CaptureWindow();
                captureWindow.Mode = 0; // 選択領域モード

                if (captureWindow.ShowDialog() == true)
                {
                    // 現在時間をファイル名として指定する
                    var capturePath = Path.GetCurrentDirectory() + @"\Capture\" + $"{DateTime.Now:yyyyMMddHHmmss}.png";

                    // 選択領域をキャプチャ
                    var capturedMat = ScreenCaptureHelper.CaptureRegion(captureWindow.SelectedRegion);

                    // 指定されたファイル名で保存
                    ScreenCaptureHelper.SaveCapture(capturedMat, $"{capturePath}");

                    imageItem.ImagePath = capturePath;
                }
            }
        }

        [RelayCommand]
        public void PickPoint(int lineNumber)
        {
            var item = CommandList[lineNumber - 1];

            if (item == null) return;

            if (item is IClickItem clickItem)
            {
                // キャプチャウィンドウを表示
                var captureWindow = new CaptureWindow();
                captureWindow.Mode = 1; // ポイント選択モード

                if (captureWindow.ShowDialog() == true)
                {
                    clickItem.X = (int)captureWindow.SelectedPoint.X;
                    clickItem.Y = (int)captureWindow.SelectedPoint.Y;
                }
            }
        }

        [RelayCommand]
        public void Browse(int lineNumber)
        {
            var item = CommandList[lineNumber - 1];

            if (item == null) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp|All Files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false,
            };

            if (dialog.ShowDialog() == true)
            {
                if (item is IImageCommandSettings imageItem)
                {
                    imageItem.ImagePath = dialog.FileName;
                }
            }
        }

        [RelayCommand]
        public void CheckLeftMouseButton(int lineNumber)
        {
            var item = CommandList[lineNumber - 1];

            if (item == null) return;

            if (item is IClickItem clickItem)
            {
                clickItem.Button = MouseButton.Left;
            }
        }

        [RelayCommand]
        public void CheckRightMouseButton(int lineNumber)
        {
            var item = CommandList[lineNumber - 1];

            if (item == null) return;

            if (item is IClickItem clickItem)
            {
                clickItem.Button = MouseButton.Right;
            }
        }

        [RelayCommand]
        public void CheckMiddleMouseButton(int lineNumber)
        {
            var item = CommandList[lineNumber - 1];

            if (item == null) return;

            if (item is IClickItem clickItem)
            {
                clickItem.Button = MouseButton.Middle;
            }
        }

        [RelayCommand]
        public async Task Run()
        {
            if (!CommandList.Items.Where(x => x.IsRunning == true).Any())
            {
                await RunAsync();
            }
            else
            {
                _cts?.Cancel();
            }
        }

        public async Task RunAsync()
        {

            var macro = MacroFactory.CreateMacro(CommandList.Items, UpdateRunning);

            try
            {
                _cts = new CancellationTokenSource();
                RunButtonText = "Stop";
                RunButtonColor = Brushes.Red;

                await macro.Execute(_cts.Token);
            }
            catch (Exception ex)
            {
                if (_cts != null && !_cts.Token.IsCancellationRequested)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                RunButtonText = "Run";
                RunButtonColor = Brushes.Green;
                CommandList.Items.Where(x => x.IsRunning).ToList().ForEach(x => x.IsRunning = false);
            }
        }

        private void UpdateRunning(object? sender, int lineNumber)
        {
            foreach (var item in CommandList.Items)
            {
                item.IsRunning = false;
            }

            if (lineNumber > 0)
            {
                CommandList.Items[lineNumber - 1].IsRunning = true;
            }
        }
    }
}
