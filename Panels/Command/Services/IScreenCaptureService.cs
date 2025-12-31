namespace MacroPanels.Command.Services;

/// <summary>
/// スクリーンキャプチャサービスのインターフェース
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>
    /// スクリーンショットを保存します
    /// </summary>
    /// <param name="savePath">保存先パス</param>
    /// <param name="windowTitle">対象ウィンドウのタイトル（オプション、nullの場合は画面全体）</param>
    /// <param name="windowClassName">対象ウィンドウのクラス名（オプション）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task SaveScreenshotAsync(string savePath, string? windowTitle = null, string? windowClassName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// スクリーンショットをファイルに保存します（エイリアス）
    /// </summary>
    Task CaptureToFileAsync(string filePath, string? windowTitle = null, string? windowClassName = null, CancellationToken cancellationToken = default);
}
