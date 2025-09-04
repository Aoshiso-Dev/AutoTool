using System;
using System.Drawing;
using System.Threading.Tasks;

namespace AutoTool.Services.Mouse
{
    /// <summary>
    /// マウス操作サービスのインターフェース
    /// </summary>
    public interface IMouseService
    {
        /// <summary>
        /// 現在のマウス位置を取得（スクリーン座標）
        /// </summary>
        /// <returns>マウス位置</returns>
        Point GetCurrentPosition();

        /// <summary>
        /// 指定されたウィンドウでのクライアント座標を取得
        /// </summary>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名（オプション）</param>
        /// <returns>クライアント座標、ウィンドウが見つからない場合はスクリーン座標</returns>
        Point GetClientPosition(string windowTitle, string? windowClassName = null);

        /// <summary>
        /// 右クリック待機モードを開始（非同期）
        /// </summary>
        /// <param name="windowTitle">対象ウィンドウタイトル（オプション）</param>
        /// <param name="windowClassName">対象ウィンドウクラス名（オプション）</param>
        /// <returns>右クリックされた座標</returns>
        Task<Point> WaitForRightClickAsync(string? windowTitle = null, string? windowClassName = null);

        /// <summary>
        /// 右クリック待機をキャンセル
        /// </summary>
        void CancelRightClickWait();
    }
}