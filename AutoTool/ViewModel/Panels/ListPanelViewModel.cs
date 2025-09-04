using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using AutoTool.Message;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;
using AutoTool.Model.CommandDefinition;
using AutoTool.Command.Class;
using AutoTool.Command.Interface;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using AutoTool.List.Class; // CommandList�N���X�p
using System.Windows; // Thickness�p
using System.Threading.Tasks; // Task�p
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5���S�����ŁFListPanelViewModel�iDI�Ή��Łj
    /// </summary>
    public partial class ListPanelViewModel : ObservableObject
    {
        private readonly ILogger<ListPanelViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ObservableCollection<ICommandListItem> _items = new();
        private readonly Stack<CommandListOperation> _undoStack = new();
        private readonly Stack<CommandListOperation> _redoStack = new();
        private int _completedCommands = 0;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ICommandListItem? _selectedItem;

        [ObservableProperty]
        private int _selectedIndex = -1;

        [ObservableProperty]
        private string _statusMessage = "��������";

        [ObservableProperty]
        private bool _hasUnsavedChanges = false;

        [ObservableProperty]
        private int _totalItems = 0;

        // �v���O���X�o�[�֘A�v���p�e�B
        [ObservableProperty]
        private int _totalProgress = 0;

        [ObservableProperty]
        private int _currentProgress = 0;

        [ObservableProperty]
        private string _progressText = "";

        [ObservableProperty]
        private bool _showProgress = false;

        [ObservableProperty]
        private ICommandListItem? _currentExecutingItem;

        partial void OnCurrentExecutingItemChanged(ICommandListItem? value)
        {
            try
            {
                _logger.LogDebug("CurrentExecutingItem�ύX: {OldItem} -> {NewItem}", 
                    _currentExecutingItem?.ItemType ?? "null", 
                    value?.ItemType ?? "null");
                
                // �֘A�v���p�e�B�̍X�V�ʒm
                OnPropertyChanged(nameof(CurrentExecutingDescription));
                
                // UI�̋����X�V
                OnPropertyChanged(nameof(Items));
                
                _logger.LogDebug("CurrentExecutingItem�ύX����: {Description}", CurrentExecutingDescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CurrentExecutingItem�ύX�������ɃG���[");
            }
        }

        public ObservableCollection<ICommandListItem> Items => _items;
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public bool HasItems => Items.Count > 0;

        /// <summary>
        /// �v���O���X�����擾�i�p�[�Z���e�[�W�j
        /// </summary>
        public double ProgressPercentage => TotalProgress > 0 ? (double)CurrentProgress / TotalProgress * 100 : 0;

        /// <summary>
        /// �c�莞�Ԃ̐���i�ȈՔŁj
        /// </summary>
        public string EstimatedTimeRemaining
        {
            get
            {
                if (TotalProgress <= 0 || CurrentProgress <= 0) return "�s��";
                
                var remaining = TotalProgress - CurrentProgress;
                if (remaining <= 0) return "����";
                
                // �ȈՓI�Ȑ���i���ۂ̎����ł͎��s���Ԃ��L�^����K�v����j
                var estimatedSeconds = remaining * 2; // 1�R�}���h������2�b�Ɖ���
                return $"��{estimatedSeconds}�b";
            }
        }

        /// <summary>
        /// ���ݎ��s���̃R�}���h�̐������擾
        /// </summary>
        public string CurrentExecutingDescription
        {
            get
            {
                if (CurrentExecutingItem == null) return "";
                
                var displayName = CommandRegistry.DisplayOrder.GetDisplayName(CurrentExecutingItem.ItemType) ?? CurrentExecutingItem.ItemType;
                var comment = !string.IsNullOrEmpty(CurrentExecutingItem.Comment) ? $"({CurrentExecutingItem.Comment})" : "";
                return $"���s��: {displayName} {comment}";
            }
        }

        /// <summary>
        /// DI�Ή��R���X�g���N�^
        /// </summary>
        public ListPanelViewModel(ILogger<ListPanelViewModel> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            SetupMessaging();
            _logger.LogInformation("DI�Ή�ListPanelViewModel �����������Ă��܂�");

            // �R���N�V�����ύX�̊Ď�
            _items.CollectionChanged += (s, e) =>
            {
                TotalItems = _items.Count;
                HasUnsavedChanges = true;
                
                // �v���p�e�B�ύX�ʒm�̋���
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));
                
                // MainWindowViewModel�ɃA�C�e�����ύX��ʒm
                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(_items.Count));
                
                // �R���N�V�����ύX��ɏ����x�����ăy�A�����O�X�V
                Task.Delay(50).ContinueWith(_ =>
                {
                    try
                    {
                        UpdateLineNumbers();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "�R���N�V�����ύX��̃y�A�����O�X�V�ŃG���[");
                    }
                }, TaskScheduler.Default);
            };
        }

        private void SetupMessaging()
        {
            // �R�}���h���상�b�Z�[�W�̏���
            WeakReferenceMessenger.Default.Register<AddMessage>(this, (r, m) => AddInternal(m.ItemType));
            WeakReferenceMessenger.Default.Register<DeleteMessage>(this, (r, m) => DeleteInternal());
            WeakReferenceMessenger.Default.Register<UpMessage>(this, (r, m) => MoveUpInternal());
            WeakReferenceMessenger.Default.Register<DownMessage>(this, (r, m) => MoveDownInternal());
            WeakReferenceMessenger.Default.Register<ClearMessage>(this, (r, m) => ClearInternal());
            WeakReferenceMessenger.Default.Register<UndoMessage>(this, (r, m) => UndoInternal());
            WeakReferenceMessenger.Default.Register<RedoMessage>(this, (r, m) => RedoInternal());
            
            // �A�C�e���^�C�v�ύX���b�Z�[�W�̏���
            WeakReferenceMessenger.Default.Register<ChangeItemTypeMessage>(this, (r, m) => ChangeItemType(m.OldItem, m.NewItem));
            
            // ���X�g�r���[�X�V���b�Z�[�W�̏���
            WeakReferenceMessenger.Default.Register<RefreshListViewMessage>(this, (r, m) => RefreshList());
            
            // �R�}���h���s��ԃ��b�Z�[�W�̏���
            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (r, m) => OnCommandStarted(m));
            WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, (r, m) => OnCommandFinished(m));
            WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, (r, m) => OnProgressUpdated(m));
            
            // �t�@�C�����상�b�Z�[�W�̏���
            WeakReferenceMessenger.Default.Register<LoadFileMessage>(this, (r, m) => LoadFileInternal(m.FilePath));
            WeakReferenceMessenger.Default.Register<SaveFileMessage>(this, (r, m) => SaveFileInternal(m.FilePath));
            
            // �}�N�����s��ԃ��b�Z�[�W�̏���
            WeakReferenceMessenger.Default.Register<MacroExecutionStateMessage>(this, (r, m) => SetRunningState(m.IsRunning));
        }

        partial void OnSelectedItemChanged(ICommandListItem? value)
        {
            if (value != null)
            {
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(value));
                _logger.LogDebug("�I���A�C�e���ύX: {ItemType} (�s {LineNumber})", value.ItemType, value.LineNumber);
            }
        }

        #region �O������A�N�Z�X�\�ȃ��\�b�h

        /// <summary>
        /// �O������A�N�Z�X�\�ȃA�C�e���ǉ����\�b�h
        /// </summary>
        public void Add(string itemType)
        {
            try
            {
                _logger.LogDebug("�O��Add�Ăяo��: {ItemType}", itemType);
                AddInternal(itemType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�O��Add�Ăяo�����ɃG���[: {ItemType}", itemType);
            }
        }

        public void Delete() => DeleteInternal();
        public void MoveUp() => MoveUpInternal();
        public void MoveDown() => MoveDownInternal();
        public void Clear() => ClearInternal();
        public void Undo() => UndoInternal();
        public void Redo() => RedoInternal();

        #endregion

        #region �����������\�b�h

        [RelayCommand]
        private void AddInternal(string itemType)
        {
            try
            {
                _logger.LogDebug("�A�C�e����ǉ����܂�: {ItemType}", itemType);
                var newItem = CreateItem(itemType);
                
                var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;
                
                // ������L�^
                var operation = new CommandListOperation
                {
                    Type = OperationType.Add,
                    Index = insertIndex,
                    Item = newItem.Clone(),
                    Description = $"�A�C�e���ǉ�: {itemType}"
                };

                _logger.LogDebug("�A�C�e���ǉ��O��Items��: {Count}", Items.Count);
                
                Items.Insert(insertIndex, newItem);
                
                _logger.LogDebug("�A�C�e���ǉ����Items��: {Count}", Items.Count);
                
                SelectedIndex = insertIndex;
                SelectedItem = newItem;
                
                RecordOperation(operation);
                StatusMessage = $"{itemType}��ǉ����܂���";
                
                // �v���p�e�B�ύX�ʒm�𖾎��I�ɔ���
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(SelectedIndex));
                
                // MainWindowViewModel�ɃA�C�e�����ύX��ʒm
                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(Items.Count));
                
                // MainWindowViewModel�ɑI��ύX��ʒm
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(newItem));
                
                _logger.LogInformation("�A�C�e����ǉ����܂���: {ItemType} (���v {Count}��)", itemType, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���ǉ����ɃG���[���������܂���");
                StatusMessage = $"�ǉ��G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        private void DeleteInternal()
        {
            if (SelectedItem == null)
            {
                _logger.LogDebug("�폜�Ώۂ̃A�C�e�����I������Ă��܂���");
                StatusMessage = "�폜�Ώۂ��I������Ă��܂���";
                return;
            }

            try
            {
                var index = Items.IndexOf(SelectedItem);
                var itemType = SelectedItem.ItemType;
                var itemClone = SelectedItem.Clone();
                
                // ������L�^
                var operation = new CommandListOperation
                {
                    Type = OperationType.Delete,
                    Index = index,
                    Item = itemClone,
                    Description = $"�A�C�e���폜: {itemType}"
                };

                Items.Remove(SelectedItem);

                if (Items.Count == 0)
                {
                    SelectedIndex = -1;
                    SelectedItem = null;
                }
                else if (index >= Items.Count)
                {
                    SelectedIndex = Items.Count - 1;
                    SelectedItem = Items.LastOrDefault();
                }
                else
                {
                    SelectedIndex = index;
                    SelectedItem = Items.ElementAtOrDefault(index);
                }
                
                RecordOperation(operation);
                
                // �폜��ɑI����Ԃ̕ύX��ʒm
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(SelectedItem));
                
                StatusMessage = $"{itemType}���폜���܂���";
                _logger.LogInformation("�A�C�e�����폜���܂���: {ItemType} (�c�� {Count}��)", itemType, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���폜���ɃG���[���������܂���");
                StatusMessage = $"�폜�G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        private void MoveUpInternal()
        {
            if (SelectedItem == null || SelectedIndex <= 0)
            {
                _logger.LogDebug("��ړ��ł��܂���");
                StatusMessage = "����ȏ��Ɉړ��ł��܂���";
                return;
            }

            try
            {
                var oldIndex = SelectedIndex;
                var newIndex = oldIndex - 1;
                
                // ������L�^
                var operation = new CommandListOperation
                {
                    Type = OperationType.Move,
                    Index = oldIndex,
                    NewIndex = newIndex,
                    Item = SelectedItem.Clone(),
                    Description = $"�A�C�e����ړ�: {SelectedItem.ItemType}"
                };

                Items.Move(oldIndex, newIndex);
                SelectedIndex = newIndex;
                
                RecordOperation(operation);
                StatusMessage = $"{SelectedItem.ItemType}����Ɉړ����܂���";
                _logger.LogDebug("�A�C�e������Ɉړ����܂���: {FromIndex} -> {ToIndex}", oldIndex, newIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e����ړ����ɃG���[���������܂���");
                StatusMessage = $"�ړ��G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        private void MoveDownInternal()
        {
            if (SelectedItem == null || SelectedIndex >= Items.Count - 1)
            {
                _logger.LogDebug("���ړ��ł��܂���");
                StatusMessage = "����ȏ㉺�Ɉړ��ł��܂���";
                return;
            }

            try
            {
                var oldIndex = SelectedIndex;
                var newIndex = oldIndex + 1;
                
                // ������L�^
                var operation = new CommandListOperation
                {
                    Type = OperationType.Move,
                    Index = oldIndex,
                    NewIndex = newIndex,
                    Item = SelectedItem.Clone(),
                    Description = $"�A�C�e�����ړ�: {SelectedItem.ItemType}"
                };

                Items.Move(oldIndex, newIndex);
                SelectedIndex = newIndex;
                
                RecordOperation(operation);
                StatusMessage = $"{SelectedItem.ItemType}�����Ɉړ����܂���";
                _logger.LogDebug("�A�C�e�������Ɉړ����܂���: {FromIndex} -> {ToIndex}", oldIndex, newIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e�����ړ����ɃG���[���������܂���");
                StatusMessage = $"�ړ��G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ClearInternal()
        {
            if (Items.Count == 0)
            {
                StatusMessage = "�N���A���鍀�ڂ�����܂���";
                return;
            }

            try
            {
                var count = Items.Count;
                var itemsClone = Items.Select(item => item.Clone()).ToList();
                
                // ������L�^
                var operation = new CommandListOperation
                {
                    Type = OperationType.Clear,
                    Items = itemsClone,
                    Description = $"�S�A�C�e���N���A ({count}��)"
                };

                Items.Clear();
                SelectedIndex = -1;
                SelectedItem = null;
                
                RecordOperation(operation);
                
                // �S�N���A���EditPanel��null��ʒm
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(null));
                
                StatusMessage = $"�S�A�C�e��({count}��)���N���A���܂���";
                _logger.LogInformation("�S�A�C�e�����N���A���܂���: {Count}��", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���N���A���ɃG���[���������܂���");
                StatusMessage = $"�N���A�G���[: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void UndoInternal()
        {
            if (!CanUndo) return;

            try
            {
                var operation = _undoStack.Pop();
                _redoStack.Push(operation);

                switch (operation.Type)
                {
                    case OperationType.Add:
                        Items.RemoveAt(operation.Index);
                        break;
                    case OperationType.Delete:
                        Items.Insert(operation.Index, operation.Item!);
                        break;
                    case OperationType.Move:
                        Items.Move(operation.NewIndex!.Value, operation.Index);
                        break;
                    case OperationType.Replace:
                        Items[operation.Index] = operation.Item!;
                        SelectedIndex = operation.Index;
                        SelectedItem = operation.Item;
                        break;
                    case OperationType.Clear:
                        foreach (var item in operation.Items!)
                        {
                            Items.Add(item);
                        }
                        break;
                }

                StatusMessage = $"���ɖ߂��܂���: {operation.Description}";
                _logger.LogDebug("Undo���s: {Description}", operation.Description);
                
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Undo���s���ɃG���[���������܂���");
                StatusMessage = $"Undo�G���[: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void RedoInternal()
        {
            if (!CanRedo) return;

            try
            {
                var operation = _redoStack.Pop();
                _undoStack.Push(operation);

                switch (operation.Type)
                {
                    case OperationType.Add:
                        Items.Insert(operation.Index, operation.Item!);
                        break;
                    case OperationType.Delete:
                        Items.RemoveAt(operation.Index);
                        break;
                    case OperationType.Move:
                        Items.Move(operation.Index, operation.NewIndex!.Value);
                        break;
                    case OperationType.Replace:
                        Items[operation.Index] = operation.NewItem!;
                        SelectedIndex = operation.Index;
                        SelectedItem = operation.NewItem;
                        break;
                    case OperationType.Clear:
                        Items.Clear();
                        break;
                }

                StatusMessage = $"��蒼���܂���: {operation.Description}";
                _logger.LogDebug("Redo���s: {Description}", operation.Description);
                
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redo���s���ɃG���[���������܂���");
                StatusMessage = $"Redo�G���[: {ex.Message}";
            }
        }

        #endregion

        #region �v���O���X�Ǘ�

        /// <summary>
        /// �}�N�����s�J�n���̃v���O���X������
        /// </summary>
        public void InitializeProgress()
        {
            try
            {
                TotalProgress = Items.Count(i => i.IsEnable);
                CurrentProgress = 0;
                ShowProgress = TotalProgress > 0;
                ProgressText = ShowProgress ? $"0 / {TotalProgress}" : "";
                CurrentExecutingItem = null;
                
                _logger.LogDebug("�v���O���X������: ����={TotalProgress}", TotalProgress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���O���X���������ɃG���[");
            }
        }

        /// <summary>
        /// �v���O���X�X�V
        /// </summary>
        public void UpdateProgress(int completed)
        {
            try
            {
                CurrentProgress = Math.Min(completed, TotalProgress);
                ProgressText = $"{CurrentProgress} / {TotalProgress}";
                
                if (CurrentProgress >= TotalProgress)
                {
                    ShowProgress = false;
                    ProgressText = "����";
                    CurrentExecutingItem = null;
                }
                
                _logger.LogTrace("�v���O���X�X�V: {Current}/{Total}", CurrentProgress, TotalProgress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���O���X�X�V���ɃG���[");
            }
        }

        /// <summary>
        /// �v���O���X��������
        /// </summary>
        public void CompleteProgress()
        {
            try
            {
                CurrentProgress = TotalProgress;
                ShowProgress = false;
                ProgressText = "����";
                CurrentExecutingItem = null;
                
                _logger.LogDebug("�v���O���X����");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���O���X�����������ɃG���[");
            }
        }

        /// <summary>
        /// �v���O���X���f����
        /// </summary>
        public void CancelProgress()
        {
            try
            {
                ShowProgress = false;
                ProgressText = "���f";
                CurrentExecutingItem = null;
                
                _logger.LogDebug("�v���O���X���f");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���O���X���f�������ɃG���[");
            }
        }

        #endregion

        #region �t�@�C������iDI�Ή��j

        /// <summary>
        /// ���b�Z�[�W����̃t�@�C���ǂݍ���
        /// </summary>
        private void LoadFileInternal(string filePath)
        {
            try
            {
                Load(filePath);
                // MainWindowViewModel�ɃA�C�e�����ύX��ʒm
                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(Items.Count));
                // ���O���b�Z�[�W�𑗐M
                WeakReferenceMessenger.Default.Send(new LogMessage("Info", $"�t�@�C���ǂݍ��݊���: {Path.GetFileName(filePath)} ({Items.Count}��)"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ǂݍ��ݒ��ɃG���[: {FilePath}", filePath);
                WeakReferenceMessenger.Default.Send(new LogMessage("Error", $"�t�@�C���ǂݍ��ݎ��s: {ex.Message}"));
            }
        }

        /// <summary>
        /// ���b�Z�[�W����̃t�@�C���ۑ�
        /// </summary>
        private void SaveFileInternal(string filePath)
        {
            try
            {
                Save(filePath);
                // ���O���b�Z�[�W�𑗐M
                WeakReferenceMessenger.Default.Send(new LogMessage("Info", $"�t�@�C���ۑ�����: {Path.GetFileName(filePath)} ({Items.Count}��)"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ۑ����ɃG���[: {FilePath}", filePath);
                WeakReferenceMessenger.Default.Send(new LogMessage("Error", $"�t�@�C���ۑ����s: {ex.Message}"));
            }
        }

        public void Load(string filePath)
        {
            try
            {
                StatusMessage = "�t�@�C���ǂݍ��ݒ�...";
                
                if (System.IO.File.Exists(filePath))
                {
                    // CommandList��DI����擾
                    var commandListService = _serviceProvider.GetService<CommandList>();
                    if (commandListService == null)
                    {
                        // �t�H�[���o�b�N�F���ڍ쐬
                        commandListService = new CommandList();
                        _logger.LogWarning("CommandList�T�[�r�X��������Ȃ����߁A���ڍ쐬���܂���");
                    }
                    
                    commandListService.Load(filePath);
                    
                    _logger.LogInformation("�t�@�C���ǂݍ��ݐ���: {Count}��", commandListService.Items.Count);
                    
                    // ObservableCollection��j�󂹂��ɓ��e���X�V
                    Items.Clear();
                    foreach (var item in commandListService.Items)
                    {
                        Items.Add(item);
                    }
                    
                    SelectedIndex = Items.Count > 0 ? 0 : -1;
                    SelectedItem = Items.FirstOrDefault();
                    
                    HasUnsavedChanges = false;
                    StatusMessage = $"�t�@�C����ǂݍ��݂܂���: {Path.GetFileName(filePath)} ({Items.Count}��)";
                    _logger.LogInformation("�t�@�C����ǂݍ��݂܂���: {FilePath} ({Count}��)", filePath, Items.Count);
                }
                else
                {
                    _logger.LogWarning("�t�@�C�������݂��܂���: {FilePath}", filePath);
                    Items.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ǂݍ��ݒ��ɃG���[���������܂���: {FilePath}", filePath);
                StatusMessage = $"�ǂݍ��݃G���[: {ex.Message}";
                throw;
            }
        }

        public void Save(string filePath)
        {
            try
            {
                StatusMessage = "�t�@�C���ۑ���...";
                
                // JsonSerializerOptions��DI����擾
                var jsonOptions = _serviceProvider.GetService<JsonSerializerOptions>();
                if (jsonOptions == null)
                {
                    // �t�H�[���o�b�N�F�f�t�H���g�ݒ�
                    jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    _logger.LogWarning("JsonSerializerOptions�T�[�r�X��������Ȃ����߁A�f�t�H���g�ݒ���g�p���܂���");
                }
                
                // �ۑ��p�̃f�[�^������
                var saveData = Items.Select(item => new Dictionary<string, object?>
                {
                    ["ItemType"] = item.ItemType,
                    ["LineNumber"] = item.LineNumber,
                    ["Comment"] = item.Comment,
                    ["IsEnable"] = item.IsEnable,
                    ["Description"] = item.Description
                }).ToList();
                
                var json = System.Text.Json.JsonSerializer.Serialize(saveData, jsonOptions);
                
                var directory = System.IO.Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                
                System.IO.File.WriteAllText(filePath, json);
                HasUnsavedChanges = false;
                
                StatusMessage = $"�t�@�C����ۑ����܂���: {Path.GetFileName(filePath)} ({Items.Count}��)";
                _logger.LogInformation("�t�@�C����ۑ����܂���: {FilePath} ({Count}��)", filePath, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ۑ����ɃG���[���������܂���: {FilePath}", filePath);
                StatusMessage = $"�ۑ��G���[: {ex.Message}";
                throw;
            }
        }

        #endregion

        #region �w���p�[���\�b�h�iDI�Ή��j

        private ICommandListItem CreateItem(string itemType)
        {
            try
            {
                _logger.LogDebug("CreateItem�J�n: {ItemType}", itemType);
                
                // Factory�p�^�[����CommandListItem���쐬
                var factory = _serviceProvider.GetService<AutoTool.Services.ICommandListItemFactory>();
                if (factory != null)
                {
                    var item = factory.CreateItem(itemType);
                    if (item != null)
                    {
                        item.LineNumber = Items.Count + 1;
                        if (string.IsNullOrEmpty(item.Comment))
                        {
                            item.Comment = $"�V����{itemType}�R�}���h";
                        }
                        if (string.IsNullOrEmpty(item.Description))
                        {
                            item.Description = $"{itemType}�R�}���h";
                        }
                        
                        _logger.LogDebug("Factory�ō쐬����: {ActualType}", item.GetType().Name);
                        return item;
                    }
                }
                
                _logger.LogWarning("Factory��������Ȃ����쐬���s�A�t�H�[���o�b�N���s: {ItemType}", itemType);
                
                // �t�H�[���o�b�N�FCommandRegistry�𒼐ڎg�p
                var itemTypes = CommandRegistry.GetTypeMapping();
                if (itemTypes.TryGetValue(itemType, out var type))
                {
                    _logger.LogDebug("CommandRegistry����^�C�v�擾: {Type}", type.Name);
                    
                    // DI�R���e�i����C���X�^���X���擾�����݂�
                    var serviceInstance = _serviceProvider.GetService(type);
                    if (serviceInstance is ICommandListItem item)
                    {
                        item.LineNumber = Items.Count + 1;
                        item.ItemType = itemType;
                        item.IsEnable = true;
                        if (string.IsNullOrEmpty(item.Comment))
                        {
                            item.Comment = $"�V����{itemType}�R�}���h";
                        }
                        if (string.IsNullOrEmpty(item.Description))
                        {
                            item.Description = $"{itemType}�R�}���h";
                        }
                        
                        _logger.LogDebug("DI�R���e�i�ō쐬����: {ActualType}", item.GetType().Name);
                        return item;
                    }
                    
                    // DI�Ŏ擾�ł��Ȃ��ꍇ��Activator�ō쐬
                    if (Activator.CreateInstance(type) is ICommandListItem fallbackItem)
                    {
                        fallbackItem.LineNumber = Items.Count + 1;
                        fallbackItem.ItemType = itemType;
                        fallbackItem.IsEnable = true;
                        if (string.IsNullOrEmpty(fallbackItem.Comment))
                        {
                            fallbackItem.Comment = $"�V����{itemType}�R�}���h";
                        }
                        if (string.IsNullOrEmpty(fallbackItem.Description))
                        {
                            fallbackItem.Description = $"{itemType}�R�}���h";
                        }
                        
                        _logger.LogDebug("Activator�ō쐬����: {ActualType}", fallbackItem.GetType().Name);
                        return fallbackItem;
                    }
                }

                _logger.LogWarning("CommandRegistry�ō쐬���s�ABasicCommandItem�ő��: {ItemType}", itemType);

                // �ŏI�t�H�[���o�b�N�FBasicCommandItem
                var basicItem = _serviceProvider.GetService<BasicCommandItem>();
                if (basicItem == null)
                {
                    basicItem = new BasicCommandItem();
                    _logger.LogWarning("BasicCommandItem��DI����擾�ł��Ȃ����߁A���ڍ쐬���܂���");
                }
                
                basicItem.ItemType = itemType;
                basicItem.LineNumber = Items.Count + 1;
                basicItem.Comment = $"�V����{itemType}�R�}���h";
                basicItem.Description = $"{itemType}�R�}���h";
                basicItem.IsEnable = true;
                
                _logger.LogDebug("BasicCommandItem�ō쐬����");
                return basicItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateItem���ɃG���[������: {ItemType}", itemType);
                
                // �ً}�t�H�[���o�b�N
                return new BasicCommandItem 
                { 
                    ItemType = itemType, 
                    LineNumber = Items.Count + 1,
                    Comment = $"�G���[����: {itemType}",
                    Description = "�G���[���畜�������A�C�e��",
                    IsEnable = true
                };
            }
        }

        private void UpdateLineNumbers()
        {
            try
            {
                if (_items.Count == 0)
                {
                    _logger.LogDebug("�A�C�e����0���̂��߁A�y�A�����O�������X�L�b�v");
                    return;
                }

                _logger.LogDebug("�s�ԍ��E�l�X�g���x���E�y�A�����O�X�V�J�n: {Count}��", _items.Count);

                // Step 1: �s�ԍ����X�V
                for (int i = 0; i < _items.Count; i++)
                {
                    _items[i].LineNumber = i + 1;
                }

                // Step 2: �l�X�g���x�����v�Z
                UpdateNestLevelInternal();

                // Step 3: Pair�v���p�e�B���N���A
                ClearAllPairs();

                // Step 4: �y�A�����O�����s
                UpdatePairingInternal();

                _logger.LogDebug("�s�ԍ��E�l�X�g���x���E�y�A�����O�X�V����");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�s�ԍ��X�V���ɃG���[���������܂���: {Message}", ex.Message);
                
                // �t�H�[���o�b�N�F�Œ���̍s�ԍ��X�V
                for (int i = 0; i < _items.Count; i++)
                {
                    _items[i].LineNumber = i + 1;
                }
            }
        }

        private void UpdateNestLevelInternal()
        {
            try
            {
                var nestLevel = 0;
                var loopStack = new Stack<ICommandListItem>(); // Loop�J�n�R�}���h�̃X�^�b�N
                var ifStack = new Stack<ICommandListItem>(); // If�J�n�R�}���h�̃X�^�b�N

                foreach (var item in _items)
                {
                    // �I���R�}���h�̏ꍇ�A�Ή�����J�n�R�}���h��T���ăl�X�g���x���𒲐�
                    if (item.ItemType == "Loop_End")
                    {
                        if (loopStack.Count > 0)
                        {
                            loopStack.Pop();
                            nestLevel = Math.Max(0, nestLevel - 1);
                        }
                    }
                    else if (item.ItemType == "IF_End")
                    {
                        if (ifStack.Count > 0)
                        {
                            ifStack.Pop();
                            nestLevel = Math.Max(0, nestLevel - 1);
                        }
                    }

                    // ���݂̃A�C�e���̃l�X�g���x����ݒ�
                    item.NestLevel = nestLevel;
                    
                    // �l�X�g���ɂ��邱�Ƃ������t���O��ݒ�
                    item.IsInLoop = loopStack.Count > 0;
                    item.IsInIf = ifStack.Count > 0;

                    // �J�n�R�}���h�̏ꍇ�A�X�^�b�N�ɐς�Ńl�X�g���x���𑝉�
                    if (item.ItemType == "Loop")
                    {
                        loopStack.Push(item);
                        nestLevel++;
                    }
                    else if (IsIfCommand(item.ItemType))
                    {
                        ifStack.Push(item);
                        nestLevel++;
                    }
                }

                // �c�����J�n�R�}���h������Όx��
                if (loopStack.Count > 0)
                {
                    _logger.LogWarning("�Ή�����Loop_End��������Ȃ�Loop�R�}���h��{Count}����܂�", loopStack.Count);
                }
                if (ifStack.Count > 0)
                {
                    _logger.LogWarning("�Ή�����IF_End��������Ȃ�If�R�}���h��{Count}����܂�", ifStack.Count);
                }

                _logger.LogDebug("���ǔŃl�X�g���x���v�Z����: �ő�l�X�g{MaxLevel}", nestLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�l�X�g���x���v�Z���ɃG���[: {Message}", ex.Message);
            }
        }

        private void ClearAllPairs()
        {
            try
            {
                foreach (var item in _items)
                {
                    SetPairProperty(item, null);
                }
                _logger.LogDebug("�S�y�A�v���p�e�B�N���A����");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�y�A�v���p�e�B�N���A���ɃG���[: {Message}", ex.Message);
            }
        }

        private void UpdatePairingInternal()
        {
            try
            {
                // Loop�y�A�����O
                var loopItems = _items.Where(x => x.ItemType == "Loop").ToList();
                var loopEndItems = _items.Where(x => x.ItemType == "Loop_End").ToList();

                foreach (var loopItem in loopItems)
                {
                    var correspondingEnd = loopEndItems
                        .Where(end => end.LineNumber > loopItem.LineNumber)
                        .Where(end => end.NestLevel == loopItem.NestLevel)
                        .OrderBy(end => end.LineNumber)
                        .FirstOrDefault();

                    if (correspondingEnd != null)
                    {
                        SetPairProperty(loopItem, correspondingEnd);
                        SetPairProperty(correspondingEnd, loopItem);
                        
                        // LoopCount������
                        var loopCount = GetPropertyValue<int>(loopItem, "LoopCount");
                        if (loopCount > 0)
                        {
                            SetPropertyValue(correspondingEnd, "LoopCount", loopCount);
                        }
                        
                        _logger.LogDebug("���[�v�y�A�����O����: Loop({LoopLine}) <-> Loop_End({EndLine})", 
                            loopItem.LineNumber, correspondingEnd.LineNumber);
                    }
                    else
                    {
                        _logger.LogWarning("Loop (�s {LineNumber}) �ɑΉ�����Loop_End��������܂���", loopItem.LineNumber);
                    }
                }

                // If�y�A�����O
                var ifItems = _items.Where(x => IsIfCommand(x.ItemType)).ToList();
                var ifEndItems = _items.Where(x => x.ItemType == "IF_End").ToList();

                foreach (var ifItem in ifItems)
                {
                    var correspondingEnd = ifEndItems
                        .Where(end => end.LineNumber > ifItem.LineNumber)
                        .Where(end => end.NestLevel == ifItem.NestLevel)
                        .OrderBy(end => end.LineNumber)
                        .FirstOrDefault();

                    if (correspondingEnd != null)
                    {
                        SetPairProperty(ifItem, correspondingEnd);
                        SetPairProperty(correspondingEnd, ifItem);
                        
                        _logger.LogDebug("If�y�A�����O����: {IfType}({IfLine}) <-> IF_End({EndLine})", 
                            ifItem.ItemType, ifItem.LineNumber, correspondingEnd.LineNumber);
                    }
                    else
                    {
                        _logger.LogWarning("{IfType} (�s {LineNumber}) �ɑΉ�����IF_End��������܂���", 
                            ifItem.ItemType, ifItem.LineNumber);
                    }
                }

                _logger.LogDebug("�y�A�����O��������");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�y�A�����O�������ɃG���[: {Message}", ex.Message);
            }
        }

        private void SetPairProperty(ICommandListItem item, ICommandListItem? pair)
        {
            try
            {
                var pairProperty = item.GetType().GetProperty("Pair");
                if (pairProperty != null && pairProperty.CanWrite)
                {
                    pairProperty.SetValue(item, pair);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�y�A�v���p�e�B�ݒ�Ɏ��s: {ItemType}", item.ItemType);
            }
        }

        private T GetPropertyValue<T>(ICommandListItem item, string propertyName)
        {
            try
            {
                var property = item.GetType().GetProperty(propertyName);
                if (property != null && property.CanRead)
                {
                    var value = property.GetValue(item);
                    if (value is T tValue)
                        return tValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�v���p�e�B�l�擾�Ɏ��s: {PropertyName} on {ItemType}", propertyName, item.ItemType);
            }
            return default(T)!;
        }

        private void SetPropertyValue(ICommandListItem item, string propertyName, object? value)
        {
            try
            {
                var property = item.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(item, value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�v���p�e�B�l�ݒ�Ɏ��s: {PropertyName} on {ItemType}", propertyName, item.ItemType);
            }
        }

        private bool IsStartCommand(string itemType) => itemType switch
        {
            "Loop" => true,
            "IF_ImageExist" => true,
            "IF_ImageNotExist" => true,
            "IF_ImageExist_AI" => true,
            "IF_ImageNotExist_AI" => true,
            "IF_Variable" => true,
            _ => false
        };

        private bool IsEndCommand(string itemType) => itemType switch
        {
            "Loop_End" => true,
            "IF_End" => true,
            _ => false
        };

        private bool IsIfCommand(string itemType) => itemType switch
        {
            "IF_ImageExist" => true,
            "IF_ImageNotExist" => true,
            "IF_ImageExist_AI" => true,
            "IF_ImageNotExist_AI" => true,
            "IF_Variable" => true,
            _ => false
        };

        private void RecordOperation(CommandListOperation operation)
        {
            _undoStack.Push(operation);
            _redoStack.Clear();
            
            const int maxUndoSteps = 100;
            while (_undoStack.Count > maxUndoSteps)
            {
                _undoStack.TryPop(out _);
            }
            
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        #endregion

        #region �R�}���h���s��ԊǗ�

        private void OnCommandStarted(StartCommandMessage message)
        {
            try
            {
                // ���_��Ȍ������W�b�N
                var item = FindMatchingItem(message.LineNumber, message.ItemType);
                
                if (item != null)
                {
                    item.IsRunning = true;
                    item.Progress = 0;
                    CurrentExecutingItem = item;
                    
                    _logger.LogDebug("�R�}���h�J�n: Line {LineNumber} ({MessageLineNumber}) - {ItemType} ({MessageItemType})", 
                        item.LineNumber, message.LineNumber, item.ItemType, message.ItemType);
                }
                else
                {
                    _logger.LogWarning("�R�}���h�J�n: �Ή�����A�C�e����������܂��� - Line {MessageLineNumber}, Type {MessageItemType}", 
                        message.LineNumber, message.ItemType);
                    
                    // �f�o�b�O�����o��
                    _logger.LogDebug("���݂̃A�C�e���ꗗ:");
                    foreach (var debugItem in Items.Take(10))
                    {
                        _logger.LogDebug("  Line {LineNumber}: {ItemType} (IsEnable: {IsEnable})", 
                            debugItem.LineNumber, debugItem.ItemType, debugItem.IsEnable);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h�J�n�������ɃG���[���������܂���");
            }
        }

        private void OnCommandFinished(FinishCommandMessage message)
        {
            try
            {
                var item = FindMatchingItem(message.LineNumber, message.ItemType);
                
                if (item != null)
                {
                    item.IsRunning = false;
                    item.Progress = 100;
                    
                    if (item.IsEnable)
                    {
                        _completedCommands++;
                        UpdateProgress(_completedCommands);
                    }
                    
                    _logger.LogDebug("�R�}���h����: Line {LineNumber} ({MessageLineNumber}) - {ItemType} ({MessageItemType})", 
                        item.LineNumber, message.LineNumber, item.ItemType, message.ItemType);
                    
                    // CurrentExecutingItem���N���A
                    if (CurrentExecutingItem == item)
                    {
                        CurrentExecutingItem = null;
                    }
                    
                    Task.Delay(1000).ContinueWith(_ => 
                    {
                        if (!item.IsRunning)
                        {
                            item.Progress = 0;
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("�R�}���h����: �Ή�����A�C�e����������܂��� - Line {MessageLineNumber}, Type {MessageItemType}", 
                        message.LineNumber, message.ItemType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h�����������ɃG���[���������܂���");
            }
        }

        private void OnProgressUpdated(UpdateProgressMessage message)
        {
            try
            {
                var item = FindMatchingItem(message.LineNumber, message.ItemType);
                
                if (item != null && item.IsRunning)
                {
                    item.Progress = message.Progress;
                    
                    _logger.LogTrace("�i���X�V: Line {LineNumber} ({MessageLineNumber}) - {Progress}% - {ItemType} ({MessageItemType})", 
                        item.LineNumber, message.LineNumber, message.Progress, item.ItemType, message.ItemType);
                }
                else if (item == null)
                {
                    _logger.LogTrace("�i���X�V: �Ή�����A�C�e����������܂��� - Line {MessageLineNumber}, Type {MessageItemType}", 
                        message.LineNumber, message.ItemType);
                }
                else if (!item.IsRunning)
                {
                    _logger.LogTrace("�i���X�V: �A�C�e�������s���ł͂���܂��� - Line {LineNumber}, Type {ItemType}, IsRunning: {IsRunning}", 
                        item.LineNumber, item.ItemType, item.IsRunning);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�i���X�V�������ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// ���b�Z�[�W�ɑΉ�����A�C�e��������
        /// </summary>
        private ICommandListItem? FindMatchingItem(int messageLineNumber, string messageItemType)
        {
            // 1. ���m�Ȉ�v��D��
            var exactMatch = Items.FirstOrDefault(x => 
                x.LineNumber == messageLineNumber && x.ItemType == messageItemType);
            if (exactMatch != null)
            {
                return exactMatch;
            }

            // 2. LineNumber����v�������
            var lineMatch = Items.FirstOrDefault(x => x.LineNumber == messageLineNumber);
            if (lineMatch != null)
            {
                return lineMatch;
            }

            // 3. ItemType����v���ALineNumber���߂����́i�}2�͈̔́j
            var typeAndNearLineMatch = Items.FirstOrDefault(x => 
                x.ItemType == messageItemType && 
                Math.Abs(x.LineNumber - messageLineNumber) <= 2);
            if (typeAndNearLineMatch != null)
            {
                return typeAndNearLineMatch;
            }

            // 4. ���s���̃A�C�e��������΂����D��
            var runningItem = Items.FirstOrDefault(x => x.IsRunning);
            if (runningItem != null && runningItem.ItemType == messageItemType)
            {
                return runningItem;
            }

            return null;
        }

        #endregion

        #region �s�����Ă������\�b�h�̎���

        private void ChangeItemType(ICommandListItem oldItem, ICommandListItem newItem)
        {
            try
            {
                var index = Items.IndexOf(oldItem);
                if (index >= 0)
                {
                    var operation = new CommandListOperation
                    {
                        Type = OperationType.Replace,
                        Index = index,
                        Item = oldItem.Clone(),
                        NewItem = newItem.Clone(),
                        Description = $"�^�C�v�ύX: {oldItem.ItemType} -> {newItem.ItemType}"
                    };

                    Items[index] = newItem;
                    SelectedIndex = index;
                    SelectedItem = newItem;

                    UpdateLineNumbers();
                    RecordOperation(operation);
                    StatusMessage = $"�^�C�v��ύX���܂���: {oldItem.ItemType} -> {newItem.ItemType}";
                    
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(newItem));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���^�C�v�ύX���ɃG���[���������܂���");
                StatusMessage = $"�^�C�v�ύX�G���[: {ex.Message}";
            }
        }

        private void RefreshList()
        {
            try
            {
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(SelectedIndex));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));

                StatusMessage = "���X�g���X�V���܂���";
                _logger.LogDebug("���X�g�r���[�������X�V���܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���X�g�X�V���ɃG���[���������܂���");
                StatusMessage = $"�X�V�G���[: {ex.Message}";
            }
        }

        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            StatusMessage = isRunning ? "���s��..." : "��������";
            _logger.LogDebug("���s��Ԃ�ݒ�: {IsRunning}", isRunning);
            
            if (!isRunning)
            {
                if (ShowProgress)
                {
                    CompleteProgress();
                }
                
                foreach (var item in Items)
                {
                    item.IsRunning = false;
                    item.Progress = 0;
                }
                CurrentExecutingItem = null;
            }
        }

        public void TestExecutionStateDisplay()
        {
            try
            {
                _logger.LogInformation("=== ���s��ԕ\���e�X�g�J�n ===");
                
                if (Items.Count == 0)
                {
                    _logger.LogWarning("�e�X�g�Ώۂ̃A�C�e��������܂���");
                    return;
                }

                var testItem = Items.First();
                _logger.LogInformation("�e�X�g�ΏۃA�C�e��: {ItemType} (�s{LineNumber})", testItem.ItemType, testItem.LineNumber);

                testItem.IsRunning = true;
                testItem.Progress = 50;
                CurrentExecutingItem = testItem;

                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(CurrentExecutingItem));
                OnPropertyChanged(nameof(CurrentExecutingDescription));
                
                _logger.LogInformation("=== ���s��ԕ\���e�X�g���� ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���s��ԕ\���e�X�g���ɃG���[");
            }
        }

        public void DebugItemStates()
        {
            try
            {
                _logger.LogInformation("=== �A�C�e����Ԉꗗ ===");
                _logger.LogInformation("���A�C�e����: {Count}", Items.Count);
                _logger.LogInformation("CurrentExecutingItem: {Item}", CurrentExecutingItem?.ItemType ?? "null");

                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    _logger.LogInformation("  [{Index}] {ItemType} (�s{LineNumber}) - IsRunning:{IsRunning}, Progress:{Progress}%", 
                        i, item.ItemType, item.LineNumber, item.IsRunning, item.Progress);
                }
                _logger.LogInformation("=== �A�C�e����Ԉꗗ���� ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e����ԕ\�����ɃG���[");
            }
        }

        #endregion
    }

    #region �⏕�N���X

    public class CommandListOperation
    {
        public OperationType Type { get; set; }
        public int Index { get; set; }
        public int? NewIndex { get; set; }
        public ICommandListItem? Item { get; set; }
        public ICommandListItem? NewItem { get; set; }
        public List<ICommandListItem>? Items { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public enum OperationType
    {
        Add,
        Delete,
        Move,
        Clear,
        Edit,
        Replace
    }

    public class CommandListStats
    {
        public int TotalItems { get; set; }
        public int EnabledItems { get; set; }
        public int DisabledItems { get; set; }
        public Dictionary<string, int> ItemTypeStats { get; set; } = new();
    }

    #endregion
}