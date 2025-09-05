using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using AutoTool.Message;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;
using AutoTool.Model.List.Class;
using AutoTool.Model.CommandDefinition;
using AutoTool.Command.Class;
using AutoTool.Command.Interface;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using AutoTool.List.Class;
using System.Windows;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// DI�Ή��ŁFListPanelViewModel�iCommandListService�����j
    /// </summary>
    public partial class ListPanelViewModel : ObservableObject
    {
        private readonly ILogger<ListPanelViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandListService _commandListService;
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

                OnPropertyChanged(nameof(CurrentExecutingDescription));
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

        public double ProgressPercentage => TotalProgress > 0 ? (double)CurrentProgress / TotalProgress * 100 : 0;

        public string EstimatedTimeRemaining
        {
            get
            {
                if (TotalProgress <= 0 || CurrentProgress <= 0) return "�s��";

                var remaining = TotalProgress - CurrentProgress;
                if (remaining <= 0) return "����";

                var estimatedSeconds = remaining * 2;
                return $"��{estimatedSeconds}�b";
            }
        }

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

        public ListPanelViewModel(
            ILogger<ListPanelViewModel> logger,
            IServiceProvider serviceProvider,
            CommandListService commandListService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _commandListService = commandListService ?? throw new ArgumentNullException(nameof(commandListService));

            SetupMessaging();
            _logger.LogInformation("DI�Ή�ListPanelViewModel �����������Ă��܂��iCommandListService�����Łj");

            _items.CollectionChanged += (s, e) =>
            {
                TotalItems = _items.Count;
                HasUnsavedChanges = true;

                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));

                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(_items.Count));

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
            WeakReferenceMessenger.Default.Register<AddUniversalItemMessage>(this, (r, m) => AddUniversalItem(m.Item));
            WeakReferenceMessenger.Default.Register<DeleteMessage>(this, (r, m) => DeleteInternal());
            WeakReferenceMessenger.Default.Register<UpMessage>(this, (r, m) => MoveUpInternal());
            WeakReferenceMessenger.Default.Register<DownMessage>(this, (r, m) => MoveDownInternal());
            WeakReferenceMessenger.Default.Register<ClearMessage>(this, (r, m) => ClearInternal());
            WeakReferenceMessenger.Default.Register<UndoMessage>(this, (r, m) => UndoInternal());
            WeakReferenceMessenger.Default.Register<RedoMessage>(this, (r, m) => RedoInternal());

            // �R�}���h���s��ԃ��b�Z�[�W�̏���
            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (r, m) => OnCommandStarted(m));
            WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, (r, m) => OnCommandFinished(m));
            WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, (r, m) => OnProgressUpdated(m));
            WeakReferenceMessenger.Default.Register<DoingCommandMessage>(this, (r, m) => OnCommandDoing(m));

            // �t�@�C�����상�b�Z�[�W�̏����i�����̃��b�Z�[�W�^�C�v�ɑΉ��j
            WeakReferenceMessenger.Default.Register<LoadMessage>(this, (r, m) => LoadFileInternal(m.FilePath ?? string.Empty));
            WeakReferenceMessenger.Default.Register<SaveMessage>(this, (r, m) => SaveFileInternal(m.FilePath ?? string.Empty));
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

        public void AddUniversalItem(UniversalCommandItem universalItem)
        {
            try
            {
                _logger.LogDebug("���IUniversalCommandItem��ǉ����܂�: {ItemType}", universalItem.ItemType);

                // UniversalCommandItem����ICommandListItem�ɕϊ�
                var newItem = ConvertUniversalItemToCommandListItem(universalItem);

                var insertIndex = SelectedIndex >= 0 && SelectedIndex < Items.Count ? SelectedIndex + 1 : Items.Count;

                var operation = new CommandListOperation
                {
                    Type = OperationType.Add,
                    Index = insertIndex,
                    Item = newItem.Clone(),
                    Description = $"���I�A�C�e���ǉ�: {universalItem.ItemType}"
                };

                Items.Insert(insertIndex, newItem);

                SelectedIndex = insertIndex;
                SelectedItem = newItem;

                RecordOperation(operation);
                StatusMessage = $"���I{universalItem.ItemType}��ǉ����܂���";

                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(SelectedIndex));

                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(Items.Count));
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(newItem));

                _logger.LogInformation("���I�A�C�e����ǉ����܂���: {ItemType} (���v {Count}��)", universalItem.ItemType, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���I�A�C�e���ǉ����ɃG���[���������܂���");
                StatusMessage = $"���I�ǉ��G���[: {ex.Message}";
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

                var operation = new CommandListOperation
                {
                    Type = OperationType.Add,
                    Index = insertIndex,
                    Item = newItem.Clone(),
                    Description = $"�A�C�e���ǉ�: {itemType}"
                };

                Items.Insert(insertIndex, newItem);

                SelectedIndex = insertIndex;
                SelectedItem = newItem;

                RecordOperation(operation);
                StatusMessage = $"{itemType}��ǉ����܂���";

                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(SelectedIndex));

                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(Items.Count));
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

        #region �t�@�C������

        private void LoadFileInternal(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogWarning("�t�@�C���ǂݍ���: �p�X����ł�");
                    return;
                }

                _logger.LogInformation("���b�Z�[�W����̃t�@�C���ǂݍ��݊J�n: {FilePath}", filePath);
                Load(filePath);
                WeakReferenceMessenger.Default.Send(new ItemCountChangedMessage(Items.Count));
                WeakReferenceMessenger.Default.Send(new LogMessage("Info", $"�t�@�C���ǂݍ��݊���: {Path.GetFileName(filePath)} ({Items.Count}��)"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ǂݍ��ݒ��ɃG���[: {FilePath}", filePath);
                WeakReferenceMessenger.Default.Send(new LogMessage("Error", $"�t�@�C���ǂݍ��ݎ��s: {ex.Message}"));
            }
        }

        private void SaveFileInternal(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogWarning("�t�@�C���ۑ�: �p�X����ł�");
                    return;
                }

                _logger.LogInformation("���b�Z�[�W����̃t�@�C���ۑ��J�n: {FilePath}", filePath);
                Save(filePath);
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
                _logger.LogInformation("=== ListPanelViewModel.Load�J�n ===");
                _logger.LogInformation("�Ώۃt�@�C��: {FilePath}", filePath);

                if (System.IO.File.Exists(filePath))
                {
                    _logger.LogInformation("�t�@�C�����݊m�FOK�ACommandListService���g�p���ăt�@�C���ǂݍ��݊J�n");

                    BaseCommand.SetMacroFileBasePath(filePath);

                    _commandListService.Load(filePath);
                    _logger.LogInformation("CommandListService����̓ǂݍ��ݐ���: {Count}��", _commandListService.Items.Count);

                    Items.Clear();

                    foreach (var item in _commandListService.Items)
                    {
                        Items.Add(item);
                    }

                    SelectedIndex = Items.Count > 0 ? 0 : -1;
                    SelectedItem = Items.FirstOrDefault();

                    HasUnsavedChanges = false;
                    StatusMessage = $"�t�@�C����ǂݍ��݂܂���: {Path.GetFileName(filePath)} ({Items.Count}��)";
                    _logger.LogInformation("=== ListPanelViewModel.Load����: {FilePath} ({Count}��) ===", filePath, Items.Count);
                }
                else
                {
                    _logger.LogWarning("�t�@�C�������݂��܂���: {FilePath}", filePath);
                    Items.Clear();
                    BaseCommand.SetMacroFileBasePath(null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ListPanelViewModel.Load���ɃG���[���������܂���: {FilePath}", filePath);
                StatusMessage = $"�ǂݍ��݃G���[: {ex.Message}";
                BaseCommand.SetMacroFileBasePath(null);
                throw;
            }
        }

        public void Save(string filePath)
        {
            try
            {
                StatusMessage = "�t�@�C���ۑ���...";

                _logger.LogInformation("CommandListService���g�p���ăt�@�C���ۑ��J�n: {FilePath}", filePath);

                _commandListService.Items.Clear();
                foreach (var item in Items)
                {
                    _commandListService.Items.Add(item);
                }

                _commandListService.Save(filePath);
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

        #region �w���p�[���\�b�h

        private ICommandListItem CreateItem(string itemType)
        {
            try
            {
                _logger.LogDebug("�V�����A�C�e���쐬�J�n: {TypeName}", itemType);

                // CommandRegistry���g�p���ăA�C�e�����쐬
                var newItem = CommandRegistry.CreateCommandItem(itemType);
                if (newItem != null)
                {
                    newItem.LineNumber = GetNextLineNumber();
                    newItem.Comment = $"{CommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}�̐���";

                    _logger.LogInformation("CommandRegistry�ŃR�}���h�A�C�e�����쐬: {ItemType}", itemType);
                    return newItem;
                }

                // �Ō�̎�i�FActivator�Œ��ڍ쐬�����݂�
                var typeFullName = $"AutoTool.Model.List.Class.{itemType}Item";
                var targetType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == typeFullName);

                if (targetType != null)
                {
                    if (Activator.CreateInstance(targetType) is ICommandListItem fallbackItem)
                    {
                        fallbackItem.LineNumber = Items.Count + 1;
                        fallbackItem.Comment = $"{CommandRegistry.DisplayOrder.GetDisplayName(itemType) ?? itemType}�̐���";

                        _logger.LogInformation("Activator�ŃR�}���h�A�C�e�����쐬: {ItemType}", itemType);
                        return fallbackItem;
                    }
                }

                _logger.LogWarning("�R�}���h�A�C�e���̍쐬�Ɏ��s: {TypeName}", itemType);

                // �t�H�[���o�b�N: ��{�I��CommandListItem
                return new CommandListItem
                {
                    ItemType = itemType,
                    LineNumber = GetNextLineNumber(),
                    IsEnable = true,
                    Comment = $"{itemType}�R�}���h"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateItem���ɃG���[������: {ItemType}", itemType);

                return new CommandListItem
                {
                    ItemType = itemType,
                    LineNumber = Items.Count + 1,
                    IsEnable = true,
                    Comment = $"{itemType}�R�}���h�i�G���[�����j"
                };
            }
        }

        private void UpdateLineNumbers()
        {
            try
            {
                if (_items.Count == 0) return;

                for (int i = 0; i < _items.Count; i++)
                {
                    _items[i].LineNumber = i + 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�s�ԍ��X�V���ɃG���[���������܂���: {Message}", ex.Message);
            }
        }

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

        /// <summary>
        /// UniversalCommandItem��ICommandListItem�ɕϊ�
        /// </summary>
        private ICommandListItem ConvertUniversalItemToCommandListItem(UniversalCommandItem universalItem)
        {
            try
            {
                // UniversalCommandItem��ICommandListItem���������Ă��邩�`�F�b�N
                if (universalItem is ICommandListItem commandListItem)
                {
                    commandListItem.LineNumber = Items.Count + 1;
                    _logger.LogDebug("UniversalCommandItem �� ICommandListItem �Ƃ��Ďg�p: {ItemType}", universalItem.ItemType);
                    return commandListItem;
                }

                // UniversalCommandItemWrapper ���쐬
                var wrapper = new UniversalCommandItemWrapper(universalItem)
                {
                    LineNumber = Items.Count + 1
                };

                _logger.LogDebug("UniversalCommandItemWrapper ���쐬: {ItemType}", universalItem.ItemType);
                return wrapper;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UniversalCommandItem�ϊ����ɃG���[: {ItemType}", universalItem.ItemType);

                // �t�H�[���o�b�N: BasicCommandItem
                return new BasicCommandItem
                {
                    ItemType = universalItem.ItemType,
                    LineNumber = Items.Count + 1,
                    IsEnable = universalItem.IsEnable,
                    Comment = $"���I{universalItem.ItemType}�R�}���h"
                };
            }
        }

        #endregion

        #region �R�}���h���s��ԊǗ�

        // �Ō�Ɏ��s���ꂽ�A�C�e����ǐ�
        private ICommandListItem? _lastExecutedItem;
        private readonly Dictionary<string, int> _executionCounter = new();

        private void OnCommandStarted(StartCommandMessage message)
        {
            try
            {
                _logger.LogDebug("=== OnCommandStarted�J�n ===");
                _logger.LogDebug("���b�Z�[�W��M: Line={LineNumber}, Type={ItemType}", message.LineNumber, message.ItemType);

                // ���s�J�E���^�[���X�V
                var key = $"{message.ItemType}";
                _executionCounter[key] = _executionCounter.GetValueOrDefault(key, 0) + 1;
                _logger.LogDebug("���s�J�E���^�[�X�V: {ItemType} = {Count}", message.ItemType, _executionCounter[key]);

                // ���_��Ȍ������W�b�N
                var item = FindMatchingItem(message.LineNumber, message.ItemType);

                if (item != null)
                {
                    _logger.LogDebug("�A�C�e������: Line={LineNumber}, Type={ItemType}, IsEnable={IsEnable}, IsRunning={IsRunning}",
                        item.LineNumber, item.ItemType, item.IsEnable, item.IsRunning);

                    item.IsRunning = true;
                    item.Progress = 0;
                    CurrentExecutingItem = item;
                    _lastExecutedItem = item;

                    _logger.LogDebug("�R�}���h�J�n: Line {LineNumber} ({MessageLineNumber}) - {ItemType} ({MessageItemType})",
                        item.LineNumber, message.LineNumber, item.ItemType, message.ItemType);

                    // UI�ɕύX��ʒm
                    OnPropertyChanged(nameof(Items));
                    OnPropertyChanged(nameof(CurrentExecutingItem));
                    OnPropertyChanged(nameof(CurrentExecutingDescription));

                    _logger.LogDebug("UI�X�V����: CurrentExecutingDescription={Description}", CurrentExecutingDescription);
                }
                else
                {
                    _logger.LogWarning("�R�}���h�J�n: �Ή�����A�C�e����������܂��� - Line {MessageLineNumber}, Type {MessageItemType}",
                        message.LineNumber, message.ItemType);

                    // �ڍׂȃf�o�b�O�����o��
                    _logger.LogDebug("���݂̃A�C�e���ꗗ (����: {Count}):", Items.Count);
                    _logger.LogDebug("���s�J�E���^�[��:");
                    foreach (var kvp in _executionCounter)
                    {
                        _logger.LogDebug("  {ItemType}: {Count}��", kvp.Key, kvp.Value);
                    }

                    foreach (var debugItem in Items.Take(15))
                    {
                        _logger.LogDebug("  Line {LineNumber}: {ItemType} (IsEnable: {IsEnable}, IsRunning: {IsRunning}, Progress: {Progress})",
                            debugItem.LineNumber, debugItem.ItemType, debugItem.IsEnable, debugItem.IsRunning, debugItem.Progress);
                    }
                }

                _logger.LogDebug("=== OnCommandStarted�I�� ===");
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
                _logger.LogDebug("=== OnCommandFinished�J�n ===");
                _logger.LogDebug("���b�Z�[�W��M: Line={LineNumber}, Type={ItemType}", message.LineNumber, message.ItemType);

                var item = FindMatchingItem(message.LineNumber, message.ItemType);

                if (item != null)
                {
                    _logger.LogDebug("�A�C�e������: Line={LineNumber}, Type={ItemType}, IsRunning={IsRunning}",
                        item.LineNumber, item.ItemType, item.IsRunning);

                    item.IsRunning = false;
                    item.Progress = 100;

                    if (item.IsEnable)
                    {
                        _completedCommands++;
                        UpdateProgress(_completedCommands);
                        _logger.LogDebug("�i���X�V: �����R�}���h��={CompletedCommands}", _completedCommands);
                    }

                    _logger.LogDebug("�R�}���h����: Line {LineNumber} ({MessageLineNumber}) - {ItemType} ({MessageItemType})",
                        item.LineNumber, message.LineNumber, item.ItemType, message.ItemType);

                    // CurrentExecutingItem���N���A
                    if (CurrentExecutingItem == item)
                    {
                        CurrentExecutingItem = null;
                        _logger.LogDebug("CurrentExecutingItem���N���A");
                    }

                    // UI�ɕύX��ʒm
                    OnPropertyChanged(nameof(Items));
                    OnPropertyChanged(nameof(CurrentExecutingItem));
                    OnPropertyChanged(nameof(CurrentExecutingDescription));

                    _logger.LogDebug("UI�X�V����: CurrentExecutingDescription={Description}", CurrentExecutingDescription);

                    // ��莞�Ԍ�Ƀv���O���X�����Z�b�g
                    Task.Delay(1000).ContinueWith(_ =>
                    {
                        if (!item.IsRunning)
                        {
                            item.Progress = 0;
                            OnPropertyChanged(nameof(Items));
                            _logger.LogTrace("�v���O���X���Z�b�g����: Line={LineNumber}", item.LineNumber);
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("�R�}���h����: �Ή�����A�C�e����������܂��� - Line {MessageLineNumber}, Type {MessageItemType}",
                        message.LineNumber, message.ItemType);
                }

                _logger.LogDebug("=== OnCommandFinished�I�� ===");
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
                        item.LineNumber, message.LineNumber, item.Progress, item.ItemType, message.ItemType);

                    // UI�ɕύX��ʒm
                    OnPropertyChanged(nameof(Items));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�i���X�V�������ɃG���[���������܂���");
            }
        }

        private void OnCommandDoing(DoingCommandMessage message)
        {
            try
            {
                _logger.LogDebug("=== OnCommandDoing�J�n ===");
                _logger.LogDebug("���b�Z�[�W��M: Line={LineNumber}, Type={ItemType}, Detail={Detail}",
                    message.LineNumber, message.ItemType, message.Detail);

                var item = FindMatchingItem(message.LineNumber, message.ItemType);

                if (item != null)
                {
                    _logger.LogDebug("�A�C�e������: Line={LineNumber}, Type={ItemType}, IsRunning={IsRunning}",
                        item.LineNumber, item.ItemType, item.IsRunning);

                    // �R�}���h�����s���ł��邱�Ƃ��m�F
                    if (!item.IsRunning)
                    {
                        _logger.LogDebug("���s��ԂłȂ��A�C�e�������s���ɐݒ�: Line={LineNumber}, Type={ItemType}",
                            item.LineNumber, item.ItemType);

                        item.IsRunning = true;
                        item.Progress = 0;
                        CurrentExecutingItem = item;

                        _logger.LogDebug("DoingMessage��M���ɃR�}���h���s��Ԃ�ݒ�: Line {LineNumber} - {ItemType}",
                            item.LineNumber, item.ItemType);
                    }

                    // UI�ɕύX��ʒm
                    OnPropertyChanged(nameof(Items));
                    OnPropertyChanged(nameof(CurrentExecutingItem));
                    OnPropertyChanged(nameof(CurrentExecutingDescription));

                    _logger.LogDebug("UI�X�V����: CurrentExecutingDescription={Description}", CurrentExecutingDescription);

                    _logger.LogTrace("�R�}���h���s��: Line {LineNumber} ({MessageLineNumber}) - {ItemType} ({MessageItemType}) - {Detail}",
                        item.LineNumber, message.LineNumber, item.ItemType, message.ItemType, message.Detail);
                }
                else
                {
                    _logger.LogWarning("DoingMessage: �Ή�����A�C�e����������܂��� - Line {MessageLineNumber}, Type {MessageItemType}",
                        message.LineNumber, message.ItemType);

                    // �ڍׂȃf�o�b�O�����o��
                    _logger.LogDebug("DoingMessage - ���݂̃A�C�e���ꗗ (����: {Count}):", Items.Count);
                    foreach (var debugItem in Items.Take(10))
                    {
                        _logger.LogDebug("  Line {LineNumber}: {ItemType} (IsEnable: {IsEnable}, IsRunning: {IsRunning})",
                            debugItem.LineNumber, debugItem.ItemType, debugItem.IsEnable, debugItem.IsRunning);
                    }
                }

                _logger.LogDebug("=== OnCommandDoing�I�� ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DoingMessage�������ɃG���[���������܂���");
            }
        }

        private ICommandListItem? FindMatchingItem(int messageLineNumber, string messageItemType)
        {
            try
            {
                var cleanMessageType = CleanItemType(messageItemType);

                _logger.LogTrace("=== FindMatchingItem�J�n ===");
                _logger.LogTrace("��������: MessageLine={MessageLine}, MessageType={MessageType}, CleanType={CleanType}",
                    messageLineNumber, messageItemType, cleanMessageType);

                // �C��: LineNumber=0�̏ꍇ�̂ݎ��s�����x�[�X�̌������s��
                // LineNumber > 0�̏ꍇ�͒ʏ�̌�����D��

                // 1. ���m�Ȉ�v���ŗD��iLineNumber + ItemType�j
                var exactMatch = Items.FirstOrDefault(x =>
                    x.LineNumber == messageLineNumber &&
                    (x.ItemType == messageItemType || x.ItemType == cleanMessageType));

                if (exactMatch != null)
                {
                    _logger.LogTrace("FindMatchingItem: ���m�Ȉ�v���� - Line:{Line}, Type:{Type}",
                        exactMatch.LineNumber, exactMatch.ItemType);
                    return exactMatch;
                }

                // 2. LineNumber����v������́i��������ꍇ�͍ŏ��̗L���Ȃ��́j
                var sameLineItems = Items.Where(x => x.LineNumber == messageLineNumber && x.IsEnable).ToList();
                _logger.LogTrace("����s�̗L���A�C�e����: {Count}", sameLineItems.Count);

                if (sameLineItems.Count == 1)
                {
                    _logger.LogTrace("FindMatchingItem: ����s�̗L���A�C�e������ - Line:{Line}, Type:{Type}",
                        sameLineItems[0].LineNumber, sameLineItems[0].ItemType);
                    return sameLineItems[0];
                }
                else if (sameLineItems.Count > 1)
                {
                    // ��������ꍇ�̓^�C�v���ގ����Ă�����̂�D��
                    var similarTypeMatch = sameLineItems.FirstOrDefault(x =>
                        AreItemTypesSimilar(x.ItemType, messageItemType));
                    if (similarTypeMatch != null)
                    {
                        _logger.LogTrace("FindMatchingItem: ����s�̗ގ��^�C�v���� - Line:{Line}, Type:{Type}",
                            similarTypeMatch.LineNumber, similarTypeMatch.ItemType);
                        return similarTypeMatch;
                    }

                    // �ގ��^�C�v���Ȃ��ꍇ�͍ŏ��̂���
                    _logger.LogTrace("FindMatchingItem: ����s�̍ŏ��̃A�C�e���I�� - Line:{Line}, Type:{Type}",
                        sameLineItems[0].LineNumber, sameLineItems[0].ItemType);
                    return sameLineItems[0];
                }

                // 3. LineNumber=0�̏ꍇ�̂ݎ��s�����x�[�X�̌������g�p
                if (messageLineNumber == 0)
                {
                    _logger.LogTrace("LineNumber=0�̂��ߎ��s�����x�[�X���������s");
                    return FindItemByExecutionOrder(messageItemType, cleanMessageType);
                }

                // 4. �߂��s�ԍ��Ń^�C�v����v������́i�}3�͈̔́j
                var nearbyMatch = Items.FirstOrDefault(x =>
                    Math.Abs(x.LineNumber - messageLineNumber) <= 3 &&
                    x.IsEnable &&
                    (x.ItemType == messageItemType || x.ItemType == cleanMessageType ||
                     AreItemTypesSimilar(x.ItemType, messageItemType)));

                if (nearbyMatch != null)
                {
                    _logger.LogTrace("FindMatchingItem: �ߗ׍s�Ń^�C�v��v - Line:{Line}, Type:{Type} (����:{Distance})",
                        nearbyMatch.LineNumber, nearbyMatch.ItemType, Math.Abs(nearbyMatch.LineNumber - messageLineNumber));
                    return nearbyMatch;
                }

                _logger.LogWarning("FindMatchingItem: ��v����A�C�e����������܂��� - MessageLine:{MessageLine}, MessageType:{MessageType}",
                    messageLineNumber, messageItemType);

                _logger.LogTrace("=== FindMatchingItem�I��: null ===");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FindMatchingItem���ɃG���[");
                return null;
            }
        }

        /// <summary>
        /// LineNumber=0�̏ꍇ�̎��s�����x�[�X�̌���
        /// </summary>
        private ICommandListItem? FindItemByExecutionOrder(string messageItemType, string cleanMessageType)
        {
            try
            {
                _logger.LogTrace("���s�����x�[�X�����J�n: MessageType={MessageType}, CleanType={CleanType}",
                    messageItemType, cleanMessageType);

                // 1. ���ݎ��s���̃A�C�e������^�C�v��v������
                var runningItems = Items.Where(x => x.IsRunning).ToList();
                _logger.LogTrace("���s���̃A�C�e����: {Count}", runningItems.Count);

                foreach (var runningItem in runningItems)
                {
                    if (runningItem.ItemType == messageItemType ||
                        runningItem.ItemType == cleanMessageType ||
                        AreItemTypesSimilar(runningItem.ItemType, messageItemType))
                    {
                        _logger.LogTrace("FindItemByExecutionOrder: ���s���A�C�e������^�C�v��v - Line:{Line}, Type:{Type}",
                            runningItem.LineNumber, runningItem.ItemType);
                        return runningItem;
                    }
                }

                // 2. ���s�\�Ȏ��̃A�C�e���������i�����l���j
                var nextExecutableItem = FindNextExecutableItem(messageItemType, cleanMessageType);
                if (nextExecutableItem != null)
                {
                    _logger.LogTrace("FindItemByExecutionOrder: ���̎��s�\�A�C�e������ - Line:{Line}, Type:{Type}",
                        nextExecutableItem.LineNumber, nextExecutableItem.ItemType);
                    return nextExecutableItem;
                }

                // 3. �^�C�v�݂̂ň�v�i�ŏ��Ɍ����������́j
                var typeOnlyMatch = Items.FirstOrDefault(x =>
                    x.IsEnable &&
                    (x.ItemType == messageItemType || x.ItemType == cleanMessageType ||
                     AreItemTypesSimilar(x.ItemType, messageItemType)));


                if (typeOnlyMatch != null)
                {
                    _logger.LogTrace("FindItemByExecutionOrder: �^�C�v�݈̂�v - Line:{Line}, Type:{Type}",
                        typeOnlyMatch.LineNumber, typeOnlyMatch.ItemType);
                    return typeOnlyMatch;
                }

                _logger.LogWarning("FindItemByExecutionOrder: ���s�����x�[�X�����ň�v����A�C�e����������܂���");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FindItemByExecutionOrder���ɃG���[");
                return null;
            }
        }

        /// <summary>
        /// ���s�������l�����Ď��Ɏ��s�����ׂ��A�C�e��������
        /// </summary>
        private ICommandListItem? FindNextExecutableItem(string messageItemType, string cleanMessageType)
        {
            try
            {
                // �Ō�Ɏ��s���ꂽ�A�C�e�����擾
                var lastExecutedItem = CurrentExecutingItem ??
                    Items.Where(x => x.Progress > 0).OrderByDescending(x => x.Progress).FirstOrDefault() ??
                    Items.FirstOrDefault(x => x.IsRunning);

                if (lastExecutedItem != null)
                {
                    _logger.LogTrace("�Ō�̎��s�A�C�e��: Line={LineNumber}, Type={ItemType}",
                        lastExecutedItem.LineNumber, lastExecutedItem.ItemType);

                    // ���Ɏ��s�����ׂ��A�C�e����_���I�����Ō���
                    var candidateItems = Items.Where(x =>
                        x.IsEnable &&
                        x.LineNumber > lastExecutedItem.LineNumber &&
                        (x.ItemType == messageItemType || x.ItemType == cleanMessageType ||
                         AreItemTypesSimilar(x.ItemType, messageItemType))).ToList();

                    if (candidateItems.Count > 0)
                    {
                        var nextItem = candidateItems.OrderBy(x => x.LineNumber).First();
                        _logger.LogTrace("�_���I���̃A�C�e������: Line={LineNumber}, Type={ItemType}",
                            nextItem.LineNumber, nextItem.ItemType);
                        return nextItem;
                    }
                }

                // �t�H�[���o�b�N: �ŏ��̖����s�̃}�b�`����A�C�e��
                var firstMatch = Items.Where(x =>
                    x.IsEnable &&
                    !x.IsRunning &&
                    x.Progress == 0 &&
                    (x.ItemType == messageItemType || x.ItemType == cleanMessageType ||
                     AreItemTypesSimilar(x.ItemType, messageItemType))).OrderBy(x => x.LineNumber).FirstOrDefault();

                if (firstMatch != null)
                {
                    _logger.LogTrace("�ŏ��̖����s�A�C�e������: Line={LineNumber}, Type={ItemType}",
                        firstMatch.LineNumber, firstMatch.ItemType);
                }

                return firstMatch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FindNextExecutableItem���ɃG���[");
                return null;
            }
        }

        private int GetNextLineNumber()
        {
            return Items.Count > 0 ? Items.Max(i => i.LineNumber) + 1 : 1;
        }

        private string CleanItemType(string itemType)
        {
            if (string.IsNullOrEmpty(itemType)) return itemType;

            if (itemType.EndsWith("Command"))
            {
                return itemType.Substring(0, itemType.Length - "Command".Length);
            }

            return itemType;
        }

        private bool AreItemTypesSimilar(string type1, string type2)
        {
            if (string.IsNullOrEmpty(type1) || string.IsNullOrEmpty(type2)) return false;

            var clean1 = CleanItemType(type1);
            var clean2 = CleanItemType(type2);

            if (clean1.Equals(clean2, StringComparison.OrdinalIgnoreCase)) return true;

            return (clean1, clean2) switch
            {
                ("WaitImage", "Wait_Image") or ("Wait_Image", "WaitImage") => true,
                ("ClickImage", "Click_Image") or ("Click_Image", "ClickImage") => true,
                ("ClickImageAI", "Click_Image_AI") or ("Click_Image_AI", "ClickImageAI") => true,
                ("IfImageExist", "IF_ImageExist") or ("IF_ImageExist", "IfImageExist") => true,
                ("IfImageNotExist", "IF_ImageNotExist") or ("IF_ImageNotExist", "IfImageNotExist") => true,
                ("IfImageExistAI", "IF_ImageExist_AI") or ("IF_ImageExist_AI", "IfImageExistAI") => true,
                ("IfImageNotExistAI", "IF_ImageNotExist_AI") or ("IF_ImageNotExist_AI", "IfImageNotExistAI") => true,
                ("SetVariableAI", "SetVariable_AI") or ("SetVariable_AI", "SetVariableAI") => true,
                ("IfVariable", "IF_Variable") or ("IF_Variable", "IfVariable") => true,
                ("Wait", "Wait") => true, // Wait�R�}���h�̈�v������ǉ�
                _ => false
            };
        }

        public void SetRunningState(bool isRunning)
        {
            _logger.LogDebug("=== SetRunningState�J�n: {IsRunning} ===", isRunning);

            IsRunning = isRunning;
            StatusMessage = isRunning ? "���s��..." : "��������";
            _logger.LogDebug("���s��Ԃ�ݒ�: {IsRunning}", isRunning);

            if (isRunning)
            {
                _logger.LogDebug("���s�J�n - �v���O���X������");
                InitializeProgress();
                _completedCommands = 0;

                // ���s�J�E���^�[�����Z�b�g
                _executionCounter.Clear();
                _lastExecutedItem = null;
                _logger.LogDebug("���s�J�E���^�[�ƒǐՏ�Ԃ����Z�b�g");

                // �S�A�C�e���̎��s��Ԃ����Z�b�g
                foreach (var item in Items)
                {
                    item.IsRunning = false;
                    item.Progress = 0;
                }

                _logger.LogDebug("���s�J�n - �S�A�C�e����ԃ��Z�b�g����");
            }
            else
            {
                _logger.LogDebug("���s�I�� - �N���[���A�b�v�J�n");

                if (ShowProgress)
                {
                    CompleteProgress();
                }

                // ���s�J�E���^�[�ƒǐՏ�Ԃ��N���A
                _executionCounter.Clear();
                _lastExecutedItem = null;

                // �S�A�C�e���̎��s��Ԃ��N���A
                var runningCount = 0;
                foreach (var item in Items)
                {
                    if (item.IsRunning)
                    {
                        runningCount++;
                        item.IsRunning = false;
                        item.Progress = 0;
                    }
                }

                CurrentExecutingItem = null;

                _logger.LogDebug("���s�I�� - {RunningCount}�̃A�C�e���̎��s��Ԃ��N���A", runningCount);

                // UI�ɕύX��ʒm
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(CurrentExecutingItem));
                OnPropertyChanged(nameof(CurrentExecutingDescription));
            }

            _logger.LogDebug("=== SetRunningState�I��: {IsRunning} ===", isRunning);
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

    #endregion
}