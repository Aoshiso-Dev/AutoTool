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
    /// �W��MVVM�p�^�[���ɂ��EditPanelViewModel (�����)
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

        // �A�C�e���^�C�v����v���p�e�B
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

        // ������������
        public bool IsImageBasedItem => IsWaitImageItem || IsClickImageItem || IsIfImageExistItem || IsIfImageNotExistItem || IsScreenshotItem;
        public bool IsAIBasedItem => IsClickImageAIItem || IsIfImageExistAIItem || IsIfImageNotExistAIItem || IsSetVariableAIItem;
        public bool IsVariableItem => IsIfVariableItem || IsSetVariableItem || IsSetVariableAIItem;
        public bool IsLoopRelatedItem => IsLoopItem || IsLoopEndItem || IsLoopBreakItem;
        public bool IsIfRelatedItem => IsIfImageExistItem || IsIfImageNotExistItem || IsIfImageExistAIItem || IsIfImageNotExistAIItem || IsIfVariableItem || IsIfEndItem;

        // �\������v���p�e�B
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
            
            _logger.LogInformation("EditPanelViewModel (�����) �����������܂���");
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
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "==", DisplayName = "������ (==)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "!=", DisplayName = "�������Ȃ� (!=)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = ">", DisplayName = "���傫�� (>)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "<", DisplayName = "��菬���� (<)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = ">=", DisplayName = "�ȏ� (>=)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "<=", DisplayName = "�ȉ� (<=)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "Contains", DisplayName = "�܂� (Contains)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "StartsWith", DisplayName = "�n�܂� (StartsWith)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "EndsWith", DisplayName = "�I��� (EndsWith)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "IsEmpty", DisplayName = "��ł��� (IsEmpty)" });
                Operators.Add(new AutoTool.ViewModel.Shared.OperatorItem { Key = "IsNotEmpty", DisplayName = "��łȂ� (IsNotEmpty)" });

                // AI Detect Modes
                AiDetectModes.Clear();
                AiDetectModes.Add(new AutoTool.ViewModel.Shared.AIDetectModeItem { Key = "Class", DisplayName = "�N���X���o" });
                AiDetectModes.Add(new AutoTool.ViewModel.Shared.AIDetectModeItem { Key = "Count", DisplayName = "���ʌ��o" });

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

                _logger.LogDebug("EditPanelViewModel �R���N�V��������������");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EditPanelViewModel �R���N�V�������������ɃG���[");
            }
        }

        // OnSelectedItemChanged��ObservableProperty�Ŏ������������ partial void
        partial void OnSelectedItemChanged(ICommandListItem? value)
        {
            try
            {
                _isUpdating = true;

                // �A�C�e���^�C�v�̑I�����X�V
                if (value != null)
                {
                    var displayItem = ItemTypes.FirstOrDefault(x => x.TypeName == value.ItemType);
                    SelectedItemTypeObj = displayItem;
                }
                else
                {
                    SelectedItemTypeObj = null;
                }

                // �S�Ă̔���v���p�e�B���X�V
                NotifyAllPropertiesChanged();
                
                _logger.LogDebug("SelectedItem�ύX: {ItemType}", value?.ItemType ?? "null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SelectedItem�ύX�������ɃG���[");
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
                _logger.LogDebug("ItemType�ύX�v��: {OldType} -> {NewType}", SelectedItem.ItemType, value.TypeName);

                // �V�����^�C�v�̃A�C�e�����쐬
                var newItem = CommandRegistry.CreateCommandItem(value.TypeName);
                if (newItem == null)
                {
                    _logger.LogWarning("�V�����A�C�e���̍쐬�Ɏ��s: {TypeName}", value.TypeName);
                    return;
                }

                // ��{�v���p�e�B�������p��
                newItem.LineNumber = SelectedItem.LineNumber;
                newItem.IsEnable = SelectedItem.IsEnable;
                newItem.Comment = SelectedItem.Comment;
                newItem.IsSelected = SelectedItem.IsSelected;
                newItem.IsRunning = SelectedItem.IsRunning;
                newItem.NestLevel = SelectedItem.NestLevel;

                // ���X�g�̊Y���A�C�e����u��
                _messenger.Send(new ChangeItemTypeMessage(SelectedItem, newItem));

                _logger.LogInformation("ItemType�ύX����: {OldType} -> {NewType}", SelectedItem.ItemType, value.TypeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ItemType�ύX���ɃG���[");
                
                // ���s�����ꍇ�͌��̑I����Ԃɖ߂�
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
                // �l�v���p�e�B
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

        #region Properties for data binding (���S�Ȏ���)

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
                    _logger.LogDebug("�v���p�e�B�擾�G���[: WaitHours - {Error}", ex.Message);
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
                    _logger.LogDebug("�v���p�e�B�擾�G���[: WaitMinutes - {Error}", ex.Message);
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
                    _logger.LogDebug("�v���p�e�B�擾�G���[: WaitSeconds - {Error}", ex.Message);
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
                    _logger.LogDebug("�v���p�e�B�擾�G���[: WaitMilliseconds - {Error}", ex.Message);
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
                _logger.LogError(ex, "WaitTime�ݒ�G���[");
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
                _logger.LogDebug("�v���p�e�B�擾�G���[: {Property} - {Error}", propertyName, ex.Message);
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
                _logger.LogError(ex, "�v���p�e�B�ݒ�G���[: {Property} = {Value}", propertyName, value);
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
                    Title = "�摜�t�@�C����I��",
                    Filter = "�摜�t�@�C�� (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|���ׂẴt�@�C�� (*.*)|*.*",
                    DefaultExt = ".png"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    ImagePath = openFileDialog.FileName;
                    _logger.LogInformation("�摜�t�@�C����I��: {ImagePath}", ImagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�摜�t�@�C���I�𒆂ɃG���[");
            }
        }

        [RelayCommand]
        private void BrowseModelFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "AI���f���t�@�C����I��",
                    Filter = "ONNX�t�@�C�� (*.onnx)|*.onnx|���ׂẴt�@�C�� (*.*)|*.*",
                    DefaultExt = ".onnx"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    ModelPath = openFileDialog.FileName;
                    _logger.LogInformation("���f���t�@�C����I��: {ModelPath}", ModelPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���f���t�@�C���I�𒆂ɃG���[");
            }
        }

        [RelayCommand]
        private void BrowseProgramFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "���s�t�@�C����I��",
                    Filter = "���s�t�@�C�� (*.exe)|*.exe|���ׂẴt�@�C�� (*.*)|*.*",
                    DefaultExt = ".exe"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    ProgramPath = openFileDialog.FileName;
                    _logger.LogInformation("���s�t�@�C����I��: {ProgramPath}", ProgramPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���s�t�@�C���I�𒆂ɃG���[");
            }
        }

        [RelayCommand]
        private void BrowseWorkingDirectory()
        {
            try
            {
                // Microsoft.Win32.CommonOpenFileDialog���g�p�iWindows�p�j
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "��ƃf�B���N�g����I�����Ă�������",
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "�t�H���_��I��"
                };

                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = Path.GetDirectoryName(dialog.FileName);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        WorkingDirectory = selectedPath;
                        _logger.LogInformation("��ƃf�B���N�g����I��: {WorkingDirectory}", WorkingDirectory);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ƃf�B���N�g���I�𒆂ɃG���[");
            }
        }

        [RelayCommand]
        private void BrowseSaveDirectory()
        {
            try
            {
                // Microsoft.Win32.CommonOpenFileDialog���g�p�iWindows�p�j
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "�ۑ��f�B���N�g����I�����Ă�������",
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "�t�H���_��I��"
                };

                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = Path.GetDirectoryName(dialog.FileName);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        SaveDirectory = selectedPath;
                        _logger.LogInformation("�ۑ��f�B���N�g����I��: {SaveDirectory}", SaveDirectory);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ۑ��f�B���N�g���I�𒆂ɃG���[");
            }
        }

        [RelayCommand]
        private void GetMousePosition()
        {
            try
            {
                // WinAPI���g�p���ă}�E�X�ʒu���擾
                var point = new System.Drawing.Point();
                if (GetCursorPos(out point))
                {
                    ClickX = point.X;
                    ClickY = point.Y;
                    _logger.LogInformation("�}�E�X�ʒu���擾: ({X}, {Y})", ClickX, ClickY);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�}�E�X�ʒu�擾���ɃG���[");
            }
        }

        [RelayCommand]
        private void GetWindowInfo()
        {
            try
            {
                // WindowHelper�@�\���g�p���ăE�B���h�E�����擾
                _logger.LogInformation("�E�B���h�E���擾�@�\�i�������j");
                // TODO: WindowHelper�Ƃ̘A�g����
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�E�B���h�E���擾���ɃG���[");
            }
        }

        [RelayCommand]
        private void ClearWindowInfo()
        {
            try
            {
                WindowTitle = string.Empty;
                WindowClassName = string.Empty;
                _logger.LogInformation("�E�B���h�E�����N���A");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�E�B���h�E���N���A���ɃG���[");
            }
        }

        #endregion

        #region Win32 API

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        #endregion
    }
}