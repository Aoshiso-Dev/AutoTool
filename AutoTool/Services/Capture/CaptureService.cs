using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using System.Windows;
using WindowHelper;
using AutoTool.Services.Mouse;
using AutoTool.Services.ColorPicking;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using AutoTool.Services.Capture;

namespace AutoTool.Services.Capture
{
    /// <summary>
    /// キャプチャサービスの実装（ColorPick + KeyHelper + AdvancedColorPicking機能統合）
    /// </summary>
    public class CaptureService : ICaptureService, IDisposable
    {
        private readonly ILogger<CaptureService> _logger;
        private readonly IMouseService _mouseService;
        private readonly IAdvancedColorPickingService _advancedColorPickingService;
        private bool _isCapturing = false;
        private bool _isColorPickerActive = false;
        private bool _isKeyHelperActive = false;
        private CancellationTokenSource? _colorPickerCancellation;
        private CancellationTokenSource? _keyHelperCancellation;

        #region Win32 API Declarations (KeyHelper統合)
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const int VK_MENU = 0x12;     // ALT
        private const int VK_CONTROL = 0x11;  // CTRL
        private const int VK_SHIFT = 0x10;    // SHIFT

        #endregion

        public CaptureService(ILogger<CaptureService> logger, IMouseService mouseService, IAdvancedColorPickingService advancedColorPickingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mouseService = mouseService ?? throw new ArgumentNullException(nameof(mouseService));
            _advancedColorPickingService = advancedColorPickingService ?? throw new ArgumentNullException(nameof(advancedColorPickingService));
            
            _logger.LogInformation("CaptureService が AdvancedColorPicking統合で初期化されました");
        }

        /// <summary>
        /// カラーピッカーが現在アクティブかどうか
        /// </summary>
        public bool IsColorPickerActive => _isColorPickerActive;

        /// <summary>
        /// KeyHelperが現在アクティブかどうか
        /// </summary>
        public bool IsKeyHelperActive => _isKeyHelperActive;

        /// <summary>
        /// AdvancedColorPickingの色履歴
        /// </summary>
        public IReadOnlyList<ColorInfo> ColorHistory => _advancedColorPickingService.ColorHistory;

        /// <summary>
        /// 最後に取得した色情報
        /// </summary>
        public ColorInfo? LastColorInfo => _advancedColorPickingService.LastColorInfo;

        /// <summary>
        /// カラーピッカーをキャンセル
        /// </summary>
        public void CancelColorPicker()
        {
            _colorPickerCancellation?.Cancel();
            _isColorPickerActive = false;
        }

        /// <summary>
        /// KeyHelper処理をキャンセル
        /// </summary>
        public void CancelKeyHelper()
        {
            _keyHelperCancellation?.Cancel();
            _isKeyHelperActive = false;
        }

        #region Enhanced Color Picker Methods (AdvancedColorPicking統合)

        /// <summary>
        /// 高度なカラーピッキング: 指定座標の色を取得
        /// </summary>
        public async Task<ColorInfo?> GetColorInfoAtPositionAsync(int x, int y)
        {
            try
            {
                _logger.LogDebug("高度なカラーピッキング開始: 座標 ({X}, {Y})", x, y);
                var colorInfo = await _advancedColorPickingService.GetColorAtPositionAsync(x, y);
                
                if (colorInfo != null)
                {
                    _logger.LogInformation("色情報取得成功: {ColorInfo}", colorInfo);
                }
                
                return colorInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "高度なカラーピッキングエラー: 座標 ({X}, {Y})", x, y);
                return null;
            }
        }

        /// <summary>
        /// 高度なカラーピッキング: 現在のマウス位置の色を取得
        /// </summary>
        public async Task<ColorInfo?> GetColorInfoAtCurrentMousePositionAsync()
        {
            try
            {
                _logger.LogDebug("高度なカラーピッキング開始: マウス位置");
                var colorInfo = await _advancedColorPickingService.GetColorAtCurrentMousePositionAsync();
                
                if (colorInfo != null)
                {
                    _logger.LogInformation("マウス位置色情報取得成功: {ColorInfo}", colorInfo);
                }
                
                return colorInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マウス位置高度カラーピッキングエラー");
                return null;
            }
        }

        /// <summary>
        /// 高度なカラーピキング: 指定領域の平均色を取得
        /// </summary>
        public async Task<ColorInfo?> GetAverageColorInRegionAsync(Rect region)
        {
            try
            {
                _logger.LogDebug("領域平均色取得開始: {Region}", region);
                var colorInfo = await _advancedColorPickingService.GetAverageColorInRegionAsync(region);
                
                if (colorInfo != null)
                {
                    _logger.LogInformation("領域平均色取得成功: {ColorInfo}", colorInfo);
                }
                
                return colorInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "領域平均色取得エラー: {Region}", region);
                return null;
            }
        }

        /// <summary>
        /// 高度なカラーピキング: 類似色検索
        /// </summary>
        public async Task<ColorInfo?> FindSimilarColorAsync(Color targetColor, double tolerance = 10.0)
        {
            try
            {
                _logger.LogDebug("類似色検索開始: {TargetColor}, 許容差: {Tolerance}%", _advancedColorPickingService.ColorToHex(targetColor), tolerance);
                var colorInfo = await _advancedColorPickingService.FindSimilarColorAsync(targetColor, tolerance);
                
                if (colorInfo != null)
                {
                    _logger.LogInformation("類似色検索成功: {ColorInfo}", colorInfo);
                }
                else
                {
                    _logger.LogInformation("類似色が見つかりませんでした: {TargetColor}", _advancedColorPickingService.ColorToHex(targetColor));
                }
                
                return colorInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "類似色検索エラー: {TargetColor}", _advancedColorPickingService.ColorToHex(targetColor));
                return null;
            }
        }

        /// <summary>
        /// 高度なカラーピキング: 色ヒストグラム解析
        /// </summary>
        public async Task<ColorHistogram?> GetColorHistogramAsync(Rect region)
        {
            try
            {
                _logger.LogDebug("色ヒストグラム解析開始: {Region}", region);
                var histogram = await _advancedColorPickingService.GetColorHistogramAsync(region);
                
                if (histogram != null)
                {
                    _logger.LogInformation("色ヒストグラム解析成功: {Histogram}", histogram);
                }
                
                return histogram;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "色ヒストグラム解析エラー: {Region}", region);
                return null;
            }
        }

        /// <summary>
        /// AdvancedColorPicking統計情報を取得
        /// </summary>
        public ColorHistoryStatistics GetColorHistoryStatistics()
        {
            try
            {
                return _advancedColorPickingService.GetHistoryStatistics();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdvancedColorPicking統計情報取得エラー");
                return new ColorHistoryStatistics();
            }
        }

        /// <summary>
        /// 色履歴をクリア
        /// </summary>
        public void ClearColorHistory()
        {
            try
            {
                _advancedColorPickingService.ClearHistory();
                _logger.LogInformation("色履歴をクリアしました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "色履歴クリアエラー");
            }
        }

        /// <summary>
        /// 履歴から色パレットを生成
        /// </summary>
        public IEnumerable<Color> GenerateColorPalette(int maxColors = 16)
        {
            try
            {
                var palette = _advancedColorPickingService.GenerateColorPalette(maxColors);
                _logger.LogDebug("色パレット生成完了: {Count}色", palette.Count());
                return palette;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "色パレット生成エラー");
                return Enumerable.Empty<Color>();
            }
        }

        /// <summary>
        /// 補色パレットを生成
        /// </summary>
        public IEnumerable<Color> GenerateComplementaryPalette(Color baseColor)
        {
            try
            {
                var palette = _advancedColorPickingService.GenerateComplementaryPalette(baseColor);
                _logger.LogDebug("補色パレット生成完了: ベース色 {BaseColor}", _advancedColorPickingService.ColorToHex(baseColor));
                return palette;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "補色パレット生成エラー: ベース色 {BaseColor}", _advancedColorPickingService.ColorToHex(baseColor));
                return Enumerable.Empty<Color>();
            }
        }

        #endregion

        #region Color Picker Methods (既存機能 + AdvancedColorPicking拡張)

        /// <summary>
        /// 右クリック位置の色を取得
        /// </summary>
        public async Task<Color?> CaptureColorAtRightClickAsync()
        {
            if (_isCapturing) return null;

            try
            {
                _isCapturing = true;
                _logger.LogInformation("色キャプチャを開始します（右クリック待機）");

                // AdvancedColorPicking統合バージョンを優先使用
                try
                {
                    var position = await WaitForRightClickPositionAsync();
                    if (position.HasValue)
                    {
                        var colorInfo = await _advancedColorPickingService.GetColorAtPositionAsync(position.Value.X, position.Value.Y);
                        if (colorInfo != null)
                        {
                            _logger.LogInformation("AdvancedColorPickingで色を取得: {ColorInfo}", colorInfo);
                            return colorInfo.Color;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AdvancedColorPicking使用エラー、従来方式に切り替え");
                }

                // フォールバック: 従来の方式
                var fallbackColor = await CaptureColorFromScreenAsync();
                if (fallbackColor.HasValue)
                {
                    _logger.LogInformation("フォールバック方式で色を取得: {Color}", fallbackColor.Value);
                    return fallbackColor.Value;
                }

                return null;
            }
            finally
            {
                _isCapturing = false;
            }
        }

        /// <summary>
        /// スクリーンカラーピッカーを表示して画面上の色を取得
        /// </summary>
        public async Task<Color?> CaptureColorFromScreenAsync()
        {
            if (_isColorPickerActive) return null;

            try
            {
                _isColorPickerActive = true;
                _colorPickerCancellation = new CancellationTokenSource();
                _logger.LogInformation("スクリーンカラーピッカーを開始します");

                // AdvancedColorPicking統合バージョンを使用
                try
                {
                    var colorInfo = await _advancedColorPickingService.GetColorAtCurrentMousePositionAsync();
                    if (colorInfo != null)
                    {
                        _logger.LogInformation("AdvancedColorPickingカラーピッカーで色を取得: {ColorInfo}", colorInfo);
                        return colorInfo.Color;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AdvancedColorPicking使用エラー、簡易実装に切り替え");
                }

                // フォールバック: 簡易実装
                var position = GetCurrentMousePosition();
                var color = GetColorAt(position);
                
                _logger.LogInformation("簡易カラーピッカーで色を取得: {Color} at {Position}", color, position);
                return color;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("カラーピッカーがキャンセルされました");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "カラーピッカーエラー");
                return null;
            }
            finally
            {
                _isColorPickerActive = false;
                _colorPickerCancellation?.Dispose();
                _colorPickerCancellation = null;
            }
        }

        /// <summary>
        /// 現在のマウス位置を取得
        /// </summary>
        public System.Drawing.Point GetCurrentMousePosition()
        {
            try
            {
                return _mouseService.GetCurrentMousePosition();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マウス位置取得エラー");
                return new System.Drawing.Point(0, 0);
            }
        }

        /// <summary>
        /// 現在のマウス位置の色を取得
        /// </summary>
        public Color GetColorAtCurrentMousePosition()
        {
            try
            {
                var position = GetCurrentMousePosition();
                return GetColorAt(position);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マウス位置色取得エラー");
                return Color.Black;
            }
        }

        /// <summary>
        /// 指定座標の色を取得
        /// </summary>
        public Color GetColorAt(System.Drawing.Point position)
        {
            return GetColorAt(position.X, position.Y);
        }

        /// <summary>
        /// 指定座標の色を取得
        /// </summary>
        public Color GetColorAt(int x, int y)
        {
            try
            {
                using (var bitmap = new Bitmap(1, 1))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(1, 1));
                    }
                    
                    return bitmap.GetPixel(0, 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "色取得エラー: ({X}, {Y})", x, y);
                return Color.Black;
            }
        }

        /// <summary>
        /// ColorからHex文字列に変換
        /// </summary>
        public string ColorToHex(Color color)
        {
            return _advancedColorPickingService.ColorToHex(color);
        }

        /// <summary>
        /// Hex文字列からColorに変換
        /// </summary>
        public Color? HexToColor(string hex)
        {
            return _advancedColorPickingService.HexToColor(hex);
        }

        /// <summary>
        /// System.Drawing.ColorからSystem.Windows.Media.Colorに変換
        /// </summary>
        public System.Windows.Media.Color ToMediaColor(Color drawingColor)
        {
            return _advancedColorPickingService.ToMediaColor(drawingColor);
        }

        /// <summary>
        /// System.Windows.Media.ColorからSystem.Drawing.Colorに変換
        /// </summary>
        public Color ToDrawingColor(System.Windows.Media.Color mediaColor)
        {
            return _advancedColorPickingService.ToDrawingColor(mediaColor);
        }

        #endregion

        #region KeyHelper Methods (既存機能)

        /// <summary>
        /// グローバルキーを送信します
        /// </summary>
        public void SendGlobalKey(Key key)
        {
            try
            {
                KeyDown(key);
                KeyUp(key);
                _logger.LogDebug("グローバルキー送信: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "グローバルキー送信エラー: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// グローバルキーを送信します（修飾キー付き）
        /// </summary>
        public void SendGlobalKey(Key key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            try
            {
                if (ctrl) KeyDown(Key.LeftCtrl);
                if (alt) KeyDown(Key.LeftAlt);
                if (shift) KeyDown(Key.LeftShift);

                SendGlobalKey(key);

                if (ctrl) KeyUp(Key.LeftCtrl);
                if (alt) KeyUp(Key.LeftAlt);
                if (shift) KeyUp(Key.LeftShift);

                var modifiers = new List<string>();
                if (ctrl) modifiers.Add("Ctrl");
                if (alt) modifiers.Add("Alt");
                if (shift) modifiers.Add("Shift");
                var keyCombo = modifiers.Count > 0 ? $"{string.Join("+", modifiers)}+{key}" : key.ToString();
                
                _logger.LogDebug("グローバルキー送信（修飾キー付き）: {KeyCombo}", keyCombo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "グローバルキー送信エラー（修飾キー付き）: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// 指定ウィンドウにキーを送信します
        /// </summary>
        public void SendKeyToWindow(Key key, string windowTitle = "", string windowClassName = "")
        {
            try
            {
                if (string.IsNullOrEmpty(windowTitle) && string.IsNullOrEmpty(windowClassName))
                {
                    SendGlobalKey(key);
                    return;
                }

                KeyDownToWindow(key, false, false, false, windowTitle, windowClassName);
                Thread.Sleep(100);
                KeyUpToWindow(key, false, false, false, windowTitle, windowClassName);

                _logger.LogDebug("ウィンドウキー送信: {Key} -> {WindowTitle}/{WindowClassName}", key, windowTitle, windowClassName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウキー送信エラー: {Key} -> {WindowTitle}/{WindowClassName}", key, windowTitle, windowClassName);
                throw;
            }
        }

        /// <summary>
        /// 指定ウィンドウにキーを送信します（修飾キー付き）
        /// </summary>
        public void SendKeyToWindow(Key key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "")
        {
            try
            {
                if (string.IsNullOrEmpty(windowTitle) && string.IsNullOrEmpty(windowClassName))
                {
                    SendGlobalKey(key, ctrl, alt, shift);
                    return;
                }

                KeyDownToWindow(key, ctrl, alt, shift, windowTitle, windowClassName);
                Thread.Sleep(100);
                KeyUpToWindow(key, ctrl, alt, shift, windowTitle, windowClassName);

                var modifiers = new List<string>();
                if (ctrl) modifiers.Add("Ctrl");
                if (alt) modifiers.Add("Alt");
                if (shift) modifiers.Add("Shift");
                var keyCombo = modifiers.Count > 0 ? $"{string.Join("+", modifiers)}+{key}" : key.ToString();
                
                _logger.LogDebug("ウィンドウキー送信（修飾キー付き）: {KeyCombo} -> {WindowTitle}/{WindowClassName}", keyCombo, windowTitle, windowClassName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウキー送信エラー（修飾キー付き）: {Key} -> {WindowTitle}/{WindowClassName}", key, windowTitle, windowClassName);
                throw;
            }
        }

        /// <summary>
        /// グローバルキーを非同期で送信します
        /// </summary>
        public async Task SendGlobalKeyAsync(Key key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            if (_isKeyHelperActive)
            {
                _logger.LogWarning("KeyHelper処理が既にアクティブです");
                return;
            }

            try
            {
                _isKeyHelperActive = true;
                _keyHelperCancellation = new CancellationTokenSource();

                await Task.Run(() =>
                {
                    if (_keyHelperCancellation?.Token.IsCancellationRequested == true) return;
                    SendGlobalKey(key, ctrl, alt, shift);
                }, _keyHelperCancellation.Token);

                _logger.LogDebug("非同期グローバルキー送信完了: {Key}", key);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("グローバルキー送信がキャンセルされました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "非同期グローバルキー送信エラー: {Key}", key);
                throw;
            }
            finally
            {
                _isKeyHelperActive = false;
                _keyHelperCancellation?.Dispose();
                _keyHelperCancellation = null;
            }
        }

        /// <summary>
        /// 指定ウィンドウにキーを非同期で送信します
        /// </summary>
        public async Task SendKeyToWindowAsync(Key key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "")
        {
            if (_isKeyHelperActive)
            {
                _logger.LogWarning("KeyHelper処理が既にアクティブです");
                return;
            }

            try
            {
                _isKeyHelperActive = true;
                _keyHelperCancellation = new CancellationTokenSource();

                await Task.Run(() =>
                {
                    if (_keyHelperCancellation?.Token.IsCancellationRequested == true) return;
                    SendKeyToWindow(key, ctrl, alt, shift, windowTitle, windowClassName);
                }, _keyHelperCancellation.Token);

                _logger.LogDebug("非同期ウィンドウキー送信完了: {Key} -> {WindowTitle}/{WindowClassName}", key, windowTitle, windowClassName);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ウィンドウキー送信がキャンセルされました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "非同期ウィンドウキー送信エラー: {Key} -> {WindowTitle}/{WindowClassName}", key, windowTitle, windowClassName);
                throw;
            }
            finally
            {
                _isKeyHelperActive = false;
                _keyHelperCancellation?.Dispose();
                _keyHelperCancellation = null;
            }
        }

        /// <summary>
        /// 連続でキーを送信します
        /// </summary>
        public async Task SendKeySequenceAsync(IEnumerable<Key> keys, int intervalMs = 100)
        {
            if (_isKeyHelperActive)
            {
                _logger.LogWarning("KeyHelper処理が既にアクティブです");
                return;
            }

            try
            {
                _isKeyHelperActive = true;
                _keyHelperCancellation = new CancellationTokenSource();

                var keyList = keys.ToList();
                _logger.LogInformation("キー連続送信開始: {KeyCount}個のキー、間隔{Interval}ms", keyList.Count, intervalMs);

                for (int i = 0; i < keyList.Count; i++)
                {
                    if (_keyHelperCancellation?.Token.IsCancellationRequested == true) break;

                    var key = keyList[i];
                    SendGlobalKey(key);

                    _logger.LogTrace("キー送信 {Index}/{Total}: {Key}", i + 1, keyList.Count, key);

                    if (i < keyList.Count - 1) // 最後のキー以外は間隔を置く
                    {
                        await Task.Delay(intervalMs, _keyHelperCancellation.Token);
                    }
                }

                _logger.LogInformation("キー連続送信完了: {KeyCount}個のキー", keyList.Count);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("キー連続送信がキャンセルされました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "キー連続送信エラー");
                throw;
            }
            finally
            {
                _isKeyHelperActive = false;
                _keyHelperCancellation?.Dispose();
                _keyHelperCancellation = null;
            }
        }

        /// <summary>
        /// 文字列として文字を送信します
        /// </summary>
        public async Task SendTextAsync(string text, int intervalMs = 50)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (_isKeyHelperActive)
            {
                _logger.LogWarning("KeyHelper処理が既にアクティブです");
                return;
            }

            try
            {
                _isKeyHelperActive = true;
                _keyHelperCancellation = new CancellationTokenSource();

                _logger.LogInformation("テキスト送信開始: '{Text}' ({Length}文字)、間隔{Interval}ms", text, text.Length, intervalMs);

                foreach (char c in text)
                {
                    if (_keyHelperCancellation?.Token.IsCancellationRequested == true) break;

                    // 文字をキーに変換して送信
                    var key = CharToKey(c);
                    if (key != Key.None)
                    {
                        var needShift = char.IsUpper(c) || IsShiftRequiredChar(c);
                        SendGlobalKey(key, shift: needShift);
                        
                        await Task.Delay(intervalMs, _keyHelperCancellation.Token);
                    }
                }

                _logger.LogInformation("テキスト送信完了: '{Text}'", text);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("テキスト送信がキャンセルされました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テキスト送信エラー: '{Text}'", text);
                throw;
            }
            finally
            {
                _isKeyHelperActive = false;
                _keyHelperCancellation?.Dispose();
                _keyHelperCancellation = null;
            }
        }

        /// <summary>
        /// 指定ウィンドウに文字列を送信します
        /// </summary>
        public async Task SendTextToWindowAsync(string text, string windowTitle = "", string windowClassName = "", int intervalMs = 50)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (_isKeyHelperActive)
            {
                _logger.LogWarning("KeyHelper処理が既にアクティブです");
                return;
            }

            try
            {
                _isKeyHelperActive = true;
                _keyHelperCancellation = new CancellationTokenSource();

                _logger.LogInformation("ウィンドウテキスト送信開始: '{Text}' -> {WindowTitle}/{WindowClassName}", text, windowTitle, windowClassName);

                foreach (char c in text)
                {
                    if (_keyHelperCancellation?.Token.IsCancellationRequested == true) break;

                    var key = CharToKey(c);
                    if (key != Key.None)
                    {
                        var needShift = char.IsUpper(c) || IsShiftRequiredChar(c);
                        SendKeyToWindow(key, shift: needShift, windowTitle: windowTitle, windowClassName: windowClassName);
                        
                        await Task.Delay(intervalMs, _keyHelperCancellation.Token);
                    }
                }

                _logger.LogInformation("ウィンドウテキスト送信完了: '{Text}'", text);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ウィンドウテキスト送信がキャンセルされました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウテキスト送信エラー: '{Text}' -> {WindowTitle}/{WindowClassName}", text, windowTitle, windowClassName);
                throw;
            }
            finally
            {
                _isKeyHelperActive = false;
                _keyHelperCancellation?.Dispose();
                _keyHelperCancellation = null;
            }
        }

        #endregion

        #region Existing Methods (Window Capture, Key Capture, etc.)

        /// <summary>
        /// 右クリック位置のウィンドウ情報を取得
        /// </summary>
        public async Task<WindowCaptureResult?> CaptureWindowInfoAtRightClickAsync()
        {
            if (_isCapturing) return null;

            try
            {
                _isCapturing = true;
                _logger.LogInformation("ウィンドウ情報キャプチャを開始します（右クリック待機）");

                var position = await WaitForRightClickPositionAsync();
                if (position.HasValue)
                {
                    return GetWindowInfoAt(position.Value);
                }

                return null;
            }
            finally
            {
                _isCapturing = false;
            }
        }

        /// <summary>
        /// 右クリック位置の座標を取得
        /// </summary>
        public async Task<System.Drawing.Point?> CaptureCoordinateAtRightClickAsync()
        {
            if (_isCapturing) return null;

            try
            {
                _isCapturing = true;
                _logger.LogInformation("座標キャプチャを開始します（右クリック待機）");

                var position = await WaitForRightClickPositionAsync();
                if (position.HasValue)
                {
                    _logger.LogInformation("右クリック位置で座標を取得: {Position}", position.Value);
                    return position.Value;
                }

                return null;
            }
            finally
            {
                _isCapturing = false;
            }
        }

        /// <summary>
        /// キーキャプチャを実行
        /// </summary>
        public async Task<KeyCaptureResult?> CaptureKeyAsync(string title)
        {
            try
            {
                _logger.LogInformation("キーキャプチャを開始します: {Title}", title);

                var dialog = new KeyCaptureDialog(title);
                var result = await dialog.ShowAsync();
                
                if (result != null)
                {
                    _logger.LogInformation("キーキャプチャ成功: {Key}", result.Key);
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "キーキャプチャエラー");
                return null;
            }
        }

        #endregion

        #region Private KeyHelper Helper Methods

        private void KeyDown(Key key)
        {
            keybd_event((byte)KeyInterop.VirtualKeyFromKey(key), 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        }

        private void KeyUp(Key key)
        {
            keybd_event((byte)KeyInterop.VirtualKeyFromKey(key), 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private void KeyDownToWindow(Key key, bool ctrl, bool alt, bool shift, string windowTitle, string windowClassName)
        {
            var hWnd = FindWindow(string.IsNullOrEmpty(windowClassName) ? null : windowClassName, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            if (ctrl) SendMessage(hWnd, WM_KEYDOWN, (IntPtr)VK_CONTROL, IntPtr.Zero);
            if (alt) SendMessage(hWnd, WM_KEYDOWN, (IntPtr)VK_MENU, IntPtr.Zero);
            if (shift) SendMessage(hWnd, WM_KEYDOWN, (IntPtr)VK_SHIFT, IntPtr.Zero);

            SendMessage(hWnd, WM_KEYDOWN, (IntPtr)KeyInterop.VirtualKeyFromKey(key), IntPtr.Zero);
        }

        private void KeyUpToWindow(Key key, bool ctrl, bool alt, bool shift, string windowTitle, string windowClassName)
        {
            var hWnd = FindWindow(string.IsNullOrEmpty(windowClassName) ? null : windowClassName, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            SendMessage(hWnd, WM_KEYUP, (IntPtr)KeyInterop.VirtualKeyFromKey(key), IntPtr.Zero);

            if (ctrl) SendMessage(hWnd, WM_KEYUP, (IntPtr)VK_CONTROL, IntPtr.Zero);
            if (alt) SendMessage(hWnd, WM_KEYUP, (IntPtr)VK_MENU, IntPtr.Zero);
            if (shift) SendMessage(hWnd, WM_KEYUP, (IntPtr)VK_SHIFT, IntPtr.Zero);
        }

        private Key CharToKey(char c)
        {
            return c switch
            {
                >= 'a' and <= 'z' => Key.A + (c - 'a'),
                >= 'A' and <= 'Z' => Key.A + (c - 'A'),
                >= '0' and <= '9' => Key.D0 + (c - '0'),
                ' ' => Key.Space,
                '\t' => Key.Tab,
                '\r' or '\n' => Key.Enter,
                _ => Key.None
            };
        }

        private bool IsShiftRequiredChar(char c)
        {
            return "!@#$%^&*()_+{}|:\"<>?~".Contains(c);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// 右クリック位置を待機
        /// </summary>
        private async Task<System.Drawing.Point?> WaitForRightClickPositionAsync()
        {
            try
            {
                // 右クリックイベントを捕捉
                var hookResult = await WaitForRightClickHookAsync();
                return hookResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "右クリック待機エラー");
                return null;
            }
        }

        /// <summary>
        /// 右クリックフックを待機（MouseServiceへ委譲）
        /// </summary>
        private async Task<System.Drawing.Point?> WaitForRightClickHookAsync()
        {
            try
            {
                var timeout = TimeSpan.FromSeconds(10); // 右クリック待機のタイムアウト
                _logger.LogInformation("MouseService で右クリック待機開始 (タイムアウト: {Seconds}s)", timeout.TotalSeconds);

                // MouseService に実装されているフック待機を使用
                var result = await _mouseService.WaitForRightClickWithTimeoutAsync(timeout);

                if (result.HasValue)
                {
                    _logger.LogInformation("右クリック検出: {X},{Y}", result.Value.X, result.Value.Y);
                    return result.Value;
                }

                _logger.LogInformation("右クリック待機がタイムアウトまたはキャンセルされました");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "右クリックフック待機エラー");
                return null;
            }
        }

        /// <summary>
        /// 指定位置のウィンドウ情報を取得
        /// </summary>
        private WindowCaptureResult? GetWindowInfoAt(System.Drawing.Point position)
        {
            try
            {
                // WindowHelperを使用
                var handle = WindowHelper.Info.GetWindowHandle(position.X, position.Y);
                var title = WindowHelper.Info.GetWindowTitle(handle);
                var className = WindowHelper.Info.GetWindowClassName(handle);
                
                return new WindowCaptureResult
                {
                    Handle = handle,
                    Title = title,
                    ClassName = className
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ情報取得エラー: {Position}", position);
                
                // フォールバック
                return new WindowCaptureResult
                {
                    Title = $"Window at ({position.X}, {position.Y})",
                    ClassName = "UnknownClass",
                    Handle = IntPtr.Zero
                };
            }
        }

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _advancedColorPickingService?.Dispose();
                _colorPickerCancellation?.Dispose();
                _keyHelperCancellation?.Dispose();
                _disposed = true;
                _logger.LogInformation("CaptureService がリソース解放されました");
            }
        }

        #endregion
    }

    /// <summary>
    /// キーキャプチャダイアログ（WPF実装）
    /// </summary>
    internal class KeyCaptureDialog
    {
        private readonly string _title;

        public KeyCaptureDialog(string title)
        {
            _title = title;
        }

        public async Task<KeyCaptureResult?> ShowAsync()
        {
            return await Task.Run(() =>
            {
                KeyCaptureResult? result = null;
                
                // UIスレッドで実行
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var window = new KeyCaptureWindow(_title);
                    
                    // オーナーウィンドウを設定（可能であれば）
                    if (System.Windows.Application.Current?.MainWindow != null)
                    {
                        window.Owner = System.Windows.Application.Current.MainWindow;
                    }
                    
                    var dialogResult = window.ShowDialog();
                    
                    if (dialogResult == true && window.CapturedKey.HasValue)
                    {
                        result = new KeyCaptureResult
                        {
                            Key = window.CapturedKey.Value,
                            IsCtrlPressed = window.IsCtrlPressed,
                            IsAltPressed = window.IsAltPressed,
                            IsShiftPressed = window.IsShiftPressed
                        };
                    }
                });
                
                return result;
            });
        }
    }

    /// <summary>
    /// 右クリック待機ダイアログ（暫定実装）
    /// </summary>
    internal class RightClickWaitDialog
    {
        public async Task<System.Drawing.Point?> ShowAsync()
        {
            try
            {
                // 右クリック待機を開始
                var result = System.Windows.MessageBox.Show(
                    "右クリック待機を開始します。\n\n対象の位置で右クリックしてください。\n\nキャンセルするには×ボタンを押してください。",
                    "右クリック待機",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Cancel)
                {
                    return null;
                }

                // 暫定実装：現在のマウス位置を返す
                return await Task.FromResult(new System.Drawing.Point(100, 100));
            }
            catch
            {
                return null;
            }
        }
    }
}