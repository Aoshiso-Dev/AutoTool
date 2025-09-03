using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using AutoTool.Message;
using AutoTool.Model.CommandDefinition;
using AutoTool.ViewModel.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// Phase 5���S�����ŁFButtonPanelViewModel�i���x�ȃR�}���h����j
    /// </summary>
    public partial class ButtonPanelViewModel : ObservableObject
    {
        private readonly ILogger<ButtonPanelViewModel> _logger;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _itemTypes = new();

        [ObservableProperty]
        private CommandDisplayItem? _selectedItemType;

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _recentCommands = new();

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _favoriteCommands = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _filteredItemTypes = new();

        [ObservableProperty]
        private string _selectedCategory = "���ׂ�";

        [ObservableProperty]
        private ObservableCollection<string> _categories = new();

        [ObservableProperty]
        private bool _showAdvancedOptions = false;

        [ObservableProperty]
        private int _batchAddCount = 1;

        [ObservableProperty]
        private string _statusMessage = "��������";

        public ButtonPanelViewModel(ILogger<ButtonPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("Phase 5������ButtonPanelViewModel �����������Ă��܂�");
            
            InitializeItemTypes();
            LoadRecentCommands();
            LoadFavoriteCommands();
        }

        private void InitializeItemTypes()
        {
            try
            {
                _logger.LogDebug("�A�C�e���^�C�v�̏��������J�n���܂�");

                // CommandRegistry��������
                CommandRegistry.Initialize();
                _logger.LogDebug("CommandRegistry.Initialize() ����");

                var orderedTypeNames = CommandRegistry.GetOrderedTypeNames();
                _logger.LogDebug("GetOrderedTypeNames()�Ŏ擾�����^����: {Count}", orderedTypeNames?.Count() ?? 0);

                var displayItems = orderedTypeNames?
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList() ?? new List<CommandDisplayItem>();
                
                _logger.LogDebug("�쐬����displayItems��: {Count}", displayItems.Count);
                
                ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
                FilteredItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
                SelectedItemType = ItemTypes.FirstOrDefault();
                
                // �J�e�S���[���X�g���쐬
                var categories = displayItems.Select(item => item.Category).Distinct().OrderBy(c => c).ToList();
                categories.Insert(0, "���ׂ�");
                Categories = new ObservableCollection<string>(categories);
                
                _logger.LogDebug("�A�C�e���^�C�v�̏��������������܂���: {Count}��", ItemTypes.Count);
                _logger.LogDebug("�I�����ꂽ�A�C�e��: {DisplayName}", SelectedItemType?.DisplayName ?? "�Ȃ�");
                
                StatusMessage = $"{ItemTypes.Count}�̃R�}���h�����p�\�ł�";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���^�C�v�̏��������ɃG���[���������܂���");
                StatusMessage = $"�������G���[: {ex.Message}";
                throw;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterCommands();
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            FilterCommands();
        }

        private void FilterCommands()
        {
            try
            {
                var filtered = ItemTypes.AsEnumerable();

                // �J�e�S���[�t�B���^�[
                if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "���ׂ�")
                {
                    filtered = filtered.Where(item => item.Category == SelectedCategory);
                }

                // �e�L�X�g�����t�B���^�[
                if (!string.IsNullOrEmpty(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(item => 
                        item.DisplayName.ToLower().Contains(searchLower) ||
                        item.TypeName.ToLower().Contains(searchLower) ||
                        item.Category.ToLower().Contains(searchLower));
                }

                FilteredItemTypes = new ObservableCollection<CommandDisplayItem>(filtered);
                
                // �������ʂ�����ꍇ�͍ŏ��̃A�C�e����I��
                if (FilteredItemTypes.Count > 0 && !FilteredItemTypes.Contains(SelectedItemType))
                {
                    SelectedItemType = FilteredItemTypes.First();
                }

                StatusMessage = $"{FilteredItemTypes.Count}�̃R�}���h���\������Ă��܂�";
                _logger.LogDebug("�R�}���h�t�B���^�[�K�p: {Count}�\��", FilteredItemTypes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h�t�B���^�[���ɃG���[���������܂���");
                StatusMessage = $"�t�B���^�[�G���[: {ex.Message}";
            }
        }

        #region �R�}���h����

        [RelayCommand]
        public void Add() 
        {
            try
            {
                if (SelectedItemType != null)
                {
                    _logger.LogDebug("�ǉ��R�}���h�𑗐M���܂�: {ItemType}", SelectedItemType.TypeName);
                    
                    // �o�b�`�ǉ��Ή�
                    for (int i = 0; i < BatchAddCount; i++)
                    {
                        WeakReferenceMessenger.Default.Send(new AddMessage(SelectedItemType.TypeName));
                    }
                    
                    // �ŋߎg�p�����R�}���h�ɒǉ�
                    AddToRecentCommands(SelectedItemType);
                    
                    StatusMessage = BatchAddCount > 1 
                        ? $"{SelectedItemType.DisplayName}��{BatchAddCount}�ǉ����܂���"
                        : $"{SelectedItemType.DisplayName}��ǉ����܂���";
                }
                else
                {
                    _logger.LogWarning("�A�C�e���^�C�v���I������Ă��܂���");
                    StatusMessage = "�R�}���h���I������Ă��܂���";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ǉ��R�}���h�̏������ɃG���[���������܂���");
                StatusMessage = $"�ǉ��G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void AddFromRecent(CommandDisplayItem recentCommand)
        {
            try
            {
                if (recentCommand != null)
                {
                    WeakReferenceMessenger.Default.Send(new AddMessage(recentCommand.TypeName));
                    StatusMessage = $"{recentCommand.DisplayName}��ǉ����܂����i�ŋߎg�p�j";
                    _logger.LogDebug("�ŋߎg�p�����R�}���h����ǉ�: {ItemType}", recentCommand.TypeName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ŋߎg�p�R�}���h�ǉ����ɃG���[���������܂���");
                StatusMessage = $"�ǉ��G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void AddFromFavorite(CommandDisplayItem favoriteCommand)
        {
            try
            {
                if (favoriteCommand != null)
                {
                    WeakReferenceMessenger.Default.Send(new AddMessage(favoriteCommand.TypeName));
                    StatusMessage = $"{favoriteCommand.DisplayName}��ǉ����܂����i���C�ɓ���j";
                    _logger.LogDebug("���C�ɓ���R�}���h����ǉ�: {ItemType}", favoriteCommand.TypeName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���C�ɓ���R�}���h�ǉ����ɃG���[���������܂���");
                StatusMessage = $"�ǉ��G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void AddToFavorites()
        {
            try
            {
                if (SelectedItemType != null && !FavoriteCommands.Any(f => f.TypeName == SelectedItemType.TypeName))
                {
                    FavoriteCommands.Add(SelectedItemType);
                    SaveFavoriteCommands();
                    StatusMessage = $"{SelectedItemType.DisplayName}�����C�ɓ���ɒǉ����܂���";
                    _logger.LogDebug("���C�ɓ���ɒǉ�: {ItemType}", SelectedItemType.TypeName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���C�ɓ���ǉ����ɃG���[���������܂���");
                StatusMessage = $"���C�ɓ���ǉ��G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void RemoveFromFavorites(CommandDisplayItem favoriteCommand)
        {
            try
            {
                if (favoriteCommand != null)
                {
                    FavoriteCommands.Remove(favoriteCommand);
                    SaveFavoriteCommands();
                    StatusMessage = $"{favoriteCommand.DisplayName}�����C�ɓ��肩��폜���܂���";
                    _logger.LogDebug("���C�ɓ��肩��폜: {ItemType}", favoriteCommand.TypeName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���C�ɓ���폜���ɃG���[���������܂���");
                StatusMessage = $"���C�ɓ���폜�G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Run()
        {
            try
            {
                if (IsRunning)
                {
                    _logger.LogInformation("��~�R�}���h�𑗐M���܂�");
                    WeakReferenceMessenger.Default.Send(new StopMessage());
                    StatusMessage = "�}�N�����~���Ă��܂�...";
                }
                else
                {
                    _logger.LogInformation("���s�R�}���h�𑗐M���܂�");
                    WeakReferenceMessenger.Default.Send(new RunMessage());
                    StatusMessage = "�}�N�������s���Ă��܂�...";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���s/��~�R�}���h�̏������ɃG���[���������܂���");
                StatusMessage = $"���s�G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Save() 
        {
            try
            {
                _logger.LogDebug("�ۑ��R�}���h�𑗐M���܂�");
                WeakReferenceMessenger.Default.Send(new SaveMessage());
                StatusMessage = "�t�@�C����ۑ����Ă��܂�...";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ۑ��R�}���h�̏������ɃG���[���������܂���");
                StatusMessage = $"�ۑ��G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Load() 
        {
            try
            {
                _logger.LogDebug("�ǂݍ��݃R�}���h�𑗐M���܂�");
                WeakReferenceMessenger.Default.Send(new LoadMessage());
                StatusMessage = "�t�@�C����ǂݍ���ł��܂�...";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ǂݍ��݃R�}���h�̏������ɃG���[���������܂���");
                StatusMessage = $"�ǂݍ��݃G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Clear() 
        {
            try
            {
                _logger.LogDebug("�N���A�R�}���h�𑗐M���܂�");
                WeakReferenceMessenger.Default.Send(new ClearMessage());
                StatusMessage = "�R�}���h���X�g���N���A���Ă��܂�...";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�N���A�R�}���h�̏������ɃG���[���������܂���");
                StatusMessage = $"�N���A�G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Up() 
        {
            try
            {
                _logger.LogDebug("��ړ��R�}���h�𑗐M���܂�");
                WeakReferenceMessenger.Default.Send(new UpMessage());
                StatusMessage = "�I�����ڂ���Ɉړ����܂���";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ړ��R�}���h�̏������ɃG���[���������܂���");
                StatusMessage = $"�ړ��G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Down() 
        {
            try
            {
                _logger.LogDebug("���ړ��R�}���h�𑗐M���܂�");
                WeakReferenceMessenger.Default.Send(new DownMessage());
                StatusMessage = "�I�����ڂ����Ɉړ����܂���";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���ړ��R�}���h�̏������ɃG���[���������܂���");
                StatusMessage = $"�ړ��G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Delete() 
        {
            try
            {
                _logger.LogDebug("�폜�R�}���h�𑗐M���܂�");
                WeakReferenceMessenger.Default.Send(new DeleteMessage());
                StatusMessage = "�I�����ڂ��폜���܂���";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�폜�R�}���h�̏������ɃG���[���������܂���");
                StatusMessage = $"�폜�G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Undo() 
        {
            try
            {
                _logger.LogDebug("���ɖ߂��R�}���h�𑗐M���܂�");
                WeakReferenceMessenger.Default.Send(new UndoMessage());
                StatusMessage = "��������ɖ߂��܂���";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���ɖ߂��R�}���h�̏������ɃG���[���������܂���");
                StatusMessage = $"Undo�G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void Redo() 
        {
            try
            {
                _logger.LogDebug("��蒼���R�}���h�𑗐M���܂�");
                WeakReferenceMessenger.Default.Send(new RedoMessage());
                StatusMessage = "�������蒼���܂���";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��蒼���R�}���h�̏������ɃG���[���������܂���");
                StatusMessage = $"Redo�G���[: {ex.Message}";
            }
        }

        [RelayCommand]
        public void ToggleAdvancedOptions()
        {
            ShowAdvancedOptions = !ShowAdvancedOptions;
            StatusMessage = ShowAdvancedOptions ? "���x�ȃI�v�V������\��" : "���x�ȃI�v�V�������\��";
        }

        [RelayCommand]
        public void ClearSearch()
        {
            SearchText = string.Empty;
            SelectedCategory = "���ׂ�";
            StatusMessage = "�����������N���A���܂���";
        }

        #endregion

        #region �ŋߎg�p�E���C�ɓ���Ǘ�

        private void AddToRecentCommands(CommandDisplayItem command)
        {
            try
            {
                // �����̃A�C�e�����폜
                var existing = RecentCommands.FirstOrDefault(r => r.TypeName == command.TypeName);
                if (existing != null)
                {
                    RecentCommands.Remove(existing);
                }

                // �擪�ɒǉ�
                RecentCommands.Insert(0, command);

                // �ő�10�܂ŕێ�
                while (RecentCommands.Count > 10)
                {
                    RecentCommands.RemoveAt(RecentCommands.Count - 1);
                }

                SaveRecentCommands();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ŋߎg�p�R�}���h�ǉ����ɃG���[���������܂���");
            }
        }

        private void LoadRecentCommands()
        {
            try
            {
                // TODO: �ݒ�t�@�C������ǂݍ���
                // ���݂͋�ŏ�����
                RecentCommands = new ObservableCollection<CommandDisplayItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ŋߎg�p�R�}���h�ǂݍ��ݒ��ɃG���[���������܂���");
                RecentCommands = new ObservableCollection<CommandDisplayItem>();
            }
        }

        private void SaveRecentCommands()
        {
            try
            {
                // TODO: �ݒ�t�@�C���ɕۑ�
                _logger.LogDebug("�ŋߎg�p�R�}���h��ۑ����܂���: {Count}��", RecentCommands.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ŋߎg�p�R�}���h�ۑ����ɃG���[���������܂���");
            }
        }

        private void LoadFavoriteCommands()
        {
            try
            {
                // TODO: �ݒ�t�@�C������ǂݍ���
                // ���݂͋�ŏ�����
                FavoriteCommands = new ObservableCollection<CommandDisplayItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���C�ɓ���R�}���h�ǂݍ��ݒ��ɃG���[���������܂���");
                FavoriteCommands = new ObservableCollection<CommandDisplayItem>();
            }
        }

        private void SaveFavoriteCommands()
        {
            try
            {
                // TODO: �ݒ�t�@�C���ɕۑ�
                _logger.LogDebug("���C�ɓ���R�}���h��ۑ����܂���: {Count}��", FavoriteCommands.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���C�ɓ���R�}���h�ۑ����ɃG���[���������܂���");
            }
        }

        #endregion

        #region ���̑�

        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            StatusMessage = isRunning ? "�}�N�����s��..." : "��������";
            _logger.LogDebug("���s��Ԃ�ݒ�: {IsRunning}", isRunning);
        }

        public void Prepare() 
        {
            _logger.LogDebug("ButtonPanelViewModel �̏��������s���܂�");
            StatusMessage = "��������";
        }

        /// <summary>
        /// ���p�\�ȃR�}���h���v���擾
        /// </summary>
        public CommandTypeStats GetCommandTypeStats()
        {
            return new CommandTypeStats
            {
                TotalTypes = ItemTypes.Count,
                CategoryStats = ItemTypes.GroupBy(i => i.Category).ToDictionary(g => g.Key, g => g.Count()),
                RecentCount = RecentCommands.Count,
                FavoriteCount = FavoriteCommands.Count
            };
        }

        #endregion
    }

    #region �⏕�N���X

    /// <summary>
    /// �R�}���h�^�C�v���v
    /// </summary>
    public class CommandTypeStats
    {
        public int TotalTypes { get; set; }
        public Dictionary<string, int> CategoryStats { get; set; } = new();
        public int RecentCount { get; set; }
        public int FavoriteCount { get; set; }
    }

    #endregion
}