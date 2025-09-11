using System.Drawing;

namespace AutoTool.Services.Abstractions;

/// <summary>
/// 画面キャプチャサービスのインターフェース
/// </summary>
public interface ICaptureService
{
    /// <summary>
    /// 全画面のスクリーンショットを取得します
    /// </summary>
    Task<Bitmap> CaptureScreenAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 指定された領域のスクリーンショットを取得します
    /// </summary>
    Task<Bitmap> CaptureRegionAsync(Rectangle region, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 指定された座標の色を取得します
    /// </summary>
    Color GetColorAt(int x, int y);
    
    /// <summary>
    /// 現在のマウス位置の色を取得します
    /// </summary>
    Color GetColorAtMousePosition();
    
    /// <summary>
    /// スクリーンカラーピッカーを表示します
    /// </summary>
    Task<Color?> ShowColorPickerAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 指定された画像を画面上で検索します
    /// </summary>
    Task<Point?> FindImageAsync(Bitmap targetImage, double threshold = 0.9, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 指定された画像を画面上で検索し、すべての一致する位置を返します
    /// </summary>
    Task<Point[]> FindAllImagesAsync(Bitmap targetImage, double threshold = 0.9, CancellationToken cancellationToken = default);
}