using System;
using System.Windows;
using System.Drawing;
using System.Threading.Tasks;

using Point = System.Windows.Point;

namespace AutoTool.Services.Abstractions;

/// <summary>
/// マウス操作サービスのインターフェース
/// </summary>
public interface IMouseService
{
    /// <summary>
    /// 背景クリック方式の種類
    /// </summary>
    public enum BackgroundClickMethod
    {
        /// <summary>SendMessage使用（同期的）</summary>
        SendMessage,
        /// <summary>PostMessage使用（非同期的）</summary>
        PostMessage,
        /// <summary>子ウィンドウも含めて自動検出</summary>
        AutoDetectChild,
        /// <summary>複数の方式を試行</summary>
        TryAll,
        /// <summary>ゲーム向け：DirectInput座標系使用</summary>
        GameDirectInput,
        /// <summary>ゲーム向け：フルスクリーン対応</summary>
        GameFullscreen,
        /// <summary>ゲーム向け：低レベルAPI使用</summary>
        GameLowLevel,
        /// <summary>ゲーム向け：仮想マウス</summary>
        GameVirtualMouse
    }

    // 既存のメソッド（後方互換性のため）
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

    // 新しいメソッド（MouseHelper移植）
    // マウス位置関連
    Point GetCurrentMousePosition();
    Point GetCurrentMousePosition(string windowTitle, string windowClassName = "");
    void SetMousePosition(int x, int y, string windowTitle = "", string windowClassName = "");

    // クリック操作（非同期）
    Task ClickAsync(int x, int y, string windowTitle = "", string windowClassName = "");
    Task RightClickAsync(int x, int y, string windowTitle = "", string windowClassName = "");
    Task MiddleClickAsync(int x, int y, string windowTitle = "", string windowClassName = "");

    // ドラッグ操作（非同期）
    Task DragAsync(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "");
    Task RightDragAsync(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "");
    Task MiddleDragAsync(int x1, int y1, int x2, int y2, string windowTitle = "", string windowClassName = "");

    // ホイール操作
    Task WheelAsync(int x, int y, int delta, string windowTitle = "", string windowClassName = "");
    Task HWheelAsync(int x, int y, int delta, string windowTitle = "", string windowClassName = "");

    // 背景クリック（非同期）
    Task BackgroundClickAsync(int x, int y, string windowTitle = "", string windowClassName = "", BackgroundClickMethod method = BackgroundClickMethod.SendMessage);
    Task BackgroundRightClickAsync(int x, int y, string windowTitle = "", string windowClassName = "", BackgroundClickMethod method = BackgroundClickMethod.SendMessage);
    Task BackgroundMiddleClickAsync(int x, int y, string windowTitle = "", string windowClassName = "", BackgroundClickMethod method = BackgroundClickMethod.SendMessage);

    // マウスイベント関連
    Task<Point?> WaitForRightClickWithTimeoutAsync(TimeSpan timeout = default);
    void StartMouseHook();
    void StopMouseHook();

    // マウスカーソルロック
    void LockCursor(int x, int y);
    void UnlockCursor();

    // 指定座標の色を取得
    Color GetColorAt(Point position);
}