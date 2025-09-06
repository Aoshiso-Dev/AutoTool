using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Keyboard
{
    /// <summary>
    /// キーボード操作サービスの実装
    /// </summary>
    public class KeyboardService : IKeyboardService
    {
        private readonly ILogger<KeyboardService> _logger;

        public KeyboardService(ILogger<KeyboardService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Win32 API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        // 定数
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const uint WM_CHAR = 0x0102;

        private const int VK_MENU = 0x12;     // ALT
        private const int VK_CONTROL = 0x11;  // CTRL
        private const int VK_SHIFT = 0x10;    // SHIFT

        #endregion

        #region 基本キー操作

        /// <summary>
        /// 指定されたキーを押します
        /// </summary>
        public void KeyPress(Key key)
        {
            try
            {
                KeyDown(key);
                KeyUp(key);

                _logger.LogDebug("キー押下: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "キー押下エラー: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// 修飾キーと組み合わせてキーを押します
        /// </summary>
        public void KeyPress(Key key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            try
            {
                // 修飾キーを押下
                if (ctrl) KeyDown(Key.LeftCtrl);
                if (alt) KeyDown(Key.LeftAlt);
                if (shift) KeyDown(Key.LeftShift);

                // メインキーを押下
                KeyPress(key);

                // 修飾キーを離す
                if (ctrl) KeyUp(Key.LeftCtrl);
                if (alt) KeyUp(Key.LeftAlt);
                if (shift) KeyUp(Key.LeftShift);

                var hotkeyText = GetHotkeyString(key, ctrl, alt, shift);
                _logger.LogDebug("ホットキー押下: {Hotkey}", hotkeyText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ホットキー押下エラー: {Key}, Ctrl:{Ctrl}, Alt:{Alt}, Shift:{Shift}", 
                    key, ctrl, alt, shift);
                throw;
            }
        }

        /// <summary>
        /// 特定のウィンドウに対してキーを送信します
        /// </summary>
        public void KeyPress(Key key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "")
        {
            try
            {
                // ウィンドウが指定されていない場合はグローバルキー送信
                if (string.IsNullOrEmpty(windowTitle) && string.IsNullOrEmpty(windowClassName))
                {
                    KeyPress(key, ctrl, alt, shift);
                    return;
                }

                // ウィンドウハンドルを取得
                var hWnd = GetWindowHandle(windowTitle, windowClassName);

                // ウィンドウに対してキーダウン送信
                if (ctrl) SendMessage(hWnd, WM_KEYDOWN, (IntPtr)VK_CONTROL, IntPtr.Zero);
                if (alt) SendMessage(hWnd, WM_KEYDOWN, (IntPtr)VK_MENU, IntPtr.Zero);
                if (shift) SendMessage(hWnd, WM_KEYDOWN, (IntPtr)VK_SHIFT, IntPtr.Zero);

                var virtualKey = KeyInterop.VirtualKeyFromKey(key);
                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)virtualKey, IntPtr.Zero);

                // 少し待機
                Thread.Sleep(50);

                // ウィンドウに対してキーアップ送信
                SendMessage(hWnd, WM_KEYUP, (IntPtr)virtualKey, IntPtr.Zero);

                if (ctrl) SendMessage(hWnd, WM_KEYUP, (IntPtr)VK_CONTROL, IntPtr.Zero);
                if (alt) SendMessage(hWnd, WM_KEYUP, (IntPtr)VK_MENU, IntPtr.Zero);
                if (shift) SendMessage(hWnd, WM_KEYUP, (IntPtr)VK_SHIFT, IntPtr.Zero);

                var hotkeyText = GetHotkeyString(key, ctrl, alt, shift);
                _logger.LogDebug("ウィンドウキー送信: {Hotkey} -> {WindowTitle}[{WindowClassName}]", 
                    hotkeyText, windowTitle, windowClassName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウキー送信エラー: {Key}, Window:{WindowTitle}", key, windowTitle);
                throw;
            }
        }

        #endregion

        #region 非同期バージョン

        /// <summary>
        /// 指定されたキーを非同期で押します
        /// </summary>
        public async Task KeyPressAsync(Key key)
        {
            await Task.Run(() => KeyPress(key));
        }

        /// <summary>
        /// 修飾キーと組み合わせてキーを非同期で押します
        /// </summary>
        public async Task KeyPressAsync(Key key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            await Task.Run(() => KeyPress(key, ctrl, alt, shift));
        }

        /// <summary>
        /// 特定のウィンドウに対してキーを非同期で送信します
        /// </summary>
        public async Task KeyPressAsync(Key key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "")
        {
            await Task.Run(() => KeyPress(key, ctrl, alt, shift, windowTitle, windowClassName));
        }

        #endregion

        #region 低レベル操作

        /// <summary>
        /// キーを押下します
        /// </summary>
        public void KeyDown(Key key)
        {
            try
            {
                var virtualKey = KeyInterop.VirtualKeyFromKey(key);
                keybd_event((byte)virtualKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "キーダウンエラー: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// キーを離します
        /// </summary>
        public void KeyUp(Key key)
        {
            try
            {
                var virtualKey = KeyInterop.VirtualKeyFromKey(key);
                keybd_event((byte)virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "キーアップエラー: {Key}", key);
                throw;
            }
        }

        #endregion

        #region テキスト入力

        /// <summary>
        /// テキストを入力します
        /// </summary>
        public async Task TypeTextAsync(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text)) return;

                foreach (char c in text)
                {
                    if (char.IsControl(c))
                    {
                        // 制御文字の処理
                        switch (c)
                        {
                            case '\n':
                                KeyPress(Key.Enter);
                                break;
                            case '\t':
                                KeyPress(Key.Tab);
                                break;
                            case '\b':
                                KeyPress(Key.Back);
                                break;
                            // その他の制御文字は無視
                        }
                    }
                    else
                    {
                        // 通常の文字はUnicodeで送信
                        await SendCharAsync(c);
                    }

                    // 文字間の遅延
                    await Task.Delay(10);
                }

                _logger.LogDebug("テキスト入力完了: {Length}文字", text.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テキスト入力エラー: {Text}", text);
                throw;
            }
        }

        /// <summary>
        /// 特定のウィンドウにテキストを入力します
        /// </summary>
        public async Task TypeTextAsync(string text, string windowTitle, string windowClassName = "")
        {
            try
            {
                if (string.IsNullOrEmpty(text)) return;

                var hWnd = GetWindowHandle(windowTitle, windowClassName);

                foreach (char c in text)
                {
                    if (char.IsControl(c))
                    {
                        // 制御文字の処理
                        switch (c)
                        {
                            case '\n':
                                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)KeyInterop.VirtualKeyFromKey(Key.Enter), IntPtr.Zero);
                                await Task.Delay(10);
                                SendMessage(hWnd, WM_KEYUP, (IntPtr)KeyInterop.VirtualKeyFromKey(Key.Enter), IntPtr.Zero);
                                break;
                            case '\t':
                                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)KeyInterop.VirtualKeyFromKey(Key.Tab), IntPtr.Zero);
                                await Task.Delay(10);
                                SendMessage(hWnd, WM_KEYUP, (IntPtr)KeyInterop.VirtualKeyFromKey(Key.Tab), IntPtr.Zero);
                                break;
                            case '\b':
                                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)KeyInterop.VirtualKeyFromKey(Key.Back), IntPtr.Zero);
                                await Task.Delay(10);
                                SendMessage(hWnd, WM_KEYUP, (IntPtr)KeyInterop.VirtualKeyFromKey(Key.Back), IntPtr.Zero);
                                break;
                        }
                    }
                    else
                    {
                        // 通常の文字はWM_CHARで送信
                        SendMessage(hWnd, WM_CHAR, (IntPtr)c, IntPtr.Zero);
                    }

                    // 文字間の遅延
                    await Task.Delay(10);
                }

                _logger.LogDebug("ウィンドウテキスト入力完了: {Length}文字 -> {WindowTitle}[{WindowClassName}]", 
                    text.Length, windowTitle, windowClassName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウテキスト入力エラー: {Text}, Window:{WindowTitle}", text, windowTitle);
                throw;
            }
        }

        #endregion

        #region キーの状態確認

        /// <summary>
        /// 指定されたキーが押されているかどうかを確認します
        /// </summary>
        public bool IsKeyPressed(Key key)
        {
            try
            {
                var virtualKey = KeyInterop.VirtualKeyFromKey(key);
                var keyState = GetKeyState(virtualKey);
                return (keyState & 0x8000) != 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "キー状態確認エラー: {Key}", key);
                return false;
            }
        }

        #endregion

        #region ホットキー文字列生成

        /// <summary>
        /// ホットキーの文字列表現を生成します
        /// </summary>
        public string GetHotkeyString(Key key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            var parts = new List<string>();

            if (ctrl) parts.Add("Ctrl");
            if (alt) parts.Add("Alt");
            if (shift) parts.Add("Shift");
            parts.Add(key.ToString());

            return string.Join("+", parts);
        }

        #endregion

        #region プライベートヘルパーメソッド

        /// <summary>
        /// ウィンドウハンドルを取得
        /// </summary>
        private IntPtr GetWindowHandle(string windowTitle, string windowClassName)
        {
            var hWnd = FindWindow(string.IsNullOrEmpty(windowClassName) ? null : windowClassName, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                throw new InvalidOperationException($"ウィンドウが見つかりません: '{windowTitle}' [{windowClassName}]");
            }
            if (!IsWindow(hWnd))
            {
                throw new InvalidOperationException($"無効なウィンドウハンドル: '{windowTitle}' [{windowClassName}]");
            }
            return hWnd;
        }

        /// <summary>
        /// 文字を非同期で送信
        /// </summary>
        private async Task SendCharAsync(char c)
        {
            await Task.Run(() =>
            {
                // Unicodeキーコードに変換してキー送信
                var virtualKey = (short)char.ToUpper(c);
                
                // 特殊な文字の場合はキーコードを調整
                if (char.IsLetter(c))
                {
                    bool isShiftRequired = char.IsUpper(c);
                    
                    if (isShiftRequired)
                    {
                        keybd_event((byte)VK_SHIFT, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    }
                    
                    keybd_event((byte)virtualKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    keybd_event((byte)virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    
                    if (isShiftRequired)
                    {
                        keybd_event((byte)VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    }
                }
                else if (char.IsDigit(c))
                {
                    // 数字キー
                    var digitKey = c - '0' + 0x30; // '0' = 0x30
                    keybd_event((byte)digitKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    keybd_event((byte)digitKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                }
                else
                {
                    // その他の文字（記号など）
                    // この実装は簡略化されており、完全な文字セットをサポートしていません
                    // 実際の実装では、より詳細な文字マッピングが必要です
                    KeyPress(Key.Space); // フォールバック
                }
            });
        }

        #endregion
    }
}