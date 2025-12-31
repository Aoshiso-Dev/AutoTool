using System.Windows.Media;

namespace MacroPanels.Command.Services;

/// <summary>
/// 画像検索サービスのインターフェース
/// </summary>
public interface IImageSearchService
{
    /// <summary>
    /// 指定された画像を画面上から検索します
    /// </summary>
    /// <param name="imagePath">検索対象の画像パス</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <param name="threshold">一致閾値（0.0?1.0）</param>
    /// <param name="searchColor">強調検索色（オプション）</param>
    /// <param name="windowTitle">対象ウィンドウのタイトル（オプション）</param>
    /// <param name="windowClassName">対象ウィンドウのクラス名（オプション）</param>
    /// <returns>見つかった場合は座標、見つからなかった場合はnull</returns>
    Task<OpenCvSharp.Point?> SearchImageAsync(
        string imagePath,
        CancellationToken cancellationToken,
        double threshold = 0.9,
        Color? searchColor = null,
        string? windowTitle = null,
        string? windowClassName = null);
}
