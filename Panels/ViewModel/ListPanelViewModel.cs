using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Panels.View;
using Panels.List.Class;
using System.Windows.Input;
using System.IO;
using Panels.Model.MacroFactory;
using Panels.Model.List.Interface;
using Panels.Model.List.Type;
using Panels.Message;
using CommunityToolkit.Mvvm.Messaging;
using Panels.Model;


namespace Panels.ViewModel
{
    public partial class ListPanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private CommandList _commandList = new();

        //[ObservableProperty]
        private int _executedLineNumber = 0;
        public int ExecutedLineNumber
        {
            get => _executedLineNumber;
            set
            {
                SetProperty(ref _executedLineNumber, value);
                CommandList.Items.ToList().ForEach(x => x.IsRunning = false);
                var cmd = CommandList.Items.Where(x => x.LineNumber == value).FirstOrDefault();
                if (cmd != null)
                {
                    cmd.IsRunning = true;
                }
            }
        }


        public ListPanelViewModel()
        {
        }

        [RelayCommand]
        public void Add(string itemType)
        {
            switch (itemType)
            {
                case nameof(ItemType.WaitImage):
                    CommandList.Add(new WaitImageItem { LineNumber = CommandList.Items.Count + 1, ItemType = itemType, });
                    break;
                case nameof(ItemType.ClickImage):
                    CommandList.Add(new ClickImageItem { LineNumber = CommandList.Items.Count + 1, ItemType = itemType, });
                    break;
                case nameof(ItemType.Click):
                    CommandList.Add(new ClickItem { LineNumber = CommandList.Items.Count + 1, ItemType = itemType, });
                    break;
                case nameof(ItemType.Hotkey):
                    CommandList.Add(new HotkeyItem { LineNumber = CommandList.Items.Count + 1, ItemType = itemType, });
                    break;
                case nameof(ItemType.Wait):
                    CommandList.Add(new WaitItem { LineNumber = CommandList.Items.Count + 1, ItemType = itemType, });
                    break;
                case nameof(ItemType.Loop):
                    CommandList.Add(new LoopItem { LineNumber = CommandList.Items.Count + 1, ItemType = itemType, });
                    break;
                case nameof(ItemType.EndLoop):
                    CommandList.Add(new EndLoopItem { LineNumber = CommandList.Items.Count + 1, ItemType = itemType, });
                    break;
                case nameof(ItemType.Break):
                    CommandList.Add(new BreakItem { LineNumber = CommandList.Items.Count + 1, ItemType = itemType, });
                    break;
                case nameof(ItemType.IfImageExist):
                     CommandList.Add(new IfImageExistItem { LineNumber = CommandList.Items.Count + 1, ItemType = itemType, });
                      break;
                case nameof(ItemType.IfImageNotExist):
                    CommandList.Add(new IfImageNotExistItem { LineNumber = CommandList.Items.Count + 1, ItemType = itemType, });
                    break;
                case nameof(ItemType.EndIf):
                      CommandList.Add(new EndIfItem { LineNumber = CommandList.Items.Count + 1, ItemType = itemType, });
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
        public void Capture(int lineNumber)
        {
            var item = CommandList[lineNumber - 1];

            if (item == null) return;

            // 共通のキャプチャ処理
            string? CaptureAndSaveImage()
            {
                // キャプチャウィンドウを表示
                var captureWindow = new CaptureWindow
                {
                    Mode = 0 // 選択領域モード
                };

                if (captureWindow.ShowDialog() == true)
                {
                    // キャプチャ保存先ディレクトリの存在確認と作成
                    var captureDirectory = System.IO.Path.Combine(Model.Path.GetCurrentDirectory(), "Capture");
                    if (!Directory.Exists(captureDirectory))
                    {
                        Directory.CreateDirectory(captureDirectory);
                    }

                    // 現在時間をファイル名として指定する
                    var capturePath = System.IO.Path.Combine(captureDirectory, $"{DateTime.Now:yyyyMMddHHmmss}.png");

                    // 選択領域をキャプチャ
                    var capturedMat = ScreenCaptureHelper.CaptureRegion(captureWindow.SelectedRegion);

                    // 指定されたファイル名で保存
                    ScreenCaptureHelper.SaveCapture(capturedMat, capturePath);

                    return capturePath;
                }

                return null;
            }

            // IWaitImageCommandSettingsの場合
            if (item is WaitImageItem waitImageItem)
            {
                var capturePath = CaptureAndSaveImage();
                if (capturePath != null)
                {
                    waitImageItem.ImagePath = capturePath;
                }
            }
            // IClickImageCommandSettingsの場合
            else if (item is ClickImageItem clickImageItem)
            {
                var capturePath = CaptureAndSaveImage();
                if (capturePath != null)
                {
                    clickImageItem.ImagePath = capturePath;
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
                if (item is WaitImageItem waitImageItem)
                {
                    waitImageItem.ImagePath = dialog.FileName;
                }
                else if (item is ClickImageItem clickImageItem)
                {
                    clickImageItem.ImagePath = dialog.FileName;
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
            else if(item is IClickImageItem clickImageItem)
            {
                clickImageItem.Button = MouseButton.Left;
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
    }
}
