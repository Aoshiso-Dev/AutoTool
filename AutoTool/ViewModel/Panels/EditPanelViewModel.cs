using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using AutoTool.Message;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using AutoTool.Model.List.Interface;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using ColorPickHelper;
using AutoTool.Model.CommandDefinition;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel.Helpers;
using AutoTool.ViewModel.Shared; // Phase 5������CommandHistoryManager
using CommandDisplayItem = AutoTool.ViewModel.Shared.CommandDisplayItem;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// �������ꂽEditPanelViewModel�iAutoTool.ViewModel���O��ԁj
    /// Phase 5: ���S���������ŁAMacroPanels�ˑ������S�r��
    /// </summary>
    public partial class EditPanelViewModel : ObservableObject
    {
        private readonly ILogger<EditPanelViewModel> _logger;
        private readonly CommandHistoryManager _commandHistory;
        private readonly EditPanelPropertyManager _propertyManager;
        private ICommandListItem? _item = null;
        private bool _isUpdating;
        private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(120) };

        // �v���p�e�B
        public ICommandListItem? Item
        {
            get => _item;
            set
            {
                if (SetProperty(ref _item, value))
                {
                    // Item���ύX���ꂽ���ɑΉ�����SelectedItemTypeObj���X�V
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

                    OnItemChanged();
                    UpdateProperties(); 
                    UpdateIsProperties(); 
                }
            }
        }

        [ObservableProperty]
        private AutoTool.ViewModel.Shared.CommandDisplayItem? _selectedItemTypeObj;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private int _listCount = 0;

        [ObservableProperty]
        private ObservableCollection<AutoTool.ViewModel.Shared.CommandDisplayItem> _itemTypes = new();

        [ObservableProperty]
        private ObservableCollection<MouseButton> _mouseButtons = new();

        [ObservableProperty]
        private ObservableCollection<OperatorItem> _operators = new();

        [ObservableProperty]
        private ObservableCollection<AIDetectModeItem> _aiDetectModes = new();

        // ViewModels�p��DI�Ή��R���X�g���N�^�iPhase 5���S�����Łj
        public EditPanelViewModel(
            ILogger<EditPanelViewModel> logger,
            CommandHistoryManager commandHistory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandHistory = commandHistory ?? throw new ArgumentNullException(nameof(commandHistory));
            _propertyManager = new EditPanelPropertyManager(); // AutoTool�����ł��g�p

            _logger.LogInformation("Phase 5���S����EditPanelViewModel �� DI�Ή��ŏ��������Ă��܂�");

            _refreshTimer.Tick += (s, e) => { _refreshTimer.Stop(); WeakReferenceMessenger.Default.Send(new RefreshListViewMessage()); };

            InitializeItemTypes();
            InitializeOperators();
            InitializeAIDetectModes();
        }

        private void InitializeItemTypes()
        {
            // AutoTool����CommandRegistry��������
            CommandRegistry.Initialize();

            // ���{��\�����t���̃A�C�e�����쐬�i���S������CommandDisplayItem���g�p�j
            var displayItems = CommandRegistry.GetOrderedTypeNames()
                .Select(typeName => new CommandDisplayItem
                {
                    TypeName = typeName,
                    DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                    Category = CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                })
                .ToList();

            ItemTypes = new ObservableCollection<AutoTool.ViewModel.Shared.CommandDisplayItem>(displayItems);

            foreach (var button in Enum.GetValues(typeof(MouseButton)).Cast<MouseButton>())
                MouseButtons.Add(button);

            _logger.LogInformation("Phase 5���S����EditPanelViewModel �̏��������������܂���");
        }

        private void InitializeOperators()
        {
            Operators.Clear();
            Operators.Add(new OperatorItem { Key = "Equal", DisplayName = "������" });
            Operators.Add(new OperatorItem { Key = "NotEqual", DisplayName = "�������Ȃ�" });
            Operators.Add(new OperatorItem { Key = "GreaterThan", DisplayName = "���傫��" });
            Operators.Add(new OperatorItem { Key = "LessThan", DisplayName = "��菬����" });
        }

        private void InitializeAIDetectModes()
        {
            AiDetectModes.Clear();
            AiDetectModes.Add(new AIDetectModeItem { Key = "Fast", DisplayName = "����" });
            AiDetectModes.Add(new AIDetectModeItem { Key = "Accurate", DisplayName = "�����x" });
            AiDetectModes.Add(new AIDetectModeItem { Key = "Balanced", DisplayName = "�o�����X" });
        }

        private void OnItemChanged()
        {
            try
            {
                _logger.LogDebug("�A�C�e�����ύX����܂���: {ItemType}", Item?.ItemType ?? "null");
                // Phase 5: AutoTool�����Ńv���p�e�B�}�l�[�W���[�ɂ�銮�S�ȊǗ�
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���ύX�������ɃG���[���������܂���");
            }
        }

        #region Item Type Detection Properties (Phase 5���S������)
        public bool IsListNotEmpty => ListCount > 0;
        public bool IsListEmpty => ListCount == 0;
        public bool IsListNotEmptyButNoSelection => ListCount > 0 && Item == null;
        public bool IsNotNullItem => Item != null;
        
        // Phase 5: ItemType������x�[�X����ɓ���i�^���S�����ێ��j
        public bool IsWaitImageItem => Item?.ItemType == CommandRegistry.CommandTypes.WaitImage;
        public bool IsClickImageItem => Item?.ItemType == CommandRegistry.CommandTypes.ClickImage;
        public bool IsClickImageAIItem => Item?.ItemType == CommandRegistry.CommandTypes.ClickImageAI;
        public bool IsHotkeyItem => Item?.ItemType == CommandRegistry.CommandTypes.Hotkey;
        public bool IsClickItem => Item?.ItemType == CommandRegistry.CommandTypes.Click;
        public bool IsWaitItem => Item?.ItemType == CommandRegistry.CommandTypes.Wait;
        public bool IsLoopItem => Item?.ItemType == CommandRegistry.CommandTypes.Loop;
        public bool IsEndLoopItem => Item?.ItemType == CommandRegistry.CommandTypes.LoopEnd;
        public bool IsBreakItem => Item?.ItemType == CommandRegistry.CommandTypes.LoopBreak;
        public bool IsIfImageExistItem => Item?.ItemType == CommandRegistry.CommandTypes.IfImageExist;
        public bool IsIfImageNotExistItem => Item?.ItemType == CommandRegistry.CommandTypes.IfImageNotExist;
        public bool IsIfImageExistAIItem => Item?.ItemType == CommandRegistry.CommandTypes.IfImageExistAI;
        public bool IsIfImageNotExistAIItem => Item?.ItemType == CommandRegistry.CommandTypes.IfImageNotExistAI;
        public bool IsEndIfItem => Item?.ItemType == CommandRegistry.CommandTypes.IfEnd;
        public bool IsExecuteProgramItem => Item?.ItemType == CommandRegistry.CommandTypes.Execute;
        public bool IsSetVariableItem => Item?.ItemType == CommandRegistry.CommandTypes.SetVariable;
        public bool IsSetVariableAIItem => Item?.ItemType == CommandRegistry.CommandTypes.SetVariableAI;
        public bool IsIfVariableItem => Item?.ItemType == CommandRegistry.CommandTypes.IfVariable;
        public bool IsScreenshotItem => Item?.ItemType == CommandRegistry.CommandTypes.Screenshot;
        #endregion

        #region Window Properties
        public string WindowTitleText => string.IsNullOrEmpty(WindowTitle) ? "�w��Ȃ�" : WindowTitle;
        public string WindowTitle { get => _propertyManager.WindowTitle.GetValue(Item); set { _propertyManager.WindowTitle.SetValue(Item, value); UpdateProperties(); } }
        public string WindowClassNameText => string.IsNullOrEmpty(WindowClassName) ? "�w��Ȃ�" : WindowClassName;
        public string WindowClassName { get => _propertyManager.WindowClassName.GetValue(Item); set { _propertyManager.WindowClassName.SetValue(Item, value); UpdateProperties(); } }
        #endregion

        #region Basic Properties
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

        #region Property Update Management
        private void UpdateIsProperties()
        {
            var propertyNames = new[]
            {
                nameof(IsListNotEmpty), nameof(IsListEmpty), nameof(IsListNotEmptyButNoSelection), 
                nameof(IsNotNullItem), nameof(IsWaitImageItem), 
                nameof(IsClickImageItem), nameof(IsClickImageAIItem), nameof(IsHotkeyItem), 
                nameof(IsClickItem), nameof(IsWaitItem), nameof(IsLoopItem), 
                nameof(IsEndLoopItem), nameof(IsBreakItem), nameof(IsIfImageExistItem), 
                nameof(IsIfImageNotExistItem), nameof(IsEndIfItem), nameof(IsIfImageExistAIItem), 
                nameof(IsIfImageNotExistAIItem), nameof(IsExecuteProgramItem), nameof(IsSetVariableItem), 
                nameof(IsSetVariableAIItem), nameof(IsIfVariableItem), nameof(IsScreenshotItem)
            };

            foreach (var name in propertyNames)
                OnPropertyChanged(name);
        }

        void UpdateProperties()
        {
            if (_isUpdating) return;
            
            try
            {
                _isUpdating = true;
                
                var propertyNames = new[]
                {
                    nameof(WindowTitle), nameof(WindowTitleText), 
                    nameof(WindowClassName), nameof(WindowClassNameText), 
                    nameof(Comment)
                };
                
                foreach (var name in propertyNames)
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

        /// <summary>
        /// �A�C�e����ݒ�
        /// </summary>
        public void SetItem(ICommandListItem? item)
        {
            Item = item;
        }

        /// <summary>
        /// ���X�g�J�E���g��ݒ�
        /// </summary>
        public void SetListCount(int count)
        {
            ListCount = count;
        }

        /// <summary>
        /// ���s��Ԃ�ݒ�
        /// </summary>
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
            _logger.LogDebug("Phase 5���S����EditPanelViewModel �̏������������s");
        }
    }

    // �⏕�N���X�iAutoTool.ViewModel.Panels���O��Ԃɓ����j
    public class OperatorItem
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public class AIDetectModeItem
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}