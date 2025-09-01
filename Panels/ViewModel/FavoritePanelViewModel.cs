using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MacroPanels.Message;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;

namespace MacroPanels.ViewModel
{
    /// <summary>
    /// お気に入りマクロを管理するViewModel（DI対応）
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

        // DI対応コンストラクタ
        public FavoritePanelViewModel(ILogger<FavoritePanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("FavoritePanelViewModel をDI対応で初期化しています");
            
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                _logger?.LogDebug("FavoritePanelViewModel の初期化を開始します");
                
                CreateDefaultFavorites();
                
                _logger?.LogDebug("FavoritePanelViewModel の初期化が完了しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "FavoritePanelViewModel の初期化中にエラーが発生しました");
                
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
            _logger?.LogDebug("FavoritePanelViewModel の準備を実行します");
        }

        /// <summary>
        /// お気に入りを追加
        /// </summary>
        [RelayCommand]
        public void AddFavorite()
        {
            try
            {
                var newFavorite = $"新しいお気に入り {DateTime.Now:HH:mm:ss}";
                FavoriteList.Add(newFavorite);
                
                _logger?.LogInformation("お気に入りを追加しました: {Name}", newFavorite);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "お気に入り追加中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 選択されたお気に入りを削除
        /// </summary>
        [RelayCommand]
        public void RemoveFavorite()
        {
            if (SelectedFavorite == null)
            {
                _logger?.LogDebug("削除対象のお気に入りが選択されていません");
                return;
            }

            try
            {
                var itemName = SelectedFavorite;
                FavoriteList.Remove(SelectedFavorite);
                SelectedFavorite = null;
                
                _logger?.LogInformation("お気に入りを削除しました: {Name}", itemName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "お気に入り削除中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 選択されたお気に入りを実行
        /// </summary>
        [RelayCommand]
        public void ExecuteFavorite()
        {
            if (SelectedFavorite == null)
            {
                _logger?.LogDebug("実行対象のお気に入りが選択されていません");
                return;
            }

            try
            {
                _logger?.LogInformation("お気に入りを実行します: {Name}", SelectedFavorite);
                
                // 基本的な実行メッセージを送信
                WeakReferenceMessenger.Default.Send(new RunMessage());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "お気に入り実行中にエラーが発生しました: {Name}", SelectedFavorite);
            }
        }
    }
}