using System.Collections.Generic;
using System.Drawing;

namespace AutoTool.Desktop.Panels.Services;

/// <summary>
/// 検出領域を一時的に点滅表示してユーザーへ視覚フィードバックを返すサービスです。
/// </summary>
public interface IDetectionHighlightService
{
    /// <summary>
    /// 指定領域を点滅表示します。キャンセル時は途中で処理を終了します。
    /// </summary>
    /// <param name="bounds">強調表示する画面座標の矩形です。</param>
    /// <param name="cancellationToken">処理中断に使用するトークンです。</param>
    Task BlinkAsync(IReadOnlyList<Rectangle> bounds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定領域を点滅表示します。キャンセル時は途中で処理を終了します。
    /// </summary>
    /// <param name="bounds">強調表示する画面座標の矩形です。</param>
    /// <param name="cancellationToken">処理中断に使用するトークンです。</param>
    Task BlinkAsync(Rectangle bounds, CancellationToken cancellationToken = default)
        => BlinkAsync([bounds], cancellationToken);
}

