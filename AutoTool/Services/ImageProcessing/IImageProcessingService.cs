using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp;

namespace AutoTool.Services.ImageProcessing
{
    /// <summary>
    /// 画像処理サービスのインターフェース（OpenCVHelper統合・WPF対応）
    /// </summary>
    public interface IImageProcessingService
    {
        #region Screen Capture Methods

        /// <summary>
        /// スクリーン全体をキャプチャします
        /// </summary>
        /// <returns>キャプチャされた画像のMat</returns>
        Task<Mat?> CaptureScreenAsync();

        /// <summary>
        /// 指定されたウィンドウをキャプチャします
        /// </summary>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名（オプション）</param>
        /// <returns>キャプチャされた画像のMat</returns>
        Task<Mat?> CaptureWindowAsync(string windowTitle, string windowClassName = "");

        /// <summary>
        /// 指定された領域をキャプチャします
        /// </summary>
        /// <param name="region">キャプチャする領域</param>
        /// <returns>キャプチャされた画像のMat</returns>
        Task<Mat?> CaptureRegionAsync(System.Windows.Rect region);

        /// <summary>
        /// 指定の領域をキャプチャして保存します
        /// </summary>
        /// <param name="region">キャプチャする領域</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>保存されたファイルパス、失敗の場合はnull</returns>
        Task<string?> CaptureRegionAsync(System.Windows.Rect region, CancellationToken cancellationToken = default);

        /// <summary>
        /// 指定されたウィンドウをキャプチャして保存します
        /// </summary>
        /// <param name="windowTitle">対象ウィンドウのタイトル</param>
        /// <param name="windowClassName">対象ウィンドウのクラス名（オプション）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>保存されたファイルパス、失敗の場合はnull</returns>
        Task<string?> CaptureWindowAsync(string windowTitle, string windowClassName = "", CancellationToken cancellationToken = default);

        /// <summary>
        /// ウィンドウをBitBltを使用してキャプチャします（隠れた部分も含む）
        /// </summary>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名（オプション）</param>
        /// <returns>キャプチャされた画像のMat</returns>
        Task<Mat?> CaptureWindowUsingBitBltAsync(string windowTitle, string windowClassName = "");

        #endregion

        #region Image Search Methods

        /// <summary>
        /// 色フィルタを使用して画像を検索します
        /// </summary>
        /// <param name="imagePath">検索する画像のパス</param>
        /// <param name="searchColor">検索色フィルタ</param>
        /// <param name="threshold">マッチング閾値（0.0-1.0）</param>
        /// <param name="windowTitle">検索対象のウィンドウタイトル（空の場合はスクリーン全体）</param>
        /// <param name="windowClassName">検索対象のウィンドウクラス名（オプション）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>発見された位置（中心点）、見つからない場合はnull</returns>
        Task<System.Windows.Point?> SearchImageWithColorFilterAsync(string imagePath, System.Drawing.Color searchColor, double threshold = 0.8, string windowTitle = "", string windowClassName = "", CancellationToken cancellationToken = default);

        /// <summary>
        /// スクリーン上で指定画像を検索します
        /// </summary>
        /// <param name="imagePath">検索する画像のパス</param>
        /// <param name="threshold">マッチング閾値（0.0-1.0）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>発見された位置（中心点）、見つからない場合はnull</returns>
        Task<System.Windows.Point?> SearchImageOnScreenAsync(string imagePath, double threshold = 0.8, CancellationToken cancellationToken = default);

        /// <summary>
        /// 指定ウィンドウ内で画像を検索します
        /// </summary>
        /// <param name="imagePath">検索する画像のパス</param>
        /// <param name="windowTitle">検索対象のウィンドウタイトル</param>
        /// <param name="windowClassName">検索対象のウィンドウクラス名（オプション）</param>
        /// <param name="threshold">マッチング閾値（0.0-1.0）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>発見された位置（中心点）、見つからない場合はnull</returns>
        Task<System.Windows.Point?> SearchImageInWindowAsync(string imagePath, string windowTitle, string windowClassName = "", double threshold = 0.8, CancellationToken cancellationToken = default);

        /// <summary>
        /// 複数の画像を同時に検索します
        /// </summary>
        /// <param name="imagePaths">検索する画像のパスリスト</param>
        /// <param name="threshold">マッチング閾値（0.0-1.0）</param>
        /// <param name="windowTitle">検索対象のウィンドウタイトル（空の場合はスクリーン全体）</param>
        /// <param name="windowClassName">検索対象のウィンドウクラス名（オプション）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>発見された画像の情報リスト</returns>
        Task<List<ImageSearchResult>> SearchMultipleImagesAsync(IEnumerable<string> imagePaths, double threshold = 0.8, string windowTitle = "", string windowClassName = "", CancellationToken cancellationToken = default);

        /// <summary>
        /// 指定の色に近い色を検索します
        /// </summary>
        /// <param name="targetColor">検索対象の色</param>
        /// <param name="tolerance">許容差 (0-255)</param>
        /// <param name="windowTitle">検索対象のウィンドウタイトル（空の場合は画面全体）</param>
        /// <param name="windowClassName">検索対象のウィンドウクラス名（オプション）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>見つかった位置（中心点）、見つからない場合はnull</returns>
        Task<System.Windows.Point?> SearchColorAsync(System.Drawing.Color targetColor, int tolerance = 10, string windowTitle = "", string windowClassName = "", CancellationToken cancellationToken = default);

        #endregion

        #region Image Processing Methods

        /// <summary>
        /// 画像をファイルに保存します
        /// </summary>
        /// <param name="image">保存する画像</param>
        /// <param name="filePath">保存先ファイルパス</param>
        /// <returns>保存が成功したかどうか</returns>
        Task<bool> SaveImageAsync(Mat image, string filePath);

        /// <summary>
        /// MatをBitmapSourceに変換します（WPF表示用）
        /// </summary>
        /// <param name="mat">変換するMat</param>
        /// <returns>変換されたBitmapSource</returns>
        BitmapSource MatToBitmapSource(Mat mat);

        /// <summary>
        /// BitmapSourceをMatに変換します
        /// </summary>
        /// <param name="bitmapSource">変換するBitmapSource</param>
        /// <returns>変換されたMat</returns>
        Mat BitmapSourceToMat(BitmapSource bitmapSource);

        /// <summary>
        /// 画像のサイズを変更します
        /// </summary>
        /// <param name="source">元の画像</param>
        /// <param name="newSize">新しいサイズ</param>
        /// <param name="interpolation">補間方法</param>
        /// <returns>リサイズされた画像</returns>
        Mat ResizeImage(Mat source, System.Drawing.Size newSize, InterpolationFlags interpolation = InterpolationFlags.Linear);

        /// <summary>
        /// 画像をグレースケールに変換します
        /// </summary>
        /// <param name="source">元の画像</param>
        /// <returns>グレースケール画像</returns>
        Mat ConvertToGrayscale(Mat source);

        /// <summary>
        /// 画像の色空間を変換します
        /// </summary>
        /// <param name="source">元の画像</param>
        /// <param name="colorConversion">色変換コード</param>
        /// <returns>変換された画像</returns>
        Mat ConvertColor(Mat source, ColorConversionCodes colorConversion);

        #endregion

        #region Utility Methods

        /// <summary>
        /// 指定されたファイルパスが有効な画像ファイルかどうかを確認します
        /// </summary>
        /// <param name="filePath">確認するファイルパス</param>
        /// <returns>有効な画像ファイルかどうか</returns>
        bool IsValidImageFile(string filePath);

        /// <summary>
        /// サポートされている画像形式の拡張子を取得します
        /// </summary>
        /// <returns>サポートされている拡張子のリスト</returns>
        IEnumerable<string> GetSupportedImageExtensions();

        /// <summary>
        /// 画像処理が現在アクティブかどうかを取得します
        /// </summary>
        bool IsProcessingActive { get; }

        /// <summary>
        /// 画像処理をキャンセルします
        /// </summary>
        void CancelProcessing();

        /// <summary>
        /// 現在の処理進捗を取得します（0.0-1.0）
        /// </summary>
        double ProcessingProgress { get; }

        /// <summary>
        /// 処理進捗が変更されたときのイベント
        /// </summary>
        event EventHandler<double> ProgressChanged;

        #endregion
    }

    /// <summary>
    /// 画像検索結果
    /// </summary>
    public class ImageSearchResult
    {
        /// <summary>
        /// 検索した画像のファイルパス
        /// </summary>
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>
        /// 発見された位置（中心点）
        /// </summary>
        public System.Windows.Point? FoundPosition { get; set; }

        /// <summary>
        /// マッチング度合い（0.0-1.0）
        /// </summary>
        public double MatchScore { get; set; }

        /// <summary>
        /// 検索にかかった時間
        /// </summary>
        public TimeSpan SearchDuration { get; set; }

        /// <summary>
        /// 検索が成功したかどうか
        /// </summary>
        public bool IsFound => FoundPosition.HasValue;

        /// <summary>
        /// 画像ファイル名（パスから抽出）
        /// </summary>
        public string ImageFileName => System.IO.Path.GetFileName(ImagePath);

        public override string ToString()
        {
            if (IsFound)
            {
                return $"{ImageFileName}: Found at ({FoundPosition.Value.X}, {FoundPosition.Value.Y}) - Score: {MatchScore:F3}";
            }
            return $"{ImageFileName}: Not found";
        }
    }
}