using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using ColorPickHelper;
using MacroPanels.ViewModel.Helpers;
using MacroPanels.Model.CommandDefinition;
using MacroPanels.ViewModel.Shared;
using MacroPanels.Helpers;
using Microsoft.Extensions.Logging;

namespace MacroPanels.ViewModel
{
    public partial class EditPanelViewModel : ObservableObject
    {
        private readonly ILogger<EditPanelViewModel> _logger;
        private readonly EditPanelPropertyManager _propertyManager = new();
        private CommandHistoryManager? _commandHistory;

        [ObservableProperty]
        private bool _isRunning = false;
        private bool _isUpdating;
        private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(120) };

        #region Item Management
        private ICommandListItem? _item = null;
        public ICommandListItem? Item
        {
            get => _item;
            set 
            { 
                if (SetProperty(ref _item, value))
                {
                    // Itemが変更された時に対応するSelectedItemTypeObjを更新
                    if (value != null)
                    {
                        var displayItem = ItemTypes.FirstOrDefault(x => x.TypeName == value.ItemType);
                        if (displayItem != null && _selectedItemTypeObj != displayItem)
                        {
                            _selectedItemTypeObj = displayItem;
                            OnPropertyChanged(nameof(SelectedItemTypeObj));
                        }
                    }
                    else
                    {
                        _selectedItemTypeObj = null;
                        OnPropertyChanged(nameof(SelectedItemTypeObj));
                    }
                    
                    UpdateProperties(); 
                    UpdateIsProperties(); 
                }
            }
        }

        private int _listCount = 0;
        public int ListCount
        {
            get => _listCount;
            set { SetProperty(ref _listCount, value); UpdateIsProperties(); UpdateProperties(); }
        }
        #endregion

        #region Item Type Detection Properties
        public bool IsListNotEmpty => ListCount > 0;
        public bool IsListEmpty => ListCount == 0;
        public bool IsListNotEmptyButNoSelection => ListCount > 0 && Item == null;
        public bool IsNotNullItem => Item != null;
        public bool IsWaitImageItem => Item is WaitImageItem;
        public bool IsClickImageItem => Item is ClickImageItem;
        public bool IsClickImageAIItem => Item is ClickImageAIItem;
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

        #region Window Properties
        public string WindowTitleText => string.IsNullOrEmpty(WindowTitle) ? "指定なし" : WindowTitle;
        public string WindowTitle { get => _propertyManager.WindowTitle.GetValue(Item); set { _propertyManager.WindowTitle.SetValue(Item, value); UpdateProperties(); } }
        public string WindowClassNameText => string.IsNullOrEmpty(WindowClassName) ? "指定なし" : WindowClassName;
        public string WindowClassName { get => _propertyManager.WindowClassName.GetValue(Item); set { _propertyManager.WindowClassName.SetValue(Item, value); UpdateProperties(); } }
        #endregion

        #region Image Properties
        // 画像パスは表示用は絶対パス、保存時は相対パス
        public string ImagePath 
        { 
            get 
            {
                var relativePath = _propertyManager.ImagePath.GetValue(Item);
                return string.IsNullOrEmpty(relativePath) ? relativePath : PathHelper.ToAbsolutePath(relativePath);
            } 
            set 
            { 
                var relativePath = string.IsNullOrEmpty(value) ? value : PathHelper.ToRelativePath(value);
                _propertyManager.ImagePath.SetValue(Item, relativePath); 
                UpdateProperties(); 
            } 
        }
        
        public double Threshold { get => _propertyManager.Threshold.GetValue(Item); set { _propertyManager.Threshold.SetValue(Item, value); UpdateProperties(); } }
        
        public Color? SearchColor { get => _propertyManager.SearchColor.GetValue(Item); set { _propertyManager.SearchColor.SetValue(Item, value); UpdateProperties(); OnPropertyChanged(nameof(SearchColorBrush)); OnPropertyChanged(nameof(SearchColorText)); OnPropertyChanged(nameof(SearchColorTextColor)); } }
        public Brush SearchColorBrush => new SolidColorBrush(SearchColor ?? Color.FromArgb(0, 0, 0, 0));
        public string SearchColorText => SearchColor != null ? $"R:{SearchColor.Value.R:D3} G:{SearchColor.Value.G:D3} B:{SearchColor.Value.B:D3}" : "指定なし";
        public Brush SearchColorTextColor => SearchColor != null ? new SolidColorBrush(Color.FromArgb(255, (byte)(255 - SearchColor.Value.R), (byte)(255 - SearchColor.Value.G), (byte)(255 - SearchColor.Value.B))) : new SolidColorBrush(Colors.Black);
        #endregion

        #region Timing Properties
        public int Timeout { get => _propertyManager.Timeout.GetValue(Item); set { _propertyManager.Timeout.SetValue(Item, value); UpdateProperties(); } }
        public int Interval { get => _propertyManager.Interval.GetValue(Item); set { _propertyManager.Interval.SetValue(Item, value); UpdateProperties(); } }
        #endregion

        #region Input Properties (Mouse/Keyboard)
        public System.Windows.Input.MouseButton MouseButton { get => _propertyManager.MouseButton.GetValue(Item); set { _propertyManager.MouseButton.SetValue(Item, value); UpdateProperties(); } }
        public bool Ctrl 
        { 
            get 
            {
                var value = _propertyManager.Ctrl.GetValue(Item);
                _logger.LogDebug("Ctrl getter called: Item={ItemType}, Value={Ctrl}", 
                    Item?.ItemType ?? "null", value);
                return value;
            } 
            set 
            { 
                _logger.LogDebug("Ctrl setter called: Item={ItemType}, OldValue={OldCtrl}, NewValue={NewCtrl}", 
                    Item?.ItemType ?? "null", Ctrl, value);
                
                _propertyManager.Ctrl.SetValue(Item, value);
                UpdateProperties();
            } 
        }
        public bool Alt 
        { 
            get 
            {
                var value = _propertyManager.Alt.GetValue(Item);
                _logger.LogDebug("Alt getter called: Item={ItemType}, Value={Alt}", 
                    Item?.ItemType ?? "null", value);
                return value;
            } 
            set 
            { 
                _logger.LogDebug("Alt setter called: Item={ItemType}, OldValue={OldAlt}, NewValue={NewAlt}", 
                    Item?.ItemType ?? "null", Alt, value);
                
                _propertyManager.Alt.SetValue(Item, value);
                UpdateProperties();
            } 
        }
        public bool Shift 
        { 
            get 
            {
                var value = _propertyManager.Shift.GetValue(Item);
                _logger.LogDebug("Shift getter called: Item={ItemType}, Value={Shift}", 
                    Item?.ItemType ?? "null", value);
                return value;
            } 
            set 
            { 
                _logger.LogDebug("Shift setter called: Item={ItemType}, OldValue={OldShift}, NewValue={NewShift}", 
                    Item?.ItemType ?? "null", Shift, value);
                
                _propertyManager.Shift.SetValue(Item, value);
                UpdateProperties();
            } 
        }
        public Key Key 
        { 
            get 
            {
                var value = _propertyManager.Key.GetValue(Item);
                _logger.LogDebug("Key getter called: Item={ItemType}, Value={Key}", 
                    Item?.ItemType ?? "null", value);
                return value;
            } 
            set 
            { 
                _logger.LogDebug("Key setter called: Item={ItemType}, OldValue={OldKey}, NewValue={NewKey}", 
                    Item?.ItemType ?? "null", Key, value);
                
                _propertyManager.Key.SetValue(Item, value);
                UpdateProperties();
                
                // 設定後の値を確認
                var currentValue = _propertyManager.Key.GetValue(Item);
                _logger.LogDebug("Key setter finished: Item={ItemType}, ActualValue={ActualKey}", 
                    Item?.ItemType ?? "null", currentValue);
            } 
        }
        #endregion

        #region Position Properties
        public int X { get => _propertyManager.X.GetValue(Item); set { _propertyManager.X.SetValue(Item, value); UpdateProperties(); } }
        public int Y { get => _propertyManager.Y.GetValue(Item); set { _propertyManager.Y.SetValue(Item, value); UpdateProperties(); } }
        #endregion

        #region Wait Time Properties
        public int Wait { get => _propertyManager.Wait.GetValue(Item); set { _propertyManager.Wait.SetValue(Item, value); UpdateProperties(); OnWaitChanged(); } }
        
        // 待機時間の分割プロパティ
        private int _waitHours = 0;
        public int WaitHours
        {
            get => _waitHours;
            set
            {
                if (SetProperty(ref _waitHours, Math.Max(0, value)))
                {
                    UpdateWaitFromComponents();
                }
            }
        }
        
        private int _waitMinutes = 0;
        public int WaitMinutes
        {
            get => _waitMinutes;
            set
            {
                if (SetProperty(ref _waitMinutes, Math.Max(0, Math.Min(59, value))))
                {
                    UpdateWaitFromComponents();
                }
            }
        }
        
        private int _waitSeconds = 0;
        public int WaitSeconds
        {
            get => _waitSeconds;
            set
            {
                if (SetProperty(ref _waitSeconds, Math.Max(0, Math.Min(59, value))))
                {
                    UpdateWaitFromComponents();
                }
            }
        }
        
        private int _waitMilliseconds = 0;
        public int WaitMilliseconds
        {
            get => _waitMilliseconds;
            set
            {
                if (SetProperty(ref _waitMilliseconds, Math.Max(0, Math.Min(999, value))))
                {
                    UpdateWaitFromComponents();
                }
            }
        }
        
        /// <summary>
        /// 待機時間の表示用文字列
        /// </summary>
        public string WaitTimeDisplay
        {
            get
            {
                var totalMs = Wait;
                if (totalMs <= 0) return "合計: 0ミリ秒";
                
                var hours = totalMs / (1000 * 60 * 60);
                var minutes = (totalMs % (1000 * 60 * 60)) / (1000 * 60);
                var seconds = (totalMs % (1000 * 60)) / 1000;
                var milliseconds = totalMs % 1000;
                
                var parts = new List<string>();
                if (hours > 0) parts.Add($"{hours}時間");
                if (minutes > 0) parts.Add($"{minutes}分");
                if (seconds > 0) parts.Add($"{seconds}秒");
                if (milliseconds > 0) parts.Add($"{milliseconds}ミリ秒");
                
                return $"合計: {string.Join(" ", parts)} ({totalMs:N0}ミリ秒)";
            }
        }
        #endregion

        #region Loop Properties
        public int LoopCount { get => _propertyManager.LoopCount.GetValue(Item); set { _propertyManager.LoopCount.SetValue(Item, value); UpdateProperties(); } }
        #endregion

        #region AI Properties
        // ONNXモデルパスも相対パス対応
        public string ModelPath 
        { 
            get 
            {
                var relativePath = _propertyManager.ModelPath.GetValue(Item);
                return string.IsNullOrEmpty(relativePath) ? relativePath : PathHelper.ToAbsolutePath(relativePath);
            } 
            set 
            { 
                var relativePath = string.IsNullOrEmpty(value) ? value : PathHelper.ToRelativePath(value);
                _propertyManager.ModelPath.SetValue(Item, relativePath); 
                UpdateProperties(); 
            } 
        }
        
        public int ClassID { get => _propertyManager.ClassID.GetValue(Item); set { _propertyManager.ClassID.SetValue(Item, value); UpdateProperties(); } }
        public string AIDetectMode { get => _propertyManager.Mode.GetValue(Item); set { _propertyManager.Mode.SetValue(Item, value); UpdateProperties(); } }
        public double ConfThreshold { get => _propertyManager.ConfThreshold.GetValue(Item); set { _propertyManager.ConfThreshold.SetValue(Item, value); UpdateProperties(); } }
        public double IoUThreshold { get => _propertyManager.IoUThreshold.GetValue(Item); set { _propertyManager.IoUThreshold.SetValue(Item, value); UpdateProperties(); } }
        #endregion

        #region Program Execution Properties
        // プログラムパスも相対パス対応
        public string ProgramPath 
        { 
            get 
            {
                var relativePath = _propertyManager.ProgramPath.GetValue(Item);
                return string.IsNullOrEmpty(relativePath) ? relativePath : PathHelper.ToAbsolutePath(relativePath);
            } 
            set 
            { 
                var relativePath = string.IsNullOrEmpty(value) ? value : PathHelper.ToRelativePath(value);
                _propertyManager.ProgramPath.SetValue(Item, relativePath); 
                UpdateProperties(); 
            } 
        }
        
        public string Arguments { get => _propertyManager.Arguments.GetValue(Item); set { _propertyManager.Arguments.SetValue(Item, value); UpdateProperties(); } }
        
        // ワーキングディレクトリも相対パス対応
        public string WorkingDirectory 
        { 
            get 
            {
                var relativePath = _propertyManager.WorkingDirectory.GetValue(Item);
                return string.IsNullOrEmpty(relativePath) ? relativePath : PathHelper.ToAbsolutePath(relativePath);
            } 
            set 
            { 
                var relativePath = string.IsNullOrEmpty(value) ? value : PathHelper.ToRelativePath(value);
                _propertyManager.WorkingDirectory.SetValue(Item, relativePath); 
                UpdateProperties(); 
            } 
        }
        
        public bool WaitForExit { get => _propertyManager.WaitForExit.GetValue(Item); set { _propertyManager.WaitForExit.SetValue(Item, value); UpdateProperties(); } }
        #endregion

        #region Variable Properties
        public string VariableName { get => _propertyManager.VariableName.GetValue(Item); set { _propertyManager.VariableName.SetValue(Item, value); UpdateProperties(); } }
        public string VariableValue { get => _propertyManager.VariableValue.GetValue(Item); set { _propertyManager.VariableValue.SetValue(Item, value); UpdateProperties(); } }
        public string CompareOperator { get => _propertyManager.CompareOperator.GetValue(Item); set { _propertyManager.CompareOperator.SetValue(Item, value); UpdateProperties(); } }
        public string CompareValue { get => _propertyManager.CompareValue.GetValue(Item); set { _propertyManager.CompareValue.SetValue(Item, value); UpdateProperties(); } }
        #endregion

        #region Screenshot Properties
        // 保存先ディレクトリも相対パス対応
        public string SaveDirectory 
        { 
            get 
            {
                var relativePath = _propertyManager.SaveDirectory.GetValue(Item);
                return string.IsNullOrEmpty(relativePath) ? relativePath : PathHelper.ToAbsolutePath(relativePath);
            } 
            set 
            { 
                var relativePath = string.IsNullOrEmpty(value) ? value : PathHelper.ToRelativePath(value);
                _propertyManager.SaveDirectory.SetValue(Item, relativePath); 
                UpdateProperties(); 
            } 
        }
        #endregion

        #region Comment Properties
        /// <summary>
        /// コメント（全コマンド共通）
        /// </summary>
        public string Comment 
        { 
            get => Item?.Comment ?? string.Empty; 
            set 
            { 
                if (Item != null && Item.Comment != value)
                {
                    Item.Comment = value;
                    UpdateProperties();
                }
            } 
        }
        #endregion

        #region ComboBox / Collections
        [ObservableProperty] private ObservableCollection<CommandDisplayItem> _itemTypes = new();
        
        private CommandDisplayItem? _selectedItemTypeObj;
        public CommandDisplayItem? SelectedItemTypeObj 
        { 
            get => _selectedItemTypeObj;
            set 
            {
                if (SetProperty(ref _selectedItemTypeObj, value) && value != null)
                {
                    // CommandDisplayItemが変更された時の処理
                    // TypeNameを使用してコマンドを作成
                    System.Diagnostics.Debug.WriteLine($"SelectedItemTypeObj changed to: {value.DisplayName} (TypeName: {value.TypeName})");
                    OnSelectedItemTypeChanged(value.TypeName);
                }
            }
        }
        
        public string SelectedItemType 
        { 
            get => Item?.ItemType ?? "None"; 
            set 
            { 
                if (Item == null) return;
                if (Item.ItemType == value) return; 
                
                System.Diagnostics.Debug.WriteLine($"SelectedItemType changed to: {value}");
                
                // 既存のアイテムのItemTypeを変更せず、新しいアイテムを作成
                OnSelectedItemTypeChanged(value);
            } 
        }
        
        [ObservableProperty] private ObservableCollection<System.Windows.Input.MouseButton> _mouseButtons = new();
        public System.Windows.Input.MouseButton SelectedMouseButton 
        { 
            get => MouseButton; 
            set 
            { 
                if (MouseButton != value)
                {
                    MouseButton = value;
                }
            } 
        }
        
        [ObservableProperty] private ObservableCollection<string> _operators = new();
        public string SelectedOperator { get => CompareOperator; set { CompareOperator = value; UpdateProperties(); } }
        
        [ObservableProperty] private ObservableCollection<string> _aIDetectModes = new();
        public string SelectedAIDetectMode { get => AIDetectMode; set { AIDetectMode = value; UpdateProperties(); } }
        #endregion

        #region Constructor and Initialization
        /// <summary>
        /// DI対応コンストラクタ
        /// </summary>
        public EditPanelViewModel(ILogger<EditPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _logger.LogInformation("EditPanelViewModel をDI対応で初期化しています");

            // CommandRegistryを初期化
            CommandRegistry.Initialize();
            
            _refreshTimer.Tick += (s, e) => { _refreshTimer.Stop(); WeakReferenceMessenger.Default.Send(new RefreshListViewMessage()); };
            
            // 日本語表示名付きのアイテムを作成
            var displayItems = CommandRegistry.GetOrderedTypeNames()
                .Select(typeName => new CommandDisplayItem
                {
                    TypeName = typeName,
                    DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                    Category = CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                })
                .ToList();
            
            ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
            
            // 初期選択項目を設定（デバッグ用）
            _logger.LogDebug("ItemTypes initialized with {Count} items", ItemTypes.Count);
            if (ItemTypes.Count > 0)
            {
                _logger.LogDebug("First item: {DisplayName} ({TypeName})", ItemTypes[0].DisplayName, ItemTypes[0].TypeName);
            }
                
            foreach (var button in Enum.GetValues(typeof(System.Windows.Input.MouseButton)).Cast<System.Windows.Input.MouseButton>()) 
                MouseButtons.Add(button);
                
            InitializeOperators();
            InitializeAIDetectModes();
            
            _logger.LogInformation("EditPanelViewModel の初期化が完了しました");
        }

        /// <summary>
        /// CommandHistoryを設定
        /// </summary>
        public void SetCommandHistory(CommandHistoryManager? commandHistory)
        {
            _commandHistory = commandHistory;
            _logger.LogDebug("CommandHistoryManager を設定しました");
        }

        private void InitializeOperators()
        {
            foreach (var op in new[] { "==", "!=", ">", "<", ">=", "<=", "Contains", "NotContains", "StartsWith", "EndsWith", "IsEmpty", "IsNotEmpty" })
                Operators.Add(op);
        }
        
        private void InitializeAIDetectModes()
        {
            foreach (var mode in new[] { "Class", "Count" }) AIDetectModes.Add(mode);
        }
        #endregion

        #region Wait Time Helper Methods
        /// <summary>
        /// 時間、分、秒、ミリ秒からトータルミリ秒に変換
        /// </summary>
        private void UpdateWaitFromComponents()
        {
            var totalMs = (_waitHours * 60 * 60 * 1000) + 
                         (_waitMinutes * 60 * 1000) + 
                         (_waitSeconds * 1000) + 
                         _waitMilliseconds;
            
            // 循環参照を避けるために直接プロパティマネージャーを使用
            _propertyManager.Wait.SetValue(Item, totalMs);
            
            // 表示を更新
            OnPropertyChanged(nameof(Wait));
            OnPropertyChanged(nameof(WaitTimeDisplay));
            UpdateProperties();
        }
        
        /// <summary>
        /// トータルミリ秒から時間、分、秒、ミリ秒に分解
        /// </summary>
        private void OnWaitChanged()
        {
            var totalMs = Wait;
            
            var hours = totalMs / (1000 * 60 * 60);
            var minutes = (totalMs % (1000 * 60 * 60)) / (1000 * 60);
            var seconds = (totalMs % (1000 * 60)) / 1000;
            var milliseconds = totalMs % 1000;
            
            // 循環更新を防ぐために直接フィールドを更新
            _waitHours = hours;
            _waitMinutes = minutes;
            _waitSeconds = seconds;
            _waitMilliseconds = milliseconds;
            
            // プロパティ変更通知
            OnPropertyChanged(nameof(WaitHours));
            OnPropertyChanged(nameof(WaitMinutes));
            OnPropertyChanged(nameof(WaitSeconds));
            OnPropertyChanged(nameof(WaitMilliseconds));
            OnPropertyChanged(nameof(WaitTimeDisplay));
        }
        #endregion

        #region Command Type Change Handling
        private void OnSelectedItemTypeChanged(string typeName)
        {
            if (string.IsNullOrEmpty(typeName) || Item == null)
                return;

            var lineNumber = Item.LineNumber;
            var isSelected = Item.IsSelected;
            
            try
            {
                // CommandRegistryを使用して自動生成
                var newItem = CommandRegistry.CreateCommandItem(typeName);
                if (newItem != null)
                {
                    newItem.LineNumber = lineNumber;
                    newItem.IsSelected = isSelected;
                    newItem.ItemType = typeName;
                    
                    // 一時的にUpdatePropertiesを無効化してFromItemの設定
                    _isUpdating = true;
                    try
                    {
                        _item = newItem;
                        OnPropertyChanged(nameof(Item));
                    }
                    finally
                    {
                        _isUpdating = false;
                    }
                    
                    UpdateProperties();
                    UpdateIsProperties();
                    WeakReferenceMessenger.Default.Send(new EditCommandMessage(newItem));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to create command item for type: {typeName}");
                    throw new ArgumentException($"Unknown ItemType: {typeName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating command item: {ex.Message}");
                MessageBox.Show($"コマンドアイテムの作成に失敗しました: {ex.Message}", "エラー", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // SelectedItemTypeObjが変更された時の処理を修正
        private void OnSelectedItemTypeChanged()
        {
            if (SelectedItemTypeObj != null)
            {
                // 正しくTypeNameを使用
                OnSelectedItemTypeChanged(SelectedItemTypeObj.TypeName);
            }
            else
            {
                OnSelectedItemTypeChanged(SelectedItemType);
            }
        }
        #endregion

        #region Property Update Management
        private void UpdateIsProperties()
        {
            // 配列をstaticにしてGC圧を軽減
            foreach (var name in IsPropertyNames)
                OnPropertyChanged(name);
        }

        private readonly string[] IsPropertyNames = {
            nameof(IsListNotEmpty), nameof(IsListEmpty), nameof(IsListNotEmptyButNoSelection), 
            nameof(IsNotNullItem), nameof(IsWaitImageItem), 
            nameof(IsClickImageItem), nameof(IsClickImageAIItem), nameof(IsHotkeyItem), 
            nameof(IsClickItem), nameof(IsWaitItem), nameof(IsLoopItem), 
            nameof(IsEndLoopItem), nameof(IsBreakItem), nameof(IsIfImageExistItem), 
            nameof(IsIfImageNotExistItem), nameof(IsEndIfItem), nameof(IsIfImageExistAIItem), 
            nameof(IsIfImageNotExistAIItem), nameof(IsExecuteProgramItem), nameof(IsSetVariableItem), 
            nameof(IsSetVariableAIItem), nameof(IsIfVariableItem), nameof(IsScreenshotItem)
        };

        private readonly string[] AllPropertyNames = {
            nameof(SelectedItemType), nameof(WindowTitle), nameof(WindowTitleText), 
            nameof(WindowClassName), nameof(WindowClassNameText), nameof(ImagePath), 
            nameof(Threshold), nameof(SearchColor), nameof(Timeout), nameof(Interval), 
            nameof(MouseButton), nameof(SelectedMouseButton), nameof(Ctrl), nameof(Alt), 
            nameof(Shift), nameof(Key), nameof(X), nameof(Y), nameof(Wait), nameof(LoopCount), 
            nameof(ConfThreshold), nameof(IoUThreshold), nameof(SearchColorBrush), 
            nameof(SearchColorText), nameof(SearchColorTextColor), nameof(ModelPath), 
            nameof(ClassID), nameof(AIDetectMode), nameof(ProgramPath), nameof(Arguments), 
            nameof(WorkingDirectory), nameof(WaitForExit), nameof(VariableName), 
            nameof(VariableValue), nameof(CompareOperator), nameof(CompareValue), 
            nameof(SaveDirectory), nameof(Comment), nameof(WaitHours), nameof(WaitMinutes),
            nameof(WaitSeconds), nameof(WaitMilliseconds), nameof(WaitTimeDisplay),
            // ファイル検証プロパティ - DI,Pluginブランチの内容を採用
            nameof(IsImageFileValid), nameof(IsModelFileValid), nameof(IsProgramFileValid),
            nameof(IsWorkingDirectoryValid), nameof(IsSaveDirectoryValid),
            nameof(ImageFileErrorMessage), nameof(ModelFileErrorMessage), nameof(ProgramFileErrorMessage),
            nameof(WorkingDirectoryErrorMessage), nameof(SaveDirectoryErrorMessage)
        };

        void UpdateProperties()
        {
            if (_isUpdating) return;
            
            try
            {
                _isUpdating = true;
                
                // 配列の直接列挙でパフォーマンス向上
                foreach (var name in AllPropertyNames)
                    OnPropertyChanged(name);
                
                _refreshTimer.Stop();
                _refreshTimer.Start();
            }
            finally
            {
                _isUpdating = false;
            }
        }
        #endregion

        #region Commands
        [RelayCommand] private void Enter(KeyEventArgs e) { if (e.Key == Key.Enter) UpdateProperties(); }
        [RelayCommand] public void GetWindowInfo() { var w = new GetWindowInfoWindow(); if (w.ShowDialog() == true) { WindowTitle = w.WindowTitle; WindowClassName = w.WindowClassName; } }
        [RelayCommand] public void ClearWindowInfo() { WindowTitle = string.Empty; WindowClassName = string.Empty; }
        
        [RelayCommand] 
        public void Browse() 
        { 
            var f = DialogHelper.SelectImageFile(); 
            if (f != null) 
            {
                ImagePath = f; // 相対パス変換は ImagePath プロパティで自動的に処理される
                System.Diagnostics.Debug.WriteLine($"画像ファイル選択: 絶対パス={f}, 相対パス={PathHelper.ToRelativePath(f)}");
            }
        }
        
        [RelayCommand] 
        public void Capture() 
        { 
            var cw = new CaptureWindow { Mode = 0 }; 
            if (cw.ShowDialog() == true) 
            { 
                var path = DialogHelper.CreateCaptureFilePath(); 
                var mat = OpenCVHelper.ScreenCaptureHelper.CaptureRegion(cw.SelectedRegion); 
                OpenCVHelper.ScreenCaptureHelper.SaveCapture(mat, path); 
                ImagePath = path; // 相対パス変換は ImagePath プロパティで自動的に処理される
                System.Diagnostics.Debug.WriteLine($"画像キャプチャ: 絶対パス={path}, 相対パス={PathHelper.ToRelativePath(path)}");
            } 
        }
        
        [RelayCommand] public void PickSearchColor() { var w = new ColorPickWindow(); w.ShowDialog(); SearchColor = w.Color; }
        [RelayCommand] public void ClearSearchColor() { SearchColor = null; }
        [RelayCommand] 
        public void PickPoint() 
        { 
            var cw = new CaptureWindow { Mode = 1 }; 
            if (cw.ShowDialog() == true) 
            { 
                // 絶対座標を取得
                var absoluteX = (int)cw.SelectedPoint.X;
                var absoluteY = (int)cw.SelectedPoint.Y;
                
                // ウィンドウが指定されている場合は相対座標に変換
                if (!string.IsNullOrEmpty(WindowTitle) || !string.IsNullOrEmpty(WindowClassName))
                {
                    try
                    {
                        // ウィンドウハンドルを取得
                        var windowHandle = GetWindowHandle(WindowTitle, WindowClassName);
                        if (windowHandle != IntPtr.Zero)
                        {
                            // ウィンドウの位置を取得
                            if (GetWindowRect(windowHandle, out var windowRect))
                            {
                                // 相対座標に変換
                                X = absoluteX - windowRect.Left;
                                Y = absoluteY - windowRect.Top;
                                
                                // 成功メッセージを表示
                                MessageBox.Show($"ウィンドウ相対座標を設定しました: ({X}, {Y})\nウィンドウ: {WindowTitle}[{WindowClassName}]\n絶対座標: ({absoluteX}, {absoluteY})", 
                                              "座標設定完了", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                // ウィンドウRECT取得失敗
                                X = absoluteX;
                                Y = absoluteY;
                                MessageBox.Show($"ウィンドウの位置情報が取得できませんでした。\n絶対座標 ({X}, {Y}) を設定しました。", 
                                              "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        else
                        {
                            // ウィンドウが見つからない場合は絶対座標を使用
                            X = absoluteX;
                            Y = absoluteY;
                            MessageBox.Show($"指定されたウィンドウが見つかりません。\n絶対座標 ({X}, {Y}) を設定しました。", 
                                          "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        // エラーが発生した場合は絶対座標を使用
                        X = absoluteX;
                        Y = absoluteY;
                        MessageBox.Show($"ウィンドウ情報の取得に失敗しました。\n絶対座標 ({X}, {Y}) を設定しました。\nエラー: {ex.Message}", 
                                      "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // ウィンドウが指定されていない場合は絶対座標をそのまま使用
                    X = absoluteX;
                    Y = absoluteY;
                    MessageBox.Show($"絶対座標を設定しました: ({X}, {Y})", 
                                  "座標設定完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            } 
        }
        
        [RelayCommand] 
        public void BrowseModel() 
        { 
            var f = DialogHelper.SelectModelFile(); 
            if (f != null) 
            {
                ModelPath = f; // 相対パス変換は ModelPath プロパティで自動的に処理される
                System.Diagnostics.Debug.WriteLine($"ONNXモデル選択: 絶対パス={f}, 相対パス={PathHelper.ToRelativePath(f)}");
            }
        }
        
        [RelayCommand] 
        public void BrowseProgram() 
        { 
            var f = DialogHelper.SelectExecutableFile(); 
            if (f != null) 
            {
                ProgramPath = f; // 相対パス変換は ProgramPath プロパティで自動的に処理される
                System.Diagnostics.Debug.WriteLine($"プログラム選択: 絶対パス={f}, 相対パス={PathHelper.ToRelativePath(f)}");
            }
        }
        
        [RelayCommand] 
        public void BrowseWorkingDirectory() 
        { 
            var d = DialogHelper.SelectFolder(); 
            if (d != null) 
            {
                WorkingDirectory = d; // 相対パス変換は WorkingDirectory プロパティで自動的に処理される
                System.Diagnostics.Debug.WriteLine($"ワーキングディレクトリ選択: 絶対パス={d}, 相対パス={PathHelper.ToRelativePath(d)}");
            }
        }
        
        [RelayCommand] 
        public void BrowseSaveDirectory() 
        { 
            var d = DialogHelper.SelectFolder(); 
            if (d != null) 
            {
                SaveDirectory = d; // 相対パス変換は SaveDirectory プロパティで自動的に処理される
                System.Diagnostics.Debug.WriteLine($"保存先ディレクトリ選択: 絶対パス={d}, 相対パス={PathHelper.ToRelativePath(d)}");
            }
        }
        #endregion

        #region File Validation Properties
        /// <summary>
        /// 画像ファイルの存在チェック
        /// </summary>
        public bool IsImageFileValid
        {
            get
            {
                if (string.IsNullOrEmpty(ImagePath)) return true; // 空の場合は有効扱い
                var absolutePath = PathHelper.ToAbsolutePath(ImagePath);
                return File.Exists(absolutePath);
            }
        }

        /// <summary>
        /// ONNXモデルファイルの存在チェック
        /// </summary>
        public bool IsModelFileValid
        {
            get
            {
                if (string.IsNullOrEmpty(ModelPath)) return true; // 空の場合は有効扱い
                var absolutePath = PathHelper.ToAbsolutePath(ModelPath);
                return File.Exists(absolutePath);
            }
        }

        /// <summary>
        /// 実行ファイルの存在チェック
        /// </summary>
        public bool IsProgramFileValid
        {
            get
            {
                if (string.IsNullOrEmpty(ProgramPath)) return true; // 空の場合は有効扱い
                var absolutePath = PathHelper.ToAbsolutePath(ProgramPath);
                return File.Exists(absolutePath);
            }
        }

        /// <summary>
        /// ワーキングディレクトリの存在チェック
        /// </summary>
        public bool IsWorkingDirectoryValid
        {
            get
            {
                if (string.IsNullOrEmpty(WorkingDirectory)) return true; // 空の場合は有効扱い
                var absolutePath = PathHelper.ToAbsolutePath(WorkingDirectory);
                return Directory.Exists(absolutePath);
            }
        }

        /// <summary>
        /// 保存先ディレクトリの有効性チェック
        /// </summary>
        public bool IsSaveDirectoryValid
        {
            get
            {
                if (string.IsNullOrEmpty(SaveDirectory)) return true; // 空の場合は有効扱い
                
                // 絶対パスに変換して親ディレクトリをチェック
                var absolutePath = PathHelper.ToAbsolutePath(SaveDirectory);
                var parentDir = Path.GetDirectoryName(absolutePath);
                
                // 親ディレクトリが存在すれば作成可能として有効とみなす
                return !string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir);
            }
        }

        /// <summary>
        /// 画像ファイルエラーメッセージ
        /// </summary>
        public string ImageFileErrorMessage
        {
            get
            {
                if (string.IsNullOrEmpty(ImagePath)) return "";
                if (IsImageFileValid) return "";
                return $"画像ファイルが見つかりません: {ImagePath}";
            }
        }

        /// <summary>
        /// ONNXモデルファイルエラーメッセージ
        /// </summary>
        public string ModelFileErrorMessage
        {
            get
            {
                if (string.IsNullOrEmpty(ModelPath)) return "";
                if (IsModelFileValid) return "";
                return $"ONNXモデルファイルが見つかりません: {ModelPath}";
            }
        }

        /// <summary>
        /// 実行ファイルエラーメッセージ
        /// </summary>
        public string ProgramFileErrorMessage
        {
            get
            {
                if (string.IsNullOrEmpty(ProgramPath)) return "";
                if (IsProgramFileValid) return "";
                return $"実行ファイルが見つかりません: {ProgramPath}";
            }
        }

        /// <summary>
        /// ワーキングディレクトリエラーメッセージ
        /// </summary>
        public string WorkingDirectoryErrorMessage
        {
            get
            {
                if (string.IsNullOrEmpty(WorkingDirectory)) return "";
                if (IsWorkingDirectoryValid) return "";
                return $"ワーキングディレクトリが見つかりません: {WorkingDirectory}";
            }
        }

        /// <summary>
        /// 保存先ディレクトリエラーメッセージ
        /// </summary>
        public string SaveDirectoryErrorMessage
        {
            get
            {
                if (string.IsNullOrEmpty(SaveDirectory)) return "";
                if (IsSaveDirectoryValid) return "";
                return $"保存先ディレクトリの親フォルダが見つかりません: {SaveDirectory}";
            }
        }
        #endregion

        #region Windows API Helper Methods
        // Windows API用のプライベートメソッド
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private static IntPtr GetWindowHandle(string windowTitle, string windowClassName)
        {
            // クラス名とタイトルの両方を使用してウィンドウを検索
            if (!string.IsNullOrEmpty(windowClassName) && !string.IsNullOrEmpty(windowTitle))
            {
                return FindWindow(windowClassName, windowTitle);
            }
            else if (!string.IsNullOrEmpty(windowClassName))
            {
                return FindWindow(windowClassName, null);
            }
            else if (!string.IsNullOrEmpty(windowTitle))
            {
                return FindWindow(null, windowTitle);
            }
            return IntPtr.Zero;
        }
        #endregion

        #region External API
        public ICommandListItem? GetItem() => Item;
        
        public void SetItem(ICommandListItem? item) 
        {
            _logger.LogDebug("SetItem called with: {ItemType}", item?.ItemType ?? "null");
            
            if (item is HotkeyItem hotkeyItem)
            {
                _logger.LogDebug("HotkeyItem detected: Ctrl={Ctrl}, Alt={Alt}, Shift={Shift}, Key={Key}",
                    hotkeyItem.Ctrl, hotkeyItem.Alt, hotkeyItem.Shift, hotkeyItem.Key);
            }
            
            Item = item;
            
            // 待機時間コンポーネントを更新
            if (item is IWaitItem)
            {
                OnWaitChanged();
            }
            
            // ホットキーアイテムの場合、プロパティの状態をログ出力
            if (item is HotkeyItem)
            {
                _logger.LogDebug("After SetItem - Hotkey properties: Ctrl={Ctrl}, Alt={Alt}, Shift={Shift}, Key={Key}",
                    Ctrl, Alt, Shift, Key);
            }
        }
        
        public void SetListCount(int listCount) 
        {
            ListCount = listCount;
            _logger.LogDebug("ListCount set to: {ListCount}", listCount);
        }
        
        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            _logger.LogDebug("Running state set to: {IsRunning}", isRunning);
        }
        
        public void Prepare() { }
        
        /// <summary>
        /// デバッグ用：現在の状態を出力
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public void PrintDebugState()
        {
            _logger.LogDebug("=== EditPanelViewModel Debug State ===");
            _logger.LogDebug("Item: {ItemType}", Item?.GetType().Name ?? "null");
            _logger.LogDebug("ItemType: {ItemType}", Item?.ItemType ?? "null");
            _logger.LogDebug("SelectedItemType: {SelectedItemType}", SelectedItemType);
            _logger.LogDebug("SelectedItemTypeObj: {DisplayName} ({TypeName})", SelectedItemTypeObj?.DisplayName ?? "null", SelectedItemTypeObj?.TypeName ?? "null");
            _logger.LogDebug("ItemTypes Count: {Count}", ItemTypes.Count);
            _logger.LogDebug("=== End Debug State ===");
        }

        /// <summary>
        /// デバッグ用：ホットキー設定の詳細状態を出力
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public void PrintHotkeyDebugState()
        {
            _logger.LogDebug("=== Hotkey Debug State ===");
            _logger.LogDebug("Item: {ItemType}", Item?.GetType().Name ?? "null");
            _logger.LogDebug("Item is HotkeyItem: {IsHotkeyItem}", Item is HotkeyItem);
            _logger.LogDebug("IsHotkeyItem property: {IsHotkeyItemProperty}", IsHotkeyItem);
            
            if (Item is HotkeyItem hotkeyItem)
            {
                _logger.LogDebug("Direct HotkeyItem properties:");
                _logger.LogDebug("  Ctrl: {Ctrl}", hotkeyItem.Ctrl);
                _logger.LogDebug("  Alt: {Alt}", hotkeyItem.Alt);
                _logger.LogDebug("  Shift: {Shift}", hotkeyItem.Shift);
                _logger.LogDebug("  Key: {Key}", hotkeyItem.Key);
            }
            
            _logger.LogDebug("ViewModel properties:");
            _logger.LogDebug("  Ctrl: {Ctrl}", Ctrl);
            _logger.LogDebug("  Alt: {Alt}", Alt);
            _logger.LogDebug("  Shift: {Shift}", Shift);
            _logger.LogDebug("  Key: {Key}", Key);
            
            _logger.LogDebug("PropertyManager test:");
            var testCtrl = _propertyManager.Ctrl.GetValue(Item);
            var testAlt = _propertyManager.Alt.GetValue(Item);
            var testShift = _propertyManager.Shift.GetValue(Item);
            var testKey = _propertyManager.Key.GetValue(Item);
            _logger.LogDebug("  PropertyManager Ctrl: {Ctrl}", testCtrl);
            _logger.LogDebug("  PropertyManager Alt: {Alt}", testAlt);
            _logger.LogDebug("  PropertyManager Shift: {Shift}", testShift);
            _logger.LogDebug("  PropertyManager Key: {Key}", testKey);
            
            _logger.LogDebug("=== End Hotkey Debug State ===");
        }
        #endregion
    }
}