using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Class;
using AutoTool.Model.List.Type;
using AutoTool.Model.CommandDefinition;
using AutoTool.ViewModel.Shared;
using AutoTool.ViewModel.Shared.UndoRedoCommands;
using AutoTool.Message;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5���S�����ŁFListPanelViewModel�i���ۂ̃R�}���h����@�\�t���j
    /// MacroPanels�ˑ����폜���AAutoTool�����ł̂ݎg�p
    /// </summary>
    public partial class ListPanelViewModel : ObservableObject
    {
        private readonly ILogger<ListPanelViewModel> _logger;
        private readonly CommandHistoryManager _commandHistory;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private CommandList _commandList = new();

        [ObservableProperty]
        private ObservableCollection<ICommandListItem> _items = new();

        [ObservableProperty]
        private ICommandListItem? _selectedItem;

        [ObservableProperty]
        private int _selectedLineNumber = 0;

        /// <summary>
        /// Phase 5���S�����ŃR���X�g���N�^
        /// </summary>
        public ListPanelViewModel(ILogger<ListPanelViewModel> logger, CommandHistoryManager commandHistory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandHistory = commandHistory ?? throw new ArgumentNullException(nameof(commandHistory));

            SetupMessaging();
            InitializeCommands();

            _logger.LogInformation("Phase 5���S����ListPanelViewModel����������");
        }

        private void SetupMessaging()
        {
            // ���b�Z�[�W����M���ăR�}���h��������s
            WeakReferenceMessenger.Default.Register<AddMessage>(this, (r, m) =>
            {
                AddCommand(m.ItemType);
            });

            WeakReferenceMessenger.Default.Register<DeleteMessage>(this, (r, m) =>
            {
                DeleteSelectedCommand();
            });

            WeakReferenceMessenger.Default.Register<UpMessage>(this, (r, m) =>
            {
                MoveCommandUp();
            });

            WeakReferenceMessenger.Default.Register<DownMessage>(this, (r, m) =>
            {
                MoveCommandDown();
            });

            WeakReferenceMessenger.Default.Register<ClearMessage>(this, (r, m) =>
            {
                ClearAllCommands();
            });

            WeakReferenceMessenger.Default.Register<SaveMessage>(this, (r, m) =>
            {
                if (string.IsNullOrEmpty(m.FilePath))
                {
                    Save(); // �f�t�H���g�ۑ�
                }
                else
                {
                    Save(m.FilePath); // �w��p�X�ۑ�
                }
            });

            WeakReferenceMessenger.Default.Register<LoadMessage>(this, (r, m) =>
            {
                if (string.IsNullOrEmpty(m.FilePath))
                {
                    Load(); // �f�t�H���g�ǂݍ���
                }
                else
                {
                    Load(m.FilePath); // �w��p�X�ǂݍ���
                }
            });
        }

        private void InitializeCommands()
        {
            // �����T���v���R�}���h��ǉ�
            var sampleCommands = new[]
            {
                CreateBasicCommand("Wait", "�T���v���ҋ@�R�}���h"),
                CreateBasicCommand("Click", "�T���v���N���b�N�R�}���h"),
                CreateBasicCommand("Loop", "�T���v�����[�v�R�}���h")
            };

            foreach (var command in sampleCommands)
            {
                Items.Add(command);
                _commandList.Items.Add(command);
            }

            UpdateLineNumbers();
            _logger.LogInformation("�����T���v���R�}���h��ǉ����܂���: {Count}��", sampleCommands.Length);
        }

        /// <summary>
        /// �R�}���h��ǉ��iUndo/Redo�Ή��j
        /// </summary>
        public void AddCommand(string itemType)
        {
            try
            {
                _logger.LogDebug("�R�}���h�ǉ��J�n: {ItemType}", itemType);

                // �V�����R�}���h���쐬
                var newItem = CreateBasicCommand(itemType, $"{itemType}�R�}���h");
                var targetIndex = Math.Max(0, _selectedLineNumber + 1);

                var addCommand = new AddItemCommand(
                    newItem,
                    targetIndex,
                    (item, index) =>
                    {
                        InsertAt(index, item);
                        _logger.LogDebug("�R�}���h�ǉ����s: {ItemType} at {Index}", item.ItemType, index);
                    },
                    (index) =>
                    {
                        RemoveAt(index);
                        _logger.LogDebug("�R�}���h�ǉ�������: at {Index}", index);
                    }
                );

                _commandHistory.ExecuteCommand(addCommand);
                _logger.LogInformation("�R�}���h�ǉ�����: {ItemType} at {Index}", itemType, targetIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h�ǉ����ɃG���[���������܂���: {ItemType}", itemType);
            }
        }

        /// <summary>
        /// �I�����ꂽ�R�}���h���폜�iUndo/Redo�Ή��j
        /// </summary>
        public void DeleteSelectedCommand()
        {
            try
            {
                if (SelectedItem == null)
                {
                    _logger.LogDebug("�폜�Ώۂ̃A�C�e�����I������Ă��܂���");
                    return;
                }

                var selectedIndex = _selectedLineNumber;
                var itemToDelete = SelectedItem.Clone();

                var removeCommand = new RemoveItemCommand(
                    itemToDelete,
                    selectedIndex,
                    (item, index) =>
                    {
                        InsertAt(index, item);
                        _logger.LogDebug("�R�}���h�폜������: {ItemType} at {Index}", item.ItemType, index);
                    },
                    (index) =>
                    {
                        RemoveAt(index);
                        _logger.LogDebug("�R�}���h�폜���s: at {Index}", index);
                    }
                );

                _commandHistory.ExecuteCommand(removeCommand);
                _logger.LogInformation("�R�}���h�폜����: index {Index}", selectedIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h�폜���ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// �R�}���h����Ɉړ��iUndo/Redo�Ή��j
        /// </summary>
        public void MoveCommandUp()
        {
            try
            {
                var fromIndex = _selectedLineNumber;
                var toIndex = fromIndex - 1;

                if (toIndex < 0)
                {
                    _logger.LogDebug("�ŏ�ʃA�C�e���̂��ߏ�ړ��ł��܂���");
                    return;
                }

                var moveCommand = new MoveItemCommand(
                    fromIndex, toIndex,
                    (from, to) =>
                    {
                        MoveItem(from, to);
                        _selectedLineNumber = to;
                        _logger.LogDebug("�R�}���h��ړ�: {From} �� {To}", from, to);
                    }
                );

                _commandHistory.ExecuteCommand(moveCommand);
                _logger.LogInformation("�R�}���h��ړ�����: {From} �� {To}", fromIndex, toIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h��ړ����ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// �R�}���h�����Ɉړ��iUndo/Redo�Ή��j
        /// </summary>
        public void MoveCommandDown()
        {
            try
            {
                var fromIndex = _selectedLineNumber;
                var toIndex = fromIndex + 1;

                if (toIndex >= Items.Count)
                {
                    _logger.LogDebug("�ŉ��ʃA�C�e���̂��߉��ړ��ł��܂���");
                    return;
                }

                var moveCommand = new MoveItemCommand(
                    fromIndex, toIndex,
                    (from, to) =>
                    {
                        MoveItem(from, to);
                        _selectedLineNumber = to;
                        _logger.LogDebug("�R�}���h���ړ�: {From} �� {To}", from, to);
                    }
                );

                _commandHistory.ExecuteCommand(moveCommand);
                _logger.LogInformation("�R�}���h���ړ�����: {From} �� {To}", fromIndex, toIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h���ړ����ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// �S�R�}���h���N���A�iUndo/Redo�Ή��j
        /// </summary>
        public void ClearAllCommands()
        {
            try
            {
                var currentItems = Items.ToList();
                if (!currentItems.Any())
                {
                    _logger.LogDebug("�N���A�Ώۂ̃A�C�e��������܂���");
                    return;
                }

                var clearCommand = new ClearAllCommand(
                    currentItems,
                    () =>
                    {
                        Clear();
                        _logger.LogDebug("�S�R�}���h�N���A���s");
                    },
                    (items) =>
                    {
                        RestoreItems(items);
                        _logger.LogDebug("�S�R�}���h�N���A������: {Count}������", items.Count());
                    }
                );

                _commandHistory.ExecuteCommand(clearCommand);
                _logger.LogInformation("�S�R�}���h�N���A����: {Count}���폜", currentItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�S�R�}���h�N���A���ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// �t�@�C���ɕۑ�
        /// </summary>
        public void Save(string? filePath = null)
        {
            try
            {
                var path = filePath ?? Path.Combine(Environment.CurrentDirectory, "default_macro.json");
                var data = new
                {
                    Commands = Items.Select(item => new
                    {
                        ItemType = item.ItemType,
                        Comment = item.Comment,
                        IsEnable = item.IsEnable,
                        LineNumber = item.LineNumber,
                        NestLevel = item.NestLevel
                    }).ToArray()
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                File.WriteAllText(path, jsonContent);
                _logger.LogInformation("�}�N���t�@�C���ۑ�����: {Path}, {Count}�̃R�}���h", path, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�}�N���t�@�C���ۑ����ɃG���[���������܂���: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// �t�@�C������ǂݍ���
        /// </summary>
        public void Load(string? filePath = null)
        {
            try
            {
                var path = filePath ?? Path.Combine(Environment.CurrentDirectory, "default_macro.json");
                if (!File.Exists(path))
                {
                    _logger.LogWarning("�ǂݍ��ݑΏۃt�@�C�������݂��܂���: {Path}", path);
                    return;
                }

                var jsonContent = File.ReadAllText(path);
                using var document = System.Text.Json.JsonDocument.Parse(jsonContent);
                
                Clear();

                if (document.RootElement.TryGetProperty("Commands", out var commandsElement))
                {
                    foreach (var commandElement in commandsElement.EnumerateArray())
                    {
                        var itemType = commandElement.GetProperty("ItemType").GetString() ?? "Unknown";
                        var comment = commandElement.GetProperty("Comment").GetString() ?? "";
                        var isEnable = commandElement.GetProperty("IsEnable").GetBoolean();

                        var item = CreateBasicCommand(itemType, comment);
                        item.IsEnable = isEnable;
                        
                        Items.Add(item);
                        _commandList.Items.Add(item);
                    }
                }

                UpdateLineNumbers();
                _commandHistory.Clear(); // �t�@�C���ǂݍ��݌�͗������N���A
                _logger.LogInformation("�}�N���t�@�C���ǂݍ��݊���: {Path}, {Count}�̃R�}���h", path, Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�}�N���t�@�C���ǂݍ��ݒ��ɃG���[���������܂���: {FilePath}", filePath);
                throw;
            }
        }

        #region �������상�\�b�h

        public void InsertAt(int index, ICommandListItem item)
        {
            Items.Insert(index, item);
            _commandList.Items.Insert(index, item);
            UpdateLineNumbers();
            OnPropertyChanged(nameof(Items));
        }

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < Items.Count)
            {
                Items.RemoveAt(index);
                _commandList.Items.RemoveAt(index);
                UpdateLineNumbers();
                
                // �I���ʒu�𒲐�
                if (_selectedLineNumber >= Items.Count)
                {
                    _selectedLineNumber = Math.Max(0, Items.Count - 1);
                }
                OnPropertyChanged(nameof(Items));
            }
        }

        public void MoveItem(int fromIndex, int toIndex)
        {
            if (fromIndex >= 0 && fromIndex < Items.Count && toIndex >= 0 && toIndex < Items.Count)
            {
                var item = Items[fromIndex];
                Items.RemoveAt(fromIndex);
                Items.Insert(toIndex, item);

                var commandItem = _commandList.Items[fromIndex];
                _commandList.Items.RemoveAt(fromIndex);
                _commandList.Items.Insert(toIndex, commandItem);

                UpdateLineNumbers();
                OnPropertyChanged(nameof(Items));
            }
        }

        public void ReplaceAt(int index, ICommandListItem newItem)
        {
            if (index >= 0 && index < Items.Count)
            {
                Items[index] = newItem;
                _commandList.Items[index] = newItem;
                UpdateLineNumbers();
                OnPropertyChanged(nameof(Items));
            }
        }

        public void Clear()
        {
            Items.Clear();
            _commandList.Items.Clear();
            _selectedLineNumber = 0;
            SelectedItem = null;
            OnPropertyChanged(nameof(Items));
        }

        private void RestoreItems(IEnumerable<ICommandListItem> items)
        {
            Clear();
            foreach (var item in items)
            {
                Items.Add(item.Clone());
                _commandList.Items.Add(item.Clone());
            }
            UpdateLineNumbers();
            OnPropertyChanged(nameof(Items));
        }

        private BasicCommandItem CreateBasicCommand(string itemType, string comment)
        {
            return new BasicCommandItem
            {
                ItemType = itemType,
                Comment = comment,
                IsEnable = true,
                LineNumber = Items.Count + 1,
                NestLevel = 0
            };
        }

        private void UpdateLineNumbers()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].LineNumber = i + 1;
            }
        }

        #endregion

        #region �p�u���b�N���\�b�h

        public int GetCount() => Items.Count;

        public ICommandListItem? GetItem(int lineNumber)
        {
            var index = lineNumber - 1;
            return index >= 0 && index < Items.Count ? Items[index] : null;
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(SelectedItem));
        }

        public void Prepare()
        {
            _logger.LogDebug("Phase 5���S����ListPanelViewModel��������");
        }

        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
            _logger.LogDebug("���s��Ԃ�ݒ�: {IsRunning}", isRunning);
        }

        #endregion
    }
}