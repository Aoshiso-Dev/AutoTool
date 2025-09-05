using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AutoTool.Services.Capture
{
    /// <summary>
    /// キャプチャサービスのインターフェース
    /// </summary>
    public interface ICaptureService
    {
        /// <summary>
        /// 右クリック位置の色を取得
        /// </summary>
        /// <returns>取得した色。キャンセル時はnull</returns>
        Task<Color?> CaptureColorAtRightClickAsync();

        /// <summary>
        /// 現在のマウス位置を取得
        /// </summary>
        /// <returns>マウス位置</returns>
        System.Drawing.Point GetCurrentMousePosition();

        /// <summary>
        /// 右クリック位置のウィンドウ情報を取得
        /// </summary>
        /// <returns>ウィンドウ情報。キャンセル時はnull</returns>
        Task<WindowCaptureResult?> CaptureWindowInfoAtRightClickAsync();

        /// <summary>
        /// 右クリック位置の座標を取得
        /// </summary>
        /// <returns>取得した座標。キャンセル時はnull</returns>
        Task<System.Drawing.Point?> CaptureCoordinateAtRightClickAsync();

        /// <summary>
        /// キーキャプチャを実行
        /// </summary>
        /// <param name="title">ダイアログタイトル</param>
        /// <returns>キャプチャしたキー。キャンセル時はnull</returns>
        Task<Key?> CaptureKeyAsync(string title);

        /// <summary>
        /// 指定座標の色を取得
        /// </summary>
        /// <param name="position">座標</param>
        /// <returns>色</returns>
        Color GetColorAt(System.Drawing.Point position);
    }

    /// <summary>
    /// ウィンドウキャプチャ結果
    /// </summary>
    public class WindowCaptureResult
    {
        public string Title { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public IntPtr Handle { get; set; } = IntPtr.Zero;

        public override string ToString()
        {
            return string.IsNullOrEmpty(ClassName) ? Title : $"{Title} ({ClassName})";
        }
    }
}