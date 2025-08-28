using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using MacroPanels.Message;
using System.Collections.ObjectModel;
using MacroPanels.Model.List.Type;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using System.Security.Cryptography.X509Certificates;
using MacroPanels.List.Class;
using MacroPanels.Model.List.Interface;
using MacroPanels.View;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Collections;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

using ColorPickHelper;

namespace MacroPanels.ViewModel
{
    public partial class EditPanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isRunning = false;

        private bool _isUpdating;

        // RefreshList のデバウンス用
        private readonly DispatcherTimer _refreshTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };

        #region Item
        private ICommandListItem? _item = null;
        public ICommandListItem? Item
        {
            get => _item;
            set
            {
                SetProperty(ref _item, value);

                UpdateProperties();
                UpdateIsProperties();
            }
        }
        #endregion

        #region ListCount
        private int _listCount = 0;
        public int ListCount
        {
            get => _listCount;
            set
            {
                SetProperty(ref _listCount, value);

                UpdateIsProperties();
                UpdateProperties();
            }
        }
        #endregion

        #region IsProperties
        public bool IsListNotEmpty => ListCount > 0;
        public bool IsNotNullItem => Item != null;
        public bool IsWaitImageItem => Item is WaitImageItem;
        public bool IsClickImageItem => Item is ClickImageItem;
        public bool IsHotkeyItem => Item is HotkeyItem;
        public bool IsClickItem => Item is ClickItem;
        public bool IsWaitItem => Item is WaitItem;
        public bool IsLoopItem => Item is LoopItem;
        public bool IsEndLoopItem => Item is EndLoopItem;
        public bool IsBreakItem => Item is BreakItem;
        public bool IsIfImageExistItem => Item is IfImageExistItem;
        public bool IsIfImageNotExistItem => Item is IfImageNotExistItem;
        public bool IsIfImageExistAIItem => Item is IfImageExistAIItem;
        public bool IsIfImageNotExistAIItem => Item is IfImageNotExistAIItem;
        public bool IsEndIfItem => Item is EndIfItem;
        public bool IsExecuteProgramItem => Item is ExecuteProgramItem;
        #endregion

        #region Properties

        public string WindowTitleText
        {
            get => string.IsNullOrEmpty(WindowTitle) ? "指定なし" : WindowTitle;
        }

        public string WindowTitle
        {
            get
            {
                return Item switch
                {
                    IWaitImageItem waitImageItem => waitImageItem.WindowTitle,
                    IClickImageItem clickImageItem => clickImageItem.WindowTitle,
                    IHotkeyItem hotkeyItem => hotkeyItem.WindowTitle,
                    //IClickItem clickItem => clickItem.WindowTitle,
                    IIfImageExistItem ifImageExistItem => ifImageExistItem.WindowTitle,
                    IIfImageNotExistItem ifImageNotExistItem => ifImageNotExistItem.WindowTitle,
                    IIfImageExistAIItem ifImageExistAIItem => ifImageExistAIItem.WindowTitle,
                    IIfImageNotExistAIItem ifImageNotExistAIItem => ifImageNotExistAIItem.WindowTitle,
                    _ => string.Empty,
                };
            }
            set
            {
                switch (Item)
                {
                    case IWaitImageItem waitImageItem:
                        waitImageItem.WindowTitle = value;
                        break;
                    case IClickImageItem clickImageItem:
                        clickImageItem.WindowTitle = value;
                        break;
                    case IHotkeyItem hotkeyItem:
                        hotkeyItem.WindowTitle = value;
                        break;
                    //case IClickItem clickItem:
                    //    clickItem.WindowTitle = value;
                    //    break;
                    case IIfImageExistItem ifImageExistItem:
                        ifImageExistItem.WindowTitle = value;
                        break;
                    case IIfImageNotExistItem ifImageNotExistItem:
                        ifImageNotExistItem.WindowTitle = value;
                        break;
                    case IIfImageExistAIItem ifImageExistAIItem:
                        ifImageExistAIItem.WindowTitle = value;
                        break;
                    case IIfImageNotExistAIItem ifImageNotExistAIItem:
                        ifImageNotExistAIItem.WindowTitle = value;
                        break;
                }

                UpdateProperties();
            }
        }

        public string WindowClassNameText
        {
            get => string.IsNullOrEmpty(WindowClassName) ? "指定なし" : WindowClassName;
        }

        public string WindowClassName
        {
            get
            {
                return Item switch
                {
                    IWaitImageItem waitImageItem => waitImageItem.WindowClassName,
                    IClickImageItem clickImageItem => clickImageItem.WindowClassName,
                    IHotkeyItem hotkeyItem => hotkeyItem.WindowClassName,
                    //IClickItem clickItem => clickItem.WindowClassName,
                    IIfImageExistItem ifImageExistItem => ifImageExistItem.WindowClassName,
                    IIfImageNotExistItem ifImageNotExistItem => ifImageNotExistItem.WindowClassName,
                    IIfImageExistAIItem ifImageExistAIItem => ifImageExistAIItem.WindowClassName,
                    IIfImageNotExistAIItem ifImageNotExistAIItem => ifImageNotExistAIItem.WindowClassName,
                    _ => string.Empty,
                };
            }
            set
            {
                switch (Item)
                {
                    case IWaitImageItem waitImageItem:
                        waitImageItem.WindowClassName = value;
                        break;
                    case IClickImageItem clickImageItem:
                        clickImageItem.WindowClassName = value;
                        break;
                    case IHotkeyItem hotkeyItem:
                        hotkeyItem.WindowClassName = value;
                        break;
                    //case IClickItem clickItem:
                    //    clickItem.WindowClassName = value;
                    //    break;
                    case IIfImageExistItem ifImageExistItem:
                        ifImageExistItem.WindowClassName = value;
                        break;
                    case IIfImageNotExistItem ifImageNotExistItem:
                        ifImageNotExistItem.WindowClassName = value;
                        break;
                    case IIfImageExistAIItem ifImageExistAIItem:
                        ifImageExistAIItem.WindowClassName = value;
                        break;
                    case IIfImageNotExistAIItem ifImageNotExistAIItem:
                        ifImageNotExistAIItem.WindowClassName = value;
                        break;
                }

                UpdateProperties();
            }
        }

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

                UpdateProperties();
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

                UpdateProperties();
            }
        }

        public Color? SearchColor
        {
            get
            {
                return Item switch
                {
                    IWaitImageItem waitImageItem => waitImageItem.SearchColor,
                    IClickImageItem clickImageItem => clickImageItem.SearchColor,
                    IIfImageExistItem ifImageExistItem => ifImageExistItem.SearchColor,
                    IIfImageNotExistItem ifImageNotExistItem => ifImageNotExistItem.SearchColor,
                    _ => null,
                };
            }
            set
            {
                switch (Item)
                {
                    case IWaitImageItem waitImageItem:
                        waitImageItem.SearchColor = value;
                        break;
                    case IClickImageItem clickImageItem:
                        clickImageItem.SearchColor = value;
                        break;
                    case IIfImageExistItem ifImageExistItem:
                        ifImageExistItem.SearchColor = value;
                        break;
                    case IIfImageNotExistItem ifImageNotExistItem:
                        ifImageNotExistItem.SearchColor = value;
                        break;
                }

                UpdateProperties();

                OnPropertyChanged(nameof(SearchColorBrush));
                OnPropertyChanged(nameof(SearchColorText));
                OnPropertyChanged(nameof(SearchColorTextColor));
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
                    IIfImageExistAIItem ifImageExistAIItem => ifImageExistAIItem.Timeout,
                    IIfImageNotExistAIItem ifImageNotExistAIItem => ifImageNotExistAIItem.Timeout,
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
                    case IIfImageExistAIItem ifImageExistAIItem:
                        ifImageExistAIItem.Timeout = value;
                        break;
                    case IIfImageNotExistAIItem ifImageNotExistAIItem:
                        ifImageNotExistAIItem.Timeout = value;
                        break;
                }

                UpdateProperties();
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
                    IIfImageExistAIItem ifImageExistAIItem => ifImageExistAIItem.Interval,
                    IIfImageNotExistAIItem ifImageNotExistAIItem => ifImageNotExistAIItem.Interval,
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
                    case IIfImageExistAIItem ifImageExistAIItem:
                        ifImageExistAIItem.Interval = value;
                        break;
                    case IIfImageNotExistAIItem ifImageNotExistAIItem:
                        ifImageNotExistAIItem.Interval = value;
                        break;
                }

                UpdateProperties();
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

                UpdateProperties();
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

                UpdateProperties();
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

                UpdateProperties();
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

                UpdateProperties();
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

                UpdateProperties();
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

                UpdateProperties();
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

                UpdateProperties();
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

                UpdateProperties();
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

                UpdateProperties();
            }
        }

        public string ModelPath
        {
            get
            {
                return Item switch
                {
                    IfImageExistAIItem ifImageExistAIItem => ifImageExistAIItem.ModelPath,
                    IfImageNotExistAIItem ifImageNotExistAIItem => ifImageNotExistAIItem.ModelPath,
                    _ => string.Empty,
                };
            }
            set
            {
                switch (Item)
                {
                    case IfImageExistAIItem ifImageExistAIItem:
                        ifImageExistAIItem.ModelPath = value;
                        break;
                    case IfImageNotExistAIItem ifImageNotExistAIItem:
                        ifImageNotExistAIItem.ModelPath = value;
                        break;
                }
                UpdateProperties();
            }
        }

        public int ClassID
        {
            get
            {
                return Item switch
                {
                    IfImageExistAIItem ifImageExistAIItem => ifImageExistAIItem.ClassID,
                    IfImageNotExistAIItem ifImageNotExistAIItem => ifImageNotExistAIItem.ClassID,
                    _ => 0,
                };
            }
            set
            {
                switch (Item)
                {
                    case IfImageExistAIItem ifImageExistAIItem:
                        ifImageExistAIItem.ClassID = value;
                        break;
                    case IfImageNotExistAIItem ifImageNotExistAIItem:
                        ifImageNotExistAIItem.ClassID = value;
                        break;
                }
                UpdateProperties();
            }
        }

        public string ProgramPath
        {
            get
            {
                return Item is ExecuteProgramItem executeProgramItem ? executeProgramItem.ProgramPath : string.Empty;
            }
            set
            {
                if (Item is ExecuteProgramItem executeProgramItem)
                {
                    executeProgramItem.ProgramPath = value;
                }
                UpdateProperties();
            }
        }

        public string Arguments
        {
            get
            {
                return Item is ExecuteProgramItem executeProgramItem ? executeProgramItem.Arguments : string.Empty;
            }
            set
            {
                if (Item is ExecuteProgramItem executeProgramItem)
                {
                    executeProgramItem.Arguments = value;
                }
                UpdateProperties();
            }
        }

        public string WorkingDirectory
        {
            get
            {
                return Item is ExecuteProgramItem executeProgramItem ? executeProgramItem.WorkingDirectory : string.Empty;
            }
            set
            {
                if (Item is ExecuteProgramItem executeProgramItem)
                {
                    executeProgramItem.WorkingDirectory = value;
                }
                UpdateProperties();
            }
        }

        public bool WaitForExit
        {
            get
            {
                return Item is ExecuteProgramItem executeProgramItem ? executeProgramItem.WaitForExit : false;
            }
            set
            {
                if (Item is ExecuteProgramItem executeProgramItem)
                {
                    executeProgramItem.WaitForExit = value;
                }
                UpdateProperties();
            }
        }
        #endregion

        #region ColorPicker

        public Brush SearchColorBrush
        {
            get
            {
                return new SolidColorBrush(SearchColor ?? Color.FromArgb(0, 0, 0, 0));
            }
        }

        public string SearchColorText
        {
            get
            {
                // RGBの文字列を返す
                return SearchColor != null ? $"R:{SearchColor.Value.R:D3} G:{SearchColor.Value.G:D3} B:{SearchColor.Value.B:D3}" : "指定なし";
            }
        }

        public Brush SearchColorTextColor
        {
            get
            {
                // 色が指定されている場合は、その色の反転色を返す
                return SearchColor != null ? new SolidColorBrush(Color.FromArgb(255, (byte)(255 - SearchColor.Value.R), (byte)(255 - SearchColor.Value.G), (byte)(255 - SearchColor.Value.B))) : new SolidColorBrush(Colors.Black);
            }
        }
        #endregion

        #region ComboBox
        [ObservableProperty]
        private ObservableCollection<string> _itemTypes = new();
        public string SelectedItemType
        {
            get { return Item != null ? Item.ItemType : "None"; }
            set
            {
                if (Item == null)
                {
                    return;
                }

                if (Item.ItemType == value)
                {
                    return;
                }

                Item.ItemType = value;
                OnSelectedItemTypeChanged();
                // RefreshListViewMessage は UpdateProperties 内でデバウンス送信
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

                UpdateProperties();
            }
        }
        #endregion


        public EditPanelViewModel()
        {
            _refreshTimer.Tick += (s, e) =>
            {
                _refreshTimer.Stop();
                WeakReferenceMessenger.Default.Send(new RefreshListViewMessage());
            };

            foreach(var type in ItemType.GetTypes())
            {
                ItemTypes.Add(type);
            }

            foreach(var button in Enum.GetValues(typeof(System.Windows.Input.MouseButton)).Cast<System.Windows.Input.MouseButton>())
            {
                MouseButtons.Add(button);
            }
        }

        #region OnChanged
        private void OnSelectedItemTypeChanged()
        {
            var lineNumber = Item?.LineNumber ?? 0;
            var isSelected = Item?.IsSelected ?? false;

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
                case nameof(ItemType.IfImageExistAI):
                    Item = new IfImageExistAIItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.IfImageExistAI) };
                    break;
                case nameof(ItemType.IfImageNotExistAI):
                    Item = new IfImageNotExistAIItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.IfImageNotExistAI) };
                    break;
            }

            UpdateProperties();
            UpdateIsProperties();

            WeakReferenceMessenger.Default.Send(new EditCommandMessage(Item));
        }
        #endregion

        #region Update
        private void UpdateIsProperties()
        {
            OnPropertyChanged(nameof(IsListNotEmpty));
            OnPropertyChanged(nameof(IsNotNullItem));
            OnPropertyChanged(nameof(IsWaitImageItem));
            OnPropertyChanged(nameof(IsClickImageItem));
            OnPropertyChanged(nameof(IsHotkeyItem));
            OnPropertyChanged(nameof(IsClickItem));
            OnPropertyChanged(nameof(IsWaitItem));
            OnPropertyChanged(nameof(IsLoopItem));
            OnPropertyChanged(nameof(IsEndLoopItem));
            OnPropertyChanged(nameof(IsBreakItem));
            OnPropertyChanged(nameof(IsIfImageExistItem));
            OnPropertyChanged(nameof(IsIfImageNotExistItem));
            OnPropertyChanged(nameof(IsEndIfItem));
            OnPropertyChanged(nameof(IsIfImageExistAIItem));
            OnPropertyChanged(nameof(IsIfImageNotExistAIItem));
            OnPropertyChanged(nameof(IsExecuteProgramItem));
        }

        void UpdateProperties()
        {
            if(_isUpdating)
            {
                return;
            }

            try
            {
                _isUpdating = true;

                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(WindowTitleText));
                OnPropertyChanged(nameof(WindowClassName));
                OnPropertyChanged(nameof(WindowClassNameText));
                OnPropertyChanged(nameof(ImagePath));
                OnPropertyChanged(nameof(Threshold));
                OnPropertyChanged(nameof(SearchColor));
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
                OnPropertyChanged(nameof(ModelPath));
                OnPropertyChanged(nameof(ClassID));
                OnPropertyChanged(nameof(SelectedItemType));
                OnPropertyChanged(nameof(SelectedMouseButton));
                OnPropertyChanged(nameof(ProgramPath));
                OnPropertyChanged(nameof(Arguments));
                OnPropertyChanged(nameof(WorkingDirectory));
                OnPropertyChanged(nameof(WaitForExit));

                // 設定画面表示用
                OnPropertyChanged(nameof(SearchColorBrush));
                OnPropertyChanged(nameof(SearchColorText));
                OnPropertyChanged(nameof(SearchColorTextColor));

                // デバウンス送信
                _refreshTimer.Stop();
                _refreshTimer.Start();
            }
            finally
            {
                _isUpdating = false;
            }
        }
        #endregion

        #region Command
        [RelayCommand]
        private void Enter(KeyEventArgs e)
        {
            // エンターキーが押されたときのみ処理を行う
            if (e.Key == Key.Enter)
            {
                UpdateProperties();
            }
        }

        [RelayCommand]
        public void GetWindowInfo()
        {
            var getWindowInfoWindow = new GetWindowInfoWindow();

            if (getWindowInfoWindow.ShowDialog() == true)
            {
                WindowTitle = getWindowInfoWindow.WindowTitle;
                WindowClassName = getWindowInfoWindow.WindowClassName;
            }
        }
        
        [RelayCommand]
        public void ClearWindowInfo()
        {
            WindowTitle = string.Empty;
            WindowClassName = string.Empty;
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
        public void PickSearchColor()
        {
            var colorPickWindow = new ColorPickWindow();
            colorPickWindow.ShowDialog();
            SearchColor = colorPickWindow.Color;
        }

        [RelayCommand]
        public void ClearSearchColor()
        {
            SearchColor = null;
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
        public void BrowseModel()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ONNX Files (*.onnx)|*.onnx|All Files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false,
            };
            if (dialog.ShowDialog() == true)
            {
                if (Item is IfImageExistAIItem ifImageExistAIItem)
                {
                    ifImageExistAIItem.ModelPath = dialog.FileName;
                }
                else if (Item is IfImageNotExistAIItem ifImageNotExistAIItem)
                {
                    ifImageNotExistAIItem.ModelPath = dialog.FileName;
                }
                UpdateProperties();
            }
        }

        [RelayCommand]
        public void BrowseProgram()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executable Files (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|All Files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false,
            };
            if (dialog.ShowDialog() == true)
            {
                if (Item is ExecuteProgramItem executeProgramItem)
                {
                    executeProgramItem.ProgramPath = dialog.FileName;
                }
                UpdateProperties();
            }
        }

        [RelayCommand]
        public void BrowseWorkingDirectory()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog() { Multiselect = false };

            if (dialog.ShowDialog() == true)
            {
                if (Item is ExecuteProgramItem executeProgramItem)
                {
                    executeProgramItem.WorkingDirectory = dialog.FolderName;
                }
                UpdateProperties();
            }
        }
        #endregion

        #region Call from MainWindowViewModel
        public ICommandListItem? GetItem()
        {
            return Item;
        }

        public void SetItem(ICommandListItem? item)
        {
            Item = item;
        }

        public void SetListCount(int listCount)
        {
            ListCount = listCount;
        }

        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
        }

        public void Prepare()
        {
        }

        #endregion
    }
}