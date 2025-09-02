using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// �������ꂽFavoritePanelViewModel�iAutoTool.ViewModel���O��ԁj
    /// Phase 2: �v���W�F�N�g�\�������̈�Ƃ��Ĉړ�
    /// </summary>
    public partial class FavoritePanelViewModel : ObservableObject
    {
        private readonly ILogger<FavoritePanelViewModel>? _logger;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ObservableCollection<string> _favoriteList = new();

        [ObservableProperty]
        private string? _selectedFavorite;

        // ���K�V�[�T�|�[�g�p�R���X�g���N�^
        public FavoritePanelViewModel()
        {
            Initialize();
        }

        // DI�Ή��R���X�g���N�^�iAutoTool�����Łj
        public FavoritePanelViewModel(ILogger<FavoritePanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("AutoTool����FavoritePanelViewModel ��DI�Ή��ŏ��������Ă��܂�");
            
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                _logger?.LogDebug("AutoTool����FavoritePanelViewModel �̏��������J�n���܂�");
                
                CreateDefaultFavorites();
                
                _logger?.LogDebug("AutoTool����FavoritePanelViewModel �̏��������������܂���");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AutoTool����FavoritePanelViewModel �̏��������ɃG���[���������܂���");
                
                // �t�H�[���o�b�N�F�f�t�H���g�̂��C�ɓ�����쐬
                CreateDefaultFavorites();
            }
        }

        private void CreateDefaultFavorites()
        {
            try
            {
                _logger?.LogDebug("�f�t�H���g�̂��C�ɓ�����쐬���܂�");
                
                FavoriteList.Clear();
                FavoriteList.Add("��{�N���b�N����");
                FavoriteList.Add("�摜�����N���b�N");
                FavoriteList.Add("�z�b�g�L�[���M");
                FavoriteList.Add("�ҋ@����");
                
                _logger?.LogInformation("�f�t�H���g�̂��C�ɓ�����쐬���܂���: {Count}��", FavoriteList.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "�f�t�H���g���C�ɓ���쐬���ɃG���[���������܂���");
            }
        }

        /// <summary>
        /// ���s��Ԃ�ݒ�
        /// </summary>
        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
            _logger?.LogDebug("���s��Ԃ�ݒ�: {IsRunning}", isRunning);
        }

        /// <summary>
        /// ��������
        /// </summary>
        public void Prepare()
        {
            _logger?.LogDebug("AutoTool����FavoritePanelViewModel �̏������������s");
        }
    }
}