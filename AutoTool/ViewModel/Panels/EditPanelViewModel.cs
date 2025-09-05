using AutoTool.Message;
using AutoTool.Model.CommandDefinition;
using AutoTool.Model.List.Class;
using AutoTool.Model.MacroFactory;
using AutoTool.ViewModel.Shared;
using AutoTool.Services.Capture;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.LineDescriptor;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using static MouseHelper.Input;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// 動的UI生成対応EditPanelViewModel
    /// </summary>
    public partial class EditPanelViewModel : ObservableObject
    {
        private readonly ILogger<EditPanelViewModel> _logger;
        private readonly IMessenger _messenger;
        private readonly AutoTool.Services.Mouse.IMouseService _mouseService;
        private readonly ICaptureService _captureService;
        private readonly IServiceProvider _serviceProvider;
        private bool _isUpdating = false;

        // マクロファイルのベースパス（相対パス解決用）
        private string _macroFileBasePath = string.Empty;

        [ObservableProperty]
        private UniversalCommandItem? _selectedItem;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private bool _isWaitingForRightClick = false;

        [ObservableProperty]
        private string _mouseWaitMessage = string.Empty;

        // 動的設定UI用の新しいプロパティ
        [ObservableProperty]
        private ObservableCollection<SettingCategoryGroup> _settingGroups = new();

        [ObservableProperty]
        private ObservableCollection<SettingDefinition> _settingDefinitions = new();

        [ObservableProperty]
        private bool _isDynamicItem = false;

        [ObservableProperty]
        private bool _isLegacyItem = false;

        // Collections for binding (既存)
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

        // 画像プレビュー関連プロパティ
        [ObservableProperty]
        private System.Windows.Media.ImageSource? _imagePreview;

        [ObservableProperty]
        private bool _hasImagePreview = false;

        [ObservableProperty]
        private string _imageInfo = string.Empty;

        [ObservableProperty]
        private bool _hasImageInfo = false;

        [ObservableProperty]
        private string _modelInfo = string.Empty;

        [ObservableProperty]
        private bool _hasModelInfo = false;

        [ObservableProperty]
        private string _programInfo = string.Empty;

        [ObservableProperty]
        private bool _hasProgramInfo = false;

        [ObservableProperty]
        private string _saveDirectoryInfo = string.Empty;

        [ObservableProperty]
        private bool _hasSaveDirectoryInfo = false;

        // 動的設定値のディクショナリ
        [ObservableProperty]
        private Dictionary<string, object?> _dynamicValues = new();

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

        // アイテムタイプ判定プロパティ（既存システム用）
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
        public bool IsListNotEmptyButNoSelection => false; // EditPanelでは常にfalse

        // 動的UI制御プロパティ
        public bool ShowDynamicSettings => IsDynamicItem && SettingGroups.Count > 0;
        public bool ShowLegacySettings => IsLegacyItem;

        public EditPanelViewModel(
            ILogger<EditPanelViewModel> logger, 
            AutoTool.Services.Mouse.IMouseService mouseService,
            ICaptureService captureService,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mouseService = mouseService ?? throw new ArgumentNullException(nameof(mouseService));
            _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _messenger = WeakReferenceMessenger.Default;

            InitializeCollections();
            SetupMessaging();

            _logger.LogInformation("EditPanelViewModel (動的UI対応版) を初期化しました");
        }

        private void SetupMessaging()
        {
            try
            {
                _messenger.Register<ChangeSelectedMessage>(this, (r, m) =>
                {
                    try
                    {
                        _logger.LogDebug("=== ChangeSelectedMessage受信: {ItemType} ===", m.SelectedItem?.ItemType ?? "null");
                        
                        if (m.SelectedItem != null)
                        {
                            _logger.LogInformation("📋 EditPanel選択受信: {ItemType} (ActualType: {ActualType})", 
                                m.SelectedItem.ItemType, m.SelectedItem.GetType().Name);
                        }
                        
                        SelectedItem = m.SelectedItem;
                        
                        _logger.LogDebug("=== ChangeSelectedMessage処理完了 ===");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ChangeSelectedMessage処理中にエラー");
                    }
                });

                _messenger.Register<ChangeItemTypeMessage>(this, (r, m) =>
                {
                    try
                    {
                        _logger.LogDebug("ChangeItemTypeMessage受信: {OldType} -> {NewType}",
                            m.OldItem?.ItemType, m.NewItem?.ItemType);
                        
                        // アイテムタイプ変更後の選択更新
                        if (SelectedItem == m.OldItem)
                        {
                            SelectedItem = m.NewItem;
                            _logger.LogInformation("✅ アイテムタイプ変更後の選択更新完了");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ChangeItemTypeMessage処理中にエラー");
                    }
                });

                _messenger.Register<MacroExecutionStateMessage>(this, (r, m) =>
                {
                    try
                    {
                        _logger.LogDebug("MacroExecutionStateMessage受信: {IsRunning}", m.IsRunning);
                        IsRunning = m.IsRunning;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "MacroExecutionStateMessage処理中にエラー");
                    }
                });

                // ファイル読み込み時のベースパス更新（遅延更新付き）が発生した場合
                _messenger.Register<LoadMessage>(this, async (r, m) =>
                {
                    try
                    {
                        UpdateMacroFileBasePath(m.FilePath);
                        // ファイル読み込み完了後に少し待ってからプレビュー更新
                        await Task.Delay(100);
                        if (SelectedItem != null)
                        {
                            UpdateImagePreview();
                            UpdateModelInfo();
                            UpdateProgramInfo();
                            UpdateSaveDirectoryInfo();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "LoadMessage処理中にエラー");
                    }
                });

                // LoadFileMessage に対する処理
                _messenger.Register<LoadFileMessage>(this, async (r, m) =>
                {
                    try
                    {
                        UpdateMacroFileBasePath(m.FilePath);
                        await Task.Delay(100);
                        if (SelectedItem != null)
                        {
                            UpdateImagePreview();
                            UpdateModelInfo();
                            UpdateProgramInfo();
                            UpdateSaveDirectoryInfo();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "LoadFileMessage処理中にエラー");
                    }
                });

                // SaveMessage と SaveFileMessage に対する処理
                _messenger.Register<SaveMessage>(this, (r, m) => 
                {
                    try
                    {
                        UpdateMacroFileBasePath(m.FilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "SaveMessage処理中にエラー");
                    }
                });
                
                _messenger.Register<SaveFileMessage>(this, (r, m) => 
                {
                    try
                    {
                        UpdateMacroFileBasePath(m.FilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "SaveFileMessage処理中にエラー");
                    }
                });

                _logger.LogDebug("メッセージング設定完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "メッセージング設定中にエラー");
            }
        }

        private void InitializeCollections()
        {
            try
            {
                _logger.LogDebug("コレクション初期化開始");

                // CommandTypes
                DirectCommandRegistry.Initialize(_serviceProvider);
                var displayItems = DirectCommandRegistry.GetOrderedTypeNames()
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = DirectCommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = DirectCommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList();
                ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
                _logger.LogDebug("ItemTypes初期化完了: {Count}個", ItemTypes.Count);

                // MouseButtons
                MouseButtons.Clear();
                foreach (var button in Enum.GetValues(typeof(MouseButton)).Cast<MouseButton>())
                    MouseButtons.Add(button);
                _logger.LogDebug("MouseButtons初期化完了: {Count}個", MouseButtons.Count);

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
                _logger.LogDebug("KeyList初期化完了: {Count}個", KeyList.Count);

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
                _logger.LogDebug("Operators初期化完了: {Count}個", Operators.Count);

                // AI Detect Modes
                AiDetectModes.Clear();
                AiDetectModes.Add(new AutoTool.ViewModel.Shared.AIDetectModeItem { Key = "Class", DisplayName = "クラス検出" });
                AiDetectModes.Add(new AutoTool.ViewModel.Shared.AIDetectModeItem { Key = "Count", DisplayName = "数量検出" });
                _logger.LogDebug("AiDetectModes初期化完了: {Count}個", AiDetectModes.Count);

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
                _logger.LogDebug("BackgroundClickMethods初期化完了: {Count}個", BackgroundClickMethods.Count);

                _logger.LogInformation("EditPanelViewModel コレクション初期化完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EditPanelViewModel コレクション初期化中にエラー");
                throw;
            }
        }

        // OnSelectedItemChangedはObservablePropertyで自動生成される partial void
        partial void OnSelectedItemChanged(UniversalCommandItem? value)
        {
            try
            {
                _isUpdating = true;

                _logger.LogDebug("=== SelectedItem変更開始 ===");
                _logger.LogDebug("新しいアイテム: {ItemType} (ActualType: {ActualType})", 
                    value?.ItemType ?? "null", value?.GetType().Name ?? "null");

                if (value != null)
                {
                    // UniversalCommandItem が選択された場合
                    _logger.LogDebug("UniversalCommandItemとして直接使用: {ItemType}", value.ItemType);
                    
                    // 動的システムの処理
                    _logger.LogDebug("動的設定初期化開始: {ItemType}", value.ItemType);
                    InitializeDynamicSettings(value);
                    IsDynamicItem = true;
                    IsLegacyItem = false;
                    
                    _logger.LogInformation("✅ 動的システムアイテム選択: {ItemType} (設定項目: {SettingCount}個, グループ: {GroupCount}個)", 
                        value.ItemType, SettingDefinitions.Count, SettingGroups.Count);
                }
                else
                {
                    // 選択なし
                    IsDynamicItem = false;
                    IsLegacyItem = false;
                    SettingGroups.Clear();
                    SettingDefinitions.Clear();
                    
                    _logger.LogDebug("アイテム選択なし");
                }

                // ShowDynamicSettings と ShowLegacySettings の更新を強制
                OnPropertyChanged(nameof(ShowDynamicSettings));
                OnPropertyChanged(nameof(ShowLegacySettings));

                _logger.LogDebug("最終的な動的設定表示状態: IsDynamicItem={IsDynamic}, IsLegacyItem={IsLegacy}, ShowDynamicSettings={ShowDynamic}, ShowLegacySettings={ShowLegacy}",
                    IsDynamicItem, IsLegacyItem, ShowDynamicSettings, ShowLegacySettings);

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

                // 画像プレビューと関連情報を更新
                UpdateImagePreview();
                UpdateModelInfo();
                UpdateProgramInfo();
                UpdateSaveDirectoryInfo();

                // 全ての判定プロパティを更新
                NotifyAllPropertiesChanged();

                _logger.LogDebug("=== SelectedItem変更完了 ===");
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
                var newItem = DirectCommandRegistry.CreateCommandItem(value.TypeName);
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
            // 基本判定プロパティ
            var properties = new[]
            {
                nameof(IsWaitImageItem), nameof(IsClickImageItem), nameof(IsClickImageAIItem),
                nameof(IsHotkeyItem), nameof(IsClickItem), nameof(IsWaitItem),
                nameof(IsLoopItem), nameof(IsLoopEndItem), nameof(IsLoopBreakItem),
                nameof(IsIfImageExistItem), nameof(IsIfImageNotExistItem), nameof(IsIfImageExistAIItem),
                nameof(IsIfImageNotExistAIItem), nameof(IsIfEndItem),
                nameof(IsIfVariableItem), nameof(IsExecuteItem), nameof(IsSetVariableItem),
                nameof(IsSetVariableAIItem), nameof(IsScreenshotItem), nameof(IsImageBasedItem),
                nameof(IsAIBasedItem), nameof(IsVariableItem), nameof(IsLoopRelatedItem),
                nameof(IsIfRelatedItem), nameof(ShowWindowInfo), nameof(ShowAdvancedSettings),
                nameof(IsNotNullItem), nameof(IsListEmpty), nameof(IsListNotEmpty),
                nameof(IsListNotEmptyButNoSelection)
            };

            // 動的設定UI制御プロパティを追加
            var dynamicProperties = new[]
            {
                nameof(IsDynamicItem), nameof(IsLegacyItem),
                nameof(ShowDynamicSettings), nameof(ShowLegacySettings)
            };

            // 値プロパティ
            var valueProperties = new[]
            {
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

            // 基本判定プロパティの通知
            foreach (var property in properties)
            {
                OnPropertyChanged(property);
            }

            // 動的設定プロパティの通知
            foreach (var property in dynamicProperties)
            {
                OnPropertyChanged(property);
            }

            // 値プロパティの通知
            foreach (var property in valueProperties)
            {
                OnPropertyChanged(property);
            }

            _logger.LogDebug("全プロパティ変更通知完了: {Count}個", properties.Length + dynamicProperties.Length + valueProperties.Length);
        }

        /// <summary>
        /// マクロファイルのベースパスを更新
        /// </summary>
        private void UpdateMacroFileBasePath(string? filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    _macroFileBasePath = Path.GetDirectoryName(filePath) ?? string.Empty;
                    _logger.LogDebug("マクロファイルベースパス更新: {BasePath}", _macroFileBasePath);

                    // ベースパスが更新された時に、既に選択されているアイテムがある場合はプレビューを更新
                    if (SelectedItem != null)
                    {
                        _logger.LogDebug("ベースパス更新に伴い、選択中アイテムのプレビューを更新: {ItemType}", SelectedItem.ItemType);
                        UpdateImagePreview();
                        UpdateModelInfo();
                        UpdateProgramInfo();
                        UpdateSaveDirectoryInfo();

                        // プロパティ変更通知も送信して、UIの表示を更新
                        NotifyAllPropertiesChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロファイルベースパス更新エラー: {FilePath}", filePath);
            }
        }

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

        #region Properties for data binding (完全版)

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
            set
            {
                // 選択されたパスを相対パスに変換してから保存
                var pathToSave = ConvertToRelativePath(value);
                SetItemProperty("ImagePath", pathToSave);
                // 画像パスが変更された時に画像プレビューを更新
                UpdateImagePreview();
                _logger.LogDebug("ImagePath更新: {Original} -> {Relative}", value, pathToSave);
            }
        }

        public double Threshold
        {
            get => GetItemProperty<double>("Threshold");
            set => SetItemProperty("Threshold", value);
        }

        public System.Windows.Media.Color? SearchColor
        {
            get => GetItemProperty<System.Windows.Media.Color?>("SearchColor");
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

        // Wait time properties - WaitItemのWaitプロパティ（ミリ秒）に対応
        public int WaitHours
        {
            get
            {
                var waitMs = GetItemProperty<int>("Wait");
                return (int)TimeSpan.FromMilliseconds(waitMs).TotalHours;
            }
            set => SetWaitTime(hours: value);
        }

        public int WaitMinutes
        {
            get
            {
                var waitMs = GetItemProperty<int>("Wait");
                return TimeSpan.FromMilliseconds(waitMs).Minutes;
            }
            set => SetWaitTime(minutes: value);
        }

        public int WaitSeconds
        {
            get
            {
                var waitMs = GetItemProperty<int>("Wait");
                return TimeSpan.FromMilliseconds(waitMs).Seconds;
            }
            set => SetWaitTime(seconds: value);
        }

        public int WaitMilliseconds
        {
            get
            {
                var waitMs = GetItemProperty<int>("Wait");
                return TimeSpan.FromMilliseconds(waitMs).Milliseconds;
            }
            set => SetWaitTime(milliseconds: value);
        }

        private void SetWaitTime(int? hours = null, int? minutes = null, int? seconds = null, int? milliseconds = null)
        {
            if (SelectedItem == null) return;

            try
            {
                // 現在の値を取得
                var currentWaitMs = GetItemProperty<int>("Wait");
                var currentTime = TimeSpan.FromMilliseconds(currentWaitMs);

                var newTime = new TimeSpan(
                    0, // days
                    hours ?? (int)currentTime.TotalHours,
                    minutes ?? currentTime.Minutes,
                    seconds ?? currentTime.Seconds,
                    milliseconds ?? currentTime.Milliseconds
                );

                // WaitItemのWaitプロパティ（ミリ秒）に設定
                var totalMs = (int)newTime.TotalMilliseconds;
                SetItemProperty("Wait", totalMs);

                // 他の時間プロパティの更新通知
                OnPropertyChanged(nameof(WaitHours));
                OnPropertyChanged(nameof(WaitMinutes));
                OnPropertyChanged(nameof(WaitSeconds));
                OnPropertyChanged(nameof(WaitMilliseconds));

                _logger.LogDebug("Wait時間設定: {Hours}h {Minutes}m {Seconds}s {Milliseconds}ms (総計: {TotalMs}ms)",
                    newTime.Hours, newTime.Minutes, newTime.Seconds, newTime.Milliseconds, totalMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wait時間設定エラー");
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
            set
            {
                var pathToSave = ConvertToRelativePath(value);
                SetItemProperty("ModelPath", pathToSave);
                UpdateModelInfo();
            }
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
            set
            {
                var pathToSave = ConvertToRelativePath(value);
                SetItemProperty("ProgramPath", pathToSave);
                UpdateProgramInfo();
            }
        }

        public string Arguments
        {
            get => GetItemProperty<string>("Arguments") ?? string.Empty;
            set => SetItemProperty("Arguments", value);
        }

        public string WorkingDirectory
        {
            get => GetItemProperty<string>("WorkingDirectory") ?? string.Empty;
            set
            {
                var pathToSave = ConvertToRelativePath(value);
                SetItemProperty("WorkingDirectory", pathToSave);
            }
        }

        public bool WaitForExit
        {
            get => GetItemProperty<bool>("WaitForExit");
            set => SetItemProperty("WaitForExit", value);
        }

        public string SaveDirectory
        {
            get => GetItemProperty<string>("SaveDirectory") ?? string.Empty;
            set
            {
                var pathToSave = ConvertToRelativePath(value);
                SetItemProperty("SaveDirectory", pathToSave);
                UpdateSaveDirectoryInfo();
            }
        }

        #endregion

        [RelayCommand]
        private void BrowseImageFile()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "画像ファイルを選択",
                    Filter = "画像ファイル (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|すべてのファイル (*.*)|*.*"
                };
                var currentImagePath = GetItemProperty<string>("ImagePath") ?? string.Empty;
                var currentPath = ResolvePath(currentImagePath);
                if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
                    dialog.FileName = Path.GetFileName(currentPath);
                }
                else if (!string.IsNullOrEmpty(_macroFileBasePath))
                {
                    dialog.InitialDirectory = _macroFileBasePath;
                }
                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = dialog.FileName;
                    var pathToSave = ConvertToRelativePath(selectedPath);
                    SetItemProperty("ImagePath", pathToSave);
                    UpdateImagePreview();
                    _logger.LogInformation("画像ファイルを選択: {ImagePath} (相対パス: {RelativePath})",
                        selectedPath, pathToSave);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "画像ファイル選択中にエラー");
            }
        }

        [RelayCommand]
        private void ClearImagePreview()
        {
            try
            {
                SetItemProperty("ImagePath", string.Empty);
                ImagePreview = null;
                _logger.LogInformation("画像プレビューをクリア");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "画像プレビュークリア中にエラー");
            }
        }

        [RelayCommand]
        private void BrowseModelFile()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "AIモデルファイルを選択",
                    Filter = "ONNXファイル (*.onnx)|*.onnx|すべてのファイル (*.*)|*.*"
                };
                var currentModelPath = GetItemProperty<string>("ModelPath") ?? string.Empty;
                var currentPath = ResolvePath(currentModelPath);
                if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
                    dialog.FileName = Path.GetFileName(currentPath);
                }
                else if (!string.IsNullOrEmpty(_macroFileBasePath))
                {
                    dialog.InitialDirectory = _macroFileBasePath;
                }
                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = dialog.FileName;
                    var pathToSave = ConvertToRelativePath(selectedPath);
                    SetItemProperty("ModelPath", pathToSave);
                    UpdateModelInfo();
                    _logger.LogInformation("AIモデルファイルを選択: {ModelPath} (相対パス: {RelativePath})",
                        selectedPath, pathToSave);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AIモデルファイル選択中にエラー");
            }
        }

        [RelayCommand]
        private void BrowseProgramFile()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "実行ファイルを選択",
                    Filter = "実行ファイル (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|すべてのファイル (*.*)|*.*"
                };
                var currentProgramPath = GetItemProperty<string>("ProgramPath") ?? string.Empty;
                var currentPath = ResolvePath(currentProgramPath);
                if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
                    dialog.FileName = Path.GetFileName(currentPath);
                }
                else if (!string.IsNullOrEmpty(_macroFileBasePath))
                {
                    dialog.InitialDirectory = _macroFileBasePath;
                }
                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = dialog.FileName;
                    var pathToSave = ConvertToRelativePath(selectedPath);
                    SetItemProperty("ProgramPath", pathToSave);
                    UpdateProgramInfo();
                    _logger.LogInformation("実行ファイルを選択: {ProgramPath} (相対パス: {RelativePath})",
                        selectedPath, pathToSave);
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
                // 簡易的なフォルダ選択（OpenFileDialogを使った代替実装）
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "作業ディレクトリを選択 (任意のファイルを選択してディレクトリを指定)",
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "フォルダを選択"
                };
                var currentWorkingDir = GetItemProperty<string>("WorkingDirectory") ?? string.Empty;
                var currentPath = ResolvePath(currentWorkingDir);
                if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                {
                    dialog.InitialDirectory = currentPath;
                }
                else if (!string.IsNullOrEmpty(_macroFileBasePath))
                {
                    dialog.InitialDirectory = _macroFileBasePath;
                }
                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = Path.GetDirectoryName(dialog.FileName);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        var pathToSave = ConvertToRelativePath(selectedPath);
                        SetItemProperty("WorkingDirectory", pathToSave);
                        _logger.LogInformation("作業ディレクトリを選択: {WorkingDirectory} (相対パス: {RelativePath})",
                            selectedPath, pathToSave);
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
                // 簡易的なフォルダ選択（OpenFileDialogを使った代替実装）
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "保存ディレクトリを選択 (任意のファイルを選択してディレクトリを指定)",
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "フォルダを選択"
                };

                var currentSaveDir = GetItemProperty<string>("SaveDirectory") ?? string.Empty;
                var currentPath = ResolvePath(currentSaveDir);
                if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                {
                    dialog.InitialDirectory = currentPath;
                }
                else if (!string.IsNullOrEmpty(_macroFileBasePath))
                {
                    dialog.InitialDirectory = _macroFileBasePath;
                }

                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = Path.GetDirectoryName(dialog.FileName);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        var pathToSave = ConvertToRelativePath(selectedPath);
                        SetItemProperty("SaveDirectory", pathToSave);
                        _logger.LogInformation("保存ディレクトリを選択: {SaveDirectory} (相対パス: {RelativePath})",
                            selectedPath, pathToSave);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存ディレクトリ選択中にエラー");
            }
        }


        [RelayCommand]
        private void ClearWindowInfo()
        {
            try
            {
                SetItemProperty("WindowTitle", string.Empty);
                SetItemProperty("WindowClassName", string.Empty);
                _logger.LogInformation("ウィンドウ情報をクリア");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ情報クリア中にエラー");
            }
        }

        #region Win32 API

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        #endregion

        #region Diagnostics

        /// <summary>
        /// バインディング診断メソッド - デバッグ用
        /// </summary>
        public void DiagnosticProperties()
        {
            try
            {
                _logger.LogInformation("=== EditPanelViewModel プロパティ診断 ===");
                _logger.LogInformation("マクロファイルベースパス: {BasePath}", _macroFileBasePath);
                _logger.LogInformation("SelectedItem: {SelectedItem}", SelectedItem?.ItemType ?? "null");
                _logger.LogInformation("IsNotNullItem: {IsNotNullItem}", IsNotNullItem);
                _logger.LogInformation("IsListEmpty: {IsListEmpty}", IsListEmpty);
                _logger.LogInformation("IsListNotEmpty: {IsListNotEmpty}", IsListNotEmpty);
                _logger.LogInformation("IsWaitImageItem: {IsWaitImageItem}", IsWaitImageItem);
                _logger.LogInformation("IsClickImageItem: {IsClickImageItem}", IsClickImageItem);
                _logger.LogInformation("IsWaitItem: {IsWaitItem}", IsWaitItem);
                _logger.LogInformation("ShowWindowInfo: {ShowWindowInfo}", ShowWindowInfo);
                _logger.LogInformation("ShowAdvancedSettings: {ShowAdvancedSettings}", ShowAdvancedSettings);
                _logger.LogInformation("ItemTypes.Count: {Count}", ItemTypes.Count);
                _logger.LogInformation("SelectedItemTypeObj: {SelectedItemTypeObj}", SelectedItemTypeObj?.DisplayName ?? "null");

                if (SelectedItem != null)
                {
                    _logger.LogInformation("SelectedItem詳細:");
                    _logger.LogInformation("  ItemType: {ItemType}", SelectedItem.ItemType);
                    _logger.LogInformation("  Comment: {Comment}", SelectedItem.Comment);
                    _logger.LogInformation("  LineNumber: {LineNumber}", SelectedItem.LineNumber);
                    _logger.LogInformation("  IsEnable: {IsEnable}", SelectedItem.IsEnable);
                    _logger.LogInformation("  ActualType: {ActualType}", SelectedItem.GetType().Name);

                    // パス関連プロパティの診断
                    if (IsImageBasedItem || IsAIBasedItem)
                    {
                        _logger.LogInformation("  パス関連プロパティ診断:");
                        var imagePath = ImagePath;
                        var resolvedImagePath = ResolvePath(imagePath);
                        _logger.LogInformation("    ImagePath: {ImagePath}", imagePath);
                        _logger.LogInformation("    解決されたImagePath: {ResolvedPath}", resolvedImagePath);
                        _logger.LogInformation("    ファイル存在: {Exists}", FileExistsResolved(imagePath));

                        if (IsAIBasedItem)
                        {
                            var modelPath = ModelPath;
                            var resolvedModelPath = ResolvePath(modelPath);
                            _logger.LogInformation("    ModelPath: {ModelPath}", modelPath);
                            _logger.LogInformation("    解決されたModelPath: {ResolvedPath}", resolvedModelPath);
                            _logger.LogInformation("    モデルファイル存在: {Exists}", FileExistsResolved(modelPath));
                        }
                    }

                    if (IsExecuteItem)
                    {
                        var programPath = ProgramPath;
                        var workingDir = WorkingDirectory;
                        _logger.LogInformation("    ProgramPath: {ProgramPath}", programPath);
                        _logger.LogInformation("    解決されたProgramPath: {ResolvedPath}", ResolvePath(programPath));
                        _logger.LogInformation("    WorkingDirectory: {WorkingDirectory}", workingDir);
                        _logger.LogInformation("    解決されたWorkingDirectory: {ResolvedPath}", ResolvePath(workingDir));
                    }

                    if (IsScreenshotItem)
                    {
                        var saveDir = SaveDirectory;
                        _logger.LogInformation("    SaveDirectory: {SaveDirectory}", saveDir);
                        _logger.LogInformation("    解決されたSaveDirectory: {ResolvedPath}", ResolvePath(saveDir));
                        _logger.LogInformation("    ディレクトリ存在: {Exists}", DirectoryExistsResolved(saveDir));
                    }

                    // WaitItemの場合は待機時間プロパティを詳細チェック
                    if (IsWaitItem)
                    {
                        _logger.LogInformation("  Wait関連プロパティ診断:");
                        var waitMs = GetItemProperty<int>("Wait");
                        _logger.LogInformation("    Wait (ミリ秒): {WaitMs}", waitMs);
                        _logger.LogInformation("    WaitHours: {WaitHours}", WaitHours);
                        _logger.LogInformation("    WaitMinutes: {WaitMinutes}", WaitMinutes);
                        _logger.LogInformation("    WaitSeconds: {WaitSeconds}", WaitSeconds);
                        _logger.LogInformation("    WaitMilliseconds: {WaitMilliseconds}", WaitMilliseconds);
                    }

                    // リフレクションで利用可能なプロパティを一覧表示
                    _logger.LogInformation("  利用可能なプロパティ:");
                    var props = SelectedItem.GetType().GetProperties();
                    foreach (var prop in props.Take(10)) // 最初の10個だけ
                    {
                        try
                        {
                            var value = prop.GetValue(SelectedItem);
                            _logger.LogInformation("    {PropertyName}: {Value} ({Type})",
                                prop.Name, value ?? "null", prop.PropertyType.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation("    {PropertyName}: エラー ({Error})", prop.Name, ex.Message);
                        }
                    }
                }

                _logger.LogInformation("=== 診断完了 ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プロパティ診断中にエラー");
            }
        }

        /// <summary>
        /// バインディングテスト用メソッド
        /// </summary>
        public void TestPropertyNotification()
        {
            try
            {
                _logger.LogInformation("プロパティ変更通知テスト開始");

                // 全ての判定プロパティを明示的に更新
                NotifyAllPropertiesChanged();

                _logger.LogInformation("プロパティ変更通知テスト完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プロパティ変更通知テスト中にエラー");
            }
        }

        #endregion

        #region ファイルパス解決ヘルパー

        /// <summary>
        /// 相対パスまたは絶対パスを解決して、実際のファイルパスを返す
        /// </summary>
        /// <param name="path">解決するパス</param>
        /// <returns>解決されたパス、ファイルが存在しない場合は元のパス</returns>
        private string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            try
            {
                // 既に絶対パスの場合
                if (Path.IsPathRooted(path))
                {
                    return path;
                }

                // 相対パスの場合、マクロファイルのベースパスから解決
                if (!string.IsNullOrEmpty(_macroFileBasePath))
                {
                    var resolvedPath = Path.Combine(_macroFileBasePath, path);
                    resolvedPath = Path.GetFullPath(resolvedPath); // 正規化

                    // 解決されたパスにファイルが存在するか確認
                    if (File.Exists(resolvedPath) || Directory.Exists(resolvedPath))
                    {
                        _logger.LogTrace("相対パス解決成功: {RelativePath} -> {AbsolutePath}", path, resolvedPath);
                        return resolvedPath;
                    }
                }

                // カレントディレクトリからの相対パスとして試行
                var currentDirPath = Path.GetFullPath(path);
                if (File.Exists(currentDirPath) || Directory.Exists(currentDirPath))
                {
                    _logger.LogTrace("カレントディレクトリから解決: {RelativePath} -> {AbsolutePath}", path, currentDirPath);
                    return currentDirPath;
                }

                _logger.LogTrace("パス解決失敗、元のパスを返す: {Path}", path);
                return path;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "パス解決中にエラー: {Path}", path);
                return path;
            }
        }

        /// <summary>
        /// 絶対パスを相対パスに変換（可能な場合）
        /// </summary>
        /// <param name="absolutePath">絶対パス</param>
        /// <returns>相対パス、変換できない場合は絶対パス</returns>
        private string ConvertToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || string.IsNullOrEmpty(_macroFileBasePath))
                return absolutePath;

            try
            {
                if (Path.IsPathRooted(absolutePath))
                {
                    var relativePath = Path.GetRelativePath(_macroFileBasePath, absolutePath);

                    // 相対パスが親ディレクトリを多用していない場合のみ使用
                    if (!relativePath.StartsWith("..\\..."))
                    {
                        _logger.LogTrace("絶対パスを相対パスに変換: {AbsolutePath} -> {RelativePath}", absolutePath, relativePath);
                        return relativePath;
                    }
                }

                return absolutePath;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "相対パス変換中にエラー: {Path}", absolutePath);
                return absolutePath;
            }
        }

        /// <summary>
        /// ファイルが存在するかチェック（相対パス/絶対パス両方対応）
        /// </summary>
        /// <param name="path">チェックするパス</param>
        /// <returns>ファイルが存在する場合true</returns>
        private bool FileExistsResolved(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var resolvedPath = ResolvePath(path);
            return File.Exists(resolvedPath);
        }

        /// <summary>
        /// ディレクトリが存在するかチェック（相対パス/絶対パス両方対応）
        /// </summary>
        /// <param name="path">チェックするパス</param>
        /// <returns>ディレクトリが存在する場合true</returns>
        private bool DirectoryExistsResolved(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var resolvedPath = ResolvePath(path);
            return Directory.Exists(resolvedPath);
        }

        #endregion

        #region Update Info Methods

        /// <summary>
        /// 画像プレビューを更新
        /// </summary>
        private void UpdateImagePreview()
        {
            try
            {
                var imagePath = GetItemProperty<string>("ImagePath") ?? string.Empty;
                var resolvedPath = ResolvePath(imagePath);

                _logger.LogTrace("画像プレビュー更新開始: {ImagePath} -> {ResolvedPath}", imagePath, resolvedPath);

                if (string.IsNullOrEmpty(imagePath) || !FileExistsResolved(imagePath))
                {
                    ImagePreview = null;
                    HasImagePreview = false;
                    ImageInfo = string.IsNullOrEmpty(imagePath) ? string.Empty : $"ファイルが見つかりません: {imagePath}";
                    HasImageInfo = !string.IsNullOrEmpty(ImageInfo);
                    _logger.LogDebug("画像プレビューをクリア: {ImagePath} (解決パス: {ResolvedPath})", imagePath, resolvedPath);
                    return;
                }

                // ファイル情報を取得
                var fileInfo = new FileInfo(resolvedPath);

                // 画像をBitmapImageとして読み込み
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(resolvedPath);
                bitmap.DecodePixelWidth = 300; // プレビュー用にリサイズ
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // UI スレッド外からの使用のためFreeze

                ImagePreview = bitmap;
                HasImagePreview = true;

                // 画像情報を設定（相対パス表示 + ファイル情報）
                var fileSizeKB = fileInfo.Length / 1024.0;
                var pathDisplay = imagePath != resolvedPath ? $"{imagePath} ({Path.GetFileName(resolvedPath)})" : imagePath;
                ImageInfo = $"{pathDisplay}\n{bitmap.PixelWidth} x {bitmap.PixelHeight} px, {fileSizeKB:F1} KB";
                HasImageInfo = true;

                _logger.LogInformation("画像プレビューを更新: {ImagePath} -> {ResolvedPath} ({Width}x{Height}, {Size:F1}KB)",
                    imagePath, resolvedPath, bitmap.PixelWidth, bitmap.PixelHeight, fileSizeKB);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "画像プレビュー更新エラー: {ImagePath}", GetItemProperty<string>("ImagePath"));
                ImagePreview = null;
                HasImagePreview = false;
                ImageInfo = $"画像読み込みエラー: {ex.Message}";
                HasImageInfo = true;
            }
        }

        /// <summary>
        /// モデルファイル情報を更新
        /// </summary>
        private void UpdateModelInfo()
        {
            try
            {
                var modelPath = GetItemProperty<string>("ModelPath") ?? string.Empty;
                var resolvedPath = ResolvePath(modelPath);

                if (string.IsNullOrEmpty(modelPath) || !FileExistsResolved(modelPath))
                {
                    ModelInfo = string.IsNullOrEmpty(modelPath) ? string.Empty : $"ファイルが見つかりません: {modelPath}";
                    HasModelInfo = !string.IsNullOrEmpty(ModelInfo);
                    return;
                }

                var fileInfo = new FileInfo(resolvedPath);
                var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                var pathDisplay = modelPath != resolvedPath ? $"{modelPath} ({Path.GetFileName(resolvedPath)})" : modelPath;
                ModelInfo = $"{pathDisplay}\n{fileSizeMB:F1} MB, {fileInfo.LastWriteTime:yyyy/MM/dd HH:mm}";
                HasModelInfo = true;

                _logger.LogDebug("モデル情報を更新: {ModelPath} -> {ResolvedPath} ({Size:F1}MB)", modelPath, resolvedPath, fileSizeMB);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "モデル情報更新エラー: {ModelPath}", GetItemProperty<string>("ModelPath"));
                ModelInfo = $"ファイル情報取得エラー: {ex.Message}";
                HasModelInfo = true;
            }
        }

        /// <summary>
        /// プログラムファイル情報を更新
        /// </summary>
        private void UpdateProgramInfo()
        {
            try
            {
                var programPath = GetItemProperty<string>("ProgramPath") ?? string.Empty;
                var resolvedPath = ResolvePath(programPath);

                if (string.IsNullOrEmpty(programPath) || !FileExistsResolved(programPath))
                {
                    ProgramInfo = string.IsNullOrEmpty(programPath) ? string.Empty : $"ファイルが見つかりません: {programPath}";
                    HasProgramInfo = !string.IsNullOrEmpty(ProgramInfo);
                    return;
                }

                var fileInfo = new FileInfo(resolvedPath);
                var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                var pathDisplay = programPath != resolvedPath ? $"{programPath} ({Path.GetFileName(resolvedPath)})" : programPath;
                ProgramInfo = $"{pathDisplay}\n{fileSizeMB:F1} MB, {fileInfo.LastWriteTime:yyyy/MM/dd HH:mm}";
                HasProgramInfo = true;

                _logger.LogDebug("プログラム情報を更新: {ProgramPath} -> {ResolvedPath} ({Size:F1}MB)", programPath, resolvedPath, fileSizeMB);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プログラム情報更新エラー: {ProgramPath}", GetItemProperty<string>("ProgramPath"));
                ProgramInfo = $"ファイル情報取得エラー: {ex.Message}";
                HasProgramInfo = true;
            }
        }

        /// <summary>
        /// 保存ディレクトリ情報を更新
        /// </summary>
        private void UpdateSaveDirectoryInfo()
        {
            try
            {
                var saveDir = GetItemProperty<string>("SaveDirectory") ?? string.Empty;
                var resolvedPath = ResolvePath(saveDir);

                if (string.IsNullOrEmpty(saveDir))
                {
                    SaveDirectoryInfo = string.Empty;
                    HasSaveDirectoryInfo = false;
                    return;
                }

                if (DirectoryExistsResolved(saveDir))
                {
                    var dirInfo = new DirectoryInfo(resolvedPath);
                    var fileCount = dirInfo.GetFiles().Length;
                    var pathDisplay = saveDir != resolvedPath ? $"{saveDir} ({Path.GetFileName(resolvedPath)})" : saveDir;
                    SaveDirectoryInfo = $"{pathDisplay}\n既存ディレクトリ: {fileCount} ファイル";
                    HasSaveDirectoryInfo = true;
                }
                else
                {
                    SaveDirectoryInfo = $"{saveDir}\n新規ディレクトリ (作成予定)";
                    HasSaveDirectoryInfo = true;
                }

                _logger.LogDebug("保存ディレクトリ情報を更新: {SaveDir} -> {ResolvedPath}", saveDir, resolvedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存ディレクトリ情報更新エラー: {SaveDir}", GetItemProperty<string>("SaveDirectory"));
                SaveDirectoryInfo = $"ディレクトリ情報取得エラー: {ex.Message}";
                HasSaveDirectoryInfo = true;
            }
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void ExecuteCommand()
        {
            try
            {
                // 実行中の場合は何もしない
                if (IsRunning) return;

                // 現在選択されているアイテムを取得
                var item = SelectedItem;
                if (item == null)
                {
                    _logger.LogWarning("実行アイテムが選択されていません");
                    return;
                }

                _logger.LogInformation("実行開始: {ItemType} - {Comment}", item.ItemType, item.Comment);

                // 実行状態に遷移
                IsRunning = true;
                item.IsRunning = true;
                OnPropertyChanged(nameof(IsRunning));

                // マクロの実行など
                // TODO: マクロ実行ロジックの実装

                _logger.LogInformation("実行完了: {ItemType} - {Comment}", item.ItemType, item.Comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド実行中にエラー: {ItemType}", SelectedItem?.ItemType);
            }
            finally
            {
                // 後始末
                IsRunning = false;
                if (SelectedItem != null)
                {
                    SelectedItem.IsRunning = false;
                }
                OnPropertyChanged(nameof(IsRunning));
            }
        }

        #endregion

        [RelayCommand]
        private async Task GetMousePosition()
        {
            try
            {
                _logger.LogInformation("マウス位置取得開始（右クリック待機）");

                IsWaitingForRightClick = true;
                MouseWaitMessage = "対象位置で右クリックしてください";

                // キャプチャサービスを使用
                var position = await _captureService.CaptureCoordinateAtRightClickAsync();
                
                if (position.HasValue)
                {
                    // 取得した座標を設定
                    ClickX = position.Value.X;
                    ClickY = position.Value.Y;

                    NotifyAllPropertiesChanged();

                    _logger.LogInformation("マウス位置設定完了: ({X}, {Y})", position.Value.X, position.Value.Y);
                }
                else
                {
                    _logger.LogInformation("マウス位置取得がキャンセルされました");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マウス位置取得中にエラー");
            }
            finally
            {
                IsWaitingForRightClick = false;
                MouseWaitMessage = string.Empty;
            }
        }

        [RelayCommand]
        private void GetCurrentMousePosition()
        {
            try
            {
                _logger.LogInformation("現在のマウス位置取得");

                // キャプチャサービスを使用
                var position = _captureService.GetCurrentMousePosition();

                // 取得した座標を設定
                ClickX = position.X;
                ClickY = position.Y;
                
                _logger.LogInformation("現在のマウス位置設定完了: ({X}, {Y})", position.X, position.Y);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "現在のマウス位置取得中にエラー");
            }
        }

        [RelayCommand]
        private async Task GetWindowInfoAsync()
        {
            try
            {
                _logger.LogInformation("ウィンドウ情報取得開始（右クリック待機）");
                
                IsWaitingForRightClick = true;
                MouseWaitMessage = "対象ウィンドウで右クリックしてください";

                // キャプチャサービスを使用
                var windowInfo = await _captureService.CaptureWindowInfoAtRightClickAsync();
                
                if (windowInfo != null)
                {
                    SetItemProperty("WindowTitle", windowInfo.Title, nameof(WindowTitle));
                    SetItemProperty("WindowClassName", windowInfo.ClassName, nameof(WindowClassName));
                    OnPropertyChanged(nameof(WindowTitle));
                    OnPropertyChanged(nameof(WindowClassName));
                    
                    _logger.LogInformation("ウィンドウ情報取得成功: Title={Title}, ClassName={ClassName}", 
                        windowInfo.Title, windowInfo.ClassName);
                }
                else
                {
                    _logger.LogInformation("ウィンドウ情報取得がキャンセルされました");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ情報取得中にエラー");
            }
            finally
            {
                IsWaitingForRightClick = false;
                MouseWaitMessage = string.Empty;
            }
        }

        /// <summary>
        /// 動的設定の初期化
        /// </summary>
        private void InitializeDynamicSettings(UniversalCommandItem universalItem)
        {
            try
            {
                _logger.LogDebug("=== 動的設定初期化開始: {ItemType} ===", universalItem.ItemType);
                
                // 設定定義を初期化
                _logger.LogDebug("UniversalCommandItem.InitializeSettingDefinitions()呼び出し");
                universalItem.InitializeSettingDefinitions();
                
                // 設定定義をカテゴリ別にグループ化
                var definitions = universalItem.SettingDefinitions ?? new List<SettingDefinition>();
                SettingDefinitions = new ObservableCollection<SettingDefinition>(definitions);
                
                _logger.LogDebug("設定定義取得完了: {ItemType}, 項目数: {Count}", universalItem.ItemType, definitions.Count);
                
                if (definitions.Count == 0)
                {
                    _logger.LogWarning("❌ 設定定義が空です: {ItemType}", universalItem.ItemType);
                    
                    // DirectCommandRegistryから直接取得を試行
                    _logger.LogDebug("DirectCommandRegistryから設定定義を直接取得を試行");
                    var directDefinitions = DirectCommandRegistry.GetSettingDefinitions(universalItem.ItemType);
                    if (directDefinitions.Count > 0)
                    {
                        _logger.LogInformation("✅ DirectCommandRegistryから設定定義を取得: {Count}項目", directDefinitions.Count);
                        definitions = directDefinitions;
                        SettingDefinitions = new ObservableCollection<SettingDefinition>(definitions);
                        
                        // UniversalCommandItemに設定定義を設定
                        universalItem.SettingDefinitions = definitions;
                    }
                    else
                    {
                        _logger.LogError("❌ DirectCommandRegistryからも設定定義を取得できませんでした: {ItemType}", universalItem.ItemType);
                        SettingGroups.Clear();
                        DynamicValues.Clear();
                        return;
                    }
                }
                
                // 動的設定値を初期化
                _logger.LogDebug("動的設定値初期化開始");
                InitializeDynamicValues(universalItem, definitions);
                
                // 現在値を設定定義に同期
                _logger.LogDebug("現在値を設定定義に同期開始");
                SyncCurrentValuesToDefinitions(universalItem, definitions);
                
                // カテゴリごとにグループ化
                var groups = definitions
                    .GroupBy(d => d.Category ?? "基本設定")
                    .Select(g => new SettingCategoryGroup
                    {
                        Category = g.Key,
                        Settings = new ObservableCollection<SettingDefinition>(g.ToList())
                    })
                    .ToList();

                _logger.LogDebug("設定グループ作成完了: {ItemType}, グループ数: {GroupCount}", universalItem.ItemType, groups.Count);
                
                // 各グループの詳細をログ出力
                foreach (var group in groups)
                {
                    _logger.LogDebug("📁 グループ: {Category}, 設定項目数: {SettingCount}", group.Category, group.Settings.Count);
                    foreach (var setting in group.Settings.Take(3)) // 最初の3つのみ
                    {
                        _logger.LogDebug("  ⚙️ {DisplayName} ({PropertyName}): {ControlType} = {CurrentValue}", 
                            setting.DisplayName, setting.PropertyName, setting.ControlType, setting.CurrentValue);
                    }
                }

                // グローバル設定を適用
                _logger.LogDebug("グローバル設定適用開始");
                ApplyGlobalSettings(groups);
                    
                SettingGroups = new ObservableCollection<SettingCategoryGroup>(groups);
                
                _logger.LogInformation("✅ 動的設定初期化完了: {ItemType}, カテゴリ数: {CategoryCount}, 設定項目数: {SettingCount}",
                    universalItem.ItemType, groups.Count, definitions.Count);
                _logger.LogDebug("=== 動的設定初期化終了 ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 動的設定初期化エラー: {ItemType}", universalItem.ItemType);
                SettingGroups.Clear();
                SettingDefinitions.Clear();
                DynamicValues.Clear();
            }
        }

        /// <summary>
        /// 動的設定値の初期化
        /// </summary>
        private void InitializeDynamicValues(UniversalCommandItem universalItem, List<SettingDefinition> definitions)
        {
            DynamicValues.Clear();
            
            foreach (var definition in definitions)
            {
                var currentValue = universalItem.GetSetting<object>(definition.PropertyName);
                if (currentValue != null)
                {
                    DynamicValues[definition.PropertyName] = currentValue;
                }
                else if (definition.DefaultValue != null)
                {
                    DynamicValues[definition.PropertyName] = definition.DefaultValue;
                    universalItem.SetSetting(definition.PropertyName, definition.DefaultValue);
                }
                
                _logger.LogTrace("動的値初期化: {PropertyName} = {Value}", 
                    definition.PropertyName, DynamicValues.GetValueOrDefault(definition.PropertyName));
            }
            
            OnPropertyChanged(nameof(DynamicValues));
        }

        /// <summary>
        /// 現在値を設定定義に同期
        /// </summary>
        private void SyncCurrentValuesToDefinitions(UniversalCommandItem universalItem, List<SettingDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                var currentValue = DynamicValues.GetValueOrDefault(definition.PropertyName) ?? 
                                   universalItem.GetSetting<object>(definition.PropertyName) ??
                                   definition.DefaultValue;
                
                definition.CurrentValue = currentValue;
                
                _logger.LogTrace("現在値同期: {PropertyName} = {CurrentValue}", 
                    definition.PropertyName, currentValue);
            }
        }

        /// <summary>
        /// グローバル設定の適用（ソースコレクションの設定など）
        /// </summary>
        private void ApplyGlobalSettings(List<SettingCategoryGroup> categoryGroups)
        {
            foreach (var group in categoryGroups)
            {
                foreach (var setting in group.Settings)
                {
                    // ソースコレクションが指定されている場合は実際のコレクションを設定
                    if (!string.IsNullOrEmpty(setting.SourceCollection))
                    {
                        var sourceData = DirectCommandRegistry.GetSourceCollection(setting.SourceCollection);
                        if (sourceData != null)
                        {
                            // ソースデータをSettingDefinitionに格納
                            setting.SourceItems = sourceData.ToList();
                            _logger.LogTrace("ソースコレクション設定: {PropertyName} -> {SourceCollection} ({ItemCount}個)",
                                setting.PropertyName, setting.SourceCollection, sourceData.Length);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 動的プロパティ取得
        /// </summary>
        public object? GetDynamicProperty(string propertyName)
        {
            if (SelectedItem is UniversalCommandItem universalItem)
            {
                return universalItem.GetSetting<object>(propertyName);
            }
            else if (DynamicValues.TryGetValue(propertyName, out var value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// 動的プロパティ設定
        /// </summary>
        public void SetDynamicProperty(string propertyName, object? value)
        {
            if (SelectedItem is UniversalCommandItem universalItem)
            {
                universalItem.SetSetting(propertyName, value);
                DynamicValues[propertyName] = value;
                
                // 設定定義の現在値も更新
                var settingDefinition = SettingDefinitions.FirstOrDefault(s => s.PropertyName == propertyName);
                if (settingDefinition != null)
                {
                    settingDefinition.CurrentValue = value;
                    _logger.LogTrace("設定定義の現在値更新: {PropertyName} = {Value}", propertyName, value);
                }
                
                OnPropertyChanged(nameof(DynamicValues));
                _logger.LogTrace("動的プロパティ設定: {PropertyName} = {Value}", propertyName, value);
            }
        }

        /// <summary>
        /// 動的設定の保存
        /// </summary>
        public bool SaveDynamicSettings()
        {
            try
            {
                if (SelectedItem is not UniversalCommandItem universalItem)
                    return false;

                // 設定検証
                var validationErrors = new List<string>();
                foreach (var definition in SettingDefinitions)
                {
                    if (definition.IsRequired)
                    {
                        var value = universalItem.GetSetting<object>(definition.PropertyName);
                        if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                        {
                            validationErrors.Add($"{definition.DisplayName}は必須項目です");
                        }
                    }
                }

                if (validationErrors.Count > 0)
                {
                    _logger.LogWarning("動的設定検証エラー: {Errors}", string.Join(", ", validationErrors));
                    return false;
                }

                _logger.LogInformation("動的設定保存完了: {ItemType}", universalItem.ItemType);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "動的設定保存エラー");
                return false;
            }
        }

        #region Dynamic Action Handlers
        /// <summary>
       /// 動的アクション実行
        /// </summary>
        [RelayCommand]
        public async Task ExecuteActionAsync(ActionExecutionContext context)
        {
            if (SelectedItem == null || string.IsNullOrEmpty(context.ActionType)) return;

            try
            {
                _logger.LogDebug("動的アクション実行: {ActionType} for {PropertyName}", 
                    context.ActionType, context.SettingDefinition.PropertyName);

                switch (context.ActionType)
                {
                    case "Browse":
                        await ExecuteBrowseActionAsync(context);
                        break;
                    case "GetMousePosition":
                        await ExecuteGetMousePositionActionAsync(context);
                        break;
                    case "GetCurrentPosition":
                        ExecuteGetCurrentPositionActionAsync(context);
                        break;
                    case "GetWindowInfo":
                        await ExecuteGetWindowInfoActionAsync(context);
                        break;
                    case "CaptureColor":
                        await ExecuteCaptureColorActionAsync(context);
                        break;
                    case "CaptureKey":
                        await ExecuteCaptureKeyActionAsync(context);
                        break;
                    case "Clear":
                        ExecuteClearAction(context);
                        break;
                    default:
                        _logger.LogWarning("未知のアクション: {ActionType}", context.ActionType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アクション実行エラー: {ActionType}", context.ActionType);
            }
        }

        private async Task ExecuteBrowseActionAsync(ActionExecutionContext context)
        {
            var setting = context.SettingDefinition;

            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = $"{setting.DisplayName}を選択",
                    Filter = setting.FileFilter ?? "すべてのファイル (*.*)|*.*"
                };
                var currentValue = GetDynamicProperty(setting.PropertyName)?.ToString() ?? string.Empty;
                var currentPath = ResolvePath(currentValue);
                if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
                    dialog.FileName = Path.GetFileName(currentPath);
                }
                else if (!string.IsNullOrEmpty(_macroFileBasePath))
                {
                    dialog.InitialDirectory = _macroFileBasePath;
                }

                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = dialog.FileName;
                    var pathToSave = ConvertToRelativePath(selectedPath);
                    SetDynamicProperty(setting.PropertyName, pathToSave);
                    
                    _logger.LogInformation("ファイル選択: {PropertyName} = {FileName} (相対パス: {RelativePath})", 
                        setting.PropertyName, selectedPath, pathToSave);

                    // プレビュー更新
                    if (setting.PropertyName == "ImagePath")
                    {
                        UpdateImagePreview();
                    }
                    else if (setting.PropertyName == "ModelPath")
                    {
                        UpdateModelInfo();
                    }
                    else if (setting.PropertyName == "ProgramPath")
                    {
                        UpdateProgramInfo();
                    }
                    else if (setting.PropertyName == "SaveDirectory")
                    {
                        UpdateSaveDirectoryInfo();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル選択アクションエラー: {PropertyName}", setting.PropertyName);
            }
        }

        private async Task ExecuteGetMousePositionActionAsync(ActionExecutionContext context)
        {
            try
            {
                _logger.LogInformation("動的マウス位置取得開始（右クリック待機）");

                IsWaitingForRightClick = true;
                MouseWaitMessage = "対象位置で右クリックしてください";

                // キャプチャサービスを使用
                var position = await _captureService.CaptureCoordinateAtRightClickAsync();
                
                if (position.HasValue)
                {
                    // 動的プロパティに設定
                    SetDynamicProperty("X", position.Value.X);
                    SetDynamicProperty("Y", position.Value.Y);
                    
                    // CoordinatePickerの場合は統合プロパティにも設定
                    SetDynamicProperty("MousePosition", position.Value);
                    
                    _logger.LogInformation("動的マウス位置設定完了: ({X}, {Y})", position.Value.X, position.Value.Y);
                }
                else
                {
                    _logger.LogInformation("動的マウス位置取得がキャンセルされました");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "動的マウス位置取得中にエラー");
            }
            finally
            {
                IsWaitingForRightClick = false;
                MouseWaitMessage = string.Empty;
            }
        }

        private void ExecuteGetCurrentPositionActionAsync(ActionExecutionContext context)
        {
            try
            {
                _logger.LogInformation("動的現在マウス位置取得");

                // キャプチャサービスを使用
                var position = _captureService.GetCurrentMousePosition();

                // 動的プロパティに設定
                SetDynamicProperty("X", position.X);
                SetDynamicProperty("Y", position.Y);
                
                // CoordinatePickerの場合は統合プロパティにも設定
                SetDynamicProperty("MousePosition", position);

                _logger.LogInformation("動的現在マウス位置設定完了: ({X}, {Y})", position.X, position.Y);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "動的現在マウス位置取得中にエラー");
            }
        }

        private async Task ExecuteGetWindowInfoActionAsync(ActionExecutionContext context)
        {
            try
            {
                _logger.LogInformation("動的ウィンドウ情報取得開始（右クリック待機）");
                
                IsWaitingForRightClick = true;
                MouseWaitMessage = "対象ウィンドウで右クリックしてください";

                // キャプチャサービスを使用
                var windowInfo = await _captureService.CaptureWindowInfoAtRightClickAsync();
                
                if (windowInfo != null)
                {
                    SetDynamicProperty("WindowTitle", windowInfo.Title);
                    SetDynamicProperty("WindowClassName", windowInfo.ClassName);
                    
                    _logger.LogInformation("動的ウィンドウ情報取得成功: Title={Title}, ClassName={ClassName}", 
                        windowInfo.Title, windowInfo.ClassName);
                }
                else
                {
                    _logger.LogInformation("動的ウィンドウ情報取得がキャンセルされました");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "動的ウィンドウ情報取得中にエラー");
            }
            finally
            {
                IsWaitingForRightClick = false;
                MouseWaitMessage = string.Empty;
            }
        }

        private async Task ExecuteCaptureColorActionAsync(ActionExecutionContext context)
        {
            try
            {
                _logger.LogInformation("動的色キャプチャ開始（右クリック待機）");
                
                IsWaitingForRightClick = true;
                MouseWaitMessage = "対象の色で右クリックしてください";

                // キャプチャサービスを使用
                var color = await _captureService.CaptureColorAtRightClickAsync();
                
                if (color.HasValue)
                {
                    // System.Drawing.ColorからSystem.Windows.Media.Colorに変換
                    var mediaColor = System.Windows.Media.Color.FromArgb(
                        color.Value.A, color.Value.R, color.Value.G, color.Value.B);
                    
                    SetDynamicProperty(context.SettingDefinition.PropertyName, color.Value);
                    
                    // HotKeyCommandの場合は適切なプロパティに設定
                    if (SelectedItem?.ItemType == "Hotkey")
                    {
                        SetDynamicProperty("Key", color.Value);
                    }
                    
                    _logger.LogInformation("動的色キャプチャ完了: {Color}", color.Value);
                }
                else
                {
                    _logger.LogInformation("動的色キャプチャがキャンセルされました");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "動的色キャプチャ中にエラー");
            }
            finally
            {
                IsWaitingForRightClick = false;
                MouseWaitMessage = string.Empty;
            }
        }

        private async Task ExecuteCaptureKeyActionAsync(ActionExecutionContext context)
        {
            try
            {
                _logger.LogInformation("動的キーキャプチャ開始");

                // キャプチャサービスを使用
                var key = await _captureService.CaptureKeyAsync(context.SettingDefinition.DisplayName);
                
                if (key != null)
                {
                    SetDynamicProperty(context.SettingDefinition.PropertyName, key);
                    
                    // HotKeyCommandの場合は適切なプロパティに設定
                    if (SelectedItem?.ItemType == "Hotkey")
                    {
                        SetDynamicProperty("Key", key.Key);
                    }
                    
                    _logger.LogInformation("動的キーキャプチャ完了: {Key}", key.DisplayText);
                }
                else
                {
                    _logger.LogInformation("動的キーキャプチャがキャンセルされました");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "動的キーキャプチャ中にエラー");
            }
        }

        private void ExecuteClearAction(ActionExecutionContext context)
        {
            var setting = context.SettingDefinition;

            try
            {
                // 指定されたプロパティに対してクリア処理
                if (SelectedItem != null)
                {
                    if (setting.PropertyName == "ImagePath")
                    {
                        SetDynamicProperty("ImagePath", string.Empty);
                        UpdateImagePreview();
                    }
                    else if (setting.PropertyName == "ModelPath")
                    {
                        SetDynamicProperty("ModelPath", string.Empty);
                        UpdateModelInfo();
                    }
                    else if (setting.PropertyName == "ProgramPath")
                    {
                        SetDynamicProperty("ProgramPath", string.Empty);
                        UpdateProgramInfo();
                    }
                    else if (setting.PropertyName == "WindowTitle")
                    {
                        SetDynamicProperty("WindowTitle", string.Empty);
                        SetDynamicProperty("WindowClassName", string.Empty);
                    }
                    else if (setting.PropertyName == "X" || setting.PropertyName == "Y")
                    {
                        SetDynamicProperty("X", 0);
                        SetDynamicProperty("Y", 0);
                        SetDynamicProperty("MousePosition", new System.Drawing.Point(0, 0));
                    }
                    else
                    {
                        // 一般的なクリア処理
                        var defaultValue = setting.DefaultValue ?? GetDefaultValueForType(setting.PropertyName);
                        SetDynamicProperty(setting.PropertyName, defaultValue);
                    }
                    
                    _logger.LogInformation("クリアアクション実行: {PropertyName}", setting.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "クリアアクションエラー: {PropertyName}", setting.PropertyName);
            }
        }

        private object? GetDefaultValueForType(string propertyName)
        {
            // プロパティ名に基づいてデフォルト値を返す
            return propertyName.ToLower() switch
            {
                var p when p.Contains("timeout") => 5000,
                var p when p.Contains("interval") => 500,
                var p when p.Contains("threshold") => 0.8,
                var p when p.Contains("count") => 1,
                var p when p.Contains("x") || p.Contains("y") => 0,
                var p when p.Contains("path") || p.Contains("title") || p.Contains("name") => string.Empty,
                var p when p.Contains("enable") || p.Contains("use") => false,
                _ => null
            };
        }

        #endregion
    }

    /// <summary>
    /// 設定カテゴリグループ
    /// </summary>
    public class SettingCategoryGroup
    {
        public string Category { get; set; } = string.Empty;
        public ObservableCollection<SettingDefinition> Settings { get; set; } = new();
    }

    /// <summary>
    /// アクション実行コンテキスト
    /// </summary>
    public class ActionExecutionContext
    {
        public SettingDefinition SettingDefinition { get; set; } = new();
        public string ActionType { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}