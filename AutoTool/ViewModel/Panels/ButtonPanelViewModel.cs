using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using AutoTool.Model.CommandDefinition;
using AutoTool.ViewModel.Shared;
using AutoTool.Message;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// �������ꂽButtonPanelViewModel�iAutoTool.ViewModel���O��ԁj
    /// Phase 3: ���S���������ŁA����CommandDisplayItem�g�p
    /// </summary>
    public partial class ButtonPanelViewModel : ObservableObject
    {
        private readonly ILogger<ButtonPanelViewModel> _logger;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ObservableCollection<AutoTool.ViewModel.Shared.CommandDisplayItem> _itemTypes = new();

        [ObservableProperty]
        private AutoTool.ViewModel.Shared.CommandDisplayItem? _selectedItemType;

        // DI�Ή��R���X�g���N�^�iPhase 3���S�����Łj
        public ButtonPanelViewModel(ILogger<ButtonPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("Phase 3���S����ButtonPanelViewModel ��DI�Ή��ŏ��������Ă��܂�");
            
            InitializeItemTypes();
        }

        private void InitializeItemTypes()
        {
            try
            {
                _logger.LogDebug("�A�C�e���^�C�v�̏��������J�n���܂�");

                // CommandRegistry��������
                AutoTool.Model.CommandDefinition.CommandRegistry.Initialize();
                _logger.LogDebug("CommandRegistry.Initialize() ����");

                // ������CommandDisplayItem���g�p���ăA�C�e���^�C�v���擾
                var orderedTypeNames = AutoTool.Model.CommandDefinition.CommandRegistry.GetOrderedTypeNames();
                _logger.LogDebug("GetOrderedTypeNames()�Ŏ擾�����^����: {Count}", orderedTypeNames?.Count() ?? 0);

                var displayItems = orderedTypeNames?
                    .Select(typeName => new CommandDisplayItem // AutoTool.ViewModel.Shared�ł��g�p
                    {
                        TypeName = typeName,
                        DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList() ?? new List<CommandDisplayItem>();
                
                _logger.LogDebug("�쐬����displayItems��: {Count}", displayItems.Count);
                
                ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
                SelectedItemType = ItemTypes.FirstOrDefault();
                
                _logger.LogDebug("Phase 3���S����ButtonPanelViewModel���������������܂���: {Count}��", ItemTypes.Count);
                _logger.LogDebug("�I�����ꂽ�A�C�e��: {DisplayName}", SelectedItemType?.DisplayName ?? "�Ȃ�");
                
                // �f�o�b�O�p�F�ŏ��̐����ڂ����O�o��
                for (int i = 0; i < Math.Min(5, ItemTypes.Count); i++)
                {
                    var item = ItemTypes[i];
                    _logger.LogDebug("ItemType[{Index}]: {DisplayName} ({TypeName})", i, item.DisplayName, item.TypeName);
                }

                // ����ɏڍׂȃf�o�b�O���
                if (ItemTypes.Count == 0)
                {
                    _logger.LogError("ItemTypes����ł��ICommandRegistry�̏�Ԃ��m�F���܂��B");
                    
                    // CommandRegistry�̏�Ԃ��`�F�b�N
                    var allTypes = CommandRegistry.GetOrderedTypeNames();
                    if (allTypes == null)
                    {
                        _logger.LogError("CommandRegistry.GetOrderedTypeNames() �� null ��Ԃ��܂���");
                    }
                    else
                    {
                        _logger.LogError("CommandRegistry.GetOrderedTypeNames() �� {Count} �̌^��Ԃ��܂���", allTypes.Count());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���^�C�v�̏��������ɃG���[���������܂���");
                throw;
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
                }
                else
                {
                    _logger.LogInformation("���s�R�}���h�𑗐M���܂�");
                    WeakReferenceMessenger.Default.Send(new RunMessage());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���s/��~�R�}���h�̏������ɃG���[���������܂���");
            }
        }

        [RelayCommand]
        public void Save() 
        {
            _logger.LogDebug("�ۑ��R�}���h�𑗐M���܂�");
            WeakReferenceMessenger.Default.Send(new SaveMessage());
        }

        [RelayCommand]
        public void Load() 
        {
            _logger.LogDebug("�ǂݍ��݃R�}���h�𑗐M���܂�");
            WeakReferenceMessenger.Default.Send(new LoadMessage());
        }

        [RelayCommand]
        public void Clear() 
        {
            _logger.LogDebug("�N���A�R�}���h�𑗐M���܂�");
            WeakReferenceMessenger.Default.Send(new ClearMessage());
        }

        [RelayCommand]
        public void Add() 
        {
            try
            {
                if (SelectedItemType != null)
                {
                    _logger.LogDebug("�ǉ��R�}���h�𑗐M���܂�: {ItemType}", SelectedItemType.TypeName);
                    WeakReferenceMessenger.Default.Send(new AddMessage(SelectedItemType.TypeName));
                }
                else
                {
                    _logger.LogWarning("�A�C�e���^�C�v���I������Ă��܂���");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ǉ��R�}���h�̏������ɃG���[���������܂���");
            }
        }

        [RelayCommand]
        public void Up() 
        {
            _logger.LogDebug("��ړ��R�}���h�𑗐M���܂�");
            WeakReferenceMessenger.Default.Send(new UpMessage());
        }

        [RelayCommand]
        public void Down() 
        {
            _logger.LogDebug("���ړ��R�}���h�𑗐M���܂�");
            WeakReferenceMessenger.Default.Send(new DownMessage());
        }

        [RelayCommand]
        public void Delete() 
        {
            _logger.LogDebug("�폜�R�}���h�𑗐M���܂�");
            WeakReferenceMessenger.Default.Send(new DeleteMessage());
        }

        [RelayCommand]
        public void Undo() 
        {
            try
            {
                _logger.LogDebug("���ɖ߂��R�}���h�𑗐M���܂�");
                WeakReferenceMessenger.Default.Send(new UndoMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���ɖ߂��R�}���h�̏������ɃG���[���������܂���");
            }
        }

        [RelayCommand]
        public void Redo() 
        {
            try
            {
                _logger.LogDebug("��蒼���R�}���h�𑗐M���܂�");
                WeakReferenceMessenger.Default.Send(new RedoMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��蒼���R�}���h�̏������ɃG���[���������܂���");
            }
        }

        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            _logger.LogDebug("���s��Ԃ�ݒ�: {IsRunning}", isRunning);
        }

        public void Prepare() 
        {
            _logger.LogDebug("Phase 3���S����ButtonPanelViewModel �̏��������s���܂�");
        }
    }
}