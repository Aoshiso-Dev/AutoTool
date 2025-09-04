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
using System.Windows.Media;
using System.IO;
using AutoTool.ViewModel.Shared;
using System.Runtime.CompilerServices;
using System;
using System.Linq;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// 標準MVVMパターンによるEditPanelViewModel (統一版)
    /// </summary>
    public partial class EditPanelViewModel : ObservableObject
    {
        private readonly ILogger<EditPanelViewModel> _logger;
        private readonly IMessenger _messenger;
        private bool _isUpdating = false;
        
        [ObservableProperty]
        private ICommandListItem? _selectedItem;

        [ObservableProperty]
        private bool _isRunning = false;

        // Collections for binding
        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _itemTypes = new();

        [ObservableProperty]
        private ObservableCollection<AutoTool.ViewModel.Shared.OperatorItem> _operators = new();

        [ObservableProperty]
        private ObservableCollection<AutoTool.ViewModel.Shared.AIDetectModeItem> _aiDetectModes = new();

        [ObservableProperty]
        private ObservableCollection<AutoTool.ViewModel.Shared.BackgroundClickMethodItem> _backgroundClickMethods = new();

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

        // アイテムタイプ判定プロパティ
        public bool IsWaitImageItem => SelectedItem?.ItemType == "Wait_Image";
        public bool IsClickImageItem => SelectedItem?.ItemType == "Click_Image";
        public bool IsClickImageAIItem => SelectedItem?.ItemType == "Click_Image_AI";
        public bool IsHotkeyItem => SelectedItem?.ItemType == "Hotkey";
        public bool IsClickItem => SelectedItem?.ItemType == "Click";
        public bool IsWaitItem => SelectedItem?.ItemType == "Wait";
        public bool IsLoopItem => SelectedItem?.ItemType == "Loop";
        public bool IsLoopEndItem => SelectedItem?.ItemType == "Loop_End";
        public bool IsLoopBreakItem => SelectedItem?.ItemType == "Loop_Break";
        public bool IsIfImageExistItem => SelectedItem?.ItemType == "IF_ImageExist";
        public bool IsIfImageNotExistItem => SelectedItem?.ItemType == "IF_ImageNotExist";
        public bool IsIfImageExistAIItem => SelectedItem?.ItemType == "IF_ImageExist_AI";
        public bool IsIfImageNotExistAIItem => SelectedItem?.ItemType == "IF_ImageNotExist_AI";
        public bool IsIfEndItem => SelectedItem?.ItemType == "IF_End";
        public bool IsIfVariableItem => SelectedItem?.ItemType == "IF_Variable";
        public bool IsExecuteItem => SelectedItem?.ItemType == "Execute";
        public bool IsSetVariableItem => SelectedItem?.ItemType == "SetVariable";
        public bool IsSetVariableAIItem => SelectedItem?.ItemType == "SetVariable_AI";
        public bool IsScreenshotItem => SelectedItem?.ItemType == "Screenshot";

        // 複合条件判定
        public bool IsImageBasedItem => IsWaitImageItem || IsClickImageItem || IsIfImageExistItem || IsIfImageNotExistItem || IsScreenshotItem;
        public bool IsAIBasedItem => IsClickImageAIItem || IsIfImageExistAIItem || IsIfImageNotExistAIItem || IsSetVariableAIItem;
        public bool IsVariableItem => IsIfVariableItem || IsSetVariableItem || IsSetVariableAIItem;
        public bool IsLoopRelatedItem => IsLoopItem || IsLoopEndItem || IsLoopBreakItem;
        public bool IsIfRelatedItem => IsIfImageExistItem || IsIfImageNotExistItem || IsIfImageExistAIItem || IsIfImageNotExistAIItem || IsIfVariableItem || IsIfEndItem;

        // 表示制御プロパティ
        public bool ShowWindowInfo => IsWaitImageItem || IsClickImageItem || IsHotkeyItem || IsClickItem || IsScreenshotItem || IsAIBasedItem;
        public bool ShowAdvancedSettings => IsClickImageItem || IsClickItem || (IsAIBasedItem && !IsIfRelatedItem);
        public bool IsNotNullItem => SelectedItem != null;
        public bool IsListEmpty => SelectedItem == null;
        public bool IsListNotEmpty => SelectedItem != null;

        public EditPanelViewModel(ILogger<EditPanelViewModel> logger, IMessenger messenger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            InitializeCollections();
            SetupMessaging();
            
            _logger.LogInformation("EditPanelViewModel (統一版) を初期化しました");
        }

        private void SetupMessaging()
        {
            _messenger.Register<ChangeSelectedMessage>(this, (r, m) => 
            {
                SelectedItem = m.SelectedItem;
            });
        }

        private void InitializeCollections()
        {
            try
            {
                // CommandTypes
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

                // MouseButtons
                MouseButtons.Clear();
                foreach (var button in Enum.GetValues(typeof(MouseButton)).Cast<MouseButton>())
                    MouseButtons.Add(button);

                // Keys
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

                // Operators
                Operators.Clear();
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "==", DisplayName = "等しい (==)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "!=", DisplayName = "等しくない (!=)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = ">", DisplayName = "より大きい (>)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "<", DisplayName = "より小さい (<)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = ">=", DisplayName = "以上 (>=)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "<=", DisplayName = "以下 (<=)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "Contains", DisplayName = "含む (Contains)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "StartsWith", DisplayName = "始まる (StartsWith)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "EndsWith", DisplayName = "終わる (EndsWith)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "IsEmpty", DisplayName = "空である (IsEmpty)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "IsNotEmpty", DisplayName = "空でない (IsNotEmpty)" });

                // AI Detect Modes
                AiDetectModes.Clear();
                AiDetectModes.Add(new AutoTool.ViewModel.Shared.AIDetectModeItem { Key = "Class", DisplayName = "クラス検出" });
                AiDetectModes.Add(new AutoTool.ViewModel.Shared.AIDetectModeItem { Key = "Count", DisplayName = "数量検出" });

                // Background Click Methods
                BackgroundClickMethods.Clear();
                BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 0, DisplayName = "SendMessage" });
                BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 1, DisplayName = "PostMessage" });
                BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 2, DisplayName = "AutoDetectChild" });
                BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 3, DisplayName = "TryAll" });
                BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 4, DisplayName = "GameDirectInput" });
                BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 5, DisplayName = "GameFullscreen" });
                BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 6, DisplayName = "GameLowLevel" });
                BackgroundClickMethods.Add(new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 7, DisplayName = "GameVirtualMouse" });

                _logger.LogDebug("EditPanelViewModel コレクション初期化完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EditPanelViewModel コレクション初期化中にエラー");
            }
        }

        // OnSelectedItemChangedはObservablePropertyで自動生成される partial void
        partial void OnSelectedItemChanged(ICommandListItem? value)
        {
            try
            {
                _isUpdating = true;

                // アイテムタイプの選択を更新
                if (value != null)
                {
                    var displayItem = ItemTypes.FirstOrDefault(x => x.TypeName == value.ItemType);
                    SelectedItemTypeObj = displayItem;
                }
                else
                {
                    SelectedItemTypeObj = null;
                }

                // 全ての判定プロパティを更新
                NotifyAllPropertiesChanged();
                
                _logger.LogDebug("SelectedItem変更: {ItemType}", value?.ItemType ?? "null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SelectedItem変更処理中にエラー");
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void OnSelectedItemTypeObjChanged(CommandDisplayItem? value)
        {
            if (_isUpdating || value == null || SelectedItem == null) return;

            try
            {
                _logger.LogDebug("ItemType変更要求: {OldType} -> {NewType}", SelectedItem.ItemType, value.TypeName);

                // 新しいタイプのアイテムを作成
                var newItem = CommandRegistry.CreateCommandItem(value.TypeName);
                if (newItem == null)
                {
                    _logger.LogWarning("新しいアイテムの作成に失敗: {TypeName}", value.TypeName);
                    return;
                }

                // 基本プロパティを引き継ぎ
                newItem.LineNumber = SelectedItem.LineNumber;
                newItem.IsEnable = SelectedItem.IsEnable;
                newItem.Comment = SelectedItem.Comment;
                newItem.IsSelected = SelectedItem.IsSelected;
                newItem.IsRunning = SelectedItem.IsRunning;
                newItem.NestLevel = SelectedItem.NestLevel;

                // リストの該当アイテムを置換
                _messenger.Send(new ChangeItemTypeMessage(SelectedItem, newItem));

                _logger.LogInformation("ItemType変更完了: {OldType} -> {NewType}", SelectedItem.ItemType, value.TypeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ItemType変更中にエラー");
                
                // 失敗した場合は元の選択状態に戻す
                _isUpdating = true;
                try
                {
                    var oldDisplayItem = ItemTypes.FirstOrDefault(x => x.TypeName == SelectedItem.ItemType);
                    SelectedItemTypeObj = oldDisplayItem;
                }
                finally
                {
                    _isUpdating = false;
                }
            }
        }

        private void NotifyAllPropertiesChanged()
        {
            var properties = new[]
            {
                nameof(IsWaitImageItem), nameof(IsClickImageItem), nameof(IsClickImageAIItem),
                nameof(IsHotkeyItem), nameof(IsClickItem), nameof(IsWaitItem),
                nameof(IsLoopItem), nameof(IsLoopEndItem), nameof(IsLoopBreakItem),
                nameof(IsIfImageExistItem), nameof(IsIfImageNotExistItem), nameof(IsIfImageExistAIItem),
                nameof(IsIfImageNotExistAIItem), nameof(IsIfEndItem), nameof(IsIfVariableItem),
                nameof(IsExecuteItem), nameof(IsSetVariableItem), nameof(IsSetVariableAIItem),
                nameof(IsScreenshotItem), nameof(IsImageBasedItem), nameof(IsAIBasedItem),
                nameof(IsVariableItem), nameof(IsLoopRelatedItem), nameof(IsIfRelatedItem),
                nameof(ShowWindowInfo), nameof(ShowAdvancedSettings), nameof(IsNotNullItem),
                nameof(IsListEmpty), nameof(IsListNotEmpty),
                // 値プロパティ
                nameof(Comment), nameof(WindowTitle), nameof(WindowClassName),
                nameof(ImagePath), nameof(Threshold), nameof(SearchColor),
                nameof(Timeout), nameof(Interval), nameof(MouseButton),
                nameof(ClickX), nameof(ClickY), nameof(UseBackgroundClick), nameof(BackgroundClickMethod),
                nameof(CtrlKey), nameof(AltKey), nameof(ShiftKey), nameof(SelectedKey),
                nameof(WaitHours), nameof(WaitMinutes), nameof(WaitSeconds), nameof(WaitMilliseconds),
                nameof(LoopCount), nameof(VariableName), nameof(VariableValue), nameof(VariableOperator),
                nameof(ModelPath), nameof(ClassID), nameof(ConfThreshold), nameof(IoUThreshold),
                nameof(AiDetectMode), nameof(ProgramPath), nameof(Arguments), nameof(WorkingDirectory),
                nameof(WaitForExit), nameof(SaveDirectory)
            };

            foreach (var property in properties)
            {
                OnPropertyChanged(property);
            }
        }

        #region Properties for data binding (安全な実装)

        public string Comment
        {
            get => SelectedItem?.Comment ?? string.Empty;
            set
            {
                if (SelectedItem != null && SelectedItem.Comment != value)
                {
                    SelectedItem.Comment = value;
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

        // Wait time properties
        public int WaitHours
        {
            get
            {
                if (SelectedItem == null) return 0;
                try
                {
                    var property = SelectedItem.GetType().GetProperty("WaitTime");
                    if (property != null)
                    {
                        var value = property.GetValue(SelectedItem);
                        if (value is TimeSpan timeSpan)
                            return (int)timeSpan.TotalHours;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("プロパティ取得エラー: WaitHours - {Error}", ex.Message);
                }
                return 0;
            }
            set => SetWaitTime(hours: value);
        }

        public int WaitMinutes
        {
            get
            {
                if (SelectedItem == null) return 0;
                try
                {
                    var property = SelectedItem.GetType().GetProperty("WaitTime");
                    if (property != null)
                    {
                        var value = property.GetValue(SelectedItem);
                        if (value is TimeSpan timeSpan)
                            return timeSpan.Minutes;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("プロパティ取得エラー: WaitMinutes - {Error}", ex.Message);
                }
                return 0;
            }
            set => SetWaitTime(minutes: value);
        }

        public int WaitSeconds
        {
            get
            {
                if (SelectedItem == null) return 0;
                try
                {
                    var property = SelectedItem.GetType().GetProperty("WaitTime");
                    if (property != null)
                    {
                        var value = property.GetValue(SelectedItem);
                        if (value is TimeSpan timeSpan)
                            return timeSpan.Seconds;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("プロパティ取得エラー: WaitSeconds - {Error}", ex.Message);
                }
                return 0;
            }
            set => SetWaitTime(seconds: value);
        }

        public int WaitMilliseconds
        {
            get
            {
                if (SelectedItem == null) return 0;
                try
                {
                    var property = SelectedItem.GetType().GetProperty("WaitTime");
                    if (property != null)
                    {
                        var value = property.GetValue(SelectedItem);
                        if (value is TimeSpan timeSpan)
                            return timeSpan.Milliseconds;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("プロパティ取得エラー: WaitMilliseconds - {Error}", ex.Message);
                }
                return 0;
            }
            set => SetWaitTime(milliseconds: value);
        }

        private void SetWaitTime(int? hours = null, int? minutes = null, int? seconds = null, int? milliseconds = null)
        {
            if (SelectedItem == null) return;

            try
            {
                var currentHours = WaitHours;
                var currentMinutes = WaitMinutes;
                var currentSeconds = WaitSeconds;
                var currentMilliseconds = WaitMilliseconds;

                var newTime = new TimeSpan(
                    0, // days
                    hours ?? currentHours,
                    minutes ?? currentMinutes,
                    seconds ?? currentSeconds,
                    milliseconds ?? currentMilliseconds
                );

                SetItemProperty("WaitTime", newTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WaitTime設定エラー");
            }
        }

        public int LoopCount
        {
            get => GetItemProperty<int>("LoopCount");
            set => SetItemProperty("LoopCount", value);
        }

        public string VariableName
        {
            get => GetItemProperty<string>("VariableName") ?? string.Empty;
            set => SetItemProperty("VariableName", value);
        }

        public string VariableValue
        {
            get => GetItemProperty<string>("VariableValue") ?? string.Empty;
            set => SetItemProperty("VariableValue", value);
        }

        public string VariableOperator
        {
            get => GetItemProperty<string>("VariableOperator") ?? string.Empty;
            set => SetItemProperty("VariableOperator", value);
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
            get => GetItemProperty<string>("AiDetectMode") ?? string.Empty;
            set => SetItemProperty("AiDetectMode", value);
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
            if (SelectedItem == null) return default(T)!;

            try
            {
                var property = SelectedItem.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(SelectedItem);
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

        private void SetItemProperty(string propertyName, object? value, [CallerMemberName] string? callerName = null)
        {
            if (SelectedItem == null) return;

            try
            {
                var property = SelectedItem.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    var currentValue = property.GetValue(SelectedItem);
                    if (!Equals(currentValue, value))
                    {
                        property.SetValue(SelectedItem, value);
                        OnPropertyChanged(callerName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プロパティ設定エラー: {Property} = {Value}", propertyName, value);
            }
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void BrowseImageFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "画像ファイルを選択",
                    Filter = "画像ファイル (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|すべてのファイル (*.*)|*.*",
                    DefaultExt = ".png"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    ImagePath = openFileDialog.FileName;
                    _logger.LogInformation("画像ファイルを選択: {ImagePath}", ImagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "画像ファイル選択中にエラー");
            }
        }

        [RelayCommand]
        private void BrowseModelFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "AIモデルファイルを選択",
                    Filter = "ONNXファイル (*.onnx)|*.onnx|すべてのファイル (*.*)|*.*",
                    DefaultExt = ".onnx"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    ModelPath = openFileDialog.FileName;
                    _logger.LogInformation("モデルファイルを選択: {ModelPath}", ModelPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "モデルファイル選択中にエラー");
            }
        }

        [RelayCommand]
        private void BrowseProgramFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "実行ファイルを選択",
                    Filter = "実行ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*",
                    DefaultExt = ".exe"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    ProgramPath = openFileDialog.FileName;
                    _logger.LogInformation("実行ファイルを選択: {ProgramPath}", ProgramPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "実行ファイル選択中にエラー");
            }
        }

        [RelayCommand]
        private void BrowseWorkingDirectory()
        {
            try
            {
                // Microsoft.Win32.CommonOpenFileDialogを使用（Windows用）
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "作業ディレクトリを選択してください",
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "フォルダを選択"
                };

                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = Path.GetDirectoryName(dialog.FileName);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        WorkingDirectory = selectedPath;
                        _logger.LogInformation("作業ディレクトリを選択: {WorkingDirectory}", WorkingDirectory);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "作業ディレクトリ選択中にエラー");
            }
        }

        [RelayCommand]
        private void BrowseSaveDirectory()
        {
            try
            {
                // Microsoft.Win32.CommonOpenFileDialogを使用（Windows用）
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "保存ディレクトリを選択してください",
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "フォルダを選択"
                };

                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = Path.GetDirectoryName(dialog.FileName);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        SaveDirectory = selectedPath;
                        _logger.LogInformation("保存ディレクトリを選択: {SaveDirectory}", SaveDirectory);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存ディレクトリ選択中にエラー");
            }
        }

        [RelayCommand]
        private void GetMousePosition()
        {
            try
            {
                // WinAPIを使用してマウス位置を取得
                var point = new System.Drawing.Point();
                if (GetCursorPos(out point))
                {
                    ClickX = point.X;
                    ClickY = point.Y;
                    _logger.LogInformation("マウス位置を取得: ({X}, {Y})", ClickX, ClickY);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マウス位置取得中にエラー");
            }
        }

        [RelayCommand]
        private void GetWindowInfo()
        {
            try
            {
                // WindowHelper機能を使用してウィンドウ情報を取得
                _logger.LogInformation("ウィンドウ情報取得機能（未実装）");
                // TODO: WindowHelperとの連携実装
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ情報取得中にエラー");
            }
        }

        [RelayCommand]
        private void ClearWindowInfo()
        {
            try
            {
                WindowTitle = string.Empty;
                WindowClassName = string.Empty;
                _logger.LogInformation("ウィンドウ情報をクリア");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ情報クリア中にエラー");
            }
        }

        #endregion

        #region Win32 API

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        #endregion
    }
}