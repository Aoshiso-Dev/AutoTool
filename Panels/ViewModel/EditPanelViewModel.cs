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

namespace MacroPanels.ViewModel
{
    public partial class EditPanelViewModel : ObservableObject
    {
        private readonly EditPanelPropertyManager _propertyManager = new();

        [ObservableProperty]
        private bool _isRunning = false;
        private bool _isUpdating;
        private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(120) };

        #region Item
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
        #endregion

        #region ListCount
        private int _listCount = 0;
        public int ListCount
        {
            get => _listCount;
            set { SetProperty(ref _listCount, value); UpdateIsProperties(); UpdateProperties(); }
        }
        #endregion

        #region IsProperties
        public bool IsListNotEmpty => ListCount > 0;
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

        #region Properties (via PropertyManager)
        public string WindowTitleText => string.IsNullOrEmpty(WindowTitle) ? "指定なし" : WindowTitle;
        public string WindowTitle { get => _propertyManager.WindowTitle.GetValue(Item); set { _propertyManager.WindowTitle.SetValue(Item, value); UpdateProperties(); } }
        public string WindowClassNameText => string.IsNullOrEmpty(WindowClassName) ? "指定なし" : WindowClassName;
        public string WindowClassName { get => _propertyManager.WindowClassName.GetValue(Item); set { _propertyManager.WindowClassName.SetValue(Item, value); UpdateProperties(); } }
        public string ImagePath { get => _propertyManager.ImagePath.GetValue(Item); set { _propertyManager.ImagePath.SetValue(Item, value); UpdateProperties(); } }
        public double Threshold { get => _propertyManager.Threshold.GetValue(Item); set { _propertyManager.Threshold.SetValue(Item, value); UpdateProperties(); } }
        public Color? SearchColor { get => _propertyManager.SearchColor.GetValue(Item); set { _propertyManager.SearchColor.SetValue(Item, value); UpdateProperties(); OnPropertyChanged(nameof(SearchColorBrush)); OnPropertyChanged(nameof(SearchColorText)); OnPropertyChanged(nameof(SearchColorTextColor)); } }
        public int Timeout { get => _propertyManager.Timeout.GetValue(Item); set { _propertyManager.Timeout.SetValue(Item, value); UpdateProperties(); } }
        public int Interval { get => _propertyManager.Interval.GetValue(Item); set { _propertyManager.Interval.SetValue(Item, value); UpdateProperties(); } }
        public System.Windows.Input.MouseButton MouseButton { get => _propertyManager.MouseButton.GetValue(Item); set { _propertyManager.MouseButton.SetValue(Item, value); UpdateProperties(); } }
        public bool Ctrl { get => _propertyManager.Ctrl.GetValue(Item); set { _propertyManager.Ctrl.SetValue(Item, value); UpdateProperties(); } }
        public bool Alt { get => _propertyManager.Alt.GetValue(Item); set { _propertyManager.Alt.SetValue(Item, value); UpdateProperties(); } }
        public bool Shift { get => _propertyManager.Shift.GetValue(Item); set { _propertyManager.Shift.SetValue(Item, value); UpdateProperties(); } }
        public Key Key { get => _propertyManager.Key.GetValue(Item); set { _propertyManager.Key.SetValue(Item, value); UpdateProperties(); } }
        public int X { get => _propertyManager.X.GetValue(Item); set { _propertyManager.X.SetValue(Item, value); UpdateProperties(); } }
        public int Y { get => _propertyManager.Y.GetValue(Item); set { _propertyManager.Y.SetValue(Item, value); UpdateProperties(); } }
        public int Wait { get => _propertyManager.Wait.GetValue(Item); set { _propertyManager.Wait.SetValue(Item, value); UpdateProperties(); } }
        public int LoopCount { get => _propertyManager.LoopCount.GetValue(Item); set { _propertyManager.LoopCount.SetValue(Item, value); UpdateProperties(); } }
        public string ModelPath { get => _propertyManager.ModelPath.GetValue(Item); set { _propertyManager.ModelPath.SetValue(Item, value); UpdateProperties(); } }
        public int ClassID { get => _propertyManager.ClassID.GetValue(Item); set { _propertyManager.ClassID.SetValue(Item, value); UpdateProperties(); } }
        public string AIDetectMode { get => _propertyManager.Mode.GetValue(Item); set { _propertyManager.Mode.SetValue(Item, value); UpdateProperties(); } }
        public string ProgramPath { get => _propertyManager.ProgramPath.GetValue(Item); set { _propertyManager.ProgramPath.SetValue(Item, value); UpdateProperties(); } }
        public string Arguments { get => _propertyManager.Arguments.GetValue(Item); set { _propertyManager.Arguments.SetValue(Item, value); UpdateProperties(); } }
        public string WorkingDirectory { get => _propertyManager.WorkingDirectory.GetValue(Item); set { _propertyManager.WorkingDirectory.SetValue(Item, value); UpdateProperties(); } }
        public bool WaitForExit { get => _propertyManager.WaitForExit.GetValue(Item); set { _propertyManager.WaitForExit.SetValue(Item, value); UpdateProperties(); } }
        public string VariableName { get => _propertyManager.VariableName.GetValue(Item); set { _propertyManager.VariableName.SetValue(Item, value); UpdateProperties(); } }
        public string VariableValue { get => _propertyManager.VariableValue.GetValue(Item); set { _propertyManager.VariableValue.SetValue(Item, value); UpdateProperties(); } }
        public string CompareOperator { get => _propertyManager.CompareOperator.GetValue(Item); set { _propertyManager.CompareOperator.SetValue(Item, value); UpdateProperties(); } }
        public string CompareValue { get => _propertyManager.CompareValue.GetValue(Item); set { _propertyManager.CompareValue.SetValue(Item, value); UpdateProperties(); } }
        public string SaveDirectory { get => _propertyManager.SaveDirectory.GetValue(Item); set { _propertyManager.SaveDirectory.SetValue(Item, value); UpdateProperties(); } }
        public double ConfThreshold { get => _propertyManager.ConfThreshold.GetValue(Item); set { _propertyManager.ConfThreshold.SetValue(Item, value); UpdateProperties(); } }
        public double IoUThreshold { get => _propertyManager.IoUThreshold.GetValue(Item); set { _propertyManager.IoUThreshold.SetValue(Item, value); UpdateProperties(); } }
        #endregion

        #region ColorPicker
        public Brush SearchColorBrush => new SolidColorBrush(SearchColor ?? Color.FromArgb(0, 0, 0, 0));
        public string SearchColorText => SearchColor != null ? $"R:{SearchColor.Value.R:D3} G:{SearchColor.Value.G:D3} B:{SearchColor.Value.B:D3}" : "指定なし";
        public Brush SearchColorTextColor => SearchColor != null ? new SolidColorBrush(Color.FromArgb(255, (byte)(255 - SearchColor.Value.R), (byte)(255 - SearchColor.Value.G), (byte)(255 - SearchColor.Value.B))) : new SolidColorBrush(Colors.Black);
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

        public EditPanelViewModel()
        {
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
            System.Diagnostics.Debug.WriteLine($"ItemTypes initialized with {ItemTypes.Count} items");
            if (ItemTypes.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"First item: {ItemTypes[0].DisplayName} ({ItemTypes[0].TypeName})");
            }
                
            foreach (var button in Enum.GetValues(typeof(System.Windows.Input.MouseButton)).Cast<System.Windows.Input.MouseButton>()) 
                MouseButtons.Add(button);
                
            InitializeOperators();
            InitializeAIDetectModes();
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

        #region OnChanged
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

        #region Update
        private void UpdateIsProperties()
        {
            // 配列をstaticにしてGC圧を軽減
            foreach (var name in IsPropertyNames)
                OnPropertyChanged(name);
        }

        private static readonly string[] IsPropertyNames = {
            nameof(IsListNotEmpty), nameof(IsNotNullItem), nameof(IsWaitImageItem), 
            nameof(IsClickImageItem), nameof(IsClickImageAIItem), nameof(IsHotkeyItem), 
            nameof(IsClickItem), nameof(IsWaitItem), nameof(IsLoopItem), 
            nameof(IsEndLoopItem), nameof(IsBreakItem), nameof(IsIfImageExistItem), 
            nameof(IsIfImageNotExistItem), nameof(IsEndIfItem), nameof(IsIfImageExistAIItem), 
            nameof(IsIfImageNotExistAIItem), nameof(IsExecuteProgramItem), nameof(IsSetVariableItem), 
            nameof(IsSetVariableAIItem), nameof(IsIfVariableItem), nameof(IsScreenshotItem)
        };

        private static readonly string[] AllPropertyNames = {
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
            nameof(SaveDirectory)
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
        [RelayCommand] public void Browse() { var f = DialogHelper.SelectImageFile(); if (f != null) ImagePath = f; }
        [RelayCommand] public void Capture() { var cw = new CaptureWindow { Mode = 0 }; if (cw.ShowDialog() == true) { var path = DialogHelper.CreateCaptureFilePath(); var mat = OpenCVHelper.ScreenCaptureHelper.CaptureRegion(cw.SelectedRegion); OpenCVHelper.ScreenCaptureHelper.SaveCapture(mat, path); ImagePath = path; } }
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
        [RelayCommand] public void BrowseModel() { var f = DialogHelper.SelectModelFile(); if (f != null) ModelPath = f; }
        [RelayCommand] public void BrowseProgram() { var f = DialogHelper.SelectExecutableFile(); if (f != null) ProgramPath = f; }
        [RelayCommand] public void BrowseWorkingDirectory() { var d = DialogHelper.SelectFolder(); if (d != null) WorkingDirectory = d; }
        [RelayCommand] public void BrowseSaveDirectory() { var d = DialogHelper.SelectFolder(); if (d != null) SaveDirectory = d; }
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
            System.Diagnostics.Debug.WriteLine($"SetItem called with: {item?.ItemType ?? "null"}");
            Item = item;
        }
        
        public void SetListCount(int listCount) => ListCount = listCount;
        public void SetRunningState(bool isRunning) => IsRunning = isRunning;
        public void Prepare() { }
        
        /// <summary>
        /// デバッグ用：現在の状態を出力
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public void PrintDebugState()
        {
            System.Diagnostics.Debug.WriteLine("=== EditPanelViewModel Debug State ===");
            System.Diagnostics.Debug.WriteLine($"Item: {Item?.GetType().Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"ItemType: {Item?.ItemType ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"SelectedItemType: {SelectedItemType}");
            System.Diagnostics.Debug.WriteLine($"SelectedItemTypeObj: {SelectedItemTypeObj?.DisplayName ?? "null"} ({SelectedItemTypeObj?.TypeName ?? "null"})");
            System.Diagnostics.Debug.WriteLine($"ItemTypes Count: {ItemTypes.Count}");
            System.Diagnostics.Debug.WriteLine("=== End Debug State ===");
        }
        #endregion
    }
}