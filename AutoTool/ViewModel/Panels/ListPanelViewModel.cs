using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using AutoTool.Command.Interface;
using AutoTool.Services;
using AutoTool.Message;
using AutoTool.Model.List.Interface;
using AutoTool.ViewModel.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using AutoTool.Model.CommandDefinition;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// ListPanelViewModel (Phase 5������) - CommandRegistry -> DirectCommandRegistry�Ή�
    /// </summary>
    public partial class ListPanelViewModel : ObservableObject, IRecipient<RunMessage>, IRecipient<StopMessage>, IRecipient<AddMessage>, IRecipient<ClearMessage>, IRecipient<UpMessage>, IRecipient<DownMessage>, IRecipient<DeleteMessage>, IRecipient<UndoMessage>, IRecipient<RedoMessage>, IRecipient<LoadMessage>, IRecipient<SaveMessage>
    {
        private readonly ILogger<ListPanelViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICommandListItemFactory _commandListItemFactory;
        private readonly IRecentFileService _recentFileService;

        private ICommand? _currentExecutingCommand;
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private ObservableCollection<ICommandListItem> _items = new();

        [ObservableProperty]
        private ICommandListItem? _selectedItem;

        [ObservableProperty]
        private int _selectedIndex = -1;

        [ObservableProperty]
        private ICommandListItem? _currentExecutingItem;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private bool _canExecute = true;

        [ObservableProperty]
        private int _totalItems;

        [ObservableProperty]
        private int _executedItems;

        [ObservableProperty]
        private int _currentLineNumber;

        [ObservableProperty]
        private string _statusMessage = "��������";

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private ObservableCollection<string> _executionLog = new();

        [ObservableProperty]
        private bool _isLogVisible = false;

        [ObservableProperty]
        private string _currentFileName = "�V�K�t�@�C��";

        [ObservableProperty]
        private bool _hasUnsavedChanges = false;

        [ObservableProperty]
        private ObservableCollection<string> _undoStack = new();

        [ObservableProperty]
        private ObservableCollection<string> _redoStack = new();

        [ObservableProperty]
        private TimeSpan _elapsedTime;

        [ObservableProperty]
        private string _currentCommandDescription = string.Empty;

        /// <summary>
        /// �A�C�e�������݂��邩�ǂ���
        /// </summary>
        public bool HasItems => Items.Count > 0;

        /// <summary>
        /// Undo���\���ǂ���
        /// </summary>
        public bool CanUndo => UndoStack.Count > 0;

        /// <summary>
        /// Redo���\���ǂ���
        /// </summary>
        public bool CanRedo => RedoStack.Count > 0;

        public ListPanelViewModel(
            ILogger<ListPanelViewModel> logger,
            IServiceProvider serviceProvider,
            ICommandListItemFactory commandListItemFactory,
            IRecentFileService recentFileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _commandListItemFactory = commandListItemFactory ?? throw new ArgumentNullException(nameof(commandListItemFactory));
            _recentFileService = recentFileService ?? throw new ArgumentNullException(nameof(recentFileService));

            TotalItems = Items.Count;

            // ���b�Z�[�W���O�V�X�e���ɓo�^
            WeakReferenceMessenger.Default.Register<RunMessage>(this);
            WeakReferenceMessenger.Default.Register<StopMessage>(this);
            WeakReferenceMessenger.Default.Register<AddMessage>(this);
            WeakReferenceMessenger.Default.Register<ClearMessage>(this);
            WeakReferenceMessenger.Default.Register<UpMessage>(this);
            WeakReferenceMessenger.Default.Register<DownMessage>(this);
            WeakReferenceMessenger.Default.Register<DeleteMessage>(this);
            WeakReferenceMessenger.Default.Register<UndoMessage>(this);
            WeakReferenceMessenger.Default.Register<RedoMessage>(this);
            WeakReferenceMessenger.Default.Register<LoadMessage>(this);
            WeakReferenceMessenger.Default.Register<SaveMessage>(this);

            _logger.LogInformation("ListPanelViewModel (Phase 5) ������������܂���");

            // �v���p�e�B�ύX�̊Ď�
            Items.CollectionChanged += (s, e) =>
            {
                TotalItems = Items.Count;
                HasUnsavedChanges = true;
                OnPropertyChanged(nameof(HasItems));
            };
        }

        public void Receive(RunMessage message)
        {
            _logger.LogInformation("�}�N�����s�J�n");
            _ = Task.Run(async () => await ExecuteMacroAsync());
        }

        public void Receive(StopMessage message)
        {
            _logger.LogInformation("�}�N�����s��~�v��");
            StopMacro();
        }

        public void Receive(AddMessage message)
        {
            _logger.LogDebug("�R�}���h�ǉ��v��: {ItemType}", message.ItemType);
            AddItem(message.ItemType);
        }

        public void Receive(ClearMessage message)
        {
            _logger.LogDebug("���X�g�N���A�v��");
            ClearAll();
        }

        public void Receive(UpMessage message)
        {
            _logger.LogDebug("�A�C�e����ړ��v��");
            MoveUp();
        }

        public void Receive(DownMessage message)
        {
            _logger.LogDebug("�A�C�e�����ړ��v��");
            MoveDown();
        }

        public void Receive(DeleteMessage message)
        {
            _logger.LogDebug("�A�C�e���폜�v��");
            DeleteSelected();
        }

        public void Receive(UndoMessage message)
        {
            _logger.LogDebug("Undo�v��");
            Undo();
        }

        public void Receive(RedoMessage message)
        {
            _logger.LogDebug("Redo�v��");
            Redo();
        }

        public void Receive(LoadMessage message)
        {
            _logger.LogDebug("�t�@�C���ǂݍ��ݗv��");
            _ = LoadFileAsync();
        }

        public void Receive(SaveMessage message)
        {
            _logger.LogDebug("�t�@�C���ۑ��v��");
            _ = SaveFileAsync();
        }

        // ���s���R�}���h�̕\�������X�V���鏈��
        partial void OnCurrentExecutingItemChanged(ICommandListItem? value)
        {
            if (value != null)
            {
                var displayName = DirectCommandRegistry.DisplayOrder.GetDisplayName(CurrentExecutingItem.ItemType) ?? CurrentExecutingItem.ItemType;
                CurrentCommandDescription = $"���s��: {displayName} (�s {value.LineNumber})";
            }
            else
            {
                CurrentCommandDescription = string.Empty;
            }
        }

        // �I���A�C�e���ύX���̏�����ǉ�
        partial void OnSelectedItemChanged(ICommandListItem? value)
        {
            try
            {
                _logger.LogDebug("=== ListPanel�I��ύX: {OldItem} -> {NewItem} ===", 
                    _selectedItem?.ItemType ?? "null", value?.ItemType ?? "null");

                if (value != null)
                {
                    _logger.LogInformation("?? �R�}���h�I��: {ItemType} (�s�ԍ�: {LineNumber}, �^�C�v: {ActualType})", 
                        value.ItemType, value.LineNumber, value.GetType().Name);

                    // EditPanel�ɑI��ύX��ʒm
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(value));
                    
                    // �I���A�C�e���������\�����邽��IsSelected���X�V
                    foreach (var item in Items)
                    {
                        item.IsSelected = (item == value);
                    }

                    StatusMessage = $"�I��: {DirectCommandRegistry.DisplayOrder.GetDisplayName(value.ItemType)} (�s {value.LineNumber})";
                }
                else
                {
                    _logger.LogDebug("�R�}���h�I������");
                    
                    // EditPanel�ɑI��������ʒm
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(null));
                    
                    // �S�Ă̑I����Ԃ��N���A
                    foreach (var item in Items)
                    {
                        item.IsSelected = false;
                    }

                    StatusMessage = "�R�}���h���I������Ă��܂���";
                }

                _logger.LogDebug("=== ListPanel�I��ύX���� ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SelectedItem�ύX�������ɃG���[");
            }
        }

        private void AddItem(string itemType)
        {
            try
            {
                _logger.LogInformation("=== �R�}���h�ǉ������J�n: {ItemType} ===", itemType);

                // 1. UniversalCommandItem�Ƃ��Ēǉ������s�iDirectCommandRegistry�g�p�j
                try
                {
                    var universalItem = DirectCommandRegistry.CreateUniversalItem(itemType);
                    if (universalItem != null)
                    {
                        universalItem.LineNumber = GetNextLineNumber();
                        universalItem.Comment = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}�̐���";
                        
                        var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                        Items.Insert(insertIndex, universalItem);
                        
                        SelectedItem = universalItem;
                        SelectedIndex = Items.IndexOf(universalItem);
                        
                        // EditPanel�ɑI��ύX��ʒm
                        WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(universalItem));
                        
                        _logger.LogInformation("? UniversalCommandItem�Ƃ��Ēǉ�����: {ItemType} (�s�ԍ�: {LineNumber}, �}���ʒu: {Index})", 
                            itemType, universalItem.LineNumber, insertIndex);
                        StatusMessage = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType)}��ǉ����܂���";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "UniversalCommandItem�쐬���s�A�]����Factory���g�p: {ItemType}", itemType);
                }

                // 2. �]����CommandListItemFactory�Ńt�H�[���o�b�N
                var newItem = _commandListItemFactory.CreateItem(itemType);
                if (newItem != null)
                {
                    newItem.LineNumber = GetNextLineNumber();
                    newItem.Comment = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}�̐���";
                    
                    var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                    Items.Insert(insertIndex, newItem);
                    
                    SelectedItem = newItem;
                    SelectedIndex = Items.IndexOf(newItem);
                    
                    // EditPanel�ɑI��ύX��ʒm
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(newItem));
                    
                    _logger.LogInformation("? CommandListItemFactory�Œǉ�����: {ItemType} (�s�ԍ�: {LineNumber}, �}���ʒu: {Index})", 
                        itemType, newItem.LineNumber, insertIndex);
                    StatusMessage = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType)}��ǉ����܂���";
                }
                else
                {
                    // 3. �ŏI�t�H�[���o�b�N: BasicCommandItem
                    try
                    {
                        var fallbackItem = new AutoTool.Model.List.Type.BasicCommandItem
                        {
                            ItemType = itemType,
                            LineNumber = GetNextLineNumber(),
                            IsEnable = true,
                            Comment = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}�̐���"
                        };
                        
                        var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                        Items.Insert(insertIndex, fallbackItem);
                        
                        SelectedItem = fallbackItem;
                        SelectedIndex = Items.IndexOf(fallbackItem);
                        
                        // EditPanel�ɑI��ύX��ʒm
                        WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(fallbackItem));
                        
                        _logger.LogWarning("?? BasicCommandItem�Ƃ��ăt�H�[���o�b�N�ǉ�: {ItemType} (�s�ԍ�: {LineNumber}, �}���ʒu: {Index})", 
                            itemType, fallbackItem.LineNumber, insertIndex);
                        StatusMessage = $"{DirectCommandRegistry.DisplayOrder.GetDisplayName(itemType)}��ǉ����܂����i��{���[�h�j";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "? BasicCommandItem�쐬�����s: {ItemType}", itemType);
                        StatusMessage = $"�R�}���h�ǉ��Ɏ��s���܂���: {itemType}";
                        return;
                    }
                }

                _logger.LogInformation("=== �R�}���h�ǉ���������: {ItemType} (���A�C�e����: {TotalCount}) ===", 
                    itemType, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? AddItem���ɃG���[: {ItemType}", itemType);
                StatusMessage = $"�ǉ��G���[: {ex.Message}";
            }
        }

        private void ClearAll()
        {
            try
            {
                Items.Clear();
                SelectedItem = null;
                SelectedIndex = -1;
                StatusMessage = "���ׂẴA�C�e�����N���A���܂���";
                _logger.LogDebug("�S�A�C�e���N���A����");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�N���A���ɃG���[");
                StatusMessage = $"�N���A�G���[: {ex.Message}";
            }
        }

        private void MoveUp()
        {
            try
            {
                if (SelectedItem != null && SelectedIndex > 0)
                {
                    var index = Items.IndexOf(SelectedItem);
                    Items.RemoveAt(index);
                    Items.Insert(index - 1, SelectedItem);
                    SelectedIndex = index - 1;
                    StatusMessage = "�A�C�e������Ɉړ����܂���";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ړ����ɃG���[");
                StatusMessage = $"�ړ��G���[: {ex.Message}";
            }
        }

        private void MoveDown()
        {
            try
            {
                if (SelectedItem != null && SelectedIndex < Items.Count - 1)
                {
                    var index = Items.IndexOf(SelectedItem);
                    Items.RemoveAt(index);
                    Items.Insert(index + 1, SelectedItem);
                    SelectedIndex = index + 1;
                    StatusMessage = "�A�C�e�������Ɉړ����܂���";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���ړ����ɃG���[");
                StatusMessage = $"�ړ��G���[: {ex.Message}";
            }
        }

        private void DeleteSelected()
        {
            try
            {
                if (SelectedItem != null)
                {
                    var index = Items.IndexOf(SelectedItem);
                    Items.Remove(SelectedItem);
                    
                    // �I���ʒu�𒲐�
                    if (Items.Count > 0)
                    {
                        if (index >= Items.Count) index = Items.Count - 1;
                        SelectedItem = Items[index];
                        SelectedIndex = index;
                    }
                    else
                    {
                        SelectedItem = null;
                        SelectedIndex = -1;
                    }
                    
                    StatusMessage = "�I���A�C�e�����폜���܂���";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�폜���ɃG���[");
                StatusMessage = $"�폜�G���[: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            try
            {
                if (UndoStack.Count > 0)
                {
                    var action = UndoStack.Last();
                    UndoStack.Remove(action);
                    RedoStack.Add(action);
                    StatusMessage = "��������ɖ߂��܂���";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Undo���ɃG���[");
                StatusMessage = $"Undo�G���[: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            try
            {
                if (RedoStack.Count > 0)
                {
                    var action = RedoStack.Last();
                    RedoStack.Remove(action);
                    UndoStack.Add(action);
                    StatusMessage = "�������蒼���܂���";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redo���ɃG���[");
                StatusMessage = $"Redo�G���[: {ex.Message}";
            }
        }

        private async Task LoadFileAsync()
        {
            try
            {
                StatusMessage = "�t�@�C����ǂݍ��ݒ�...";
                // TODO: �t�@�C���ǂݍ��ݏ����̎���
                await Task.Delay(100);
                StatusMessage = "�t�@�C���ǂݍ��݊���";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ǂݍ��ݒ��ɃG���[");
                StatusMessage = $"�ǂݍ��݃G���[: {ex.Message}";
            }
        }

        private async Task SaveFileAsync()
        {
            try
            {
                StatusMessage = "�t�@�C����ۑ���...";
                // TODO: �t�@�C���ۑ������̎���
                await Task.Delay(100);
                StatusMessage = "�t�@�C���ۑ�����";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ۑ����ɃG���[");
                StatusMessage = $"�ۑ��G���[: {ex.Message}";
            }
        }

        private async Task ExecuteMacroAsync()
        {
            try
            {
                IsRunning = true;
                StatusMessage = "�}�N�����s��...";
                
                // TODO: �}�N�����s�����̎���
                await Task.Delay(1000);
                
                StatusMessage = "�}�N�����s����";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�}�N�����s���ɃG���[");
                StatusMessage = $"���s�G���[: {ex.Message}";
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void StopMacro()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                IsRunning = false;
                StatusMessage = "�}�N�����s���~���܂���";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�}�N����~���ɃG���[");
                StatusMessage = $"��~�G���[: {ex.Message}";
            }
        }

        /// <summary>
        /// ���s��Ԃ�ݒ�
        /// </summary>
        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
            if (isRunning)
            {
                StatusMessage = "���s��...";
                _logger.LogDebug("���s��Ԃɐݒ肳��܂���");
            }
            else
            {
                StatusMessage = "��������";
                CurrentExecutingItem = null;
                _logger.LogDebug("��~��Ԃɐݒ肳��܂���");
            }
        }

        /// <summary>
        /// �v���O���X��������
        /// </summary>
        public void InitializeProgress()
        {
            Progress = 0;
            ExecutedItems = 0;
            ElapsedTime = TimeSpan.Zero;
            ExecutionLog.Clear();
            _logger.LogDebug("�v���O���X������������܂���");
        }

        private int GetNextLineNumber()
        {
            return Items.Count > 0 ? Items.Max(x => x.LineNumber) + 1 : 1;
        }
    }
}