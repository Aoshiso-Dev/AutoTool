using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// お気に入りパネルビューモデル
    /// </summary>
    public partial class FavoritePanelViewModel : ObservableObject
    {
        private readonly ILogger<FavoritePanelViewModel> _logger;

        [ObservableProperty]
        private bool _isRunning = false;

        public FavoritePanelViewModel(ILogger<FavoritePanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("FavoritePanelViewModel初期化完了");
        }

        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            _logger.LogDebug("実行状態を設定: {IsRunning}", isRunning);
        }

        /// <summary>
        /// 準備処理
        /// </summary>
        public void Prepare()
        {
            try
            {
                _logger.LogDebug("FavoritePanelViewModel の準備処理を実行します");
                // 必要に応じて準備処理を追加
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FavoritePanelViewModel 準備処理中にエラーが発生しました");
            }
        }
    }
}