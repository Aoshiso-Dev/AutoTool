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

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5���S�����ŁFListPanelViewModel�i�R�}���h�Ǘ������j
    /// </summary>
    public partial class ListPanelViewModel : ObservableObject
    {
        private readonly ILogger<ListPanelViewModel> _logger;
        private readonly ObservableCollection<ICommandListItem> _items = new();
        private readonly Stack<CommandListOperation> _undoStack = new();
        private readonly Stack<CommandListOperation> _redoStack = new();

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

        public ObservableCollection<ICommandListItem> Items => _items;
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public bool HasItems => Items.Count > 0;

        public ListPanelViewModel(ILogger<ListPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            SetupMessaging();
            _logger.LogInformation("Phase 5������ListPanelViewModel �����������Ă��܂�");

            // �R���N�V�����ύX�̊Ď�
            _items.CollectionChanged += (s, e) =>
            {
                TotalItems = _items.Count;
                HasUnsavedChanges = true;
                UpdateLineNumbers();
            };
        }

        private void SetupMessaging()
        {
            // �R�}���h���상�b�Z�[�W�̏���
            WeakReferenceMessenger.Default.Register<AddMessage>(this, (r, m) => Add(m.ItemType));
            WeakReferenceMessenger.Default.Register<DeleteMessage>(this, (r, m) => Delete());
            WeakReferenceMessenger.Default.Register<UpMessage>(this, (r, m) => MoveUp());
            WeakReferenceMessenger.Default.Register<DownMessage>(this, (r, m) => MoveDown());
            WeakReferenceMessenger.Default.Register<ClearMessage>(this, (r, m) => Clear());
            WeakReferenceMessenger.Default.Register<UndoMessage>(this, (r, m) => Undo());
            WeakReferenceMessenger.Default.Register<RedoMessage>(this, (r, m) => Redo());
            
            // �A�C�e���^�C�v�ύX���b�Z�[�W�̏���
            WeakReferenceMessenger.Default.Register<ChangeItemTypeMessage>(this, (r, m) => ChangeItemType(m.OldItem, m.NewItem));
            
            // ���X�g�r���[�X�V���b�Z�[�W�̏���
            WeakReferenceMessenger.Default.Register<RefreshListViewMessage>(this, (r, m) => RefreshList());
            
            // �R�}���h���s��ԃ��b�Z�[�W�̏���
            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (r, m) => OnCommandStarted(m));
            WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, (r, m) => OnCommandFinished(m));
            WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, (r, m) => OnProgressUpdated(m));
        }

        partial void OnSelectedItemChanged(ICommandListItem? value)
        {
            if (value != null)
            {
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(value));
                _logger.LogDebug("�I���A�C�e���ύX: {ItemType} (�s {LineNumber})", value.ItemType, value.LineNumber);
            }
        }

        #region �R�}���h����

        [RelayCommand]
        private void Add(string itemType)
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

                Items.Insert(insertIndex, newItem);
                SelectedIndex = insertIndex;
                SelectedItem = newItem;
                
                RecordOperation(operation);
                StatusMessage = $"{itemType}��ǉ����܂���";
                _logger.LogInformation("�A�C�e����ǉ����܂���: {ItemType} (���v {Count}��)", itemType, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���ǉ����ɃG���[���������܂���");
                StatusMessage = $"�ǉ��G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Delete()
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
        private void MoveUp()
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
        private void MoveDown()
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
        private void Clear()
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

        #endregion

        #region Undo/Redo�@�\

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
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
        private void Redo()
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

        #region �A�C�e���^�C�v�ύX

        /// <summary>
        /// �A�C�e���^�C�v��ύX
        /// </summary>
        private void ChangeItemType(ICommandListItem oldItem, ICommandListItem newItem)
        {
            try
            {
                var index = Items.IndexOf(oldItem);
                if (index >= 0)
                {
                    // ���̃A�C�e����ۑ��iUndo�p�j
                    var operation = new CommandListOperation
                    {
                        Type = OperationType.Replace,
                        Index = index,
                        Item = oldItem.Clone(),
                        NewItem = newItem.Clone(),
                        Description = $"�^�C�v�ύX: {oldItem.ItemType} -> {newItem.ItemType}"
                    };

                    // �A�C�e����u��
                    Items[index] = newItem;
                    SelectedIndex = index;
                    SelectedItem = newItem;

                    // �s�ԍ��ƃl�X�g���x�����X�V
                    UpdateLineNumbers();

                    RecordOperation(operation);
                    StatusMessage = $"�^�C�v��ύX���܂���: {oldItem.ItemType} -> {newItem.ItemType}";
                    
                    // EditPanel�ɐV�����A�C�e����ʒm
                    WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(newItem));

                    _logger.LogInformation("�A�C�e���^�C�v��ύX���܂���: {OldType} -> {NewType} (�s {LineNumber})", 
                        oldItem.ItemType, newItem.ItemType, newItem.LineNumber);
                }
                else
                {
                    _logger.LogWarning("�ύX�Ώۂ̃A�C�e����������܂���ł���");
                    StatusMessage = "�ύX�Ώۂ̃A�C�e����������܂���ł���";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���^�C�v�ύX���ɃG���[���������܂���");
                StatusMessage = $"�^�C�v�ύX�G���[: {ex.Message}";
            }
        }

        /// <summary>
        /// ���X�g�r���[�������X�V
        /// </summary>
        private void RefreshList()
        {
            try
            {
                // ���ׂẴv���p�e�B�ύX�ʒm�𔭉�
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(SelectedIndex));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(HasItems));

                // �e�A�C�e���̕`��v���p�e�B���X�V
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    if (item != null)
                    {
                        // ItemType�v���p�e�B�̕ύX�ʒm�𔭉΂���UI�X�V
                        var currentType = item.ItemType;
                        item.ItemType = currentType; // �����l���Đݒ肵�Ēʒm����
                    }
                }

                StatusMessage = "���X�g���X�V���܂���";
                _logger.LogDebug("���X�g�r���[�������X�V���܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���X�g�X�V���ɃG���[���������܂���");
                StatusMessage = $"�X�V�G���[: {ex.Message}";
            }
        }

        #endregion

        #region �w���p�[���\�b�h

        private ICommandListItem CreateItem(string itemType)
        {
            // CommandRegistry���g�p���ăA�C�e�����쐬
            var itemTypes = CommandRegistry.GetTypeMapping();
            if (itemTypes.TryGetValue(itemType, out var type))
            {
                if (Activator.CreateInstance(type) is ICommandListItem item)
                {
                    item.LineNumber = Items.Count + 1;
                    item.ItemType = itemType;
                    return item;
                }
            }

            // �t�H�[���o�b�N�Ƃ��Ċ�{�I�ȃA�C�e�����쐬
            return new BasicCommandItem 
            { 
                ItemType = itemType, 
                LineNumber = Items.Count + 1,
                Comment = $"�V����{itemType}�R�}���h",
                Description = $"{itemType}�R�}���h"
            };
        }

        private void UpdateLineNumbers()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].LineNumber = i + 1;
            }
            
            // �y�A�����O������ǉ�
            UpdateNestLevel();
            UpdatePairing();
        }

        /// <summary>
        /// �l�X�g���x�����X�V
        /// </summary>
        private void UpdateNestLevel()
        {
            var nestLevel = 0;

            foreach (var item in Items)
            {
                // �l�X�g���x�������炷�R�}���h�i�I���n�j
                if (IsEndCommand(item.ItemType))
                {
                    nestLevel = Math.Max(0, nestLevel - 1);
                }

                item.NestLevel = nestLevel;

                // �l�X�g���x���𑝂₷�R�}���h�i�J�n�n�j
                if (IsStartCommand(item.ItemType))
                {
                    nestLevel++;
                }
            }
        }

        /// <summary>
        /// �y�A�����O���X�V
        /// </summary>
        private void UpdatePairing()
        {
            try
            {
                PairLoopItems();
                PairIfItems();
                _logger.LogDebug("�y�A�����O�X�V����");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�y�A�����O�X�V���ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// ���[�v�A�C�e���̃y�A�����O
        /// </summary>
        private void PairLoopItems()
        {
            // �܂������̃y�A�����O���N���A
            ClearLoopPairing();

            var loopItems = Items.Where(x => x.ItemType == "Loop").ToList();
            var loopEndItems = Items.Where(x => x.ItemType == "Loop_End").ToList();

            foreach (var loopItem in loopItems)
            {
                // �Ή�����Loop_End��T��
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
                    SetPropertyValue(correspondingEnd, "LoopCount", loopCount);
                    
                    _logger.LogDebug("���[�v�y�A�����O: Loop({LineNum1}) <-> Loop_End({LineNum2})", 
                        loopItem.LineNumber, correspondingEnd.LineNumber);
                }
                else
                {
                    _logger.LogWarning("Loop (�s {LineNumber}) �ɑΉ�����Loop_End��������܂���", loopItem.LineNumber);
                }
            }
        }

        /// <summary>
        /// IF�A�C�e���̃y�A�����O
        /// </summary>
        private void PairIfItems()
        {
            // �܂������̃y�A�����O���N���A
            ClearIfPairing();

            var ifItems = Items.Where(x => IsIfCommand(x.ItemType)).ToList();
            var ifEndItems = Items.Where(x => x.ItemType == "IF_End").ToList();

            foreach (var ifItem in ifItems)
            {
                // �Ή�����IF_End��T��
                var correspondingEnd = ifEndItems
                    .Where(end => end.LineNumber > ifItem.LineNumber)
                    .Where(end => end.NestLevel == ifItem.NestLevel)
                    .OrderBy(end => end.LineNumber)
                    .FirstOrDefault();

                if (correspondingEnd != null)
                {
                    SetPairProperty(ifItem, correspondingEnd);
                    SetPairProperty(correspondingEnd, ifItem);
                    
                    _logger.LogDebug("IF�y�A�����O: {IfType}({LineNum1}) <-> IF_End({LineNum2})", 
                        ifItem.ItemType, ifItem.LineNumber, correspondingEnd.LineNumber);
                }
                else
                {
                    _logger.LogWarning("{IfType} (�s {LineNumber}) �ɑΉ�����IF_End��������܂���", 
                        ifItem.ItemType, ifItem.LineNumber);
                }
            }
        }

        /// <summary>
        /// ���[�v�y�A�����O���N���A
        /// </summary>
        private void ClearLoopPairing()
        {
            foreach (var item in Items.Where(x => x.ItemType == "Loop" || x.ItemType == "Loop_End"))
            {
                SetPairProperty(item, null);
            }
        }

        /// <summary>
        /// IF�y�A�����O���N���A
        /// </summary>
        private void ClearIfPairing()
        {
            foreach (var item in Items.Where(x => IsIfCommand(x.ItemType) || x.ItemType == "IF_End"))
            {
                SetPairProperty(item, null);
            }
        }

        /// <summary>
        /// �y�A�v���p�e�B��ݒ�
        /// </summary>
        private void SetPairProperty(ICommandListItem item, ICommandListItem? pair)
        {
            var pairProperty = item.GetType().GetProperty("Pair");
            if (pairProperty != null && pairProperty.CanWrite)
            {
                pairProperty.SetValue(item, pair);
            }
        }

        /// <summary>
        /// �v���p�e�B�l���擾
        /// </summary>
        private T GetPropertyValue<T>(ICommandListItem item, string propertyName)
        {
            var property = item.GetType().GetProperty(propertyName);
            if (property != null && property.CanRead)
            {
                var value = property.GetValue(item);
                if (value is T tValue)
                    return tValue;
            }
            return default(T)!;
        }

        /// <summary>
        /// �v���p�e�B�l��ݒ�
        /// </summary>
        private void SetPropertyValue(ICommandListItem item, string propertyName, object? value)
        {
            var property = item.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(item, value);
            }
        }

        /// <summary>
        /// �J�n�R�}���h���ǂ����𔻒�
        /// </summary>
        private bool IsStartCommand(string itemType)
        {
            return itemType switch
            {
                "Loop" => true,
                "IF_ImageExist" => true,
                "IF_ImageNotExist" => true,
                "IF_ImageExist_AI" => true,
                "IF_ImageNotExist_AI" => true,
                "IF_Variable" => true,
                _ => false
            };
        }

        /// <summary>
        /// �I���R�}���h���ǂ����𔻒�
        /// </summary>
        private bool IsEndCommand(string itemType)
        {
            return itemType switch
            {
                "Loop_End" => true,
                "IF_End" => true,
                _ => false
            };
        }

        /// <summary>
        /// IF�R�}���h���ǂ����𔻒�
        /// </summary>
        private bool IsIfCommand(string itemType)
        {
            return itemType switch
            {
                "IF_ImageExist" => true,
                "IF_ImageNotExist" => true,
                "IF_ImageExist_AI" => true,
                "IF_ImageNotExist_AI" => true,
                "IF_Variable" => true,
                _ => false
            };
        }

        /// <summary>
        /// ������L�^�iUndo/Redo�p�j
        /// </summary>
        private void RecordOperation(CommandListOperation operation)
        {
            _undoStack.Push(operation);
            _redoStack.Clear(); // �V�������삪�s��ꂽ��Redo�X�^�b�N���N���A
            
            // �X�^�b�N�T�C�Y�����i�����������j
            const int maxUndoSteps = 100;
            while (_undoStack.Count > maxUndoSteps)
            {
                _undoStack.TryPop(out _);
            }
            
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        #endregion

        #region �t�@�C������

        public void Load(string filePath)
        {
            try
            {
                StatusMessage = "�t�@�C���ǂݍ��ݒ�...";
                
                if (System.IO.File.Exists(filePath))
                {
                    var json = System.IO.File.ReadAllText(filePath);
                    
                    // �K�؂Ȍ^�Ńf�V���A���C�Y
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    };
                    
                    var itemData = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json, options);
                    
                    Items.Clear();
                    if (itemData != null)
                    {
                        foreach (var itemDict in itemData)
                        {
                            if (itemDict.TryGetValue("ItemType", out var itemTypeObj) && itemTypeObj is string itemType)
                            {
                                var item = CreateItem(itemType);
                                
                                // �v���p�e�B�𕜌�
                                foreach (var kvp in itemDict)
                                {
                                    var property = item.GetType().GetProperty(kvp.Key);
                                    if (property != null && property.CanWrite)
                                    {
                                        try
                                        {
                                            var value = Convert.ChangeType(kvp.Value, property.PropertyType);
                                            property.SetValue(item, value);
                                        }
                                        catch
                                        {
                                            // �v���p�e�B�ݒ莸�s�͖���
                                        }
                                    }
                                }
                                
                                Items.Add(item);
                            }
                        }
                    }
                }
                
                SelectedIndex = Items.Count > 0 ? 0 : -1;
                SelectedItem = Items.FirstOrDefault();
                UpdateLineNumbers();
                
                // �������N���A
                _undoStack.Clear();
                _redoStack.Clear();
                HasUnsavedChanges = false;
                
                StatusMessage = $"�t�@�C����ǂݍ��݂܂���: {Path.GetFileName(filePath)} ({Items.Count}��)";
                _logger.LogInformation("�t�@�C����ǂݍ��݂܂���: {FilePath} ({Count}��)", filePath, Items.Count);
                
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
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
                
                // �ۑ��p�̃f�[�^������
                var saveData = Items.Select(item => new Dictionary<string, object?>
                {
                    ["ItemType"] = item.ItemType,
                    ["LineNumber"] = item.LineNumber,
                    ["Comment"] = item.Comment,
                    ["IsEnable"] = item.IsEnable,
                    ["Description"] = item.Description
                    // �K�v�ɉ����đ��̃v���p�e�B���ǉ�
                }).ToList();
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(saveData, options);
                
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

        #region ���؁E���v

        /// <summary>
        /// �R�}���h���X�g�̌���
        /// </summary>
        [RelayCommand]
        private void ValidateCommands()
        {
            try
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    
                    // ��{����
                    if (string.IsNullOrEmpty(item.ItemType))
                    {
                        errors.Add($"�s {i + 1}: �A�C�e���^�C�v���ݒ肳��Ă��܂���");
                    }

                    // �y�A���؁iLoop�AIf�n�j
                    if (item.ItemType.StartsWith("Loop") && !item.ItemType.EndsWith("_End") && !item.ItemType.EndsWith("_Break"))
                    {
                        // TODO: �y�A���؃��W�b�N
                    }
                }

                if (errors.Count > 0)
                {
                    StatusMessage = $"���؃G���[: {errors.Count}��";
                    WeakReferenceMessenger.Default.Send(new ValidationMessage("Error", string.Join("\n", errors), true));
                }
                else if (warnings.Count > 0)
                {
                    StatusMessage = $"���،x��: {warnings.Count}��";
                    WeakReferenceMessenger.Default.Send(new ValidationMessage("Warning", string.Join("\n", warnings)));
                }
                else
                {
                    StatusMessage = "���؊���: ��肠��܂���";
                    WeakReferenceMessenger.Default.Send(new ValidationMessage("Success", "�R�}���h���X�g�ɖ��͂���܂���"));
                }

                _logger.LogInformation("�R�}���h���X�g���؊���: �G���[{ErrorCount}��, �x��{WarningCount}��", errors.Count, warnings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h���X�g���ؒ��ɃG���[���������܂���");
                StatusMessage = $"���؃G���[: {ex.Message}";
            }
        }

        /// <summary>
        /// ���v���̎擾
        /// </summary>
        public CommandListStats GetStats()
        {
            var stats = new CommandListStats
            {
                TotalItems = Items.Count,
                EnabledItems = Items.Count(i => i.IsEnable),
                DisabledItems = Items.Count(i => !i.IsEnable),
                ItemTypeStats = Items.GroupBy(i => i.ItemType).ToDictionary(g => g.Key, g => g.Count())
            };

            return stats;
        }

        #endregion

        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            StatusMessage = isRunning ? "���s��..." : "��������";
            _logger.LogDebug("���s��Ԃ�ݒ�: {IsRunning}", isRunning);
        }

        /// <summary>
        /// ��������
        /// </summary>
        public void Prepare()
        {
            try
            {
                _logger.LogDebug("ListPanelViewModel �̏������������s���܂�");
                
                // ���s�֘A�̏�Ԃ����Z�b�g
                foreach (var item in Items)
                {
                    item.IsRunning = false;
                    item.Progress = 0;
                }
                
                StatusMessage = "��������";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ListPanelViewModel �����������ɃG���[���������܂���");
            }
        }

        #region �R�}���h���s��ԏ���

        /// <summary>
        /// �R�}���h�J�n����
        /// </summary>
        private void OnCommandStarted(StartCommandMessage message)
        {
            try
            {
                // LineNumber�ŃA�C�e������肵��IsRunning=true�ɐݒ�
                var item = Items.FirstOrDefault(x => x.LineNumber == message.Command.LineNumber);
                if (item != null)
                {
                    item.IsRunning = true;
                    item.Progress = 0;
                    _logger.LogDebug("�R�}���h�J�n: Line {LineNumber} - {ItemType}", item.LineNumber, item.ItemType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h�J�n�������ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// �R�}���h��������
        /// </summary>
        private void OnCommandFinished(FinishCommandMessage message)
        {
            try
            {
                // LineNumber�ŃA�C�e������肵��IsRunning=false�ɐݒ�
                var item = Items.FirstOrDefault(x => x.LineNumber == message.Command.LineNumber);
                if (item != null)
                {
                    item.IsRunning = false;
                    item.Progress = 100; // ��������100%
                    _logger.LogDebug("�R�}���h����: Line {LineNumber} - {ItemType}", item.LineNumber, item.ItemType);
                    
                    // �����x������Progress���N���A
                    Task.Delay(1000).ContinueWith(_ => 
                    {
                        if (!item.IsRunning) // �܂����s���łȂ���΃N���A
                        {
                            item.Progress = 0;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h�����������ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// �i���X�V����
        /// </summary>
        private void OnProgressUpdated(UpdateProgressMessage message)
        {
            try
            {
                // LineNumber�ŃA�C�e������肵��Progress���X�V
                var item = Items.FirstOrDefault(x => x.LineNumber == message.Command.LineNumber);
                if (item != null && item.IsRunning)
                {
                    item.Progress = message.Progress;
                    _logger.LogTrace("�i���X�V: Line {LineNumber} - {Progress}%", item.LineNumber, message.Progress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�i���X�V�������ɃG���[���������܂���");
            }
        }

        #endregion
    }

    #region �⏕�N���X

    /// <summary>
    /// �R�}���h���X�g����̋L�^
    /// </summary>
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

    /// <summary>
    /// ����^�C�v
    /// </summary>
    public enum OperationType
    {
        Add,
        Delete,
        Move,
        Clear,
        Edit,
        Replace
    }

    /// <summary>
    /// �R�}���h���X�g���v
    /// </summary>
    public class CommandListStats
    {
        public int TotalItems { get; set; }
        public int EnabledItems { get; set; }
        public int DisabledItems { get; set; }
        public Dictionary<string, int> ItemTypeStats { get; set; } = new();
    }

    #endregion
}