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
using MacroPanels.List.Class;
using MacroPanels.Model.List.Interface;
using MacroPanels.View;
using System.IO;
using System.Windows.Controls;
using System.Collections;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using MacroPanels.ViewModel.Helpers;
using ColorPickHelper;

namespace MacroPanels.ViewModel
{
    public partial class EditPanelViewModel : ObservableObject
    {
        private readonly EditPanelPropertyManager _propertyManager = new();

        [ObservableProperty]
        private bool _isRunning = false;

        private bool _isUpdating;

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
        public bool IsEndLoopItem => Item is LoopEndItem;
        public bool IsBreakItem => Item is LoopBreakItem;
        public bool IsIfImageExistItem => Item is IfImageExistItem;
        public bool IsIfImageNotExistItem => Item is IfImageNotExistItem;
        public bool IsIfImageExistAIItem => Item is IfImageExistAIItem;
        public bool IsIfImageNotExistAIItem => Item is IfImageNotExistAIItem;
        public bool IsEndIfItem => Item is IfEndItem;
        public bool IsExecuteProgramItem => Item is ExecuteItem;
        public bool IsSetVariableItem => Item is SetVariableItem;
        public bool IsSetVariableAIItem => Item is SetVariableAIItem;
        public bool IsIfVariableItem => Item is IfVariableItem;
        public bool IsScreenshotItem => Item is ScreenshotItem;
        #endregion

        #region Properties - Refactored using PropertyManager
        
        // ウィンドウ関連
        public string WindowTitleText => string.IsNullOrEmpty(WindowTitle) ? "指定なし" : WindowTitle;
        public string WindowTitle
        {
            get => _propertyManager.WindowTitle.GetValue(Item);
            set
            {
                _propertyManager.WindowTitle.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public string WindowClassNameText => string.IsNullOrEmpty(WindowClassName) ? "指定なし" : WindowClassName;
        public string WindowClassName
        {
            get => _propertyManager.WindowClassName.GetValue(Item);
            set
            {
                _propertyManager.WindowClassName.SetValue(Item, value);
                UpdateProperties();
            }
        }

        // 画像関連
        public string ImagePath
        {
            get => _propertyManager.ImagePath.GetValue(Item);
            set
            {
                _propertyManager.ImagePath.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public double Threshold
        {
            get => _propertyManager.Threshold.GetValue(Item);
            set
            {
                _propertyManager.Threshold.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public Color? SearchColor
        {
            get => _propertyManager.SearchColor.GetValue(Item);
            set
            {
                _propertyManager.SearchColor.SetValue(Item, value);
                UpdateProperties();
                OnPropertyChanged(nameof(SearchColorBrush));
                OnPropertyChanged(nameof(SearchColorText));
                OnPropertyChanged(nameof(SearchColorTextColor));
            }
        }

        // タイミング関連
        public int Timeout
        {
            get => _propertyManager.Timeout.GetValue(Item);
            set
            {
                _propertyManager.Timeout.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public int Interval
        {
            get => _propertyManager.Interval.GetValue(Item);
            set
            {
                _propertyManager.Interval.SetValue(Item, value);
                UpdateProperties();
            }
        }

        // マウス・キーボード関連
        public System.Windows.Input.MouseButton MouseButton
        {
            get => _propertyManager.MouseButton.GetValue(Item);
            set
            {
                _propertyManager.MouseButton.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public bool Ctrl
        {
            get => _propertyManager.Ctrl.GetValue(Item);
            set
            {
                _propertyManager.Ctrl.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public bool Alt
        {
            get => _propertyManager.Alt.GetValue(Item);
            set
            {
                _propertyManager.Alt.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public bool Shift
        {
            get => _propertyManager.Shift.GetValue(Item);
            set
            {
                _propertyManager.Shift.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public System.Windows.Input.Key Key
        {
            get => _propertyManager.Key.GetValue(Item);
            set
            {
                _propertyManager.Key.SetValue(Item, value);
                UpdateProperties();
            }
        }

        // 座標関連
        public int X
        {
            get => _propertyManager.X.GetValue(Item);
            set
            {
                _propertyManager.X.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public int Y
        {
            get => _propertyManager.Y.GetValue(Item);
            set
            {
                _propertyManager.Y.SetValue(Item, value);
                UpdateProperties();
            }
        }

        // 待機・ループ関連
        public int Wait
        {
            get => _propertyManager.Wait.GetValue(Item);
            set
            {
                _propertyManager.Wait.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public int LoopCount
        {
            get => _propertyManager.LoopCount.GetValue(Item);
            set
            {
                _propertyManager.LoopCount.SetValue(Item, value);
                UpdateProperties();
            }
        }

        // AI関連
        public string ModelPath
        {
            get => _propertyManager.ModelPath.GetValue(Item);
            set
            {
                _propertyManager.ModelPath.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public int ClassID
        {
            get => _propertyManager.ClassID.GetValue(Item);
            set
            {
                _propertyManager.ClassID.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public string AIMode
        {
            get => _propertyManager.Mode.GetValue(Item);
            set
            {
                _propertyManager.Mode.SetValue(Item, value);
                UpdateProperties();
            }
        }

        // プログラム実行関連
        public string ProgramPath
        {
            get => _propertyManager.ProgramPath.GetValue(Item);
            set
            {
                _propertyManager.ProgramPath.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public string Arguments
        {
            get => _propertyManager.Arguments.GetValue(Item);
            set
            {
                _propertyManager.Arguments.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public string WorkingDirectory
        {
            get => _propertyManager.WorkingDirectory.GetValue(Item);
            set
            {
                _propertyManager.WorkingDirectory.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public bool WaitForExit
        {
            get => _propertyManager.WaitForExit.GetValue(Item);
            set
            {
                _propertyManager.WaitForExit.SetValue(Item, value);
                UpdateProperties();
            }
        }

        // 変数関連
        public string VariableName
        {
            get => _propertyManager.VariableName.GetValue(Item);
            set
            {
                _propertyManager.VariableName.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public string VariableValue
        {
            get => _propertyManager.VariableValue.GetValue(Item);
            set
            {
                _propertyManager.VariableValue.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public string CompareOperator
        {
            get => _propertyManager.CompareOperator.GetValue(Item);
            set
            {
                _propertyManager.CompareOperator.SetValue(Item, value);
                UpdateProperties();
            }
        }

        public string CompareValue
        {
            get => _propertyManager.CompareValue.GetValue(Item);
            set
            {
                _propertyManager.CompareValue.SetValue(Item, value);
                UpdateProperties();
            }
        }

        // スクリーンショット関連
        public string SaveDirectory
        {
            get => _propertyManager.SaveDirectory.GetValue(Item);
            set
            {
                _propertyManager.SaveDirectory.SetValue(Item, value);
                UpdateProperties();
            }
        }
        #endregion

        #region ColorPicker
        public Brush SearchColorBrush => new SolidColorBrush(SearchColor ?? Color.FromArgb(0, 0, 0, 0));

        public string SearchColorText => SearchColor != null 
            ? $"R:{SearchColor.Value.R:D3} G:{SearchColor.Value.G:D3} B:{SearchColor.Value.B:D3}" 
            : "指定なし";

        public Brush SearchColorTextColor => SearchColor != null 
            ? new SolidColorBrush(Color.FromArgb(255, (byte)(255 - SearchColor.Value.R), (byte)(255 - SearchColor.Value.G), (byte)(255 - SearchColor.Value.B))) 
            : new SolidColorBrush(Colors.Black);
        #endregion

        #region ComboBox
        [ObservableProperty]
        private ObservableCollection<string> _itemTypes = new();
        
        public string SelectedItemType
        {
            get => Item?.ItemType ?? "None";
            set
            {
                if (Item == null || Item.ItemType == value) return;
                Item.ItemType = value;
                OnSelectedItemTypeChanged();
            }
        }

        [ObservableProperty]
        private ObservableCollection<System.Windows.Input.MouseButton> _mouseButtons = new();
        
        public System.Windows.Input.MouseButton SelectedMouseButton
        {
            get => MouseButton;
            set
            {
                MouseButton = value;
                UpdateProperties();
            }
        }

        [ObservableProperty]
        private ObservableCollection<string> _operators = new();
        
        public string SelectedOperator
        {
            get => CompareOperator;
            set
            {
                CompareOperator = value;
                UpdateProperties();
            }
        }

        [ObservableProperty]
        private ObservableCollection<string> _aIModes = new();

        public string SelectedAIMode
        {
            get => AIMode;
            set
            {
                AIMode = value;
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

            InitializeOperators();

            InitializeAIModes();
        }

        private void InitializeOperators()
        {
            var operators = new[]
            {
                "==", "!=", ">", "<", ">=", "<=",
                "Contains", "NotContains", "StartsWith", "EndsWith",
                "IsEmpty", "IsNotEmpty"
            };

            foreach (var op in operators)
            {
                Operators.Add(op);
            }
        }

        private void InitializeAIModes()
        {
            var aiModes = new[]
            {
                "Class", "Count"
            };
            foreach (var mode in aiModes)
            {
                AIModes.Add(mode);
            }
        }

        #region OnChanged
        private void OnSelectedItemTypeChanged()
        {
            var lineNumber = Item?.LineNumber ?? 0;
            var isSelected = Item?.IsSelected ?? false;

            Item = SelectedItemType switch
            {
                nameof(ItemType.Click) => new ClickItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Click) },
                nameof(ItemType.Click_Image) => new ClickImageItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Click_Image) },
                nameof(ItemType.Hotkey) => new HotkeyItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Hotkey) },
                nameof(ItemType.Execute) => new ExecuteItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Execute) },
                nameof(ItemType.Screenshot) => new ScreenshotItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Screenshot) },
                nameof(ItemType.Wait) => new WaitItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Wait) },
                nameof(ItemType.Wait_Image) => new WaitImageItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Wait_Image) },
                nameof(ItemType.Loop) => new LoopItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Loop) },
                nameof(ItemType.Loop_End) => new LoopEndItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Loop_End) },
                nameof(ItemType.Loop_Break) => new LoopBreakItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.Loop_Break) },
                nameof(ItemType.IF_ImageExist) => new IfImageExistItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.IF_ImageExist) },
                nameof(ItemType.IF_ImageNotExist) => new IfImageNotExistItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.IF_ImageNotExist) },
                nameof(ItemType.IF_ImageExist_AI) => new IfImageExistAIItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.IF_ImageExist_AI) },
                nameof(ItemType.IF_ImageNotExist_AI) => new IfImageNotExistAIItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.IF_ImageNotExist_AI) },
                nameof(ItemType.IF_End) => new IfEndItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.IF_End) },
                nameof(ItemType.SetVariable) => new SetVariableItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.SetVariable) },
                nameof(ItemType.SetVariable_AI) => new SetVariableAIItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.SetVariable_AI) },
                nameof(ItemType.IF_Variable) => new IfVariableItem() { LineNumber = lineNumber, IsSelected = isSelected, ItemType = nameof(ItemType.IF_Variable) },
                _ => throw new ArgumentException($"Unknown ItemType: {SelectedItemType}")
            };

            UpdateProperties();
            UpdateIsProperties();
            WeakReferenceMessenger.Default.Send(new EditCommandMessage(Item));
        }
        #endregion

        #region Update
        private void UpdateIsProperties()
        {
            var propertyNames = new[]
            {
                nameof(IsListNotEmpty), nameof(IsNotNullItem), nameof(IsWaitImageItem),
                nameof(IsClickImageItem), nameof(IsHotkeyItem), nameof(IsClickItem),
                nameof(IsWaitItem), nameof(IsLoopItem), nameof(IsEndLoopItem),
                nameof(IsBreakItem), nameof(IsIfImageExistItem), nameof(IsIfImageNotExistItem),
                nameof(IsEndIfItem), nameof(IsIfImageExistAIItem), nameof(IsIfImageNotExistAIItem),
                nameof(IsExecuteProgramItem), nameof(IsSetVariableItem), nameof(IsSetVariableAIItem), nameof(IsIfVariableItem),
                nameof(IsScreenshotItem)
            };

            foreach (var propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }

        void UpdateProperties()
        {
            if (_isUpdating) return;

            try
            {
                _isUpdating = true;

                var propertiesToUpdate = new[]
                {
                    nameof(SelectedItemType), nameof(WindowTitle), nameof(WindowTitleText),
                    nameof(WindowClassName), nameof(WindowClassNameText), nameof(ImagePath),
                    nameof(Threshold), nameof(SearchColor), nameof(Timeout), nameof(Interval),
                    nameof(MouseButton), nameof(SelectedMouseButton), nameof(Ctrl), nameof(Alt),
                    nameof(Shift), nameof(Key), nameof(X), nameof(Y), nameof(Wait),
                    nameof(LoopCount), nameof(SelectedOperator), nameof(VariableName),
                    nameof(VariableValue), nameof(CompareOperator), nameof(CompareValue),
                    nameof(ModelPath), nameof(ClassID), nameof(ProgramPath), nameof(Arguments),
                    nameof(WorkingDirectory), nameof(WaitForExit), nameof(SaveDirectory),
                    nameof(SearchColorBrush), nameof(SearchColorText), nameof(SearchColorTextColor),
                    nameof(AIMode), nameof(SelectedAIMode)
                };

                foreach (var property in propertiesToUpdate)
                {
                    OnPropertyChanged(property);
                }

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
            var fileName = DialogHelper.SelectImageFile();
            if (fileName != null)
            {
                ImagePath = fileName;
            }
        }

        [RelayCommand]
        public void Capture()
        {
            var captureWindow = new CaptureWindow { Mode = 0 };
            if (captureWindow.ShowDialog() == true)
            {
                var capturePath = DialogHelper.CreateCaptureFilePath();
                var capturedMat = OpenCVHelper.ScreenCaptureHelper.CaptureRegion(captureWindow.SelectedRegion);
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
            var captureWindow = new CaptureWindow { Mode = 1 };
            if (captureWindow.ShowDialog() == true)
            {
                X = (int)captureWindow.SelectedPoint.X;
                Y = (int)captureWindow.SelectedPoint.Y;
            }
        }

        [RelayCommand]
        public void BrowseModel()
        {
            var fileName = DialogHelper.SelectModelFile();
            if (fileName != null)
            {
                ModelPath = fileName;
            }
        }

        [RelayCommand]
        public void BrowseProgram()
        {
            var fileName = DialogHelper.SelectExecutableFile();
            if (fileName != null)
            {
                ProgramPath = fileName;
            }
        }

        [RelayCommand]
        public void BrowseWorkingDirectory()
        {
            var folderName = DialogHelper.SelectFolder();
            if (folderName != null)
            {
                WorkingDirectory = folderName;
            }
        }

        [RelayCommand]
        public void BrowseSaveDirectory()
        {
            var folderName = DialogHelper.SelectFolder();
            if (folderName != null)
            {
                SaveDirectory = folderName;
            }
        }
        #endregion

        #region Call from MainWindowViewModel
        public ICommandListItem? GetItem() => Item;
        public void SetItem(ICommandListItem? item) => Item = item;
        public void SetListCount(int listCount) => ListCount = listCount;
        public void SetRunningState(bool isRunning) => IsRunning = isRunning;
        public void Prepare() { }
        #endregion
    }
}