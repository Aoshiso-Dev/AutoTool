using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Panels.Message;
using System.Collections.ObjectModel;
using Panels.Model.List.Type;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using System.Security.Cryptography.X509Certificates;
using Panels.List.Class;
using Panels.Model.List.Interface;
using Panels.View;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Panels.ViewModel
{
    public partial class EditPanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isRunning = false;
        
        #region Item
        private ICommandListItem _item = new CommandListItem();
        public ICommandListItem Item
        {
            get { return _item; }
            set
            {
                if (SetProperty(ref _item, value))
                {
                    UpdateVisibility();
                    UpdateItemProperties();
                }
            }
        }
        #endregion

        #region ItemProperties

        public bool IsSelected
        {
            get
            {
                return Item.IsSelected;
            }
            set
            {
                Item.IsSelected = value;
            }
        }

        public int LineNumber
        {
            get
            {
                return Item.LineNumber;
            }
            set
            {
                Item.LineNumber = value;
            }
        }

        public string ImagePath
        {
            get
            {
                if (Item is IWaitImageItem waitImageItem)
                {
                    return waitImageItem.ImagePath;
                }
                else if (Item is IClickImageItem clickImageItem)
                {
                    return clickImageItem.ImagePath;
                }
                return string.Empty;
            }
            set
            {
                if (Item is IWaitImageItem waitImageItem)
                {
                    waitImageItem.ImagePath = value;
                }
                else if (Item is IClickImageItem clickImageItem)
                {
                    clickImageItem.ImagePath = value;
                }

                UpdateItemProperties();
            }
        }

        public double Threshold
        {
            get
            {
                if (Item is IWaitImageItem waitImageItem)
                {
                    return waitImageItem.Threshold;
                }
                else if (Item is IClickImageItem clickImageItem)
                {
                    return clickImageItem.Threshold;
                }
                return 0;
            }
            set
            {
                if (Item is IWaitImageItem waitImageItem)
                {
                    waitImageItem.Threshold = value;
                }
                else if (Item is IClickImageItem clickImageItem)
                {
                    clickImageItem.Threshold = value;
                }

                UpdateItemProperties();
            }
        }

        public int Timeout
        {
            get
            {
                if (Item is IWaitImageItem waitImageItem)
                {
                    return waitImageItem.Timeout;
                }
                else if (Item is IClickImageItem clickImageItem)
                {
                    return clickImageItem.Timeout;
                }
                return 0;
            }
            set
            {
                if (Item is IWaitImageItem waitImageItem)
                {
                    waitImageItem.Timeout = value;
                }
                else if (Item is IClickImageItem clickImageItem)
                {
                    clickImageItem.Timeout = value;
                }

                UpdateItemProperties();
            }
        }

        public int Interval
        {
            get
            {
                if (Item is IWaitImageItem waitImageItem)
                {
                    return waitImageItem.Interval;
                }
                else if (Item is IClickImageItem clickImageItem)
                {
                    return clickImageItem.Interval;
                }
                return 0;
            }
            set
            {
                if (Item is IWaitImageItem waitImageItem)
                {
                    waitImageItem.Interval = value;
                }
                else if (Item is IClickImageItem clickImageItem)
                {
                    clickImageItem.Interval = value;
                }
            }
        }

        public System.Windows.Input.MouseButton MouseButton
        {
            get
            {
                if (Item is IClickImageItem clickImageItem)
                {
                    return clickImageItem.Button;
                }
                else if (Item is IClickItem clickItem)
                {
                    return clickItem.Button;
                }

                return System.Windows.Input.MouseButton.Left;
            }
            set
            {
                if (Item is IClickImageItem clickImageItem)
                {
                    clickImageItem.Button = value;
                }
                else if (Item is IClickItem clickItem)
                {
                    clickItem.Button = value;
                }

                UpdateItemProperties();
            }
        }

        public bool Ctrl
        {
            get
            {
                if (Item is IHotkeyItem hotkeyItem)
                {
                    return hotkeyItem.Ctrl;
                }
                return false;
            }
            set
            {
                if (Item is IHotkeyItem hotkeyItem)
                {
                    hotkeyItem.Ctrl = value;
                }

                UpdateItemProperties();
            }
        }

        public bool Alt
        {
            get
            {
                if (Item is IHotkeyItem hotkeyItem)
                {
                    return hotkeyItem.Alt;
                }
                return false;
            }
            set
            {
                if (Item is IHotkeyItem hotkeyItem)
                {
                    hotkeyItem.Alt = value;
                }

                UpdateItemProperties();
            }
        }

        public bool Shift
        {
            get
            {
                if (Item is IHotkeyItem hotkeyItem)
                {
                    return hotkeyItem.Shift;
                }
                return false;
            }
            set
            {
                if (Item is IHotkeyItem hotkeyItem)
                {
                    hotkeyItem.Shift = value;
                }

                UpdateItemProperties();
            }
        }

        public System.Windows.Input.Key Key
        {
            get
            {
                if (Item is IHotkeyItem hotkeyItem)
                {
                    return hotkeyItem.Key;
                }
                return System.Windows.Input.Key.Escape;
            }
            set
            {
                if (Item is IHotkeyItem hotkeyItem)
                {
                    hotkeyItem.Key = value;
                }

                UpdateItemProperties();
            }
        }

        public int X
        {
            get
            {
                if (Item is IClickItem clickItem)
                {
                    return clickItem.X;
                }
                return 0;
            }
            set
            {
                if (Item is IClickItem clickItem)
                {
                    clickItem.X = value;
                }

                UpdateItemProperties();
            }
        }

        public int Y
        {
            get
            {
                if (Item is IClickItem clickItem)
                {
                    return clickItem.Y;
                }
                return 0;
            }
            set
            {
                if (Item is IClickItem clickItem)
                {
                    clickItem.Y = value;
                }

                UpdateItemProperties();
            }
        }

        public int Wait
        {
            get
            {
                if (Item is IWaitItem waitItem)
                {
                    return waitItem.Wait;
                }
                return 0;
            }
            set
            {
                if (Item is IWaitItem waitItem)
                {
                    waitItem.Wait = value;
                }

                UpdateItemProperties();
            }
        }
        #endregion

        #region Visibility
        [ObservableProperty]
        private Visibility _visibilityLineNumber = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility _visibilityCommand = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility _visibilityImagePath = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility _visibilityThreshold = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility _visibilityTimeout = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility _visibilityInterval = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility _visibilityXYPos = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility _visibilityMouseButton = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility _visibilityHotkey = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility _visibilityClick = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility _visibilityWait = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility _visibilityLoop = Visibility.Collapsed;
        #endregion

        #region ComboBox
        [ObservableProperty]
        private ObservableCollection<string> _itemTypes = new();
        public string SelectedItemType {
            get { return Item.ItemType; }
            set 
            { 
                Item.ItemType = value;
                OnSelectedItemTypeChanged();
                UpdateItemProperties();
            } 
        }

        [ObservableProperty]
        private ObservableCollection<System.Windows.Input.MouseButton> _mouseButtons = new();
        private System.Windows.Input.MouseButton _selectedMouseButton = System.Windows.Input.MouseButton.Left;
        public System.Windows.Input.MouseButton SelectedMouseButton
        {
            get
            {
                if (Item is IClickImageItem clickImageItem)
                {
                    return clickImageItem.Button;
                }
                else if (Item is IClickItem clickItem)
                {
                    return clickItem.Button;
                }
                return System.Windows.Input.MouseButton.Left;
            }
            set
            {
                if (Item is IClickImageItem clickImageItem)
                {
                    clickImageItem.Button = value;
                }
                else if (Item is IClickItem clickItem)
                {
                    clickItem.Button = value;

                }
            }
        }
        #endregion


        public EditPanelViewModel()
        {
            foreach(var type in ItemType.GetTypes())
            {
                ItemTypes.Add(type);
            }

            foreach(var button in Enum.GetValues(typeof(System.Windows.Input.MouseButton)).Cast<System.Windows.Input.MouseButton>())
            {
                MouseButtons.Add(button);
            }
        }

        private void OnSelectedItemTypeChanged()
        {
            var lineNumber = Item.LineNumber;
            var isSelected = Item.IsSelected;

            switch (SelectedItemType)
            {
                case nameof(ItemType.WaitImage):
                    Item = new WaitImageItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.WaitImage) };
                    break;
                case nameof(ItemType.ClickImage):
                    Item = new ClickImageItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.ClickImage) };
                    break;
                case nameof(ItemType.Hotkey):
                    Item = new HotkeyItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Hotkey) };
                    break;
                case nameof(ItemType.Click):
                    Item = new ClickItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Click) };
                    break;
                case nameof(ItemType.Wait):
                    Item = new WaitItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Wait) };
                    break;
                case nameof(ItemType.Loop):
                    Item = new LoopItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Loop) };
                    break;
                case nameof(ItemType.EndLoop):
                    Item = new EndLoopItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.EndLoop) };
                    break;
                case nameof(ItemType.Break):
                    Item = new BreakItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Break) };
                    break;
                case nameof(ItemType.IfImageExist):
                    Item = new IfImageExistItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.IfImageExist) };
                    break;
                case nameof(ItemType.IfImageNotExist):
                    Item = new IfImageNotExistItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.IfImageNotExist) };
                    break;
                case nameof(ItemType.EndIf):
                    Item = new EndIfItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.EndIf) };
                    break;
            }
        }

        private void UpdateVisibility()
        {
            if (Item == null)
            {
                return;
            }

            VisibilityLineNumber = Visibility.Visible;
            VisibilityCommand = Visibility.Visible;

            VisibilityImagePath = Visibility.Collapsed;
            VisibilityThreshold = Visibility.Collapsed;
            VisibilityTimeout = Visibility.Collapsed;
            VisibilityInterval = Visibility.Collapsed;
            VisibilityXYPos = Visibility.Collapsed;
            VisibilityMouseButton = Visibility.Collapsed;
            VisibilityHotkey = Visibility.Collapsed;
            VisibilityClick = Visibility.Collapsed;
            VisibilityWait = Visibility.Collapsed;
            VisibilityLoop = Visibility.Collapsed;

            LineNumber = Item.LineNumber;

            if (Item is IWaitImageItem)
            {
                VisibilityImagePath = Visibility.Visible;
                VisibilityThreshold = Visibility.Visible;
                VisibilityTimeout = Visibility.Visible;
                VisibilityInterval = Visibility.Visible;
            }
            else if (Item is IClickImageItem)
            {
                VisibilityImagePath = Visibility.Visible;
                VisibilityThreshold = Visibility.Visible;
                VisibilityTimeout = Visibility.Visible;
                VisibilityInterval = Visibility.Visible;
                VisibilityMouseButton = Visibility.Visible;
            }
            else if (Item is IHotkeyItem)
            {
                VisibilityHotkey = Visibility.Visible;
            }
            else if (Item is IClickItem)
            {
                VisibilityXYPos = Visibility.Visible;
                VisibilityMouseButton = Visibility.Visible;
            }
            else if (Item is IWaitItem)
            {
                VisibilityWait = Visibility.Visible;
            }
            else if (Item is ILoopItem)
            {
                VisibilityLoop = Visibility.Visible;
            }
            else if(Item is IEndLoopItem)
            {
            }
            else if(Item is IBreakItem)
            {
            }
            else if (Item is IIfImageExistItem)
            {
                VisibilityLoop = Visibility.Visible;
            }
            else if (Item is IIfImageNotExistItem)
            {
                VisibilityLoop = Visibility.Visible;
            }
            else if (Item is IEndIfItem)
            {
            }

        }

        void UpdateItemProperties()
        {
            if (Item == null)
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedItemType));
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(LineNumber));
            OnPropertyChanged(nameof(ImagePath));
            OnPropertyChanged(nameof(Threshold));
            OnPropertyChanged(nameof(Timeout));
            OnPropertyChanged(nameof(Interval));
            OnPropertyChanged(nameof(MouseButton));
            OnPropertyChanged(nameof(Ctrl));
            OnPropertyChanged(nameof(Alt));
            OnPropertyChanged(nameof(Shift));
            OnPropertyChanged(nameof(Key));
            OnPropertyChanged(nameof(X));
            OnPropertyChanged(nameof(Y));
            OnPropertyChanged(nameof(Wait));

            WeakReferenceMessenger.Default.Send(new ApplyMessage());
        }

        [RelayCommand]
        public void Browse()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp|All Files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false,
            };

            if (dialog.ShowDialog() == true)
            {
                ImagePath = dialog.FileName;
            }
        }

        [RelayCommand]
        public void Capture()
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

                ImagePath = capturePath;
            }
        }

        [RelayCommand]
        public void PickPoint()
        {
            // キャプチャウィンドウを表示
            var captureWindow = new CaptureWindow();
            captureWindow.Mode = 1; // ポイント選択モード

            if (captureWindow.ShowDialog() == true)
            {
                X = (int)captureWindow.SelectedPoint.X;
                Y = (int)captureWindow.SelectedPoint.Y;
            }
        }
    }
}