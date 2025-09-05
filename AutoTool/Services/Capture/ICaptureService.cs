using System;
using System.Collections.Generic;
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
        /// <returns>キャプチャしたキー情報。キャンセル時はnull</returns>
        Task<KeyCaptureResult?> CaptureKeyAsync(string title);

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

    /// <summary>
    /// キーキャプチャ結果
    /// </summary>
    public class KeyCaptureResult
    {
        public Key Key { get; set; }
        public bool IsCtrlPressed { get; set; }
        public bool IsAltPressed { get; set; }
        public bool IsShiftPressed { get; set; }

        /// <summary>
        /// キーの表示用文字列を取得
        /// </summary>
        public string DisplayText
        {
            get
            {
                var parts = new List<string>();

                if (IsCtrlPressed) parts.Add("Ctrl");
                if (IsAltPressed) parts.Add("Alt");
                if (IsShiftPressed) parts.Add("Shift");
                
                parts.Add(GetKeyDisplayName(Key));

                return string.Join(" + ", parts);
            }
        }

        private static string GetKeyDisplayName(Key key)
        {
            return key switch
            {
                // ファンクションキー
                Key.F1 => "F1", Key.F2 => "F2", Key.F3 => "F3", Key.F4 => "F4",
                Key.F5 => "F5", Key.F6 => "F6", Key.F7 => "F7", Key.F8 => "F8",
                Key.F9 => "F9", Key.F10 => "F10", Key.F11 => "F11", Key.F12 => "F12",

                // 数字キー
                Key.D0 => "0", Key.D1 => "1", Key.D2 => "2", Key.D3 => "3", Key.D4 => "4",
                Key.D5 => "5", Key.D6 => "6", Key.D7 => "7", Key.D8 => "8", Key.D9 => "9",

                // 特殊キー
                Key.Space => "Space", Key.Enter => "Enter", Key.Escape => "Escape",
                Key.Tab => "Tab", Key.Back => "Backspace", Key.Delete => "Delete",

                // 矢印キー
                Key.Up => "↑", Key.Down => "↓", Key.Left => "←", Key.Right => "→",

                // その他
                _ => key.ToString()
            };
        }

        public override string ToString() => DisplayText;
    }
}