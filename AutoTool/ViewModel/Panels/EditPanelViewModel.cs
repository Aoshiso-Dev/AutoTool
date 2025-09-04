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
        public bool IsListNotEmptyButNoSelection => false; // EditPanel�ł͏��false�i�I�����ꂽ�A�C�e��������ꍇ�̂ݕ\������邽�߁j

        public EditPanelViewModel(ILogger<EditPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messenger = WeakReferenceMessenger.Default;

            InitializeCollections();
            SetupMessaging();
            
            _logger.LogInformation("EditPanelViewModel (�����) �����������܂���");
        }

        private void SetupMessaging()
        {
            try
            {
                _messenger.Register<ChangeSelectedMessage>(this, (r, m) => 
                {
                    _logger.LogDebug("ChangeSelectedMessage��M: {ItemType}", m.SelectedItem?.ItemType ?? "null");
                    SelectedItem = m.SelectedItem;
                });

                _messenger.Register<ChangeItemTypeMessage>(this, (r, m) =>
                {
                    _logger.LogDebug("ChangeItemTypeMessage��M: {OldType} -> {NewType}", 
                        m.OldItem?.ItemType, m.NewItem?.ItemType);
                });

                _messenger.Register<MacroExecutionStateMessage>(this, (r, m) =>
                {
                    _logger.LogDebug("MacroExecutionStateMessage��M: {IsRunning}", m.IsRunning);
                    IsRunning = m.IsRunning;
                });

                _logger.LogDebug("���b�Z�[�W���O�ݒ芮��");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���b�Z�[�W���O�ݒ蒆�ɃG���[");
            }
        }

        private void InitializeCollections()
        {
            try
            {
                _logger.LogDebug("�R���N�V�����������J�n");

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
                _logger.LogDebug("ItemTypes����������: {Count}��", ItemTypes.Count);

                // MouseButtons
                MouseButtons.Clear();
                foreach (var button in Enum.GetValues(typeof(MouseButton)).Cast<MouseButton>())
                    MouseButtons.Add(button);
                _logger.LogDebug("MouseButtons����������: {Count}��", MouseButtons.Count);

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
                _logger.LogDebug("KeyList����������: {Count}��", KeyList.Count);

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
                _logger.LogDebug("Operators����������: {Count}��", Operators.Count);

                // AI Detect Modes
                AiDetectModes.Clear();
                AiDetectModes.Add(new AutoTool.ViewModel.Shared.AIDetectModeItem { Key = "Class", DisplayName = "�N���X���o" });
                AiDetectModes.Add(new AutoTool.ViewModel.Shared.AIDetectModeItem { Key = "Count", DisplayName = "���ʌ��o" });
                _logger.LogDebug("AiDetectModes����������: {Count}��", AiDetectModes.Count);

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
                _logger.LogDebug("BackgroundClickMethods����������: {Count}��", BackgroundClickMethods.Count);

                _logger.LogInformation("EditPanelViewModel �R���N�V��������������");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EditPanelViewModel �R���N�V�������������ɃG���[");
                throw;
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
                nameof(IsListEmpty), nameof(IsListNotEmpty), nameof(IsListNotEmptyButNoSelection),
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
            
            _logger.LogDebug("�S�v���p�e�B�ύX�ʒm����: {Count}��", properties.Length);
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

        // Wait time properties - WaitItem��Wait�v���p�e�B�i�~���b�j�ɑΉ�
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
                // ���݂̒l���擾
                var currentWaitMs = GetItemProperty<int>("Wait");
                var currentTime = TimeSpan.FromMilliseconds(currentWaitMs);

                var newTime = new TimeSpan(
                    0, // days
                    hours ?? (int)currentTime.TotalHours,
                    minutes ?? currentTime.Minutes,
                    seconds ?? currentTime.Seconds,
                    milliseconds ?? currentTime.Milliseconds
                );

                // WaitItem��Wait�v���p�e�B�i�~���b�j�ɐݒ�
                var totalMs = (int)newTime.TotalMilliseconds;
                SetItemProperty("Wait", totalMs);
                
                // ���̎��ԃv���p�e�B�̍X�V�ʒm
                OnPropertyChanged(nameof(WaitHours));
                OnPropertyChanged(nameof(WaitMinutes));
                OnPropertyChanged(nameof(WaitSeconds));
                OnPropertyChanged(nameof(WaitMilliseconds));
                
                _logger.LogDebug("Wait���Ԑݒ�: {Hours}h {Minutes}m {Seconds}s {Milliseconds}ms (���v: {TotalMs}ms)", 
                    newTime.Hours, newTime.Minutes, newTime.Seconds, newTime.Milliseconds, totalMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wait���Ԑݒ�G���[");
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

        #region Diagnostics

        /// <summary>
        /// �o�C���f�B���O�f�f���\�b�h - �f�o�b�O�p
        /// </summary>
        public void DiagnosticProperties()
        {
            try
            {
                _logger.LogInformation("=== EditPanelViewModel �v���p�e�B�f�f ===");
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
                    _logger.LogInformation("SelectedItem�ڍ�:");
                    _logger.LogInformation("  ItemType: {ItemType}", SelectedItem.ItemType);
                    _logger.LogInformation("  Comment: {Comment}", SelectedItem.Comment);
                    _logger.LogInformation("  LineNumber: {LineNumber}", SelectedItem.LineNumber);
                    _logger.LogInformation("  IsEnable: {IsEnable}", SelectedItem.IsEnable);
                    _logger.LogInformation("  ActualType: {ActualType}", SelectedItem.GetType().Name);
                    
                    // WaitItem�̏ꍇ�͑ҋ@���ԃv���p�e�B���ڍ׃`�F�b�N
                    if (IsWaitItem)
                    {
                        _logger.LogInformation("  Wait�֘A�v���p�e�B�f�f:");
                        var waitMs = GetItemProperty<int>("Wait");
                        _logger.LogInformation("    Wait (�~���b): {WaitMs}", waitMs);
                        _logger.LogInformation("    WaitHours: {WaitHours}", WaitHours);
                        _logger.LogInformation("    WaitMinutes: {WaitMinutes}", WaitMinutes);
                        _logger.LogInformation("    WaitSeconds: {WaitSeconds}", WaitSeconds);
                        _logger.LogInformation("    WaitMilliseconds: {WaitMilliseconds}", WaitMilliseconds);
                    }
                    
                    // ���t���N�V�����ŗ��p�\�ȃv���p�e�B���ꗗ�\��
                    _logger.LogInformation("  ���p�\�ȃv���p�e�B:");
                    var props = SelectedItem.GetType().GetProperties();
                    foreach (var prop in props.Take(10)) // �ŏ���10����
                    {
                        try
                        {
                            var value = prop.GetValue(SelectedItem);
                            _logger.LogInformation("    {PropertyName}: {Value} ({Type})", 
                                prop.Name, value ?? "null", prop.PropertyType.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation("    {PropertyName}: �G���[ ({Error})", prop.Name, ex.Message);
                        }
                    }
                }
                
                _logger.LogInformation("=== �f�f���� ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���p�e�B�f�f���ɃG���[");
            }
        }

        /// <summary>
        /// �o�C���f�B���O�e�X�g�p���\�b�h
        /// </summary>
        public void TestPropertyNotification()
        {
            try
            {
                _logger.LogInformation("�v���p�e�B�ύX�ʒm�e�X�g�J�n");
                
                // �S�Ă̔���v���p�e�B�𖾎��I�ɍX�V
                NotifyAllPropertiesChanged();
                
                _logger.LogInformation("�v���p�e�B�ύX�ʒm�e�X�g����");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���p�e�B�ύX�ʒm�e�X�g���ɃG���[");
            }
        }

        #endregion
    }
}