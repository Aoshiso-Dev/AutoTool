using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// 統合されたFavoritePanelViewModel（AutoTool.ViewModel名前空間）
    /// Phase 2: プロジェクト構造統合の一環として移動
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

        // レガシーサポート用コンストラクタ
        public FavoritePanelViewModel()
        {
            Initialize();
        }

        // DI対応コンストラクタ（AutoTool統合版）
        public FavoritePanelViewModel(ILogger<FavoritePanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("AutoTool統合FavoritePanelViewModel をDI対応で初期化しています");
            
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                _logger?.LogDebug("AutoTool統合FavoritePanelViewModel の初期化を開始します");
                
                CreateDefaultFavorites();
                
                _logger?.LogDebug("AutoTool統合FavoritePanelViewModel の初期化が完了しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AutoTool統合FavoritePanelViewModel の初期化中にエラーが発生しました");
                
                // フォールバック：デフォルトのお気に入りを作成
                CreateDefaultFavorites();
            }
        }

        private void CreateDefaultFavorites()
        {
            try
            {
                _logger?.LogDebug("デフォルトのお気に入りを作成します");
                
                FavoriteList.Clear();
                FavoriteList.Add("基本クリック操作");
                FavoriteList.Add("画像検索クリック");
                FavoriteList.Add("ホットキー送信");
                FavoriteList.Add("待機処理");
                
                _logger?.LogInformation("デフォルトのお気に入りを作成しました: {Count}件", FavoriteList.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "デフォルトお気に入り作成中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 実行状態を設定
        /// </summary>
        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
            _logger?.LogDebug("実行状態を設定: {IsRunning}", isRunning);
        }

        /// <summary>
        /// 準備処理
        /// </summary>
        public void Prepare()
        {
            _logger?.LogDebug("AutoTool統合FavoritePanelViewModel の準備処理を実行");
        }
    }
}