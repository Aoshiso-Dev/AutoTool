using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using AutoTool.Message;
using AutoTool.Model.List.Interface;
using AutoTool.Model.CommandDefinition;
using AutoTool.ViewModel.Shared;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5���S�����ŁFEditPanelViewModel�i�S�R�}���h�ݒ�Ή��j
    /// </summary>
    public partial class EditPanelViewModel : ObservableObject
    {
        private readonly ILogger<EditPanelViewModel> _logger;
        private ICommandListItem? _item = null;
        private bool _isUpdating = false;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private int _listCount = 0;

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _itemTypes = new();

        [ObservableProperty]
        private CommandDisplayItem? _selectedItemTypeObj;

        [ObservableProperty]
        private ObservableCollection<MouseButton> _mouseButtons = new();

        [ObservableProperty]
        private ObservableCollection<Key> _keyList = new();

        [ObservableProperty]
        private ObservableCollection<OperatorItem> _operators = new();

        [ObservableProperty]
        private ObservableCollection<AIDetectModeItem> _aiDetectModes = new();

        [ObservableProperty]
        private ObservableCollection<BackgroundClickMethodItem> _backgroundClickMethods = new();

        #region ��{�v���p�e�B
        [ObservableProperty]
        private string _comment = string.Empty;

        [ObservableProperty]
        private string _windowTitle = string.Empty;

        [ObservableProperty]
        private string _windowClassName = string.Empty;
        #endregion

        #region �摜�֘A�v���p�e�B
        [ObservableProperty]
        private string _imagePath = string.Empty;

        [ObservableProperty]
        private double _threshold = 0.8;

        [ObservableProperty]
        private Color? _searchColor = null;

        [ObservableProperty]
        private int _timeout = 5000;

        [ObservableProperty]
        private int _interval = 500;
        #endregion

        #region �N���b�N�֘A�v���p�e�B
        [ObservableProperty]
        private MouseButton _mouseButton = MouseButton.Left;

        [ObservableProperty]
        private int _clickX = 0;

        [ObservableProperty]
        private int _clickY = 0;

        [ObservableProperty]
        private bool _useBackgroundClick = false;

        [ObservableProperty]
        private int _backgroundClickMethod = 0;
        #endregion

        #region �z�b�g�L�[�֘A�v���p�e�B
        [ObservableProperty]
        private bool _ctrlKey = false;

        [ObservableProperty]
        private bool _altKey = false;

        [ObservableProperty]
        private bool _shiftKey = false;

        [ObservableProperty]
        private Key _selectedKey = Key.Escape;

        [ObservableProperty]
        private string _hotkeyText = string.Empty;
        #endregion

        #region �ҋ@�֘A�v���p�e�B
        [ObservableProperty]
        private int _waitTime = 1000;

        [ObservableProperty]
        private int _waitHours = 0;

        [ObservableProperty]
        private int _waitMinutes = 0;

        [ObservableProperty]
        private int _waitSeconds = 1;

        [ObservableProperty]
        private int _waitMilliseconds = 0;
        #endregion

        #region ���[�v�֘A�v���p�e�B
        [ObservableProperty]
        private int _loopCount = 1;
        #endregion

        #region �ϐ��֘A�v���p�e�B
        [ObservableProperty]
        private string _variableName = string.Empty;

        [ObservableProperty]
        private string _variableValue = string.Empty;

        [ObservableProperty]
        private string _variableOperator = "==";
        #endregion

        #region AI�֘A�v���p�e�B
        [ObservableProperty]
        private string _modelPath = string.Empty;

        [ObservableProperty]
        private int _classID = 0;

        [ObservableProperty]
        private double _confThreshold = 0.5;

        [ObservableProperty]
        private double _ioUThreshold = 0.25;

        [ObservableProperty]
        private string _aiDetectMode = "Class";
        #endregion

        #region �v���O�������s�֘A�v���p�e�B
        [ObservableProperty]
        private string _programPath = string.Empty;

        [ObservableProperty]
        private string _arguments = string.Empty;

        [ObservableProperty]
        private string _workingDirectory = string.Empty;

        [ObservableProperty]
        private bool _waitForExit = false;
        #endregion

        #region �X�N���[���V���b�g�֘A�v���p�e�B
        [ObservableProperty]
        private string _saveDirectory = string.Empty;
        #endregion

        #region �A�C�e���^�C�v����v���p�e�B
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
        
        // ������������
        public bool IsImageBasedItem => IsWaitImageItem || IsClickImageItem || IsIfImageExistItem || IsIfImageNotExistItem;
        public bool IsAIBasedItem => IsClickImageAIItem || IsIfImageExistAIItem || IsIfImageNotExistAIItem || IsSetVariableAIItem;
        public bool IsVariableItem => IsSetVariableItem || IsIfVariableItem || IsSetVariableAIItem;
        public bool IsLoopRelatedItem => IsLoopItem || IsLoopEndItem || IsLoopBreakItem;
        public bool IsIfRelatedItem => IsIfImageExistItem || IsIfImageNotExistItem || IsIfImageExistAIItem || IsIfImageNotExistAIItem || IsIfVariableItem || IsIfEndItem;
        
        // �E�B���h�E�ΏۃR�}���h���ǂ����̔���iWindowTargetCommandSettings���p������R�}���h�j
        public bool HasWindowTarget => IsWaitImageItem || IsClickImageItem || IsClickImageAIItem || 
                                      IsIfImageExistItem || IsIfImageNotExistItem || IsIfImageExistAIItem || IsIfImageNotExistAIItem ||
                                      IsHotkeyItem || IsClickItem || IsScreenshotItem || IsSetVariableAIItem;
        
        // �E�B���h�E���\������i�E�B���h�E�ΏۃR�}���h�̂݁j
        public bool ShowWindowInfo => IsNotNullItem && HasWindowTarget;
        
        // �ݒ荀�ڕ\������i���[�v���f�͊�{���ƃR�����g�̂݁j
        public bool ShowAdvancedSettings => IsNotNullItem && !IsLoopBreakItem;
        #endregion

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

        public bool IsNotNullItem => Item != null;

        public EditPanelViewModel(ILogger<EditPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            SetupMessaging();
            InitializeItemTypes();
            InitializeCollections();
            
            _logger.LogInformation("Phase 5������EditPanelViewModel �����������Ă��܂�");
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
                _logger.LogError(ex, "ItemTypes���������ɃG���[���������܂���");
            }
        }

        private void InitializeCollections()
        {
            // MouseButton �̏�����
            MouseButtons.Clear();
            foreach (var button in Enum.GetValues(typeof(MouseButton)).Cast<MouseButton>())
                MouseButtons.Add(button);

            // Key �̏������i�悭�g�p�����L�[�̂݁j
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

            // Operator �̏�����
            Operators.Clear();
            Operators.Add(new OperatorItem { Key = "==", DisplayName = "������ (==)" });
            Operators.Add(new OperatorItem { Key = "!=", DisplayName = "�������Ȃ� (!=)" });
            Operators.Add(new OperatorItem { Key = ">", DisplayName = "���傫�� (>)" });
            Operators.Add(new OperatorItem { Key = "<", DisplayName = "��菬���� (<)" });
            Operators.Add(new OperatorItem { Key = ">=", DisplayName = "�ȏ� (>=)" });
            Operators.Add(new OperatorItem { Key = "<=", DisplayName = "�ȉ� (<=)" });
            Operators.Add(new OperatorItem { Key = "Contains", DisplayName = "�܂� (Contains)" });
            Operators.Add(new OperatorItem { Key = "StartsWith", DisplayName = "�n�܂� (StartsWith)" });
            Operators.Add(new OperatorItem { Key = "EndsWith", DisplayName = "�I��� (EndsWith)" });
            Operators.Add(new OperatorItem { Key = "IsEmpty", DisplayName = "��ł��� (IsEmpty)" });
            Operators.Add(new OperatorItem { Key = "IsNotEmpty", DisplayName = "��łȂ� (IsNotEmpty)" });

            // AI Detect Mode �̏�����
            AiDetectModes.Clear();
            AiDetectModes.Add(new AIDetectModeItem { Key = "Class", DisplayName = "�N���X���o" });
            AiDetectModes.Add(new AIDetectModeItem { Key = "Count", DisplayName = "���ʌ��o" });

            // Background Click Method �̏�����
            BackgroundClickMethods.Clear();
            BackgroundClickMethods.Add(new BackgroundClickMethodItem { Value = 0, DisplayName = "SendMessage" });
            BackgroundClickMethods.Add(new BackgroundClickMethodItem { Value = 1, DisplayName = "PostMessage" });
            BackgroundClickMethods.Add(new BackgroundClickMethodItem { Value = 2, DisplayName = "AutoDetectChild" });
            BackgroundClickMethods.Add(new BackgroundClickMethodItem { Value = 3, DisplayName = "TryAll" });
            BackgroundClickMethods.Add(new BackgroundClickMethodItem { Value = 4, DisplayName = "GameDirectInput" });
            BackgroundClickMethods.Add(new BackgroundClickMethodItem { Value = 5, DisplayName = "GameFullscreen" });
            BackgroundClickMethods.Add(new BackgroundClickMethodItem { Value = 6, DisplayName = "GameLowLevel" });
            BackgroundClickMethods.Add(new BackgroundClickMethodItem { Value = 7, DisplayName = "GameVirtualMouse" });
        }

        private void OnItemChanged()
        {
            try
            {
                _isUpdating = true;

                // �S�Ă̔���v���p�e�B���X�V
                OnPropertyChanged(nameof(IsNotNullItem));
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
                
                // ���������v���p�e�B�̍X�V
                OnPropertyChanged(nameof(IsImageBasedItem));
                OnPropertyChanged(nameof(IsAIBasedItem));
                OnPropertyChanged(nameof(IsVariableItem));
                OnPropertyChanged(nameof(IsLoopRelatedItem));
                OnPropertyChanged(nameof(IsIfRelatedItem));
                
                // �\������v���p�e�B�̍X�V
                OnPropertyChanged(nameof(HasWindowTarget));
                OnPropertyChanged(nameof(ShowWindowInfo));
                OnPropertyChanged(nameof(ShowAdvancedSettings));
				
                if (Item != null)
                {
                    LoadItemProperties();
                    
                    // �A�C�e���^�C�v�̑I�����X�V
                    var displayItem = ItemTypes.FirstOrDefault(x => x.TypeName == Item.ItemType);
                    SelectedItemTypeObj = displayItem;

                    _logger.LogDebug("�A�C�e���ύX: {ItemType}", Item.ItemType);
                }
                else
                {
                    ClearAllProperties();
                    SelectedItemTypeObj = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���ύX�������ɃG���[���������܂���");
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void LoadItemProperties()
        {
            if (Item == null) return;

            // ��{�v���p�e�B
            Comment = Item.Comment ?? string.Empty;
            WindowTitle = GetItemProperty<string>("WindowTitle") ?? string.Empty;
            WindowClassName = GetItemProperty<string>("WindowClassName") ?? string.Empty;

            // �摜�֘A�v���p�e�B�iWait_Image�AClickImage�AIfImage�n�ŋ��ʁj
            if (IsImageBasedItem || IsAIBasedItem)
            {
                ImagePath = GetItemProperty<string>("ImagePath") ?? string.Empty;
                Threshold = GetItemProperty<double>("Threshold");
                SearchColor = GetItemProperty<Color?>("SearchColor");
                
                // �^�C���A�E�g�ƃC���^�[�o����Wait_Image��ClickImage�̂�
                if (IsWaitImageItem || IsClickImageItem)
                {
                    Timeout = GetItemProperty<int>("Timeout");
                    Interval = GetItemProperty<int>("Interval");
                }
            }

            // �N���b�N�֘A�v���p�e�B
            if (IsClickItem || IsClickImageItem || IsClickImageAIItem)
            {
                MouseButton = GetItemProperty<MouseButton>("Button");
                UseBackgroundClick = GetItemProperty<bool>("UseBackgroundClick");
                BackgroundClickMethod = GetItemProperty<int>("BackgroundClickMethod");
                
                // ���W��Click�̂�
                if (IsClickItem)
                {
                    ClickX = GetItemProperty<int>("X");
                    ClickY = GetItemProperty<int>("Y");
                }
            }

            // �z�b�g�L�[�֘A�v���p�e�B
            if (IsHotkeyItem)
            {
                CtrlKey = GetItemProperty<bool>("Ctrl");
                AltKey = GetItemProperty<bool>("Alt");
                ShiftKey = GetItemProperty<bool>("Shift");
                SelectedKey = GetItemProperty<Key>("Key");
                HotkeyText = GetItemProperty<string>("HotkeyText") ?? string.Empty;
            }

            // �ҋ@�֘A�v���p�e�B
            if (IsWaitItem)
            {
                var totalMs = GetItemProperty<int>("Wait");
                WaitTime = totalMs;
                
                // �~���b���玞���b�ɕ���
                var totalSeconds = totalMs / 1000;
                var hours = totalSeconds / 3600;
                var minutes = (totalSeconds % 3600) / 60;
                var seconds = totalSeconds % 60;
                var milliseconds = totalMs % 1000;
                
                WaitHours = hours;
                WaitMinutes = minutes;
                WaitSeconds = seconds;
                WaitMilliseconds = milliseconds;
            }

            // ���[�v�֘A�v���p�e�B
            if (IsLoopRelatedItem)
            {
                if (IsLoopItem)
                {
                    // Loop�̏ꍇ�͎�����LoopCount���g�p
                    LoopCount = GetItemProperty<int>("LoopCount");
                }
                else if (IsLoopEndItem)
                {
                    // Loop_End�̏ꍇ�̓y�A��Loop����LoopCount���擾
                    var pairItem = GetPairItem();
                    if (pairItem != null)
                    {
                        LoopCount = GetPropertyFromItem<int>(pairItem, "LoopCount");
                    }
                    else
                    {
                        LoopCount = GetItemProperty<int>("LoopCount");
                    }
                }
                else
                {
                    LoopCount = GetItemProperty<int>("LoopCount");
                }
            }

            // �ϐ��֘A�v���p�e�B
            if (IsVariableItem)
            {
                VariableName = GetItemProperty<string>("Name") ?? string.Empty;
                VariableValue = GetItemProperty<string>("Value") ?? string.Empty;
                
                if (IsIfVariableItem)
                {
                    VariableOperator = GetItemProperty<string>("Operator") ?? "==";
                }
            }

            // AI�֘A�v���p�e�B
            if (IsAIBasedItem)
            {
                ModelPath = GetItemProperty<string>("ModelPath") ?? string.Empty;
                ClassID = GetItemProperty<int>("ClassID");
                ConfThreshold = GetItemProperty<double>("ConfThreshold");
                IoUThreshold = GetItemProperty<double>("IoUThreshold");
                AiDetectMode = GetItemProperty<string>("AIDetectMode") ?? "Class";
            }

            // �v���O�������s�֘A�v���p�e�B
            if (IsExecuteItem)
            {
                ProgramPath = GetItemProperty<string>("ProgramPath") ?? string.Empty;
                Arguments = GetItemProperty<string>("Arguments") ?? string.Empty;
                WorkingDirectory = GetItemProperty<string>("WorkingDirectory") ?? string.Empty;
                WaitForExit = GetItemProperty<bool>("WaitForExit");
            }

            // �X�N���[���V���b�g�֘A�v���p�e�B
            if (IsScreenshotItem)
            {
                SaveDirectory = GetItemProperty<string>("SaveDirectory") ?? string.Empty;
            }
        }

        private void ClearAllProperties()
        {
            Comment = string.Empty;
            WindowTitle = string.Empty;
            WindowClassName = string.Empty;
            ImagePath = string.Empty;
            Threshold = 0.8;
            SearchColor = null;
            Timeout = 5000;
            Interval = 500;
            MouseButton = MouseButton.Left;
            ClickX = 0;
            ClickY = 0;
            UseBackgroundClick = false;
            BackgroundClickMethod = 0;
            CtrlKey = false;
            AltKey = false;
            ShiftKey = false;
            SelectedKey = Key.Escape;
            HotkeyText = string.Empty;
            WaitTime = 1000;
            WaitHours = 0;
            WaitMinutes = 0;
            WaitSeconds = 1;
            WaitMilliseconds = 0;
            LoopCount = 1;
            VariableName = string.Empty;
            VariableValue = string.Empty;
            VariableOperator = "==";
            ModelPath = string.Empty;
            ClassID = 0;
            ConfThreshold = 0.5;
            IoUThreshold = 0.25;
            AiDetectMode = "Class";
            ProgramPath = string.Empty;
            Arguments = string.Empty;
            WorkingDirectory = string.Empty;
            WaitForExit = false;
            SaveDirectory = string.Empty;
        }

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
                _logger.LogDebug("�v���p�e�B�擾�G���[: {Property} - {Error}", propertyName, ex.Message);
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
                    _logger.LogDebug("�v���p�e�B�X�V: {Property} = {Value}", propertyName, value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("�v���p�e�B�ݒ�G���[: {Property} - {Error}", propertyName, ex.Message);
            }
        }

        /// <summary>
        /// �w�肳�ꂽ�A�C�e������v���p�e�B�l���擾
        /// </summary>
        private T GetPropertyFromItem<T>(ICommandListItem item, string propertyName)
        {
            if (item == null) return default(T)!;

            try
            {
                var property = item.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(item);
                    if (value is T tValue)
                        return tValue;
                    if (value != null && typeof(T).IsAssignableFrom(value.GetType()))
                        return (T)value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("�A�C�e���v���p�e�B�擾�G���[: {Property} - {Error}", propertyName, ex.Message);
            }

            return default(T)!;
        }

        #region �v���p�e�B�ύX�ʒm

        partial void OnCommentChanged(string value) => SetItemProperty("Comment", value);
        partial void OnWindowTitleChanged(string value) => SetItemProperty("WindowTitle", value);
        partial void OnWindowClassNameChanged(string value) => SetItemProperty("WindowClassName", value);
        partial void OnImagePathChanged(string value) => SetItemProperty("ImagePath", value);
        partial void OnThresholdChanged(double value) => SetItemProperty("Threshold", value);
        partial void OnSearchColorChanged(Color? value) => SetItemProperty("SearchColor", value);
        partial void OnTimeoutChanged(int value) => SetItemProperty("Timeout", value);
        partial void OnIntervalChanged(int value) => SetItemProperty("Interval", value);
        partial void OnMouseButtonChanged(MouseButton value) => SetItemProperty("Button", value);
        partial void OnClickXChanged(int value) => SetItemProperty("X", value);
        partial void OnClickYChanged(int value) => SetItemProperty("Y", value);
        partial void OnUseBackgroundClickChanged(bool value) => SetItemProperty("UseBackgroundClick", value);
        partial void OnBackgroundClickMethodChanged(int value) => SetItemProperty("BackgroundClickMethod", value);
        partial void OnCtrlKeyChanged(bool value) => SetItemProperty("Ctrl", value);
        partial void OnAltKeyChanged(bool value) => SetItemProperty("Alt", value);
        partial void OnShiftKeyChanged(bool value) => SetItemProperty("Shift", value);
        partial void OnSelectedKeyChanged(Key value) => SetItemProperty("Key", value);
        partial void OnHotkeyTextChanged(string value) => SetItemProperty("HotkeyText", value);
        partial void OnWaitTimeChanged(int value) => SetItemProperty("Wait", value);
        
        partial void OnWaitHoursChanged(int value) => CalculateWaitTime();
        partial void OnWaitMinutesChanged(int value) => CalculateWaitTime();
        partial void OnWaitSecondsChanged(int value) => CalculateWaitTime();
        partial void OnWaitMillisecondsChanged(int value) => CalculateWaitTime();
        
        /// <summary>
        /// �����b�~���b���獇�v�~���b���v�Z
        /// </summary>
        private void CalculateWaitTime()
        {
            if (_isUpdating) return;
            
            try
            {
                var totalMs = (WaitHours * 3600 + WaitMinutes * 60 + WaitSeconds) * 1000 + WaitMilliseconds;
                
                _isUpdating = true;
                WaitTime = totalMs;
                _isUpdating = false;
                
                SetItemProperty("Wait", totalMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ҋ@���Ԍv�Z���ɃG���[���������܂���");
            }
        }
        
        partial void OnLoopCountChanged(int value) 
        { 
            SetItemProperty("LoopCount", value);
            
            // Loop�̏ꍇ�A�y�A�ƂȂ�Loop_End�ɂ������l��ݒ�
            if (IsLoopItem && Item != null)
            {
                var pairItem = GetPairItem();
                if (pairItem != null)
                {
                    try
                    {
                        var pairProperty = pairItem.GetType().GetProperty("LoopCount");
                        if (pairProperty != null && pairProperty.CanWrite)
                        {
                            pairProperty.SetValue(pairItem, value);
                            _logger.LogDebug("�y�A��Loop_End�ɂ�LoopCount={Count}��ݒ肵�܂���", value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "�y�A��LoopCount�ݒ蒆�ɃG���[���������܂���");
                    }
                }
            }
        }

        /// <summary>
        /// �y�A�A�C�e�����擾
        /// </summary>
        private ICommandListItem? GetPairItem()
        {
            if (Item == null) return null;

            try
            {
                var pairProperty = Item.GetType().GetProperty("Pair");
                if (pairProperty != null && pairProperty.CanRead)
                {
                    return pairProperty.GetValue(Item) as ICommandListItem;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("�y�A�A�C�e���擾�G���[: {Error}", ex.Message);
            }

            return null;
        }
        partial void OnVariableNameChanged(string value) => SetItemProperty("Name", value);
        partial void OnVariableValueChanged(string value) => SetItemProperty("Value", value);
        partial void OnVariableOperatorChanged(string value) => SetItemProperty("Operator", value);
        partial void OnModelPathChanged(string value) => SetItemProperty("ModelPath", value);
        partial void OnClassIDChanged(int value) => SetItemProperty("ClassID", value);
        partial void OnConfThresholdChanged(double value) => SetItemProperty("ConfThreshold", value);
        partial void OnIoUThresholdChanged(double value) => SetItemProperty("IoUThreshold", value);
        partial void OnAiDetectModeChanged(string value) => SetItemProperty("AIDetectMode", value);
        partial void OnProgramPathChanged(string value) => SetItemProperty("ProgramPath", value);
        partial void OnArgumentsChanged(string value) => SetItemProperty("Arguments", value);
        partial void OnWorkingDirectoryChanged(string value) => SetItemProperty("WorkingDirectory", value);
        partial void OnWaitForExitChanged(bool value) => SetItemProperty("WaitForExit", value);
        partial void OnSaveDirectoryChanged(string value) => SetItemProperty("SaveDirectory", value);

        /// <summary>
        /// �A�C�e���^�C�v�̑I�����ύX���ꂽ�Ƃ��̏���
        /// </summary>
        partial void OnSelectedItemTypeObjChanged(CommandDisplayItem? value)
        {
            if (_isUpdating || value == null || Item == null) return;

            try
            {
                _logger.LogDebug("�A�C�e���^�C�v�ύX�v��: {OldType} -> {NewType}", Item.ItemType, value.TypeName);

                // �V�����^�C�v�̃A�C�e�����쐬
                var newItem = CommandRegistry.CreateCommandItem(value.TypeName);
                if (newItem == null)
                {
                    _logger.LogWarning("�V�����A�C�e���̍쐬�Ɏ��s���܂���: {TypeName}", value.TypeName);
                    return;
                }

                // ��{�v���p�e�B�������p��
                newItem.LineNumber = Item.LineNumber;
                newItem.IsEnable = Item.IsEnable;
                newItem.Comment = Item.Comment;
                newItem.IsSelected = Item.IsSelected;
                newItem.IsRunning = Item.IsRunning;
                newItem.NestLevel = Item.NestLevel;

                // ���ʃv���p�e�B���\�Ȍ�������p��
                TransferCompatibleProperties(Item, newItem);

                // ���X�g�̊Y���A�C�e����u��
                WeakReferenceMessenger.Default.Send(new ChangeItemTypeMessage(Item, newItem));

                _logger.LogInformation("�A�C�e���^�C�v��ύX���܂���: {OldType} -> {NewType}", Item.ItemType, value.TypeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���^�C�v�ύX���ɃG���[���������܂���");
                
                // ���s�����ꍇ�͌��̑I����Ԃɖ߂�
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

        /// <summary>
        /// �݊����̂���v���p�e�B��]��
        /// </summary>
        private void TransferCompatibleProperties(ICommandListItem fromItem, ICommandListItem toItem)
        {
            try
            {
                var fromType = fromItem.GetType();
                var toType = toItem.GetType();

                // ���ʂ��Ď����Ă���\���̂���v���p�e�B��
                var commonProperties = new[]
                {
                    "WindowTitle", "WindowClassName",
                    "ImagePath", "Threshold", "SearchColor",
                    "Timeout", "Interval",
                    "Button", "X", "Y", "UseBackgroundClick", "BackgroundClickMethod",
                    "Ctrl", "Alt", "Shift", "Key", "HotkeyText",
                    "Wait", "LoopCount",
                    "Name", "Value", "Operator",
                    "ModelPath", "ClassID", "ConfThreshold", "IoUThreshold", "AIDetectMode",
                    "ProgramPath", "Arguments", "WorkingDirectory", "WaitForExit",
                    "SaveDirectory"
                };

                foreach (var propName in commonProperties)
                {
                    var fromProp = fromType.GetProperty(propName);
                    var toProp = toType.GetProperty(propName);

                    if (fromProp != null && toProp != null && 
                        fromProp.CanRead && toProp.CanWrite &&
                        fromProp.PropertyType == toProp.PropertyType)
                    {
                        try
                        {
                            var value = fromProp.GetValue(fromItem);
                            if (value != null || !toProp.PropertyType.IsValueType)
                            {
                                toProp.SetValue(toItem, value);
                                _logger.LogDebug("�v���p�e�B�]��: {Property} = {Value}", propName, value);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug("�v���p�e�B�]�����s: {Property} - {Error}", propName, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���p�e�B�]�����ɃG���[���������܂���");
            }
        }

        #endregion

        #region �R�}���h

        [RelayCommand]
        private void BrowseImageFile()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "�摜�t�@�C����I��",
                    Filter = "�摜�t�@�C��|*.png;*.jpg;*.jpeg;*.bmp;*.gif|���ׂẴt�@�C��|*.*",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    ImagePath = dialog.FileName;
                    _logger.LogDebug("�摜�t�@�C�����I������܂���: {Path}", dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�摜�t�@�C���I�𒆂ɃG���[���������܂���");
            }
        }

        [RelayCommand]
        private void BrowseModelFile()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "���f���t�@�C����I��",
                    Filter = "ONNX���f��|*.onnx|���ׂẴt�@�C��|*.*",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    ModelPath = dialog.FileName;
                    _logger.LogDebug("���f���t�@�C�����I������܂���: {Path}", dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���f���t�@�C���I�𒆂ɃG���[���������܂���");
            }
        }

        [RelayCommand]
        private void BrowseProgramFile()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "���s�t�@�C����I��",
                    Filter = "���s�t�@�C��|*.exe|���ׂẴt�@�C��|*.*",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    ProgramPath = dialog.FileName;
                    _logger.LogDebug("�v���O�����t�@�C�����I������܂���: {Path}", dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���s�t�@�C���I�𒆂ɃG���[���������܂���");
            }
        }

        [RelayCommand]
        private void BrowseSaveDirectory()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "�ۑ����I��",
                    Filter = "���ׂẴt�@�C��|*.*",
                    CheckPathExists = true,
                    FileName = "dummy"
                };

                if (dialog.ShowDialog() == true)
                {
                    SaveDirectory = System.IO.Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                    _logger.LogDebug("�ۑ��f�B���N�g�����I������܂���: {Path}", SaveDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�H���_�I�𒆂ɃG���[���������܂���");
            }
        }

        [RelayCommand]
        private void BrowseWorkingDirectory()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "��ƃt�H���_��I��",
                    Filter = "���ׂẴt�@�C��|*.*",
                    CheckPathExists = true,
                    FileName = "dummy"
                };

                if (dialog.ShowDialog() == true)
                {
                    WorkingDirectory = System.IO.Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                    _logger.LogDebug("��ƃf�B���N�g�����I������܂���: {Path}", WorkingDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ƃt�H���_�I�𒆂ɃG���[���������܂���");
            }
        }

        [RelayCommand]
        private void DebugPrintProperties()
        {
            if (Item == null)
            {
                _logger.LogDebug("Item is null");
                return;
            }

            _logger.LogDebug("=== Current Item Properties ===");
            _logger.LogDebug("ItemType: {ItemType}", Item.ItemType);
            _logger.LogDebug("IsImageBasedItem: {Value}", IsImageBasedItem);
            _logger.LogDebug("IsAIBasedItem: {Value}", IsAIBasedItem);
            _logger.LogDebug("IsVariableItem: {Value}", IsVariableItem);
            _logger.LogDebug("IsLoopRelatedItem: {Value}", IsLoopRelatedItem);
            _logger.LogDebug("IsIfRelatedItem: {Value}", IsIfRelatedItem);
            
            var properties = Item.GetType().GetProperties();
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(Item);
                    _logger.LogDebug("{PropertyName}: {Value}", prop.Name, value ?? "null");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("{PropertyName}: Error - {Error}", prop.Name, ex.Message);
                }
            }
            _logger.LogDebug("=== End Properties ===");
        }

        [RelayCommand]
        private void GetMousePosition()
        {
            try
            {
                _logger.LogDebug("�}�E�X�ʒu�擾���J�n���܂�");
                
                // 3�b��Ƀ}�E�X�ʒu���擾
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                
                timer.Tick += (sender, e) =>
                {
                    timer.Stop();
                    
                    try
                    {
                        // Win32 API�Ń}�E�X�ʒu���擾
                        if (GetCursorPos(out var point))
                        {
                            ClickX = point.X;
                            ClickY = point.Y;
                            _logger.LogDebug("�}�E�X�ʒu���擾���܂���: ({X}, {Y})", point.X, point.Y);
                        }
                        else
                        {
                            _logger.LogWarning("�}�E�X�ʒu�̎擾�Ɏ��s���܂���");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "�}�E�X�ʒu�擾���ɃG���[���������܂���");
                    }
                };
                
                _logger.LogDebug("3�b��Ƀ}�E�X�ʒu���擾���܂�...");
                timer.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�}�E�X�ʒu�擾�̏������ɃG���[���������܂���");
            }
        }

        // Win32 API
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        #endregion

        public void SetItem(ICommandListItem? item)
        {
            Item = item;
        }

        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            _logger.LogDebug("���s��Ԃ�ݒ�: {IsRunning}", isRunning);
        }

        /// <summary>
        /// ��������
        /// </summary>
        public void Prepare()
        {
            try
            {
                _logger.LogDebug("EditPanelViewModel �̏������������s���܂�");
                // �K�v�ɉ����ď���������ǉ�
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EditPanelViewModel �����������ɃG���[���������܂���");
            }
        }
    }

    #region �⏕�N���X

    /// <summary>
    /// �o�b�N�O���E���h�N���b�N���@�A�C�e��
    /// </summary>
    public class BackgroundClickMethodItem
    {
        public int Value { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    #endregion
}