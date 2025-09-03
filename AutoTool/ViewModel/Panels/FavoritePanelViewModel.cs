using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// ���C�ɓ���p�l���r���[���f��
    /// </summary>
    public partial class FavoritePanelViewModel : ObservableObject
    {
        private readonly ILogger<FavoritePanelViewModel> _logger;

        [ObservableProperty]
        private bool _isRunning = false;

        public FavoritePanelViewModel(ILogger<FavoritePanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("FavoritePanelViewModel����������");
        }

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
            try
            {
                _logger.LogDebug("FavoritePanelViewModel �̏������������s���܂�");
                // �K�v�ɉ����ď���������ǉ�
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FavoritePanelViewModel �����������ɃG���[���������܂���");
            }
        }
    }
}