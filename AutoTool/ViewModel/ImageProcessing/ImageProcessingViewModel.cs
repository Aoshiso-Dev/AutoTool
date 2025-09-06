using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AutoTool.Services.ImageProcessing;
using OpenCvSharp;

namespace AutoTool.ViewModel.ImageProcessing
{
    /// <summary>
    /// 画像処理操作用ViewModel（WPF対応）
    /// </summary>
    public partial class ImageProcessingViewModel : ObservableObject, IDisposable
    {
        private readonly IImageProcessingService _imageProcessingService;
        private readonly ILogger<ImageProcessingViewModel> _logger;
        private CancellationTokenSource? _searchCancellation;

        #region Observable Properties

        [ObservableProperty]
        private BitmapSource? _capturedImage;

        [ObservableProperty]
        private BitmapSource? _templateImage;

        [ObservableProperty]
        private string _imagePath = string.Empty;

        [ObservableProperty]
        private string _windowTitle = string.Empty;

        [ObservableProperty]
        private string _windowClassName = string.Empty;

        [ObservableProperty]
        private double _matchThreshold = 0.8;

        [ObservableProperty]
        private bool _isProcessing = false;

        [ObservableProperty]
        private double _processingProgress = 0.0;

        [ObservableProperty]
        private string _statusMessage = "準備完了";

        [ObservableProperty]
        private System.Windows.Point? _foundPosition;

        [ObservableProperty]
        private ObservableCollection<ImageSearchResult> _searchResults = new();

        [ObservableProperty]
        private bool _showAdvancedOptions = false;

        [ObservableProperty]
        private bool _useColorFilter = false;

        [ObservableProperty]
        private System.Windows.Media.Color _filterColor = System.Windows.Media.Colors.Red;

        [ObservableProperty]
        private int _captureRegionX = 0;

        [ObservableProperty]
        private int _captureRegionY = 0;

        [ObservableProperty]
        private int _captureRegionWidth = 800;

        [ObservableProperty]
        private int _captureRegionHeight = 600;

        #endregion

        public ImageProcessingViewModel(IImageProcessingService imageProcessingService, ILogger<ImageProcessingViewModel> logger)
        {
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 進捗イベントを購読
            _imageProcessingService.ProgressChanged += OnProgressChanged;

            _logger.LogInformation("ImageProcessingViewModel が初期化されました");
        }

        #region Command Methods

        /// <summary>
        /// スクリーン全体をキャプチャ
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteImageProcessing))]
        private async Task CaptureScreenAsync()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "スクリーンをキャプチャ中...";

                var mat = await _imageProcessingService.CaptureScreenAsync();
                if (mat != null)
                {
                    CapturedImage = _imageProcessingService.MatToBitmapSource(mat);
                    StatusMessage = $"スクリーンキャプチャ完了: {mat.Width}x{mat.Height}";
                    _logger.LogInformation("スクリーンキャプチャ成功");
                    mat.Dispose();
                }
                else
                {
                    StatusMessage = "スクリーンキャプチャに失敗しました";
                    _logger.LogWarning("スクリーンキャプチャ失敗");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "スクリーンキャプチャエラー");
                StatusMessage = $"キャプチャエラー: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 指定ウィンドウをキャプチャ
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteImageProcessing))]
        private async Task CaptureWindowAsync()
        {
            if (string.IsNullOrWhiteSpace(WindowTitle))
            {
                StatusMessage = "ウィンドウタイトルを入力してください";
                return;
            }

            try
            {
                IsProcessing = true;
                StatusMessage = $"ウィンドウ '{WindowTitle}' をキャプチャ中...";

                var mat = await _imageProcessingService.CaptureWindowAsync(WindowTitle, WindowClassName);
                if (mat != null)
                {
                    CapturedImage = _imageProcessingService.MatToBitmapSource(mat);
                    StatusMessage = $"ウィンドウキャプチャ完了: {mat.Width}x{mat.Height}";
                    _logger.LogInformation("ウィンドウキャプチャ成功: {WindowTitle}", WindowTitle);
                    mat.Dispose();
                }
                else
                {
                    StatusMessage = $"ウィンドウ '{WindowTitle}' のキャプチャに失敗しました";
                    _logger.LogWarning("ウィンドウキャプチャ失敗: {WindowTitle}", WindowTitle);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウキャプチャエラー: {WindowTitle}", WindowTitle);
                StatusMessage = $"キャプチャエラー: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 指定領域をキャプチャ
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteImageProcessing))]
        private async Task CaptureRegionAsync()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "指定領域をキャプチャ中...";

                var region = new System.Windows.Rect(CaptureRegionX, CaptureRegionY, CaptureRegionWidth, CaptureRegionHeight);
                var mat = await _imageProcessingService.CaptureRegionAsync(region);
                if (mat != null)
                {
                    CapturedImage = _imageProcessingService.MatToBitmapSource(mat);
                    StatusMessage = $"領域キャプチャ完了: {mat.Width}x{mat.Height}";
                    _logger.LogInformation("領域キャプチャ成功: {Region}", region);
                    mat.Dispose();
                }
                else
                {
                    StatusMessage = "領域キャプチャに失敗しました";
                    _logger.LogWarning("領域キャプチャ失敗: {Region}", region);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "領域キャプチャエラー");
                StatusMessage = $"キャプチャエラー: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// テンプレート画像を読み込み
        /// </summary>
        [RelayCommand]
        private async Task LoadTemplateImageAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "テンプレート画像を選択",
                    Filter = "画像ファイル|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.gif;*.webp|すべてのファイル|*.*",
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    ImagePath = dialog.FileName;
                    
                    if (_imageProcessingService.IsValidImageFile(ImagePath))
                    {
                        // 画像をプレビュー表示
                        using (var mat = new Mat(ImagePath))
                        {
                            TemplateImage = _imageProcessingService.MatToBitmapSource(mat);
                        }
                        
                        StatusMessage = $"テンプレート画像を読み込みました: {Path.GetFileName(ImagePath)}";
                        _logger.LogInformation("テンプレート画像読み込み: {ImagePath}", ImagePath);
                    }
                    else
                    {
                        StatusMessage = "サポートされていない画像形式です";
                        ImagePath = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テンプレート画像読み込みエラー");
                StatusMessage = $"読み込みエラー: {ex.Message}";
            }
        }

        /// <summary>
        /// 画像検索を実行
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteImageProcessing))]
        private async Task SearchImageAsync()
        {
            if (string.IsNullOrWhiteSpace(ImagePath) || !_imageProcessingService.IsValidImageFile(ImagePath))
            {
                StatusMessage = "有効なテンプレート画像を選択してください";
                return;
            }

            try
            {
                IsProcessing = true;
                _searchCancellation = new CancellationTokenSource();
                StatusMessage = "画像を検索中...";

                System.Windows.Point? result;
                if (string.IsNullOrWhiteSpace(WindowTitle))
                {
                    if (UseColorFilter)
                    {
                        // フィルター色を System.Drawing.Color に変換
                        var filterColorDrawing = System.Drawing.Color.FromArgb(
                            FilterColor.A,
                            FilterColor.R,
                            FilterColor.G,
                            FilterColor.B);
                        
                        result = await _imageProcessingService.SearchImageWithColorFilterAsync(
                            ImagePath, filterColorDrawing, MatchThreshold, "", "", _searchCancellation.Token);
                    }
                    else
                    {
                        result = await _imageProcessingService.SearchImageOnScreenAsync(
                            ImagePath, MatchThreshold, _searchCancellation.Token);
                    }
                }
                else
                {
                    if (UseColorFilter)
                    {
                        // フィルター色を System.Drawing.Color に変換
                        var filterColorDrawing = System.Drawing.Color.FromArgb(
                            FilterColor.A,
                            FilterColor.R,
                            FilterColor.G,
                            FilterColor.B);
                        
                        result = await _imageProcessingService.SearchImageWithColorFilterAsync(
                            ImagePath, filterColorDrawing, MatchThreshold, WindowTitle, WindowClassName, _searchCancellation.Token);
                    }
                    else
                    {
                        result = await _imageProcessingService.SearchImageInWindowAsync(
                            ImagePath, WindowTitle, WindowClassName, MatchThreshold, _searchCancellation.Token);
                    }
                }

                if (result.HasValue)
                {
                    FoundPosition = result.Value;
                    StatusMessage = $"画像が見つかりました: ({result.Value.X:F0}, {result.Value.Y:F0})";
                    _logger.LogInformation("画像検索成功: {Position}", result.Value);
                }
                else
                {
                    FoundPosition = null;
                    StatusMessage = "画像が見つかりませんでした";
                    _logger.LogInformation("画像検索失敗: 閾値 {Threshold}", MatchThreshold);
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "画像検索がキャンセルされました";
                _logger.LogInformation("画像検索キャンセル");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "画像検索エラー");
                StatusMessage = $"検索エラー: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                _searchCancellation?.Dispose();
                _searchCancellation = null;
            }
        }

        /// <summary>
        /// 複数画像検索を実行
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteImageProcessing))]
        private async Task SearchMultipleImagesAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "検索する画像を複数選択",
                    Filter = "画像ファイル|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.gif;*.webp|すべてのファイル|*.*",
                    Multiselect = true
                };

                if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
                {
                    IsProcessing = true;
                    StatusMessage = $"{dialog.FileNames.Length}個の画像を検索中...";

                    var results = await _imageProcessingService.SearchMultipleImagesAsync(
                        dialog.FileNames, MatchThreshold, WindowTitle, WindowClassName);

                    SearchResults.Clear();
                    foreach (var result in results)
                    {
                        SearchResults.Add(result);
                    }

                    var foundCount = SearchResults.Count(r => r.IsFound);
                    StatusMessage = $"複数画像検索完了: {foundCount}/{SearchResults.Count}個が見つかりました";
                    _logger.LogInformation("複数画像検索完了: {Found}/{Total}", foundCount, SearchResults.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "複数画像検索エラー");
                StatusMessage = $"検索エラー: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// キャプチャ画像を保存
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteImageProcessing))]
        private async Task SaveCapturedImageAsync()
        {
            if (CapturedImage == null)
            {
                StatusMessage = "保存する画像がありません";
                return;
            }

            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "画像を保存",
                    Filter = "PNG画像|*.png|JPEG画像|*.jpg|BMP画像|*.bmp|すべてのファイル|*.*",
                    DefaultExt = "png"
                };

                if (dialog.ShowDialog() == true)
                {
                    IsProcessing = true;
                    StatusMessage = "画像を保存中...";

                    var mat = _imageProcessingService.BitmapSourceToMat(CapturedImage);
                    var success = await _imageProcessingService.SaveImageAsync(mat, dialog.FileName);
                    
                    if (success)
                    {
                        StatusMessage = $"画像を保存しました: {Path.GetFileName(dialog.FileName)}";
                        _logger.LogInformation("画像保存成功: {FilePath}", dialog.FileName);
                    }
                    else
                    {
                        StatusMessage = "画像保存に失敗しました";
                        _logger.LogWarning("画像保存失敗: {FilePath}", dialog.FileName);
                    }
                    
                    mat.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "画像保存エラー");
                StatusMessage = $"保存エラー: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 処理をキャンセル
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCancelProcessing))]
        private void CancelProcessing()
        {
            _searchCancellation?.Cancel();
            _imageProcessingService.CancelProcessing();
            StatusMessage = "処理がキャンセルされました";
            _logger.LogInformation("処理キャンセル");
        }

        /// <summary>
        /// 高度なオプションの表示切り替え
        /// </summary>
        [RelayCommand]
        private void ToggleAdvancedOptions()
        {
            ShowAdvancedOptions = !ShowAdvancedOptions;
        }

        /// <summary>
        /// 検索結果をクリア
        /// </summary>
        [RelayCommand]
        private void ClearSearchResults()
        {
            SearchResults.Clear();
            FoundPosition = null;
            StatusMessage = "検索結果をクリアしました";
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 進捗変更イベントハンドラ
        /// </summary>
        private void OnProgressChanged(object? sender, double progress)
        {
            ProcessingProgress = progress;
        }

        #endregion

        #region Can Execute Methods

        private bool CanExecuteImageProcessing => !IsProcessing;

        private bool CanCancelProcessing => IsProcessing;

        #endregion

        #region Property Changed Override

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // IsProcessingが変更されたときにコマンドの実行可能状態を更新
            if (e.PropertyName == nameof(IsProcessing))
            {
                CaptureScreenCommand.NotifyCanExecuteChanged();
                CaptureWindowCommand.NotifyCanExecuteChanged();
                CaptureRegionCommand.NotifyCanExecuteChanged();
                SearchImageCommand.NotifyCanExecuteChanged();
                SearchMultipleImagesCommand.NotifyCanExecuteChanged();
                SaveCapturedImageCommand.NotifyCanExecuteChanged();
                CancelProcessingCommand.NotifyCanExecuteChanged();
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            _searchCancellation?.Dispose();
            _imageProcessingService.ProgressChanged -= OnProgressChanged;
        }

        #endregion
    }
}