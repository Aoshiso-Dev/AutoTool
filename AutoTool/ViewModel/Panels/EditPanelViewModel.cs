using AutoTool.Message;
using AutoTool.Model.CommandDefinition;
using AutoTool.Model.List.Class;
using AutoTool.Model.List.Interface;
using AutoTool.Model.MacroFactory;
using AutoTool.ViewModel.Shared;
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
    /// �W��MVVM�p�^�[���ɂ��EditPanelViewModel (�����)
    /// </summary>
    public partial class EditPanelViewModel : ObservableObject
    {
        private readonly ILogger<EditPanelViewModel> _logger;
        private readonly IMessenger _messenger;
        private readonly AutoTool.Services.Mouse.IMouseService _mouseService;
        private bool _isUpdating = false;

        // �}�N���t�@�C���̃x�[�X�p�X�i���΃p�X�����p�j
        private string _macroFileBasePath = string.Empty;

        [ObservableProperty]
        private ICommandListItem? _selectedItem;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private bool _isWaitingForRightClick = false;

        [ObservableProperty]
        private string _mouseWaitMessage = string.Empty;

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

        // �摜�v���r���[�֘A�v���p�e�B
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

        public EditPanelViewModel(ILogger<EditPanelViewModel> logger, AutoTool.Services.Mouse.IMouseService mouseService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mouseService = mouseService ?? throw new ArgumentNullException(nameof(mouseService));
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

                // �t�@�C���ǂݍ��ݎ��̃x�[�X�p�X�X�V�i�x���X�V�t���j
                _messenger.Register<LoadMessage>(this, async (r, m) =>
                {
                    UpdateMacroFileBasePath(m.FilePath);
                    // �t�@�C���ǂݍ��݊�����ɏ����҂��Ă���v���r���[�X�V
                    await Task.Delay(100);
                    if (SelectedItem != null)
                    {
                        UpdateImagePreview();
                        UpdateModelInfo();
                        UpdateProgramInfo();
                        UpdateSaveDirectoryInfo();
                    }
                });

                _messenger.Register<LoadFileMessage>(this, async (r, m) =>
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
                });

                _messenger.Register<SaveMessage>(this, (r, m) => UpdateMacroFileBasePath(m.FilePath));
                _messenger.Register<SaveFileMessage>(this, (r, m) => UpdateMacroFileBasePath(m.FilePath));

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

                // �摜�v���r���[�Ɗ֘A�����X�V
                UpdateImagePreview();
                UpdateModelInfo();
                UpdateProgramInfo();
                UpdateSaveDirectoryInfo();

                // �S�Ă̔���v���p�e�B���X�V
                NotifyAllPropertiesChanged();

                _logger.LogDebug("SelectedItem�ύX: {ItemType} (���΃p�X�Ή�)", value?.ItemType ?? "null");
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
            // ��{����v���p�e�B
            var properties = new[]
            {
                nameof(IsWaitImageItem), nameof(IsClickImageItem), nameof(IsClickImageAIItem),
                nameof(IsHotkeyItem), nameof(IsClickItem), nameof(IsWaitItem),
                nameof(IsLoopItem), nameof(IsLoopEndItem), nameof(IsLoopBreakItem),
                nameof(IsIfImageExistItem), nameof(IsIfImageNotExistItem), nameof(IsIfImageExistAIItem),
                nameof(IsIfImageNotExistAIItem), nameof(IsIfEndItem), nameof(IsIfEndItem),
                nameof(IsIfVariableItem), nameof(IsExecuteItem), nameof(IsSetVariableItem),
                nameof(IsSetVariableAIItem), nameof(IsScreenshotItem), nameof(IsImageBasedItem),
                nameof(IsAIBasedItem), nameof(IsVariableItem), nameof(IsLoopRelatedItem),
                nameof(IsIfRelatedItem), nameof(ShowWindowInfo), nameof(ShowAdvancedSettings),
                nameof(IsNotNullItem), nameof(IsListEmpty), nameof(IsListNotEmpty),
                nameof(IsListNotEmptyButNoSelection)
            };

            // �l�v���p�e�B
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

            // ��{����v���p�e�B�̒ʒm
            foreach (var property in properties)
            {
                OnPropertyChanged(property);
            }

            // �l�v���p�e�B�̒ʒm
            foreach (var property in valueProperties)
            {
                OnPropertyChanged(property);
            }

            _logger.LogDebug("�S�v���p�e�B�ύX�ʒm����: {Count}��", properties.Length + valueProperties.Length);
        }

        /// <summary>
        /// �}�N���t�@�C���̃x�[�X�p�X���X�V
        /// </summary>
        private void UpdateMacroFileBasePath(string? filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    _macroFileBasePath = Path.GetDirectoryName(filePath) ?? string.Empty;
                    _logger.LogDebug("�}�N���t�@�C���x�[�X�p�X�X�V: {BasePath}", _macroFileBasePath);

                    // �x�[�X�p�X���X�V���ꂽ���ɁA���ɑI������Ă���A�C�e��������ꍇ�̓v���r���[���X�V
                    if (SelectedItem != null)
                    {
                        _logger.LogDebug("�x�[�X�p�X�X�V�ɔ����A�I�𒆃A�C�e���̃v���r���[���X�V: {ItemType}", SelectedItem.ItemType);
                        UpdateImagePreview();
                        UpdateModelInfo();
                        UpdateProgramInfo();
                        UpdateSaveDirectoryInfo();

                        // �v���p�e�B�ύX�ʒm�����M���āAUI�̕\�����X�V
                        NotifyAllPropertiesChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�}�N���t�@�C���x�[�X�p�X�X�V�G���[: {FilePath}", filePath);
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

        #region Properties for data binding (���S��)

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
                // �I�����ꂽ�p�X�𑊑΃p�X�ɕϊ����Ă���ۑ�
                var pathToSave = ConvertToRelativePath(value);
                SetItemProperty("ImagePath", pathToSave);
                // �摜�p�X���ύX���ꂽ���ɉ摜�v���r���[���X�V
                UpdateImagePreview();
                _logger.LogDebug("ImagePath�X�V: {Original} -> {Relative}", value, pathToSave);
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
                    Title = "�摜�t�@�C����I��",
                    Filter = "�摜�t�@�C�� (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|���ׂẴt�@�C�� (*.*)|*.*"
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
                    _logger.LogInformation("�摜�t�@�C����I��: {ImagePath} (���΃p�X: {RelativePath})",
                        selectedPath, pathToSave);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�摜�t�@�C���I�𒆂ɃG���[");
            }
        }

        [RelayCommand]
        private void ClearImagePreview()
        {
            try
            {
                SetItemProperty("ImagePath", string.Empty);
                ImagePreview = null;
                _logger.LogInformation("�摜�v���r���[���N���A");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�摜�v���r���[�N���A���ɃG���[");
            }
        }

        [RelayCommand]
        private void BrowseModelFile()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "AI���f���t�@�C����I��",
                    Filter = "ONNX�t�@�C�� (*.onnx)|*.onnx|���ׂẴt�@�C�� (*.*)|*.*"
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
                    _logger.LogInformation("AI���f���t�@�C����I��: {ModelPath} (���΃p�X: {RelativePath})",
                        selectedPath, pathToSave);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI���f���t�@�C���I�𒆂ɃG���[");
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
                    Filter = "���s�t�@�C�� (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|���ׂẴt�@�C�� (*.*)|*.*"
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
                    _logger.LogInformation("���s�t�@�C����I��: {ProgramPath} (���΃p�X: {RelativePath})",
                        selectedPath, pathToSave);
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
                // �ȈՓI�ȃt�H���_�I���iOpenFileDialog���g������֎����j
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "��ƃf�B���N�g����I�� (�C�ӂ̃t�@�C����I�����ăf�B���N�g�����w��)",
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "�t�H���_��I��"
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
                        _logger.LogInformation("��ƃf�B���N�g����I��: {WorkingDirectory} (���΃p�X: {RelativePath})",
                            selectedPath, pathToSave);
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
                // �ȈՓI�ȃt�H���_�I���iOpenFileDialog���g������֎����j
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "�ۑ��f�B���N�g����I�� (�C�ӂ̃t�@�C����I�����ăf�B���N�g�����w��)",
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "�t�H���_��I��"
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
                        _logger.LogInformation("�ۑ��f�B���N�g����I��: {SaveDirectory} (���΃p�X: {RelativePath})",
                            selectedPath, pathToSave);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ۑ��f�B���N�g���I�𒆂ɃG���[");
            }
        }


        [RelayCommand]
        private void ClearWindowInfo()
        {
            try
            {
                SetItemProperty("WindowTitle", string.Empty);
                SetItemProperty("WindowClassName", string.Empty);
                _logger.LogInformation("�E�B���h�E�����N���A");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�E�B���h�E���N���A���ɃG���[");
            }
        }

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
                _logger.LogInformation("�}�N���t�@�C���x�[�X�p�X: {BasePath}", _macroFileBasePath);
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

                    // �p�X�֘A�v���p�e�B�̐f�f
                    if (IsImageBasedItem || IsAIBasedItem)
                    {
                        _logger.LogInformation("  �p�X�֘A�v���p�e�B�f�f:");
                        var imagePath = ImagePath;
                        var resolvedImagePath = ResolvePath(imagePath);
                        _logger.LogInformation("    ImagePath: {ImagePath}", imagePath);
                        _logger.LogInformation("    �������ꂽImagePath: {ResolvedPath}", resolvedImagePath);
                        _logger.LogInformation("    �t�@�C������: {Exists}", FileExistsResolved(imagePath));

                        if (IsAIBasedItem)
                        {
                            var modelPath = ModelPath;
                            var resolvedModelPath = ResolvePath(modelPath);
                            _logger.LogInformation("    ModelPath: {ModelPath}", modelPath);
                            _logger.LogInformation("    �������ꂽModelPath: {ResolvedPath}", resolvedModelPath);
                            _logger.LogInformation("    ���f���t�@�C������: {Exists}", FileExistsResolved(modelPath));
                        }
                    }

                    if (IsExecuteItem)
                    {
                        var programPath = ProgramPath;
                        var workingDir = WorkingDirectory;
                        _logger.LogInformation("    ProgramPath: {ProgramPath}", programPath);
                        _logger.LogInformation("    �������ꂽProgramPath: {ResolvedPath}", ResolvePath(programPath));
                        _logger.LogInformation("    WorkingDirectory: {WorkingDirectory}", workingDir);
                        _logger.LogInformation("    �������ꂽWorkingDirectory: {ResolvedPath}", ResolvePath(workingDir));
                    }

                    if (IsScreenshotItem)
                    {
                        var saveDir = SaveDirectory;
                        _logger.LogInformation("    SaveDirectory: {SaveDirectory}", saveDir);
                        _logger.LogInformation("    �������ꂽSaveDirectory: {ResolvedPath}", ResolvePath(saveDir));
                        _logger.LogInformation("    �f�B���N�g������: {Exists}", DirectoryExistsResolved(saveDir));
                    }

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

        #region �t�@�C���p�X�����w���p�[

        /// <summary>
        /// ���΃p�X�܂��͐�΃p�X���������āA���ۂ̃t�@�C���p�X��Ԃ�
        /// </summary>
        /// <param name="path">��������p�X</param>
        /// <returns>�������ꂽ�p�X�A�t�@�C�������݂��Ȃ��ꍇ�͌��̃p�X</returns>
        private string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            try
            {
                // ���ɐ�΃p�X�̏ꍇ
                if (Path.IsPathRooted(path))
                {
                    return path;
                }

                // ���΃p�X�̏ꍇ�A�}�N���t�@�C���̃x�[�X�p�X�������
                if (!string.IsNullOrEmpty(_macroFileBasePath))
                {
                    var resolvedPath = Path.Combine(_macroFileBasePath, path);
                    resolvedPath = Path.GetFullPath(resolvedPath); // ���K��

                    // �������ꂽ�p�X�Ƀt�@�C�������݂��邩�m�F
                    if (File.Exists(resolvedPath) || Directory.Exists(resolvedPath))
                    {
                        _logger.LogTrace("���΃p�X��������: {RelativePath} -> {AbsolutePath}", path, resolvedPath);
                        return resolvedPath;
                    }
                }

                // �J�����g�f�B���N�g������̑��΃p�X�Ƃ��Ď��s
                var currentDirPath = Path.GetFullPath(path);
                if (File.Exists(currentDirPath) || Directory.Exists(currentDirPath))
                {
                    _logger.LogTrace("�J�����g�f�B���N�g���������: {RelativePath} -> {AbsolutePath}", path, currentDirPath);
                    return currentDirPath;
                }

                _logger.LogTrace("�p�X�������s�A���̃p�X��Ԃ�: {Path}", path);
                return path;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�p�X�������ɃG���[: {Path}", path);
                return path;
            }
        }

        /// <summary>
        /// ��΃p�X�𑊑΃p�X�ɕϊ��i�\�ȏꍇ�j
        /// </summary>
        /// <param name="absolutePath">��΃p�X</param>
        /// <returns>���΃p�X�A�ϊ��ł��Ȃ��ꍇ�͐�΃p�X</returns>
        private string ConvertToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || string.IsNullOrEmpty(_macroFileBasePath))
                return absolutePath;

            try
            {
                if (Path.IsPathRooted(absolutePath))
                {
                    var relativePath = Path.GetRelativePath(_macroFileBasePath, absolutePath);

                    // ���΃p�X���e�f�B���N�g���𑽗p���Ă��Ȃ��ꍇ�̂ݎg�p
                    if (!relativePath.StartsWith("..\\..."))
                    {
                        _logger.LogTrace("��΃p�X�𑊑΃p�X�ɕϊ�: {AbsolutePath} -> {RelativePath}", absolutePath, relativePath);
                        return relativePath;
                    }
                }

                return absolutePath;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "���΃p�X�ϊ����ɃG���[: {Path}", absolutePath);
                return absolutePath;
            }
        }

        /// <summary>
        /// �t�@�C�������݂��邩�`�F�b�N�i���΃p�X/��΃p�X�����Ή��j
        /// </summary>
        /// <param name="path">�`�F�b�N����p�X</param>
        /// <returns>�t�@�C�������݂���ꍇtrue</returns>
        private bool FileExistsResolved(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var resolvedPath = ResolvePath(path);
            return File.Exists(resolvedPath);
        }

        /// <summary>
        /// �f�B���N�g�������݂��邩�`�F�b�N�i���΃p�X/��΃p�X�����Ή��j
        /// </summary>
        /// <param name="path">�`�F�b�N����p�X</param>
        /// <returns>�f�B���N�g�������݂���ꍇtrue</returns>
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
        /// �摜�v���r���[���X�V
        /// </summary>
        private void UpdateImagePreview()
        {
            try
            {
                var imagePath = GetItemProperty<string>("ImagePath") ?? string.Empty;
                var resolvedPath = ResolvePath(imagePath);

                _logger.LogTrace("�摜�v���r���[�X�V�J�n: {ImagePath} -> {ResolvedPath}", imagePath, resolvedPath);

                if (string.IsNullOrEmpty(imagePath) || !FileExistsResolved(imagePath))
                {
                    ImagePreview = null;
                    HasImagePreview = false;
                    ImageInfo = string.IsNullOrEmpty(imagePath) ? string.Empty : $"�t�@�C����������܂���: {imagePath}";
                    HasImageInfo = !string.IsNullOrEmpty(ImageInfo);
                    _logger.LogDebug("�摜�v���r���[���N���A: {ImagePath} (�����p�X: {ResolvedPath})", imagePath, resolvedPath);
                    return;
                }

                // �t�@�C�������擾
                var fileInfo = new FileInfo(resolvedPath);

                // �摜��BitmapImage�Ƃ��ēǂݍ���
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(resolvedPath);
                bitmap.DecodePixelWidth = 300; // �v���r���[�p�Ƀ��T�C�Y
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // UI �X���b�h�O����̎g�p�̂���Freeze

                ImagePreview = bitmap;
                HasImagePreview = true;

                // �摜����ݒ�i���΃p�X�\�� + �t�@�C�����j
                var fileSizeKB = fileInfo.Length / 1024.0;
                var pathDisplay = imagePath != resolvedPath ? $"{imagePath} ({Path.GetFileName(resolvedPath)})" : imagePath;
                ImageInfo = $"{pathDisplay}\n{bitmap.PixelWidth} x {bitmap.PixelHeight} px, {fileSizeKB:F1} KB";
                HasImageInfo = true;

                _logger.LogInformation("�摜�v���r���[���X�V: {ImagePath} -> {ResolvedPath} ({Width}x{Height}, {Size:F1}KB)",
                    imagePath, resolvedPath, bitmap.PixelWidth, bitmap.PixelHeight, fileSizeKB);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�摜�v���r���[�X�V�G���[: {ImagePath}", GetItemProperty<string>("ImagePath"));
                ImagePreview = null;
                HasImagePreview = false;
                ImageInfo = $"�摜�ǂݍ��݃G���[: {ex.Message}";
                HasImageInfo = true;
            }
        }

        /// <summary>
        /// ���f���t�@�C�������X�V
        /// </summary>
        private void UpdateModelInfo()
        {
            try
            {
                var modelPath = GetItemProperty<string>("ModelPath") ?? string.Empty;
                var resolvedPath = ResolvePath(modelPath);

                if (string.IsNullOrEmpty(modelPath) || !FileExistsResolved(modelPath))
                {
                    ModelInfo = string.IsNullOrEmpty(modelPath) ? string.Empty : $"�t�@�C����������܂���: {modelPath}";
                    HasModelInfo = !string.IsNullOrEmpty(ModelInfo);
                    return;
                }

                var fileInfo = new FileInfo(resolvedPath);
                var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                var pathDisplay = modelPath != resolvedPath ? $"{modelPath} ({Path.GetFileName(resolvedPath)})" : modelPath;
                ModelInfo = $"{pathDisplay}\n{fileSizeMB:F1} MB, {fileInfo.LastWriteTime:yyyy/MM/dd HH:mm}";
                HasModelInfo = true;

                _logger.LogDebug("���f�������X�V: {ModelPath} -> {ResolvedPath} ({Size:F1}MB)", modelPath, resolvedPath, fileSizeMB);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���f�����X�V�G���[: {ModelPath}", GetItemProperty<string>("ModelPath"));
                ModelInfo = $"�t�@�C�����擾�G���[: {ex.Message}";
                HasModelInfo = true;
            }
        }

        /// <summary>
        /// �v���O�����t�@�C�������X�V
        /// </summary>
        private void UpdateProgramInfo()
        {
            try
            {
                var programPath = GetItemProperty<string>("ProgramPath") ?? string.Empty;
                var resolvedPath = ResolvePath(programPath);

                if (string.IsNullOrEmpty(programPath) || !FileExistsResolved(programPath))
                {
                    ProgramInfo = string.IsNullOrEmpty(programPath) ? string.Empty : $"�t�@�C����������܂���: {programPath}";
                    HasProgramInfo = !string.IsNullOrEmpty(ProgramInfo);
                    return;
                }

                var fileInfo = new FileInfo(resolvedPath);
                var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                var pathDisplay = programPath != resolvedPath ? $"{programPath} ({Path.GetFileName(resolvedPath)})" : programPath;
                ProgramInfo = $"{pathDisplay}\n{fileSizeMB:F1} MB, {fileInfo.LastWriteTime:yyyy/MM/dd HH:mm}";
                HasProgramInfo = true;

                _logger.LogDebug("�v���O���������X�V: {ProgramPath} -> {ResolvedPath} ({Size:F1}MB)", programPath, resolvedPath, fileSizeMB);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���O�������X�V�G���[: {ProgramPath}", GetItemProperty<string>("ProgramPath"));
                ProgramInfo = $"�t�@�C�����擾�G���[: {ex.Message}";
                HasProgramInfo = true;
            }
        }

        /// <summary>
        /// �ۑ��f�B���N�g�������X�V
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
                    SaveDirectoryInfo = $"{pathDisplay}\n�����f�B���N�g��: {fileCount} �t�@�C��";
                    HasSaveDirectoryInfo = true;
                }
                else
                {
                    SaveDirectoryInfo = $"{saveDir}\n�V�K�f�B���N�g�� (�쐬�\��)";
                    HasSaveDirectoryInfo = true;
                }

                _logger.LogDebug("�ۑ��f�B���N�g�������X�V: {SaveDir} -> {ResolvedPath}", saveDir, resolvedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ۑ��f�B���N�g�����X�V�G���[: {SaveDir}", GetItemProperty<string>("SaveDirectory"));
                SaveDirectoryInfo = $"�f�B���N�g�����擾�G���[: {ex.Message}";
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
                // ���s���̏ꍇ�͉������Ȃ�
                if (IsRunning) return;

                // ���ݑI������Ă���A�C�e�����擾
                var item = SelectedItem;
                if (item == null)
                {
                    _logger.LogWarning("���s�A�C�e�����I������Ă��܂���");
                    return;
                }

                _logger.LogInformation("���s�J�n: {ItemType} - {Comment}", item.ItemType, item.Comment);

                // ���s��ԂɑJ��
                IsRunning = true;
                item.IsRunning = true;
                OnPropertyChanged(nameof(IsRunning));

                // �}�N���̎��s�Ȃ�
                // TODO: �}�N�����s���W�b�N�̎���

                _logger.LogInformation("���s����: {ItemType} - {Comment}", item.ItemType, item.Comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h���s���ɃG���[: {ItemType}", SelectedItem?.ItemType);
            }
            finally
            {
                // ��n��
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
                _logger.LogInformation("�}�E�X�ʒu�擾�J�n");

                IsWaitingForRightClick = true;
                MouseWaitMessage = "�E�N���b�N���Ă��������i�擾�����ʒu�� X/Y �ɐݒ肵�܂��j";

                // �ΏۃE�B���h�E�����擾
                var windowTitle = GetItemProperty<string>("WindowTitle") ?? string.Empty;
                var windowClassName = GetItemProperty<string>("WindowClassName") ?? string.Empty;

                System.Drawing.Point position;

                // �E�B���h�E���w�肳��Ă���ꍇ�͉E�N���b�N�ҋ@���[�h���g�p
                if (!string.IsNullOrEmpty(windowTitle))
                {
                    _logger.LogInformation("�E�N���b�N�ҋ@���[�h�J�n: �E�B���h�E={WindowTitle}, �N���X={WindowClassName}",
                        windowTitle, windowClassName);

                    // �E�N���b�N�ҋ@�i�N���C�A���g���W�Ŏ擾�j
                    position = await _mouseService.WaitForRightClickAsync(windowTitle,
                        string.IsNullOrEmpty(windowClassName) ? null : windowClassName);
                }
                else
                {
                    _logger.LogInformation("�E�N���b�N�ҋ@���[�h�J�n: �X�N���[�����W");

                    // �X�N���[�����W�ŉE�N���b�N�ҋ@
                    position = await _mouseService.WaitForRightClickAsync();
                }

                // �擾�������W��ݒ�
                ClickX = position.X;
                ClickY = position.Y;

                NotifyAllPropertiesChanged();

                _logger.LogInformation("�}�E�X�ʒu�ݒ芮��: ({X}, {Y})", position.X, position.Y);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("�}�E�X�ʒu�擾���L�����Z������܂���");
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning("�}�E�X�ʒu�擾���^�C���A�E�g���܂���: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�}�E�X�ʒu�擾���ɃG���[");
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
                _logger.LogInformation("���݂̃}�E�X�ʒu�擾");

                // �ΏۃE�B���h�E�����擾
                var windowTitle = GetItemProperty<string>("WindowTitle") ?? string.Empty;
                var windowClassName = GetItemProperty<string>("WindowClassName") ?? string.Empty;

                System.Drawing.Point position;

                // �E�B���h�E���w�肳��Ă���ꍇ�̓N���C�A���g���W�Ŏ擾
                if (!string.IsNullOrEmpty(windowTitle))
                {
                    position = _mouseService.GetClientPosition(windowTitle, 
                        string.IsNullOrEmpty(windowClassName) ? null : windowClassName);
                    _logger.LogInformation("�N���C�A���g���W�擾: ({X}, {Y}) for �E�B���h�E={WindowTitle}", 
                        position.X, position.Y, windowTitle);
                }
                else
                {
                    position = _mouseService.GetCurrentPosition();
                    _logger.LogInformation("�X�N���[�����W�擾: ({X}, {Y})", position.X, position.Y);
                }

                // �擾�������W��ݒ�
                ClickX = position.X;
                ClickY = position.Y;

                _logger.LogInformation("���݂̃}�E�X�ʒu�ݒ芮��: ({X}, {Y})", position.X, position.Y);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���݂̃}�E�X�ʒu�擾���ɃG���[");
            }
        }

        [RelayCommand]
        private async Task GetWindowInfoAsync()
        {
            try
            {
                if (_isWaitingForRightClick) return; // ���̑ҋ@�Ƌ������Ȃ��悤��
                _logger.LogInformation("�E�B���h�E���擾�J�n (�A�N�e�B�u�ɂ��ĉE�N���b�N�Ŋm�� / Esc�ŃL�����Z��)");
                IsWaitingForRightClick = true; // �����̃C���W�P�[�^�𗬗p
                MouseWaitMessage = "�ΏۃE�B���h�E���A�N�e�B�u�ɂ��ĉE�N���b�N�Ŋm�� (Esc�ŃL�����Z��)";

                var svc = App.Current is AutoTool.App app && app.Services != null
                    ? app.Services.GetService(typeof(AutoTool.Services.Window.IWindowInfoService)) as AutoTool.Services.Window.IWindowInfoService
                    : null;
                if (svc == null)
                {
                    _logger.LogWarning("IWindowInfoService�������ł��܂���ł���");
                    return;
                }
                var (title, className) = await svc.WaitForActiveWindowSelectionAsync();
                if (!string.IsNullOrEmpty(title))
                {
                    SetItemProperty("WindowTitle", title, nameof(WindowTitle));
                    SetItemProperty("WindowClassName", className, nameof(WindowClassName));
                    OnPropertyChanged(nameof(WindowTitle));
                    OnPropertyChanged(nameof(WindowClassName));
                    _logger.LogInformation("�E�B���h�E���擾���� Title={Title} Class={Class}", title, className);
                }
                else
                {
                    _logger.LogInformation("�E�B���h�E���͋�ł���");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("�E�B���h�E���擾�L�����Z��");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�E�B���h�E���擾���ɃG���[");
            }
            finally
            {
                IsWaitingForRightClick = false;
                MouseWaitMessage = string.Empty;
            }
        }
    }
}