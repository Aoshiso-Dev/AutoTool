using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace AutoTool.Services.Capture
{
    /// <summary>
    /// ウィンドウキャプチャ結果
    /// </summary>
    public class WindowCaptureResult
    {
        /// <summary>
        /// ウィンドウタイトル
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// ウィンドウクラス名
        /// </summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// ウィンドウハンドル
        /// </summary>
        public IntPtr Handle { get; set; } = IntPtr.Zero;

        /// <summary>
        /// 文字列表現
        /// </summary>
        /// <returns>ウィンドウ情報の文字列</returns>
        public override string ToString()
        {
            return $"Title: {Title}, ClassName: {ClassName}, Handle: {Handle}";
        }
    }

    /// <summary>
    /// キーキャプチャ結果
    /// </summary>
    public class KeyCaptureResult
    {
        /// <summary>
        /// キャプチャされたキー
        /// </summary>
        public Key Key { get; set; }

        /// <summary>
        /// Ctrlキーが押されているか
        /// </summary>
        public bool IsCtrlPressed { get; set; }

        /// <summary>
        /// Altキーが押されているか
        /// </summary>
        public bool IsAltPressed { get; set; }

        /// <summary>
        /// Shiftキーが押されているか
        /// </summary>
        public bool IsShiftPressed { get; set; }

        /// <summary>
        /// キーの表示テキスト
        /// </summary>
        public string DisplayText
        {
            get
            {
                var parts = new List<string>();
                if (IsCtrlPressed) parts.Add("Ctrl");
                if (IsAltPressed) parts.Add("Alt");
                if (IsShiftPressed) parts.Add("Shift");
                parts.Add(Key.ToString());
                return string.Join(" + ", parts);
            }
        }
    }
}