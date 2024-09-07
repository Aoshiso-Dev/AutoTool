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
using Panels.List;
using Panels.Command.Interface;
using Panels.Command.Factory;


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

        public ListPanelViewModel()
        {
            ItemTypes.Add(ItemType.WaitImage);
            ItemTypes.Add(ItemType.ClickImage);
            ItemTypes.Add(ItemType.Click);
            ItemTypes.Add(ItemType.Hotkey);
            ItemTypes.Add(ItemType.Wait);
            ItemTypes.Add(ItemType.Loop);
            ItemTypes.Add(ItemType.EndLoop);
            //ItemTypes.Add(ItemType.If);
            //ItemTypes.Add(ItemType.EndIf);
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
                    //case nameof(ItemType.If):
                    //    CommandList.Add(new IfItem { LineNumber = CommandList.Items.Count + 1, ItemType = SelectedItemType, });
                    //    break;
                    //case nameof(ItemType.EndIf):
                    //    CommandList.Add(new EndIfItem { LineNumber = CommandList.Items.Count + 1, ItemType = SelectedItemType, });
                    //    break;
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

            // 現在時間をファイル名として指定する
            var capturePath = Path.GetCurrentDirectory() + @"\Capture\" + $"{DateTime.Now:yyyyMMddHHmmss}.png";

            var captureWindow = new CaptureWindow { FileName = capturePath };

            if (captureWindow.ShowDialog() == true)
            {
                if (item is IImageCommandSettings imageItem)
                {
                    imageItem.ImagePath = capturePath;
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
        public void Run()
        {
            try {
                if (_cts == null)
                {
                    _cts = new CancellationTokenSource();
                    RunButtonText = "Stop";
                    RunButtonColor = Brushes.Red;
                    RunAsync(_cts.Token);
                }
                else
                {
                    _cts.Cancel();
                    _cts = null;
                    RunButtonText = "Run";
                    RunButtonColor = Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            //CommandList.CalcurateNestLevel();
            var macro = MacroFactory.CreateMacro(CommandList.Items);

            try
            {
                macro.Execute(cancellationToken);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _cts = null;
                RunButtonText = "Run";
                RunButtonColor = Brushes.Green;
            }
        }
    }
}
