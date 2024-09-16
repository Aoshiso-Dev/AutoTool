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
using System.Collections;
using System.Windows.Data;

namespace Panels.ViewModel
{
    public partial class EditPanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isRunning = false;
        
        #region Item
        private ICommandListItem? _item = new CommandListItem();
        public ICommandListItem? Item
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

        public string ImagePath
        {
            get
            {
                return Item switch
                {
                    IWaitImageItem waitImageItem => waitImageItem.ImagePath,
                    IClickImageItem clickImageItem => clickImageItem.ImagePath,
                    IIfImageExistItem ifImageExistItem => ifImageExistItem.ImagePath,
                    IIfImageNotExistItem ifImageNotExistItem => ifImageNotExistItem.ImagePath,
                    _ => string.Empty,
                };
            }
            set
            {
                switch (Item)
                {
                    case IWaitImageItem waitImageItem:
                        waitImageItem.ImagePath = value;
                        break;
                    case IClickImageItem clickImageItem:
                        clickImageItem.ImagePath = value;
                        break;
                    case IIfImageExistItem ifImageExistItem:
                        ifImageExistItem.ImagePath = value;
                        break;
                    case IIfImageNotExistItem ifImageNotExistItem:
                        ifImageNotExistItem.ImagePath = value;
                        break;
                }

                UpdateItemProperties();
            }
        }

        public double Threshold
        {
            get
            {
                return Item switch
                {
                    IWaitImageItem waitImageItem => waitImageItem.Threshold,
                    IClickImageItem clickImageItem => clickImageItem.Threshold,
                    IIfImageExistItem ifImageExistItem => ifImageExistItem.Threshold,
                    IIfImageNotExistItem ifImageNotExistItem => ifImageNotExistItem.Threshold,
                    _ => 0,
                };
            }
            set
            {
                switch (Item)
                {
                    case IWaitImageItem waitImageItem:
                        waitImageItem.Threshold = value;
                        break;
                    case IClickImageItem clickImageItem:
                        clickImageItem.Threshold = value;
                        break;
                    case IIfImageExistItem ifImageExistItem:
                        ifImageExistItem.Threshold = value;
                        break;
                    case IIfImageNotExistItem ifImageNotExistItem:
                        ifImageNotExistItem.Threshold = value;
                        break;
                }

                UpdateItemProperties();
            }
        }

        public int Timeout
        {
            get
            {
                return Item switch
                {
                    IWaitImageItem waitImageItem => waitImageItem.Timeout,
                    IClickImageItem clickImageItem => clickImageItem.Timeout,
                    IIfImageExistItem ifImageExistItem => ifImageExistItem.Timeout,
                    IIfImageNotExistItem ifImageNotExistItem => ifImageNotExistItem.Timeout,
                    _ => 0,
                };
            }
            set
            {
                switch (Item)
                {
                    case IWaitImageItem waitImageItem:
                        waitImageItem.Timeout = value;
                        break;
                    case IClickImageItem clickImageItem:
                        clickImageItem.Timeout = value;
                        break;
                    case IIfImageExistItem ifImageExistItem:
                        ifImageExistItem.Timeout = value;
                        break;
                    case IIfImageNotExistItem ifImageNotExistItem:
                        ifImageNotExistItem.Timeout = value;
                        break;
                }

                UpdateItemProperties();
            }
        }

        public int Interval
        {
            get
            {
                return Item switch
                {
                    IWaitImageItem waitImageItem => waitImageItem.Interval,
                    IClickImageItem clickImageItem => clickImageItem.Interval,
                    IIfImageExistItem ifImageExistItem => ifImageExistItem.Interval,
                    IIfImageNotExistItem ifImageNotExistItem => ifImageNotExistItem.Interval,
                    _ => 0,
                };
            }
            set
            {
                switch (Item)
                {
                    case IWaitImageItem waitImageItem:
                        waitImageItem.Interval = value;
                        break;
                    case IClickImageItem clickImageItem:
                        clickImageItem.Interval = value;
                        break;
                    case IIfImageExistItem ifImageExistItem:
                        ifImageExistItem.Interval = value;
                        break;
                    case IIfImageNotExistItem ifImageNotExistItem:
                        ifImageNotExistItem.Interval = value;
                        break;
                }
            }
        }

        public System.Windows.Input.MouseButton MouseButton
        {
            get
            {
                return Item switch
                {
                    IClickImageItem clickImageItem => clickImageItem.Button,
                    IClickItem clickItem => clickItem.Button,
                    _ => System.Windows.Input.MouseButton.Left,
                };
            }
            set
            {
                switch (Item)
                {
                    case IClickImageItem clickImageItem:
                        clickImageItem.Button = value;
                        break;
                    case IClickItem clickItem:
                        clickItem.Button = value;
                        break;
                }

                UpdateItemProperties();
            }
        }

        public bool Ctrl
        {
            get
            {
                return Item is IHotkeyItem hotkeyItem ? hotkeyItem.Ctrl : false;
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
                return Item is IHotkeyItem hotkeyItem ? hotkeyItem.Alt : false;
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
                return Item is IHotkeyItem hotkeyItem ? hotkeyItem.Shift : false;
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
                return Item is IHotkeyItem hotkeyItem ? hotkeyItem.Key : System.Windows.Input.Key.Escape;
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
                return Item is IClickItem clickItem ? clickItem.X : 0;
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
                return Item is IClickItem clickItem ? clickItem.Y : 0;
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
                return Item is IWaitItem waitItem ? waitItem.Wait : 0;
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

        public int LoopCount
        {
            get
            {
                return Item is ILoopItem loopItem ? loopItem.LoopCount : 0;

            }
            set
            {
                if (Item is ILoopItem loopItem)
                {
                    loopItem.LoopCount = value;
                }

                UpdateItemProperties();
            }
        }

        #endregion

        #region Visibility
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
        public string SelectedItemType
        {
            get { return Item != null ? Item.ItemType : "None"; }
            set
            {
                if (Item != null)
                {
                    Item.ItemType = value;
                    OnSelectedItemTypeChanged();
                    UpdateItemProperties();
                }
            }
        }

        [ObservableProperty]
        private ObservableCollection<System.Windows.Input.MouseButton> _mouseButtons = new();
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
            if(Item == null)
            {
                return;
            }

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
            VisibilityCommand = Item != null ? Visibility.Visible : Visibility.Collapsed;

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
                VisibilityImagePath = Visibility.Visible;
                VisibilityThreshold = Visibility.Visible;
                VisibilityTimeout = Visibility.Visible;
                VisibilityInterval = Visibility.Visible;
            }
            else if (Item is IIfImageNotExistItem)
            {
                VisibilityImagePath = Visibility.Visible;
                VisibilityThreshold = Visibility.Visible;
                VisibilityTimeout = Visibility.Visible;
                VisibilityInterval = Visibility.Visible;
            }
            else if (Item is IEndIfItem)
            {
            }

        }

        void UpdateItemProperties()
        {
            OnPropertyChanged(nameof(SelectedItemType));
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
            OnPropertyChanged(nameof(LoopCount));

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
                var captureDirectory = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Capture");
                if (!Directory.Exists(captureDirectory))
                {
                    Directory.CreateDirectory(captureDirectory);
                }

                // 現在時間をファイル名として指定する
                var capturePath = System.IO.Path.Combine(captureDirectory, $"{DateTime.Now:yyyyMMddHHmmss}.png");

                // 選択領域をキャプチャ
                var capturedMat = OpenCVHelper.ScreenCaptureHelper.CaptureRegion(captureWindow.SelectedRegion);

                // 指定されたファイル名で保存
                OpenCVHelper.ScreenCaptureHelper.SaveCapture(capturedMat, capturePath);

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

        [RelayCommand]
        public void Apply()
        {
            WeakReferenceMessenger.Default.Send(new ApplyMessage());
        }
    }
}