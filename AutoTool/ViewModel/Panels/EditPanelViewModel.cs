using AutoTool.Message;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Class;
using AutoTool.Model.MacroFactory;
using AutoTool.Model.CommandDefinition;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Media;
using System.IO;
using AutoTool.ViewModel.Shared;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Edit Panel用のViewModel (統合版)
    /// </summary>
    public partial class EditPanelViewModel : ObservableObject
    {
        private readonly ILogger<EditPanelViewModel> _logger;
        private bool _isUpdating = false;
        private ICommandListItem? _item;

        // Collections for binding
        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _itemTypes = new();

        [ObservableProperty]
        private ObservableCollection<OperatorItem> _operators = new();

        [ObservableProperty]
        private ObservableCollection<AIDetectModeItem> _aiDetectModes = new();

        [ObservableProperty]
        private ObservableCollection<BackgroundClickMethodItem> _backgroundClickMethods = new();

        [ObservableProperty]
        private ObservableCollection<MouseButton> _mouseButtons = new();

        [ObservableProperty]
        private ObservableCollection<Key> _keyList = new();

        // Selected items
        private CommandDisplayItem? _selectedItemTypeObj;
        public CommandDisplayItem? SelectedItemTypeObj
        {
            get => _selectedItemTypeObj;
            set
            {
                if (SetProperty(ref _selectedItemTypeObj, value))
                {
                    OnSelectedItemTypeObjChanged(value);
                }
            }
        }

        [ObservableProperty]
        private bool _isRunning = false;

        // アイテムプロパティ
        public ICommandListItem? Item
        {
            get => _item;
            set
            {
                if (SetProperty(ref _item, value))
                {
                    OnItemChanged();
                }
            }
        }

        // 判定プロパティ
        public bool IsNotNullItem => Item != null;
        public bool IsListEmpty => !IsNotNullItem;
        public bool IsListNotEmpty => IsNotNullItem;
        public bool IsListNotEmptyButNoSelection => false; // TODO: 実装が必要な場合

        // アイテムタイプ判定
        public bool IsWaitImageItem => Item?.ItemType == "Wait_Image";
        public bool IsClickImageItem => Item?.ItemType == "Click_Image";
        public bool IsClickImageAIItem => Item?.ItemType == "Click_Image_AI";
        public bool IsHotkeyItem => Item?.ItemType == "Hotkey";
        public bool IsClickItem => Item?.ItemType == "Click";
        public bool IsWaitItem => Item?.ItemType == "Wait";
        public bool IsLoopItem => Item?.ItemType == "Loop";
        public bool IsLoopEndItem => Item?.ItemType == "Loop_End";
        public bool IsLoopBreakItem => Item?.ItemType == "Loop_Break";
        public bool IsIfImageExistItem => Item?.ItemType == "IF_ImageExist";
        public bool IsIfImageNotExistItem => Item?.ItemType == "IF_ImageNotExist";
        public bool IsIfImageExistAIItem => Item?.ItemType == "IF_ImageExist_AI";
        public bool IsIfImageNotExistAIItem => Item?.ItemType == "IF_ImageNotExist_AI";
        public bool IsIfEndItem => Item?.ItemType == "IF_End";
        public bool IsIfVariableItem => Item?.ItemType == "IF_Variable";
        public bool IsExecuteItem => Item?.ItemType == "Execute";
        public bool IsSetVariableItem => Item?.ItemType == "SetVariable";
        public bool IsSetVariableAIItem => Item?.ItemType == "SetVariable_AI";
        public bool IsScreenshotItem => Item?.ItemType == "Screenshot";

        // 複合条件
        public bool IsImageBasedItem => IsWaitImageItem || IsClickImageItem || IsIfImageExistItem || IsIfImageNotExistItem;
        public bool IsAIBasedItem => IsClickImageAIItem || IsIfImageExistAIItem || IsIfImageNotExistAIItem || IsSetVariableAIItem;
        public bool IsVariableItem => IsSetVariableItem || IsIfVariableItem || IsSetVariableAIItem;
        public bool IsLoopRelatedItem => IsLoopItem || IsLoopEndItem || IsLoopBreakItem;
        public bool IsIfRelatedItem => IsIfImageExistItem || IsIfImageNotExistItem || IsIfImageExistAIItem || IsIfImageNotExistAIItem || IsIfVariableItem || IsIfEndItem;

        // 表示制御
        public bool ShowWindowInfo => IsNotNullItem && (IsWaitImageItem || IsClickImageItem || IsClickImageAIItem || IsIfImageExistItem || IsIfImageNotExistItem || IsIfImageExistAIItem || IsIfImageNotExistAIItem || IsHotkeyItem || IsClickItem || IsScreenshotItem || IsSetVariableAIItem);
        public bool ShowAdvancedSettings => IsNotNullItem && !IsLoopBreakItem;

        public EditPanelViewModel(ILogger<EditPanelViewModel> logger)
        {
            _logger = logger;
            SetupMessaging();
            InitializeItemTypes();
            InitializeCollections();
            _logger.LogDebug("EditPanelViewModel初期化完了");
        }

        private void SetupMessaging()
        {
            WeakReferenceMessenger.Default.Register<ChangeSelectedMessage>(this, (r, m) => 
            {
                Item = m.SelectedItem;
            });
        }

        private void InitializeItemTypes()
        {
            try
            {
                CommandRegistry.Initialize();

                var displayItems = CommandRegistry.GetOrderedTypeNames()
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList();

                ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ItemTypes初期化中にエラーが発生しました");
            }
        }

        private void InitializeCollections()
        {
            // MouseButton の初期化
            MouseButtons.Clear();
            foreach (var button in Enum.GetValues(typeof(MouseButton)).Cast<MouseButton>())
                MouseButtons.Add(button);

            // Key の初期化
            KeyList.Clear();
            var commonKeys = new[]
            {
                Key.Escape, Key.Enter, Key.Space, Key.Tab, Key.Back, Key.Delete,
                Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12,
                Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J, Key.K, Key.L, Key.M,
                Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T, Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
                Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9,
                Key.Up, Key.Down, Key.Left, Key.Right, Key.Home, Key.End, Key.PageUp, Key.PageDown
            };
            foreach (var key in commonKeys)
                KeyList.Add(key);

            // Operator の初期化
            Operators.Clear();
            Operators.Add(new OperatorItem { Key = "==", DisplayName = "等しい (==)" });
            Operators.Add(new OperatorItem { Key = "!=", DisplayName = "等しくない (!=)" });
            Operators.Add(new OperatorItem { Key = ">", DisplayName = "より大きい (>)" });
            Operators.Add(new OperatorItem { Key = "<", DisplayName = "より小さい (<)" });
            Operators.Add(new OperatorItem { Key = ">=", DisplayName = "以上 (>=)" });
            Operators.Add(new OperatorItem { Key = "<=", DisplayName = "以下 (<=)" });
            Operators.Add(new OperatorItem { Key = "Contains", DisplayName = "含む (Contains)" });
            Operators.Add(new OperatorItem { Key = "StartsWith", DisplayName = "始まる (StartsWith)" });
            Operators.Add(new OperatorItem { Key = "EndsWith", DisplayName = "終わる (EndsWith)" });
            Operators.Add(new OperatorItem { Key = "IsEmpty", DisplayName = "空である (IsEmpty)" });
            Operators.Add(new OperatorItem { Key = "IsNotEmpty", DisplayName = "空でない (IsNotEmpty)" });

            // AI Detect Mode の初期化
            AiDetectModes.Clear();
            AiDetectModes.Add(new AIDetectModeItem { Key = "Class", DisplayName = "クラス検出" });
            AiDetectModes.Add(new AIDetectModeItem { Key = "Count", DisplayName = "数量検出" });

            // Background Click Method の初期化
            BackgroundClickMethods.Clear();
            BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 0, DisplayName = "SendMessage" });
            BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 1, DisplayName = "PostMessage" });
            BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 2, DisplayName = "AutoDetectChild" });
            BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 3, DisplayName = "TryAll" });
            BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 4, DisplayName = "GameDirectInput" });
            BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 5, DisplayName = "GameFullscreen" });
            BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 6, DisplayName = "GameLowLevel" });
            BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 7, DisplayName = "GameVirtualMouse" });
        }

        private void OnItemChanged()
        {
            try
            {
                _isUpdating = true;

                // 全ての判定プロパティを更新
                OnPropertyChanged(nameof(IsNotNullItem));
                OnPropertyChanged(nameof(IsListEmpty));
                OnPropertyChanged(nameof(IsListNotEmpty));
                OnPropertyChanged(nameof(IsWaitImageItem));
                OnPropertyChanged(nameof(IsClickImageItem));
                OnPropertyChanged(nameof(IsClickImageAIItem));
                OnPropertyChanged(nameof(IsHotkeyItem));
                OnPropertyChanged(nameof(IsClickItem));
                OnPropertyChanged(nameof(IsWaitItem));
                OnPropertyChanged(nameof(IsLoopItem));
                OnPropertyChanged(nameof(IsLoopEndItem));
                OnPropertyChanged(nameof(IsLoopBreakItem));
                OnPropertyChanged(nameof(IsIfImageExistItem));
                OnPropertyChanged(nameof(IsIfImageNotExistItem));
                OnPropertyChanged(nameof(IsIfImageExistAIItem));
                OnPropertyChanged(nameof(IsIfImageNotExistAIItem));
                OnPropertyChanged(nameof(IsIfEndItem));
                OnPropertyChanged(nameof(IsIfVariableItem));
                OnPropertyChanged(nameof(IsExecuteItem));
                OnPropertyChanged(nameof(IsSetVariableItem));
                OnPropertyChanged(nameof(IsSetVariableAIItem));
                OnPropertyChanged(nameof(IsScreenshotItem));
                OnPropertyChanged(nameof(IsImageBasedItem));
                OnPropertyChanged(nameof(IsAIBasedItem));
                OnPropertyChanged(nameof(IsVariableItem));
                OnPropertyChanged(nameof(IsLoopRelatedItem));
                OnPropertyChanged(nameof(IsIfRelatedItem));
                OnPropertyChanged(nameof(ShowWindowInfo));
                OnPropertyChanged(nameof(ShowAdvancedSettings));

                // 値プロパティも更新通知
                OnPropertyChanged(nameof(Comment));
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(WindowClassName));
                OnPropertyChanged(nameof(ImagePath));
                OnPropertyChanged(nameof(Threshold));
                OnPropertyChanged(nameof(SearchColor));
                OnPropertyChanged(nameof(Timeout));
                OnPropertyChanged(nameof(Interval));
                OnPropertyChanged(nameof(MouseButton));
                OnPropertyChanged(nameof(ClickX));
                OnPropertyChanged(nameof(ClickY));
                OnPropertyChanged(nameof(UseBackgroundClick));
                OnPropertyChanged(nameof(BackgroundClickMethod));
                OnPropertyChanged(nameof(CtrlKey));
                OnPropertyChanged(nameof(AltKey));
                OnPropertyChanged(nameof(ShiftKey));
                OnPropertyChanged(nameof(SelectedKey));
                OnPropertyChanged(nameof(WaitHours));
                OnPropertyChanged(nameof(WaitMinutes));
                OnPropertyChanged(nameof(WaitSeconds));
                OnPropertyChanged(nameof(WaitMilliseconds));
                OnPropertyChanged(nameof(LoopCount));
                OnPropertyChanged(nameof(VariableName));
                OnPropertyChanged(nameof(VariableValue));
                OnPropertyChanged(nameof(VariableOperator));
                OnPropertyChanged(nameof(ModelPath));
                OnPropertyChanged(nameof(ClassID));
                OnPropertyChanged(nameof(ConfThreshold));
                OnPropertyChanged(nameof(IoUThreshold));
                OnPropertyChanged(nameof(AiDetectMode));
                OnPropertyChanged(nameof(ProgramPath));
                OnPropertyChanged(nameof(Arguments));
                OnPropertyChanged(nameof(WorkingDirectory));
                OnPropertyChanged(nameof(WaitForExit));
                OnPropertyChanged(nameof(SaveDirectory));

                if (Item != null)
                {
                    // アイテムタイプの選択を更新
                    var displayItem = ItemTypes.FirstOrDefault(x => x.TypeName == Item.ItemType);
                    SelectedItemTypeObj = displayItem;
                    _logger.LogDebug("アイテム変更: {ItemType}", Item.ItemType);
                }
                else
                {
                    SelectedItemTypeObj = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテム変更処理中にエラーが発生しました");
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void OnSelectedItemTypeObjChanged(CommandDisplayItem? value)
        {
            if (_isUpdating || value == null || Item == null) return;

            try
            {
                _logger.LogDebug("アイテムタイプ変更要求: {OldType} -> {NewType}", Item.ItemType, value.TypeName);

                // 新しいタイプのアイテムを作成
                var newItem = CommandRegistry.CreateCommandItem(value.TypeName);
                if (newItem == null)
                {
                    _logger.LogWarning("新しいアイテムの作成に失敗しました: {TypeName}", value.TypeName);
                    return;
                }

                // 基本プロパティを引き継ぎ
                newItem.LineNumber = Item.LineNumber;
                newItem.IsEnable = Item.IsEnable;
                newItem.Comment = Item.Comment;
                newItem.IsSelected = Item.IsSelected;
                newItem.IsRunning = Item.IsRunning;
                newItem.NestLevel = Item.NestLevel;

                // リストの該当アイテムを置換
                WeakReferenceMessenger.Default.Send(new ChangeItemTypeMessage(Item, newItem));

                _logger.LogInformation("アイテムタイプを変更しました: {OldType} -> {NewType}", Item.ItemType, value.TypeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテムタイプ変更中にエラーが発生しました");
                
                // 失敗した場合は元の選択状態に戻す
                _isUpdating = true;
                try
                {
                    var oldDisplayItem = ItemTypes.FirstOrDefault(x => x.TypeName == Item.ItemType);
                    SelectedItemTypeObj = oldDisplayItem;
                }
                finally
                {
                    _isUpdating = false;
                }
            }
        }

        #region Properties for data binding (基本実装)

        public string Comment
        {
            get => Item?.Comment ?? string.Empty;
            set
            {
                if (Item != null && Item.Comment != value)
                {
                    Item.Comment = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WindowTitle
        {
            get => GetItemProperty<string>("WindowTitle") ?? string.Empty;
            set => SetItemProperty("WindowTitle", value);
        }

        public string WindowClassName
        {
            get => GetItemProperty<string>("WindowClassName") ?? string.Empty;
            set => SetItemProperty("WindowClassName", value);
        }

        public string ImagePath
        {
            get => GetItemProperty<string>("ImagePath") ?? string.Empty;
            set => SetItemProperty("ImagePath", value);
        }

        public double Threshold
        {
            get => GetItemProperty<double>("Threshold");
            set => SetItemProperty("Threshold", value);
        }

        public Color? SearchColor
        {
            get => GetItemProperty<Color?>("SearchColor");
            set => SetItemProperty("SearchColor", value);
        }

        public int Timeout
        {
            get => GetItemProperty<int>("Timeout");
            set => SetItemProperty("Timeout", value);
        }

        public int Interval
        {
            get => GetItemProperty<int>("Interval");
            set => SetItemProperty("Interval", value);
        }

        public MouseButton MouseButton
        {
            get => GetItemProperty<MouseButton>("Button");
            set => SetItemProperty("Button", value);
        }

        public int ClickX
        {
            get => GetItemProperty<int>("X");
            set => SetItemProperty("X", value);
        }

        public int ClickY
        {
            get => GetItemProperty<int>("Y");
            set => SetItemProperty("Y", value);
        }

        public bool UseBackgroundClick
        {
            get => GetItemProperty<bool>("UseBackgroundClick");
            set => SetItemProperty("UseBackgroundClick", value);
        }

        public int BackgroundClickMethod
        {
            get => GetItemProperty<int>("BackgroundClickMethod");
            set => SetItemProperty("BackgroundClickMethod", value);
        }

        public bool CtrlKey
        {
            get => GetItemProperty<bool>("Ctrl");
            set => SetItemProperty("Ctrl", value);
        }

        public bool AltKey
        {
            get => GetItemProperty<bool>("Alt");
            set => SetItemProperty("Alt", value);
        }

        public bool ShiftKey
        {
            get => GetItemProperty<bool>("Shift");
            set => SetItemProperty("Shift", value);
        }

        public Key SelectedKey
        {
            get => GetItemProperty<Key>("Key");
            set => SetItemProperty("Key", value);
        }

        public int WaitHours
        {
            get
            {
                if (Item == null) return 0;
                var milliseconds = GetItemProperty<int>("Wait");
                return milliseconds / (1000 * 60 * 60);
            }
            set
            {
                if (Item == null) return;
                var currentMs = GetItemProperty<int>("Wait");
                var minutes = (currentMs / (1000 * 60)) % 60;
                var seconds = (currentMs / 1000) % 60;
                var ms = currentMs % 1000;
                var totalMs = (value * 60 * 60 * 1000) + (minutes * 60 * 1000) + (seconds * 1000) + ms;
                SetItemProperty("Wait", totalMs);
                OnPropertyChanged(nameof(WaitMinutes));
                OnPropertyChanged(nameof(WaitSeconds));
                OnPropertyChanged(nameof(WaitMilliseconds));
            }
        }

        public int WaitMinutes
        {
            get
            {
                if (Item == null) return 0;
                var milliseconds = GetItemProperty<int>("Wait");
                return (milliseconds / (1000 * 60)) % 60;
            }
            set
            {
                if (Item == null) return;
                var currentMs = GetItemProperty<int>("Wait");
                var hours = currentMs / (1000 * 60 * 60);
                var seconds = (currentMs / 1000) % 60;
                var ms = currentMs % 1000;
                var totalMs = (hours * 60 * 60 * 1000) + (value * 60 * 1000) + (seconds * 1000) + ms;
                SetItemProperty("Wait", totalMs);
                OnPropertyChanged(nameof(WaitHours));
                OnPropertyChanged(nameof(WaitSeconds));
                OnPropertyChanged(nameof(WaitMilliseconds));
            }
        }

        public int WaitSeconds
        {
            get
            {
                if (Item == null) return 0;
                var milliseconds = GetItemProperty<int>("Wait");
                return (milliseconds / 1000) % 60;
            }
            set
            {
                if (Item == null) return;
                var currentMs = GetItemProperty<int>("Wait");
                var hours = currentMs / (1000 * 60 * 60);
                var minutes = (currentMs / (1000 * 60)) % 60;
                var ms = currentMs % 1000;
                var totalMs = (hours * 60 * 60 * 1000) + (minutes * 60 * 1000) + (value * 1000) + ms;
                SetItemProperty("Wait", totalMs);
                OnPropertyChanged(nameof(WaitHours));
                OnPropertyChanged(nameof(WaitMinutes));
                OnPropertyChanged(nameof(WaitMilliseconds));
            }
        }

        public int WaitMilliseconds
        {
            get
            {
                if (Item == null) return 0;
                var milliseconds = GetItemProperty<int>("Wait");
                return milliseconds % 1000;
            }
            set
            {
                if (Item == null) return;
                var currentMs = GetItemProperty<int>("Wait");
                var hours = currentMs / (1000 * 60 * 60);
                var minutes = (currentMs / (1000 * 60)) % 60;
                var seconds = (currentMs / 1000) % 60;
                var totalMs = (hours * 60 * 60 * 1000) + (minutes * 60 * 1000) + (seconds * 1000) + value;
                SetItemProperty("Wait", totalMs);
                OnPropertyChanged(nameof(WaitHours));
                OnPropertyChanged(nameof(WaitMinutes));
                OnPropertyChanged(nameof(WaitSeconds));
            }
        }

        public int LoopCount
        {
            get => GetItemProperty<int>("LoopCount");
            set => SetItemProperty("LoopCount", value);
        }

        public string VariableName
        {
            get => GetItemProperty<string>("Name") ?? string.Empty;
            set => SetItemProperty("Name", value);
        }

        public string VariableValue
        {
            get => GetItemProperty<string>("Value") ?? string.Empty;
            set => SetItemProperty("Value", value);
        }

        public string VariableOperator
        {
            get => GetItemProperty<string>("Operator") ?? "==";
            set => SetItemProperty("Operator", value);
        }

        public string ModelPath
        {
            get => GetItemProperty<string>("ModelPath") ?? string.Empty;
            set => SetItemProperty("ModelPath", value);
        }

        public int ClassID
        {
            get => GetItemProperty<int>("ClassID");
            set => SetItemProperty("ClassID", value);
        }

        public double ConfThreshold
        {
            get => GetItemProperty<double>("ConfThreshold");
            set => SetItemProperty("ConfThreshold", value);
        }

        public double IoUThreshold
        {
            get => GetItemProperty<double>("IoUThreshold");
            set => SetItemProperty("IoUThreshold", value);
        }

        public string AiDetectMode
        {
            get => GetItemProperty<string>("AIDetectMode") ?? "Class";
            set => SetItemProperty("AIDetectMode", value);
        }

        public string ProgramPath
        {
            get => GetItemProperty<string>("ProgramPath") ?? string.Empty;
            set => SetItemProperty("ProgramPath", value);
        }

        public string Arguments
        {
            get => GetItemProperty<string>("Arguments") ?? string.Empty;
            set => SetItemProperty("Arguments", value);
        }

        public string WorkingDirectory
        {
            get => GetItemProperty<string>("WorkingDirectory") ?? string.Empty;
            set => SetItemProperty("WorkingDirectory", value);
        }

        public bool WaitForExit
        {
            get => GetItemProperty<bool>("WaitForExit");
            set => SetItemProperty("WaitForExit", value);
        }

        public string SaveDirectory
        {
            get => GetItemProperty<string>("SaveDirectory") ?? string.Empty;
            set => SetItemProperty("SaveDirectory", value);
        }

        #endregion

        #region Helper Methods

        private T GetItemProperty<T>(string propertyName)
        {
            if (Item == null) return default(T)!;

            try
            {
                var property = Item.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(Item);
                    if (value is T tValue)
                        return tValue;
                    if (value != null && typeof(T).IsAssignableFrom(value.GetType()))
                        return (T)value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("プロパティ取得エラー: {Property} - {Error}", propertyName, ex.Message);
            }

            return default(T)!;
        }

        private void SetItemProperty(string propertyName, object? value)
        {
            if (Item == null || _isUpdating) return;

            try
            {
                var property = Item.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(Item, value);
                    OnPropertyChanged();
                    _logger.LogDebug("プロパティ更新: {Property} = {Value}", propertyName, value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("プロパティ設定エラー: {Property} - {Error}", propertyName, ex.Message);
            }
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void BrowseImageFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "画像ファイル (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|すべてのファイル (*.*)|*.*",
                Title = "画像ファイルを選択"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ImagePath = openFileDialog.FileName;
            }
        }

        [RelayCommand]
        private void BrowseModelFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "ONNXモデルファイル (*.onnx)|*.onnx|すべてのファイル (*.*)|*.*",
                Title = "ONNXモデルファイルを選択"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ModelPath = openFileDialog.FileName;
            }
        }

        [RelayCommand]
        private void BrowseProgramFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "実行可能ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*",
                Title = "実行するプログラムを選択"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ProgramPath = openFileDialog.FileName;
            }
        }

        [RelayCommand]
        private void BrowseWorkingDirectory()
        {
            // TODO: フォルダ選択ダイアログの実装
            _logger.LogDebug("作業ディレクトリ選択要求");
        }

        [RelayCommand]
        private void BrowseSaveDirectory()
        {
            // TODO: フォルダ選択ダイアログの実装
            _logger.LogDebug("保存先ディレクトリ選択要求");
        }

        [RelayCommand]
        private void GetMousePosition()
        {
            // TODO: マウス位置取得の実装
            _logger.LogDebug("マウス位置取得要求");
        }

        [RelayCommand]
        private void GetWindowInfo()
        {
            _logger.LogDebug("ウィンドウ情報取得要求");
        }

        [RelayCommand]
        private void ClearWindowInfo()
        {
            WindowTitle = string.Empty;
            WindowClassName = string.Empty;
        }

        #endregion
    }
}